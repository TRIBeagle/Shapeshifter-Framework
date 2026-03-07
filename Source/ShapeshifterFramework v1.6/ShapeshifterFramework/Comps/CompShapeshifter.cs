// ShapeshifterFramework | Comps | CompShapeshifter.cs
// 목적 : Pawn의 변신(Shapeshift) 라이프사이클 전체를 런타임에서 관장하는 최상위 핵심 컴포넌트.
// 용도 : - 폼 적용/해제 : 조건 검증 후 능력(Ability), 헤디프, 체형(BodyType) 부여 및 원상 복원, 남은 시간 카운트다운 처리.
//        - 장비/파츠 관리 : 변신 시 설정된 규칙(드랍/인벤토리)에 따라 의복과 무기를 처리하고, 해제 시 저장된 스냅샷(HashSet)과 JobQueue를 이용해 자동 재착용 및 신체 결손 상태 완벽 복원.
//        - VerbTracker 지원 : 폼에 정의된 특수 공격(verbs/tools)을 전용 VerbTracker로 구성하여 드래프트 시 공격 토글/명령 지즈모(Gizmo) 노출.
// 주의 : 방대한 데이터를 세이브/로드(IExposable)하며, 고빈도 틱(CompTick) 내에서 LINQ를 배제하고 HashSet 기반 O(1) 탐색을 사용하여 대규모 전투 시 프레임 드랍을 원천 차단함.

using RimWorld;
using ShapeshifterFramework.Utilities;
using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace ShapeshifterFramework.Comps
{
    /// <summary>
    /// Pawn의 변신 상태를 관리하는 컴포넌트.
    /// - 현재 폼/타이머/체형 백업/부여된 능력·헤디프/장비 스냅샷을 관리한다.
    /// - 폼 정의의 verbs/tools를 전용 <see cref="VerbTracker"/>로 노출하고 지즈모를 생성한다.
    /// - 적용/해제 시 의복·무기 처리/FX/각종 캐시 더럽힘까지 일괄 수행한다.
    /// </summary>
    public class CompShapeshifter : ThingComp
    {
        #region 상태 필드/캐시

        /// <summary>현재 변신 폼. null이면 비변신.</summary>
        public ShapeshiftFormDef currentForm = null;

        /// <summary>현재 변신 여부.</summary>
        public bool isTransformed { get { return currentForm != null; } }

        /// <summary>
        /// 해당 무기가 변신 폼에 의해 소환된 전용 무기인지 확인
        /// </summary>
        public bool IsGeneratedWeapon(ThingWithComps eq)
        {
            return generatedWeapons != null && generatedWeapons.Contains(eq);
        }

        /// <summary>남은 변신 틱(카운트다운). 0 이하일 때 무시.</summary>
        private int transformTimer = 0;

        // 체형/머리형 백업(인간형만)
        private BodyTypeDef originalBodyType;
        private HeadTypeDef originalHeadType;

        // 임시 부여 요소 추적
        private readonly List<AbilityDef> tempAddedAbilities = new List<AbilityDef>();
        private readonly List<Hediff> tempAddedHediffs = new List<Hediff>();
        private readonly List<HediffDef> tempAddedHediffsDefCache = new List<HediffDef>();

        // 변신 전 장비 스냅샷(해제 시 자동 재착용용)
        private readonly List<Apparel> prevApparels = new List<Apparel>();
        private readonly List<ThingWithComps> prevWeapons = new List<ThingWithComps>();

        // 변신을 유발한 원본 아이템 (예: 변신 반지) - 드랍 보호용
        public List<Thing> sourceItems = new List<Thing>();

        // 변신 시 소환된 폼 전용 장비 추적 (해제 시 삭제 및 복사 방지용)
        private List<Apparel> generatedApparel = new List<Apparel>();
        private List<ThingWithComps> generatedWeapons = new List<ThingWithComps>();

        // 폼 전용 VerbTracker (폼 verbs/tools용)
        private VerbTracker shapeshiftVerbTracker;

        // Form 선택용 Gizmo 캐시
        private const int GizmoCacheInterval = 90;
        private int gizmoCacheTick = -9999;
        private List<ShapeshiftFormDef> gizmoFormsCache = new List<ShapeshiftFormDef>();

        // 틱(Tick) 에러 스팸 방지용 플래그
        private bool verbTickErrorLogged = false;

        // verb 자동공격 토글 상태 (키: formDefName#index)
        private readonly Dictionary<string, bool> verbAutoToggle = new Dictionary<string, bool>();

        /// <summary>변신 복귀 중 내부 재장착 허용 플래그(세이브 불필요, 런타임 전용).</summary>
        public bool suppressEquipLock = false;

        // PostLoadInit에서 Reference 연결 완료 후 AddRange하기 위한 임시 보관 필드
        private List<Hediff> __tmpHediffsLoad = null;
        private HashSet<string> __tmpPrevApIds = null;
        private HashSet<string> __tmpPrevWpIds = null;
        private bool needsGearResolve = false;

        // 추가한 헤디프(인스턴스) 추적은 기존 tempAddedHediffs 사용
        private readonly List<ShapeshifterFramework.Utilities.ShapeshiftPartRestoreRecord> tempPartRestoreRecords
            = new List<ShapeshifterFramework.Utilities.ShapeshiftPartRestoreRecord>(8);

        #endregion

        #region IVerbOwner 구현 (폼 verbs/tools → 전용 VerbTracker)

        /// <summary>
        /// 현재 폼의 verbs/tools를 제공하는 IVerbOwner 구현.
        /// </summary>
        private class ShapeshiftVerbOwner : IVerbOwner
        {
            private readonly CompShapeshifter comp;
            private static readonly List<VerbProperties> EmptyVerbProperties = new List<VerbProperties>(0);
            private static readonly List<Tool> EmptyTools = new List<Tool>(0);
            public ShapeshiftVerbOwner(CompShapeshifter c) { comp = c; }

            VerbTracker IVerbOwner.VerbTracker => comp.shapeshiftVerbTracker;

            // RimWorld 1.6: Body 항목 없음. 고유/자연 Verb는 NativeVerb 사용
            ImplementOwnerTypeDef IVerbOwner.ImplementOwnerTypeDef => ImplementOwnerTypeDefOf.NativeVerb;

            string IVerbOwner.UniqueVerbOwnerID()
            {
                var p = comp.parent as Pawn;
                return p != null ? "Shapeshift_" + p.ThingID : "Shapeshift_Unknown";
            }

            bool IVerbOwner.VerbsStillUsableBy(Pawn p)
            {
                return comp.isTransformed && (comp.parent as Pawn) == p;
            }

            Thing IVerbOwner.ConstantCaster => comp.parent as Pawn;

            /// <summary>폼에서 정의한 VerbProperties 목록(없으면 빈 리스트).</summary>
            public List<VerbProperties> VerbProperties
            {
                get
                {
                    var f = comp.currentForm;
                    return (f != null && f.verbs != null) ? f.verbs : EmptyVerbProperties;
                }
            }

            /// <summary>폼에서 정의한 Tool 목록(없으면 빈 리스트).</summary>
            public List<Tool> Tools
            {
                get
                {
                    var f = comp.currentForm;
                    return (f != null && f.tools != null) ? f.tools : EmptyTools;
                }
            }
        }

        /// <summary>
        /// 현재 폼의 verbs/tools를 제공하는 전용 <see cref="VerbTracker"/>.
        /// 폼에 verbs/tools가 없으면 null.
        /// </summary>
        public VerbTracker ShapeshiftVerbTracker
        {
            get
            {
                if (!isTransformed || currentForm == null) return null;

                bool hasVerbs = currentForm.verbs != null && currentForm.verbs.Count > 0;
                bool hasTools = currentForm.tools != null && currentForm.tools.Count > 0;
                if (!hasVerbs && !hasTools) return null;

                if (shapeshiftVerbTracker == null)
                {
                    shapeshiftVerbTracker = new VerbTracker(new ShapeshiftVerbOwner(this));
                    var pawn = parent as Pawn;
                    if (pawn != null)
                    {
                        try
                        {
                            // caster 지정
                            var verbs = shapeshiftVerbTracker.AllVerbs;
                            for (int i = 0; i < verbs.Count; i++)
                            {
                                var v = verbs[i];
                                if (v != null) v.caster = pawn;
                            }
                        }
                        catch (System.Exception ex) { Log.Error($"[SSF] VerbTracker init error: {ex}"); }
                    }
                }
                return shapeshiftVerbTracker;
            }
        }

        #endregion

        #region Verb 자동공격 토글 유틸/라벨·설명 헬퍼

        /// <summary>자동공격 토글용 내부 키 생성(formDefName#index).</summary>
        string AutoKey(Verb v)
        {
            var f = currentForm?.defName ?? "None";
            string vName = v?.verbProps?.label ?? v?.GetType().Name ?? "UnknownVerb";
            return f + "#" + vName;
        }

        /// <summary>폼 옵션의 기본 자동공격 상태를 반환(없으면 true).</summary>
        bool DefaultAutoOn(int index)
        {
            var opt = currentForm?.verbGizmoOptions;
            if (opt != null && index >= 0 && index < opt.Count)
            {
                var o = opt[index];
                if (o != null && o.autoAttackDefault.HasValue) return o.autoAttackDefault.Value;
            }
            return true; // 기본 On
        }

        /// <summary>지정 인덱스 verb의 자동공격 활성 여부.</summary>
        public bool IsAutoAttackEnabled(int index, Verb v)
        {
            if (v == null) return true;
            bool val;
            if (verbAutoToggle.TryGetValue(AutoKey(v), out val)) return val;
            return DefaultAutoOn(index);
        }

        /// <summary>자동공격 토글.</summary>
        public void ToggleAutoAttack(int index, Verb v)
        {
            bool now = IsAutoAttackEnabled(index, v);
            verbAutoToggle[AutoKey(v)] = !now;
        }

        /// <summary>자동공격 강제 On.</summary>
        public void ForceAutoAttackOn(int index, Verb v)
        {
            verbAutoToggle[AutoKey(v)] = true;
        }

        /// <summary>verb 명령 라벨(Def verbGizmoOptions 우선, 없으면 verbProps.label/기본 Attack).</summary>
        public string GetVerbLabel(int index, Verb v, bool preferToggleLabel)
        {
            var vp = v?.verbProps;
            var opt = currentForm?.verbGizmoOptions;
            if (opt != null && index >= 0 && index < opt.Count && opt[index] != null)
            {
                string s = preferToggleLabel ? opt[index].toggleLabel : opt[index].label;
                if (!string.IsNullOrEmpty(s)) return s.Translate().CapitalizeFirst();
            }

            string __label = string.IsNullOrEmpty(vp?.label) ? "SSF_Verb_Attack".Translate() : vp.label.Translate();
            return __label.CapitalizeFirst();
        }

        /// <summary>verb 명령/토글 설명(Def verbGizmoOptions 우선, 없으면 기본).</summary>
        public string GetVerbDesc(int index, Verb v, bool forToggle)
        {
            var opt = currentForm?.verbGizmoOptions;
            if (opt != null && index >= 0 && index < opt.Count && opt[index] != null)
            {
                string s = forToggle ? opt[index].toggleDesc : opt[index].desc;
                if (!string.IsNullOrEmpty(s)) return s.Translate();
            }

            if (forToggle) return "SSF_Verb_ToggleDesc".Translate();
            return "SSF_Verb_OrderDesc".Translate();
        }

        #endregion

        #region Ticking/Inspect

        /// <summary>
        /// 매 틱 호출. 타이머 경과/사망 감지/전용 <see cref="VerbTracker"/> 틱 처리.
        /// </summary>
        public override void CompTick()
        {
            base.CompTick();
            Pawn pawn = parent as Pawn;

            // 인벤토리 내부 우선 검색 추가 + HashSet 기반 O(1) 단일 순회
            if (needsGearResolve)
            {
                needsGearResolve = false;
                if (pawn != null && (__tmpPrevApIds != null || __tmpPrevWpIds != null))
                {
                    // 1. 폰의 인벤토리 우선 검색 (GearHandling.Inventory로 들어간 장비 복구용)
                    if (pawn.inventory?.innerContainer != null)
                    {
                        for (int i = 0; i < pawn.inventory.innerContainer.Count; i++)
                        {
                            var t = pawn.inventory.innerContainer[i];
                            if (__tmpPrevApIds != null && __tmpPrevApIds.Contains(t.ThingID) && t is Apparel ap)
                            {
                                prevApparels.Add(ap);
                                __tmpPrevApIds.Remove(t.ThingID);
                            }
                            else if (__tmpPrevWpIds != null && __tmpPrevWpIds.Contains(t.ThingID) && t is ThingWithComps twc)
                            {
                                prevWeapons.Add(twc);
                                __tmpPrevWpIds.Remove(t.ThingID);
                            }
                        }
                    }

                    // 2. 맵 바닥 검색 (GearHandling.Drop으로 떨어진 장비 복구용)
                    if (pawn.Map != null)
                    {
                        var allThings = pawn.Map.listerThings.AllThings;
                        for (int i = 0; i < allThings.Count; i++)
                        {
                            var t = allThings[i];
                            // [수정됨] 인벤토리에서 찾은 ID는 이미 HashSet에서 지워졌으므로 Contains(ap) 체크 불필요
                            if (__tmpPrevApIds != null && __tmpPrevApIds.Contains(t.ThingID) && t is Apparel ap)
                            {
                                prevApparels.Add(ap);
                                __tmpPrevApIds.Remove(t.ThingID);
                            }
                            else if (__tmpPrevWpIds != null && __tmpPrevWpIds.Contains(t.ThingID) && t is ThingWithComps twc)
                            {
                                prevWeapons.Add(twc);
                                __tmpPrevWpIds.Remove(t.ThingID);
                            }
                        }
                    }

                    // 찾기 완료 후 임시 리스트 비우기
                    __tmpPrevApIds = null;
                    __tmpPrevWpIds = null;
                }
            }

            if (isTransformed && currentForm != null)
            {
                // 외부 요인(캐릭터 에디터 등)으로 스탯 헤디프가 강제 삭제된 경우 변신 해제
                if (currentForm.generatedStatHediff != null
                    && pawn != null
                    && pawn.health?.hediffSet?.GetFirstHediffOfDef(currentForm.generatedStatHediff) == null)
                {
                    RemoveForm();
                    return;
                }
                if (pawn != null && pawn.Dead)
                {
                    RemoveForm();
                    return;
                }
                if (currentForm.durationTicks.HasValue && currentForm.durationTicks.Value > 0)
                {
                    transformTimer--;
                    if (transformTimer <= 0) RemoveForm();
                }
                // 1초(60틱)마다 한 번씩 핵심 유지 요건 검사
                if (pawn.IsHashIntervalTick(60))
                {
                    // 1. 다중 코어템(sourceItems) 유실 검사
                    if (this.sourceItems != null && this.sourceItems.Count > 0)
                    {
                        for (int i = this.sourceItems.Count - 1; i >= 0; i--)
                        {
                            Thing item = this.sourceItems[i];
                            if (item == null) continue;

                            bool isHeldByPawn =
                                (pawn.apparel != null && pawn.apparel.Contains(item as Apparel)) ||
                                (pawn.equipment != null && pawn.equipment.Contains(item as ThingWithComps)) ||
                                (pawn.inventory != null && pawn.inventory.innerContainer.Contains(item));

                            if (item.Destroyed || item.Spawned || !isHeldByPawn)
                            {
                                ShapeshiftDiagnostics.Info("Source item lost. Forcing shapeshift revert.");
                                // 알림 메시지 띄우기
                                Messages.Message("SSF_Message_RevertDueToItemLost".Translate(pawn.LabelShortCap, item.Label), pawn, MessageTypeDefOf.NegativeEvent, false);
                                RemoveForm();
                                return;
                            }
                        }
                    }

                    // 2. 신체적 조건(유전자, 헤디프) 유실 검사
                    if (currentForm.requiredGenes != null && currentForm.requiredGenes.Count > 0 && pawn.genes != null)
                    {
                        for (int i = 0; i < currentForm.requiredGenes.Count; i++)
                        {
                            if (!pawn.genes.HasActiveGene(currentForm.requiredGenes[i]))
                            {
                                // 알림 메시지 띄우기
                                Messages.Message("SSF_Message_RevertDueToConditionLost".Translate(pawn.LabelShortCap), pawn, MessageTypeDefOf.NegativeEvent, false);
                                RemoveForm();
                                return;
                            }
                        }
                    }

                    if (currentForm.requiredHediffs != null && currentForm.requiredHediffs.Count > 0 && pawn.health != null)
                    {
                        for (int i = 0; i < currentForm.requiredHediffs.Count; i++)
                        {
                            if (pawn.health.hediffSet.GetFirstHediffOfDef(currentForm.requiredHediffs[i]) == null)
                            {
                                // ★ 알림 메시지 띄우기
                                Messages.Message("SSF_Message_RevertDueToConditionLost".Translate(pawn.LabelShortCap), pawn, MessageTypeDefOf.NegativeEvent, false);
                                RemoveForm();
                                return;
                            }
                        }
                    }
                }
                try
                {
                    ShapeshiftVerbTracker?.VerbsTick();
                }
                catch (System.Exception ex)
                {
                    if (!verbTickErrorLogged)
                    {
                        Log.Error($"[SSF] VerbsTick error on pawn {pawn?.Name} (Logging once to prevent spam): {ex}");
                        verbTickErrorLogged = true;
                    }
                }
            }
        }

        /// <summary>남은 변신 틱(0 이하이면 0).</summary>
        private int RemainingShapeshiftTicks
        {
            get
            {
                int t = transformTimer; // 기존 카운트다운 필드 사용
                return t > 0 ? t : 0;
            }
        }

        /// <summary>인스펙트 추가 문자열(남은 시간/영구 변신 등).</summary>
        public override string CompInspectStringExtra()
        {
            if (!isTransformed || currentForm == null)
                return null;

            // durationTicks가 없거나 <=0 이면 영구 변신
            if (!currentForm.durationTicks.HasValue || currentForm.durationTicks.Value <= 0)
                return "SSF_Inspect_Permanent".Translate();

            int remain = transformTimer; // 남은 틱(CompTick에서 감소하는 기존 필드)
            if (remain <= 0) return null;

            // 바닐라 포맷(다국어 대응)
            string timeStr = GenDate.ToStringTicksToPeriod(remain, allowSeconds: false, shortForm: true);

            // "변신: 남은 시간 {0}" / "Shapeshift: {0} remaining"
            return "SSF_Inspect_Remaining".Translate(timeStr);
        }

        #endregion

        #region 변신 가능 판정/지즈모 폼 캐시

        /// <summary>
        /// 대상 Pawn이 지정 폼으로 변신 가능한지 판정.
        /// </summary>
        public bool CanTransform(Pawn pawn, ShapeshiftFormDef form)
        {
            if (pawn == null || form == null) return false;
            string prev = (isTransformed && currentForm != null) ? currentForm.defName : null;
            if (!ShapeshiftEligibility.PassBasicFilters(pawn, form, prev)) return false; // allow/disallow* 게이트
            return ShapeshiftEligibility.PassConditional(pawn, form);                    // required* 집계(Mode 적용)
        }

        /// <summary>폼 선택 지즈모 캐시 무효화.</summary>
        private void InvalidateGizmoCache()
        {
            gizmoCacheTick = -9999;
            gizmoFormsCache = null;
        }

        /// <summary>사용 가능 폼을 주기적으로 캐싱하여 반환.</summary>
        IEnumerable<ShapeshiftFormDef> GetAvailableFormsCached(Pawn pawn)
        {
            int now = Find.TickManager.TicksGame;
            if (now - gizmoCacheTick > GizmoCacheInterval || gizmoFormsCache == null)
            {
                var all = DefDatabase<ShapeshiftFormDef>.AllDefsListForReading;
                List<ShapeshiftFormDef> list = new List<ShapeshiftFormDef>(all.Count);
                for (int i = 0; i < all.Count; i++)
                {
                    var f = all[i];
                    if (f == null) continue;
                    if (!f.hideGizmo && CanTransform(pawn, f))
                        list.Add(f);
                }
                gizmoFormsCache = list;
                gizmoCacheTick = now;
            }
            return gizmoFormsCache;
        }

        /// <summary>
        /// 해당 폼의 변신 조건(requiredApparels/Weapons)을 만족시킨 코어 아이템'들'을 모두 찾아냅니다.
        /// </summary>
        private List<Thing> FindSourceItemsForForm(ShapeshiftFormDef form)
        {
            List<Thing> found = new List<Thing>();
            var pawn = parent as Pawn;
            if (pawn == null || form == null) return found;

            // 1. 의류 검사
            if (form.requiredApparels != null && form.requiredApparels.Count > 0 && pawn.apparel != null)
            {
                foreach (var ap in pawn.apparel.WornApparel)
                {
                    if (form.requiredApparels.Contains(ap.def)) found.Add(ap);
                }
            }

            // 2. 무기 검사
            if (form.requiredWeapons != null && form.requiredWeapons.Count > 0 && pawn.equipment != null)
            {
                foreach (var eq in pawn.equipment.AllEquipmentListForReading)
                {
                    if (form.requiredWeapons.Contains(eq.def)) found.Add(eq);
                }
            }

            // 3. 일반 아이템(인벤토리 소지품) 검사
            if (form.requiredItems != null && form.requiredItems.Count > 0 && pawn.inventory?.innerContainer != null)
            {
                foreach (var t in pawn.inventory.innerContainer)
                {
                    if (form.requiredItems.Contains(t.def)) found.Add(t);
                }
            }

            return found;
        }

        #endregion

        #region 변신 적용/해제

        /// <summary>지정 폼 적용(이전 폼 없음).</summary>
        public void ApplyForm(ShapeshiftFormDef form) { ApplyForm(form, null, null); }

        /// <summary>
        /// 지정 폼 적용. <paramref name="prevOverride"/>가 있으면 먼저 해제 후 전환.
        /// - 실시간 재검증(기본/조건 게이트) → 장비 스냅샷 → 장비 처리 → 체형 백업 → 능력/헤디프 부여
        ///   → 상태 적용/FX/타이머 설정 → 체형/머리형 적용 → 사운드/혈흔/살점 캐시 → VerbTracker 초기화 → 갱신.
        /// </summary>
        public void ApplyForm(ShapeshiftFormDef form, string prevOverride, List<Thing> sources = null)
        {
            var pawn = parent as Pawn;
            if (pawn == null || form == null) return;

            this.sourceItems = sources ?? new List<Thing>();

            string prev = prevOverride ?? ((isTransformed && currentForm != null) ? currentForm.defName : null);

            // 실시간 재검증
            if (!ShapeshiftEligibility.PassBasicFilters(pawn, form, prev) ||
                !ShapeshiftEligibility.PassConditional(pawn, form))
            {
                try { Messages.Message("SSF_Message_CannotTransform".Translate(form.LabelCap), MessageTypeDefOf.RejectInput, false); } catch { }
                return;
            }

            // 전환(prevOverride가 주어지면 먼저 해제)
            if (isTransformed && prevOverride != null)
                RemoveForm();

            // 변신 전 장비 스냅샷 초기화 + 캡처
            prevApparels.Clear();
            prevWeapons.Clear();
            CaptureCurrentGear(pawn);

            // 변신 시 장비 처리(폼 설정 기반)
            try
            {
                HandleGearOnTransform(pawn, form);
                SpawnAndEquipFormGear(pawn, form);
            }
            catch (Exception ex)
            {
                // 장비 드랍/인벤토리 이동 중 타 모드 충돌 등으로 에러 발생 시 로그 출력 및 진행 강행
                // (이 과정에서 크래시가 나도 변신 자체는 진행되도록 보호)
                Log.Error($"[SSF] Error handling gear during transform for {pawn.Name}: {ex}");
            }

            // 최초 변신이면 체형 백업
            if (!isTransformed && pawn.story != null)
            {
                originalBodyType = pawn.story.bodyType;
                originalHeadType = pawn.story.headType;
            }

            // 능력 부여
            tempAddedAbilities.Clear();
            if (form.addAbilities != null && form.addAbilities.Count > 0 && pawn.abilities != null)
            {
                for (int i = 0; i < form.addAbilities.Count; i++)
                {
                    AbilityDef ad = form.addAbilities[i]; if (ad == null) continue;
                    if (pawn.abilities.GetAbility(ad) == null)
                    {
                        pawn.abilities.GainAbility(ad);
                        tempAddedAbilities.Add(ad);
                    }
                }
            }

            // 헤디프 부여
            tempAddedHediffs.Clear();
            tempAddedHediffsDefCache.Clear();
            tempPartRestoreRecords.Clear();
            if (form.addHediffs != null && form.addHediffs.Count > 0 && pawn.health != null)
            {
                ShapeshifterFramework.Utilities.ShapeshiftApplyHediffUtility.ApplyHediffEntries(
                    pawn,
                    form.addHediffs,
                    tempAddedHediffs,
                    tempAddedHediffsDefCache,
                    tempPartRestoreRecords,
                    prevDefCache: tempAddedHediffsDefCache  // 이전 변신에서 우리가 추가한 것만 정리
                );
            }

            // 상태 적용
            currentForm = form;

            // 동적 스탯 헤디프 부여(바닐라 건강 탭에 스탯 변동 표시용)
            if (form.generatedStatHediff != null && pawn.health != null)
            {
                if (pawn.health.hediffSet.GetFirstHediffOfDef(form.generatedStatHediff) == null)
                {
                    Hediff statHediff = pawn.health.AddHediff(form.generatedStatHediff);
                    if (statHediff != null)
                    {
                        tempAddedHediffs.Add(statHediff);
                        tempAddedHediffsDefCache.Add(form.generatedStatHediff);
                    }
                }
            }

            ShapeshiftTransformFxUtility.PlayEnterFx(pawn, form); // 변신 시작 FX
            if (form.durationTicks.HasValue && form.durationTicks.Value > 0)
                transformTimer = form.durationTicks.Value;

            // 체형/머리형 적용(인간형만)
            if (pawn.story != null)
            {
                if (form.bodyType != null) pawn.story.bodyType = form.bodyType;
                if (form.headType != null) pawn.story.headType = form.headType;
            }

            // 런타임 캐시 등록
            ApplyRuntimeCaches(pawn, form);

            // 전용 VerbTracker는 프로퍼티 접근 시 생성 → Refresh에서 Verb 리셋 포함
            shapeshiftVerbTracker = null;

            RefreshPawn(pawn);
            InvalidateGizmoCache();
        }

        /// <summary>
        /// 현재 폼 해제.
        /// - 능력/헤디프 회수 및 파츠 원복
        /// - 장비 재착용(설정값에 따라 인벤/바닥)
        /// - 체형/머리형 원복, 캐시 정리/FX/지즈모 갱신
        /// </summary>
        public void RemoveForm()
        {
            var pawn = parent as Pawn;
            if (pawn == null) return;
            var __oldForm = currentForm;

            // 소환된 전용 장비 강제 파괴 (바닥에 떨어져 복사되는 버그 원천 차단)
            using (new ShapeshiftEquipLockScope(this)) // 락 무시
            {
                for (int i = generatedApparel.Count - 1; i >= 0; i--)
                {
                    if (generatedApparel[i] != null && !generatedApparel[i].Destroyed)
                        generatedApparel[i].Destroy(DestroyMode.Vanish);
                }
                generatedApparel.Clear();

                for (int i = generatedWeapons.Count - 1; i >= 0; i--)
                {
                    if (generatedWeapons[i] != null && !generatedWeapons[i].Destroyed)
                        generatedWeapons[i].Destroy(DestroyMode.Vanish);
                }
                generatedWeapons.Clear();
            }
            if (this.sourceItems != null) this.sourceItems.Clear(); // 기원 아이템 초기화

            // 능력 회수
            if (pawn.abilities != null && tempAddedAbilities.Count > 0)
            {
                for (int i = 0; i < tempAddedAbilities.Count; i++)
                {
                    AbilityDef ad = tempAddedAbilities[i];
                    if (ad != null) pawn.abilities.RemoveAbility(ad);
                }
                tempAddedAbilities.Clear();
            }

            // 헤디프 회수 + 원상 복원
            if (pawn.health != null)
            {
                // 1) 인스턴스로 기억하고 있는 헤디프를 먼저 안전하게 제거
                for (int i = 0; i < tempAddedHediffs.Count; i++)
                {
                    Hediff h = tempAddedHediffs[i];
                    if (h != null && pawn.health.hediffSet.hediffs.Contains(h))
                    {
                        pawn.health.RemoveHediff(h);
                        // 정상적으로 지운 녀석은 2단계 순회에서 제외
                        if (h.def != null) tempAddedHediffsDefCache.Remove(h.def);
                    }
                }

                // 2) 세이브 로드 등의 이유로 인스턴스가 꼬였으나 Def 기록은 남은 경우에만 작동하는 방어적 정리
                if (tempAddedHediffsDefCache != null && tempAddedHediffsDefCache.Count > 0)
                {
                    List<Hediff> list = pawn.health.hediffSet.hediffs;
                    for (int i = 0; i < tempAddedHediffsDefCache.Count; i++)
                    {
                        HediffDef def = tempAddedHediffsDefCache[i];
                        if (def == null) continue;

                        // 뒤에서부터 순회하며 1개만 지우고 중단
                        for (int j = list.Count - 1; j >= 0; j--)
                        {
                            if (list[j].def == def)
                            {
                                pawn.health.RemoveHediff(list[j]);
                                break;
                            }
                        }
                    }
                }

                // 3) 파츠 단위 원상 복원
                //    - 변신 전 결손이 아니었던 파츠: RestorePart로 자연복원(무출혈)
                //    - 변신 전 기존 AddedPart가 있었던 파츠: 다시 재설치(복구)
                for (int i = 0; i < tempPartRestoreRecords.Count; i++)
                {
                    var rec = tempPartRestoreRecords[i];
                    if (rec == null || rec.Part == null) continue;

                    // 변신 전 결손이 아니었다면 자연 파츠 복원
                    if (!rec.WasMissingBefore)
                    {
                        try { pawn.health.RestorePart(rec.Part); }
                        catch (System.Exception ex) { Log.Warning($"[SSF] RestorePart failed for '{rec.Part.Label}': {ex}"); }
                    }

                    // 변신 전 설치되어 있던 AddedPart들을 다시 설치(복구)
                    if (rec.PreExistingAdded != null && rec.PreExistingAdded.Count > 0)
                    {
                        for (int k = 0; k < rec.PreExistingAdded.Count; k++)
                        {
                            var prev = rec.PreExistingAdded[k];
                            if (prev?.Def == null) continue;

                            BodyPartRecord targetPart = null;
                            // 저장된 PartDef가 타겟 파츠와 같다면 본체에 부착
                            if (prev.PartDef == null || prev.PartDef == rec.Part.def)
                            {
                                targetPart = rec.Part;
                            }
                            else
                            {
                                // 타겟 파츠 하위의 모든 파츠 중 PartDef가 일치하는 부위 찾기
                                var allParts = pawn.RaceProps.body.AllParts;
                                for (int pIdx = 0; pIdx < allParts.Count; pIdx++)
                                {
                                    var x = allParts[pIdx];
                                    // notMissing.Contains(x)를 !pawn.health.hediffSet.PartIsMissing(x)로 교체
                                    if (x.def == prev.PartDef && !pawn.health.hediffSet.PartIsMissing(x) && IsPartChildOf(x, rec.Part))
                                    {
                                        targetPart = x;
                                        break;
                                    }
                                }
                            }

                            if (targetPart != null)
                            {
                                var reinst = pawn.health.AddHediff(prev.Def, targetPart, null);
                                if (reinst != null && prev.Severity.HasValue)
                                {
                                    try { reinst.Severity = prev.Severity.Value; }
                                    catch (System.Exception ex) { Log.Warning($"[SSF] Restore Severity failed for '{prev.Def.defName}': {ex}"); }
                                }
                            }
                        }
                    }
                    else
                    {
                        // 설계: WasMissingBefore == true 이면 MissingPart 그대로 유지(별도 조치 없음)
                    }
                }

                ShapeshiftDiagnostics.Info($"Revert: restored {tempPartRestoreRecords.Count} part(s)");

                tempAddedHediffs.Clear();
                tempAddedHediffsDefCache.Clear();
                tempPartRestoreRecords.Clear();
            }

            transformTimer = 0;

            // 체형/머리형 원복(인간형만)
            if (pawn.story != null)
            {
                if (originalBodyType != null) pawn.story.bodyType = originalBodyType;
                if (originalHeadType != null) pawn.story.headType = originalHeadType;
            }

            // 해제 후 자동 재착용
            ShapeshifterFrameworkSettings st = ShapeshifterFrameworkMod.Settings;
            if (st == null || st.autoReequipFromInventory || st.autoReequipFromGround)
                TryReequipPreviousGear(pawn);

            // 전용 VerbTracker 해제
            shapeshiftVerbTracker = null;

            ShapeshiftTransformFxUtility.PlayExitFx(pawn, __oldForm); // 변신 해제 FX

            currentForm = null;

            // 캐시 해제
            ShapeshiftRuntimeCaches.ClearFor(pawn);

            RefreshPawn(pawn);
            InvalidateGizmoCache();
        }

        /// <summary>
        /// 특정 신체 부위가 대상 부위의 하위 부위인지 확인합니다.
        /// </summary>
        private bool IsPartChildOf(BodyPartRecord child, BodyPartRecord parent)
        {
            if (child == null || parent == null) return false;

            BodyPartRecord current = child.parent;
            while (current != null)
            {
                if (current == parent) return true;
                current = current.parent;
            }
            return false;
        }

        #endregion

        #region 외부 알림 (예: Pawn.Kill Postfix에서 호출)

        /// <summary>
        /// Pawn이 사망했을 때 호출됨. 변신 해제 및 런타임 캐시 정리를 강제 수행.
        /// </summary>
        public void Notify_Killed(DamageInfo? dinfo, Hediff exactCulprit)
        {
            var pawn = parent as Pawn;
            if (pawn == null) return;

            // 해제되기 전 변신 상태였는지 기억
            bool wasTransformed = isTransformed;

            if (isTransformed)
            {
                RemoveForm();
            }

            // 변신 중이 아니었더라도(isTransformed == false) 알 수 없는 이유로 캐시에 찌꺼기가 
            // 남아있을 수 있으므로, 폰이 사망할 때는 무조건 캐시를 한 번 더 날려서 
            // 메모리 누수와 폰 부활 시 발생할 수 있는 유령 데이터 버그를 원천 차단함.
            ShapeshiftRuntimeCaches.ClearFor(pawn);

            // 원래 변신 상태였던 폰일 때만 디버그 로그 출력 (설정 체크는 Info 내부에서 자동 처리)
            if (wasTransformed)
            {
                ShapeshiftDiagnostics.Info($"{pawn.LabelShort} killed, shapeshift forcibly deactivated.");
            }
        }

        #endregion

        #region 내부: 장비 스냅샷/처리/재착용/드랍 유틸

        /// <summary>현재 의복/무기를 스냅샷으로 저장(해제 시 재착용용).</summary>
        void CaptureCurrentGear(Pawn pawn)
        {
            if (pawn == null) return;

            // 의복
            if (pawn.apparel != null)
            {
                List<Apparel> worn = pawn.apparel.WornApparel;
                for (int i = 0; i < worn.Count; i++)
                {
                    var a = worn[i];
                    if (a != null) prevApparels.Add(a);
                }
            }

            // 무기
            if (pawn.equipment != null)
            {
                List<ThingWithComps> eqs = pawn.equipment.AllEquipmentListForReading;
                for (int i = 0; i < eqs.Count; i++)
                {
                    var e = eqs[i];
                    if (e != null) prevWeapons.Add(e);
                }
            }
        }

        /// <summary>변신 시 장비 처리(인벤토리 이동/드랍/유지).</summary>
        void HandleGearOnTransform(Pawn pawn, ShapeshiftFormDef form)
        {
            if (pawn == null || form == null) return;

            IntVec3 pos = pawn.PositionHeld;
            Map map = pawn.MapHeld;
            ShapeshifterFrameworkSettings st = ShapeshifterFrameworkMod.Settings;

            // 의복
            if (form.apparelOnTransform != GearHandling.Keep && pawn.apparel != null)
            {
                List<Apparel> worn = pawn.apparel.WornApparel;
                List<Apparel> copy = new List<Apparel>(worn.Count);
                for (int i = 0; i < worn.Count; i++) { if (worn[i] != null) copy.Add(worn[i]); }

                for (int i = 0; i < copy.Count; i++)
                {
                    Apparel ap = copy[i];
                    if (ap == null) continue;

                    if ((sourceItems != null && sourceItems.Contains(ap)) || generatedApparel.Contains(ap)) continue;

                    if (form.apparelOnTransform == GearHandling.Inventory)
                    {
                        pawn.apparel.Remove(ap);
                        if (pawn.inventory != null && pawn.inventory.innerContainer != null)
                        {
                            if (!pawn.inventory.innerContainer.TryAdd(ap, false))
                                TryDropThing(ap, pos, map);
                        }
                        else TryDropThing(ap, pos, map);
                    }
                    else // Drop
                    {
                        Apparel dropped = null;
                        if (!pawn.apparel.TryDrop(ap, out dropped, pos, forbid: false))
                        {
                            pawn.apparel.Remove(ap);
                            TryDropThing(ap, pos, map);
                            dropped = ap;
                        }

                        if (st != null && st.forbidDroppedItemsOnTransform && dropped != null && dropped.Spawned)
                        {
                            dropped.SetForbidden(true); // 다른 폰의 개입 방지
                        }
                    }
                }
            }

            // 무기
            if (form.weaponsOnTransform != GearHandling.Keep && pawn.equipment != null)
            {
                List<ThingWithComps> list = pawn.equipment.AllEquipmentListForReading;
                List<ThingWithComps> copy = new List<ThingWithComps>(list.Count);
                for (int i = 0; i < list.Count; i++) { if (list[i] != null) copy.Add(list[i]); }

                for (int i = 0; i < copy.Count; i++)
                {
                    ThingWithComps eq = copy[i];
                    if (eq == null) continue;

                    if ((sourceItems != null && sourceItems.Contains(eq)) || generatedWeapons.Contains(eq)) continue;

                    if (form.weaponsOnTransform == GearHandling.Inventory)
                    {
                        pawn.equipment.Remove(eq);
                        if (pawn.inventory != null && pawn.inventory.innerContainer != null)
                        {
                            if (!pawn.inventory.innerContainer.TryAdd(eq, false))
                                TryDropThing(eq, pos, map);
                        }
                        else TryDropThing(eq, pos, map);
                    }
                    else // Drop
                    {
                        ThingWithComps dropped = null;
                        if (!pawn.equipment.TryDropEquipment(eq, out dropped, pos, forbid: false))
                        {
                            pawn.equipment.Remove(eq);
                            TryDropThing(eq, pos, map);
                            dropped = eq;
                        }

                        if (st != null && st.forbidDroppedItemsOnTransform && dropped != null && dropped.Spawned)
                        {
                            dropped.SetForbidden(true);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 변신 시 폼 전용 장비 소환 및 장착 (겹치는 기존 장비 스마트 처리 포함)
        /// </summary>
        void SpawnAndEquipFormGear(Pawn pawn, ShapeshiftFormDef form)
        {
            if (pawn == null || form == null) return;

            using (new ShapeshiftEquipLockScope(this)) // 강제 장착을 위해 락 임시 해제
            {
                // 1. 전용 의류 소환
                if (form.spawnApparelOnTransform != null && form.spawnApparelOnTransform.Count > 0)
                {
                    for (int i = 0; i < form.spawnApparelOnTransform.Count; i++)
                    {
                        ThingDef apparelDef = form.spawnApparelOnTransform[i];
                        if (apparelDef == null || !apparelDef.IsApparel) continue;

                        // 새 슈트와 부위가 겹치는 기존 옷들을 찾아서 인벤토리로 피신
                        if (pawn.apparel != null)
                        {
                            List<Apparel> worn = pawn.apparel.WornApparel;
                            for (int j = worn.Count - 1; j >= 0; j--)
                            {
                                Apparel existingAp = worn[j];
                                if ((sourceItems != null && sourceItems.Contains(existingAp)) || generatedApparel.Contains(existingAp)) continue;

                                // 바닐라 엔진을 이용해 겹침 여부 판정
                                if (!ApparelUtility.CanWearTogether(apparelDef, existingAp.def, pawn.RaceProps.body))
                                {
                                    pawn.apparel.Remove(existingAp); // 안전하게 벗김
                                    if (form.conflictingGearHandling == GearHandling.Drop)
                                    {
                                        TryDropThing(existingAp, pawn.PositionHeld, pawn.MapHeld);
                                    }
                                    else // Inventory (또는 Keep)
                                    {
                                        if (pawn.inventory?.innerContainer != null && pawn.inventory.innerContainer.TryAdd(existingAp, false)) { }
                                        else TryDropThing(existingAp, pawn.PositionHeld, pawn.MapHeld);
                                    }

                                    // 바닥에 떨어졌다면 상호작용 금지 처리
                                    if (existingAp.Spawned && ShapeshifterFrameworkMod.Settings != null && ShapeshifterFrameworkMod.Settings.forbidDroppedItemsOnTransform)
                                    {
                                        existingAp.SetForbidden(true);
                                    }

                                    // 해제 시 다시 입어야 하므로 장부에 기록
                                    if (!prevApparels.Contains(existingAp)) prevApparels.Add(existingAp);
                                }
                            }
                        }

                        // 1. 재료 안전망 적용
                        ThingDef stuff = null;
                        if (apparelDef.MadeFromStuff) // 재료를 요구하는 장비인지 확인
                        {
                            stuff = form.spawnApparelStuff;
                            // 모더가 재료를 안 적었거나 불일치시
                            if (stuff == null || stuff.stuffProps == null || !stuff.stuffProps.CanMake(apparelDef))
                            {
                                stuff = GenStuff.DefaultStuffFor(apparelDef); // 강제로 기본 재료(천, 강철 등)로 교체
                            }
                        }

                        // 2. 옷 생성 및 착용
                        Apparel newApparel = (Apparel)ThingMaker.MakeThing(apparelDef, stuff);

                        if (pawn.apparel != null)
                        {
                            pawn.apparel.Wear(newApparel, dropReplacedApparel: false);
                            pawn.apparel.Lock(newApparel);
                            generatedApparel.Add(newApparel);
                        }
                    }
                }

                // 2. 전용 무기 소환
                if (form.spawnWeaponOnTransform != null && form.spawnWeaponOnTransform.Count > 0)
                {
                    // 무기는 슬롯이 겹치므로 기존 무기가 있다면 인벤토리나 바닥으로 처리
                    if (pawn.equipment != null && pawn.equipment.Primary != null)
                    {
                        ThingWithComps existingWep = pawn.equipment.Primary;
                        if ((sourceItems == null || !sourceItems.Contains(existingWep)) && !generatedWeapons.Contains(existingWep))
                        {
                            pawn.equipment.Remove(existingWep);

                            // 설정값에 따라 드랍할지 인벤토리로 피신시킬지 결정
                            if (form.conflictingGearHandling == GearHandling.Drop)
                            {
                                TryDropThing(existingWep, pawn.PositionHeld, pawn.MapHeld);
                            }
                            else // Inventory (또는 Keep)
                            {
                                if (pawn.inventory?.innerContainer != null && pawn.inventory.innerContainer.TryAdd(existingWep, false)) { }
                                else TryDropThing(existingWep, pawn.PositionHeld, pawn.MapHeld);
                            }

                            // 바닥에 떨어졌다면 상호작용 금지 처리
                            if (existingWep.Spawned && ShapeshifterFrameworkMod.Settings != null && ShapeshifterFrameworkMod.Settings.forbidDroppedItemsOnTransform)
                            {
                                existingWep.SetForbidden(true);
                            }

                            if (!prevWeapons.Contains(existingWep)) prevWeapons.Add(existingWep);
                        }
                    }

                    for (int i = 0; i < form.spawnWeaponOnTransform.Count; i++)
                    {
                        ThingDef weaponDef = form.spawnWeaponOnTransform[i];
                        if (weaponDef == null || !weaponDef.IsWeapon) continue;

                        // 1. 재료 안전망 적용
                        ThingDef stuff = null;
                        if (weaponDef.MadeFromStuff) // 재료를 요구하는 장비인지 확인
                        {
                            stuff = form.spawnWeaponStuff;
                            if (stuff == null || stuff.stuffProps == null || !stuff.stuffProps.CanMake(weaponDef))
                            {
                                stuff = GenStuff.DefaultStuffFor(weaponDef); // 강제로 기본 재료(천, 강철 등)로 교체
                            }
                        }

                        // 2. 무기 생성 및 장착
                        ThingWithComps newWeapon = (ThingWithComps)ThingMaker.MakeThing(weaponDef, stuff);

                        if (pawn.equipment != null)
                        {
                            pawn.equipment.AddEquipment(newWeapon);
                            generatedWeapons.Add(newWeapon);
                        }
                    }
                }
            }
        }

        /// <summary>안전 드랍/분리 처리 유틸.</summary>
        static void TryDropThing(Thing t, IntVec3 pos, Map map)
        {
            if (t == null) return;
            try
            {
                if (map != null && pos.IsValid)
                {
                    GenPlace.TryPlaceThing(t, pos, map, ThingPlaceMode.Near);
                }
                else
                {
                    ThingOwner owner = t.holdingOwner;
                    if (owner != null) owner.Remove(t);
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"[SSF] TryDropThing failed for '{t.Label}': {ex}");
            }
        }

        /// <summary>해제 후 이전 장비 재착용(인벤/바닥, 설정값에 따름).</summary>
        void TryReequipPreviousGear(Pawn pawn)
        {
            ShapeshiftDiagnostics.Info($"TryReequip: weapons={prevWeapons.Count}, apparels={prevApparels.Count}");
            if (pawn == null || pawn.Dead) return;

            ShapeshifterFrameworkSettings st = ShapeshifterFrameworkMod.Settings;
            bool allowInv = (st == null) ? true : st.autoReequipFromInventory;
            bool allowGround = (st == null) ? true : st.autoReequipFromGround;

            var toQueue = new List<Job>(prevWeapons.Count + prevApparels.Count);

            // 변신 해제 중 내부 재장착은 착용락을 임시 해제
            using (new ShapeshiftEquipLockScope(this))
            {
                // ── 무기
                if (prevWeapons.Count > 0)
                {
                    for (int i = 0; i < prevWeapons.Count; i++)
                    {
                        ThingWithComps w = prevWeapons[i];
                        if (w == null || w.Destroyed) continue;

                        if (w.Spawned)
                        {
                            // 바닥에 있으면 잡 큐로 자연스런 장착
                            if (!allowGround) continue;

                            // 맵이 같고, 물리적으로 도달 가능한 경우에만 줍기
                            if (w.Map == pawn.MapHeld && pawn.CanReach(w, PathEndMode.ClosestTouch, Danger.Deadly))
                            {
                                if (w.IsForbidden(pawn)) w.SetForbidden(false);
                                Job job = JobMaker.MakeJob(JobDefOf.Equip, w);
                                job.playerForced = true;
                                toQueue.Add(job);
                            }
                            continue;
                        }

                        // 인벤이면 즉시 장착(실패 시 인벤 복구/가득하면 드랍)
                        if (allowInv && pawn.inventory?.innerContainer?.Contains(w) == true)
                        {
                            ShapeshiftInventoryReequipUtility.SafeEquipFromInventory(pawn, w);
                        }
                    }
                }

                // ── 의복
                if (prevApparels.Count > 0)
                {
                    for (int i = 0; i < prevApparels.Count; i++)
                    {
                        Apparel ap = prevApparels[i];
                        if (ap == null || ap.Destroyed) continue;

                        if (ap.Spawned)
                        {
                            if (!allowGround) continue;

                            if (ap.Map == pawn.MapHeld && pawn.CanReach(ap, PathEndMode.ClosestTouch, Danger.Deadly))
                            {
                                if (ap.IsForbidden(pawn)) ap.SetForbidden(false);
                                Job job = JobMaker.MakeJob(JobDefOf.Wear, ap);
                                job.playerForced = true;
                                toQueue.Add(job);
                            }
                            continue;
                        }

                        if (allowInv && pawn.inventory?.innerContainer?.Contains(ap) == true)
                        {
                            ShapeshiftInventoryReequipUtility.SafeWearFromInventory(pawn, ap, dropReplaced: true);
                        }
                    }
                }
            } // using scope 끝: 착용락 원복

            // 잡 실행(첫 작업 시작, 나머지는 큐)
            if (toQueue.Count > 0 && pawn.jobs != null)
            {
                Job first = toQueue[0];
                pawn.jobs.TryTakeOrderedJob(first);
                for (int i = 1; i < toQueue.Count; i++)
                    pawn.jobs.jobQueue.EnqueueLast(toQueue[i]);
            }

            prevWeapons.Clear();
            prevApparels.Clear();
        }

        #endregion

        #region 캐시/그래픽/버브 재초기화

        /// <summary>
        /// 폼의 런타임 캐시(사운드/혈흔/FleshType)를 등록한다.
        /// ApplyForm 시, 그리고 세이브 로드 후 PostLoadInit에서 재등록 시 공통 호출.
        /// ConditionalWeakTable은 세이브되지 않으므로 로드 후 반드시 재등록이 필요하다.
        /// </summary>
        private static void ApplyRuntimeCaches(Pawn pawn, ShapeshiftFormDef form)
        {
            if (pawn == null || form == null) return;

            // 보이스 캐시
            if (form.soundCall != null) ShapeshiftRuntimeCaches.SetCache(ShapeshiftRuntimeCaches.CallByPawn, pawn, form.soundCall);
            if (form.soundWounded != null) ShapeshiftRuntimeCaches.SetCache(ShapeshiftRuntimeCaches.WoundedByPawn, pawn, form.soundWounded);
            if (form.soundDeath != null) ShapeshiftRuntimeCaches.SetCache(ShapeshiftRuntimeCaches.DeathByPawn, pawn, form.soundDeath);
            if (form.soundAngry != null) ShapeshiftRuntimeCaches.SetCache(ShapeshiftRuntimeCaches.AngryByPawn, pawn, form.soundAngry);

            // 혈흔/스미어 캐시
            if (form.bloodDef != null) ShapeshiftRuntimeCaches.SetCache(ShapeshiftRuntimeCaches.BloodByPawn, pawn, form.bloodDef);
            if (form.bloodSmearDef != null) ShapeshiftRuntimeCaches.SetCache(ShapeshiftRuntimeCaches.SmearByPawn, pawn, form.bloodSmearDef);

            // FleshType 캐시
            if (form.fleshType != null) ShapeshiftRuntimeCaches.SetCache(ShapeshiftRuntimeCaches.FleshTypeByPawn, pawn, form.fleshType);
        }

        /// <summary>
        /// 변신 후 각종 캐시/그래픽/버브 재초기화까지 한 번에 정리.
        /// - pawn.verbTracker는 VerbsNeedReinitOnLoad()로 무효화 → AllVerbs 접근으로 즉시 재빌드
        /// - 이 컴포넌트 전용 shapeshiftVerbTracker는 null로 지워서 폼 기준으로 재빌드
        /// - 선택/그리기/세이브 캐시 등도 안전하게 더럽힘
        /// </summary>
        public static void RefreshPawn(Pawn pawn, bool forceReinitPawnVerbs = true, bool resetShapeshiftVerbs = true, bool refreshSelection = true)
        {
            if (pawn == null) return;

            // 능력/헤디프/그래픽/초상화/아틀라스/작업종류 캐시 더럽히기
            try { pawn.health?.capacities?.Notify_CapacityLevelsDirty(); } catch (System.Exception ex) { Log.Warning($"[SSF] RefreshPawn (Capacity) error: {ex}"); }
            try { pawn.health?.hediffSet?.DirtyCache(); } catch (System.Exception ex) { Log.Warning($"[SSF] RefreshPawn (Hediff) error: {ex}"); }
            try { pawn.Drawer?.renderer?.SetAllGraphicsDirty(); } catch (System.Exception ex) { Log.Warning($"[SSF] RefreshPawn (Graphics) error: {ex}"); }
            try { PortraitsCache.SetDirty(pawn); } catch (System.Exception ex) { Log.Warning($"[SSF] RefreshPawn (Portrait) error: {ex}"); }
            try { GlobalTextureAtlasManager.TryMarkPawnFrameSetDirty(pawn); } catch (System.Exception ex) { Log.Warning($"[SSF] RefreshPawn (Atlas) error: {ex}"); }
            try { pawn.Notify_DisabledWorkTypesChanged(); } catch (System.Exception ex) { Log.Warning($"[SSF] RefreshPawn (WorkTypes) error: {ex}"); }

            // 1) 바닐라 쪽 VerbTracker 재초기화
            //    VerbsNeedReinitOnLoad()가 내부 verbs를 null로 만들어 다음 접근에서 재구성되게 함.
            try
            {
                if (forceReinitPawnVerbs)
                {
                    pawn.verbTracker?.VerbsNeedReinitOnLoad();
                    // 재빌드를 지금 당장 유도: AllVerbs 접근 시 InitVerbsFromZero→InitVerbs 경로로 빌드됨
                    var _ = pawn.verbTracker?.AllVerbs;
                }
            }
            catch (System.Exception ex) { Log.Warning($"[SSF] RefreshPawn (Vanilla Verbs) error: {ex}"); }

            // 2) 변신 폼 전용 VerbTracker 재초기화
            try
            {
                var comp = pawn.TryGetComp<CompShapeshifter>();
                if (comp != null && resetShapeshiftVerbs)
                {
                    // 동일 클래스 내부이므로 private 필드 접근 가능
                    comp.shapeshiftVerbTracker = null;

                    // 즉시 재구성되어 캐스터가 pawn으로 들어가게끔 한 번 접근
                    var vt = comp.ShapeshiftVerbTracker;
                    if (vt != null)
                    {
                        var __ = vt.AllVerbs; // 강제 초기화
                    }
                }
            }
            catch (System.Exception ex) { Log.Warning($"[SSF] RefreshPawn (Shapeshift Verbs) error: {ex}"); }

            // 3) UI 갱신: 선택 토글로 지즈모 강제 새로고침
            try
            {
                if (refreshSelection && Find.Selector != null && Find.Selector.IsSelected(pawn))
                {
                    // 시그니처: (Thing obj, bool playSound = true, bool forceDesignatorDeselect = true)
                    Find.Selector.Deselect(pawn);
                    Find.Selector.Select(pawn, playSound: false, forceDesignatorDeselect: false);
                }
            }
            catch (System.Exception ex) { Log.Warning($"[SSF] RefreshPawn (UI Selection) error: {ex}"); }

            // 4) 에러 로그 스팸 방지 플래그 초기화
            try
            {
                var comp = pawn.TryGetComp<CompShapeshifter>();
                if (comp != null)
                {
                    comp.verbTickErrorLogged = false;
                }
            }
            catch (System.Exception ex) { Log.Warning($"[SSF] RefreshPawn (Error Reset) error: {ex}"); }
        }

        #endregion

        #region 저장/로드 (IExposable) — BackCompatibility 주석 포함

        /// <summary>
        /// 저장/로드. 값/참조 분리 저장 및 신규 키에 대한 기본 동작을 보장한다.
        /// BackCompatibility: verbAutoToggle 키가 없으면 기본값(On)으로 동작.
        /// </summary>
        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Defs.Look(ref currentForm, "currentForm");
            Scribe_Values.Look(ref transformTimer, "transformTimer", 0, true);
            Scribe_Defs.Look(ref originalBodyType, "originalBodyType");
            Scribe_Defs.Look(ref originalHeadType, "originalHeadType");

            // === Def 리스트 (LoadingVars에서 즉시 사용 가능) ===

            List<AbilityDef> __tmpAbilities = null;
            if (Scribe.mode == LoadSaveMode.Saving) __tmpAbilities = tempAddedAbilities;
            Scribe_Collections.Look(ref __tmpAbilities, "tempAddedAbilities", LookMode.Def);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                tempAddedAbilities.Clear();
                if (__tmpAbilities != null) tempAddedAbilities.AddRange(__tmpAbilities);
            }

            List<HediffDef> __tmpHediffDefs = null;
            if (Scribe.mode == LoadSaveMode.Saving) __tmpHediffDefs = tempAddedHediffsDefCache;
            Scribe_Collections.Look(ref __tmpHediffDefs, "tempAddedHediffsDefCache", LookMode.Def);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                tempAddedHediffsDefCache.Clear();
                if (__tmpHediffDefs != null) tempAddedHediffsDefCache.AddRange(__tmpHediffDefs);
            }

            // === Reference 리스트 - hediff (폰 내부 객체라 Reference 정상 작동) ===

            List<Hediff> __tmpHediffs = null;
            if (Scribe.mode == LoadSaveMode.Saving) __tmpHediffs = tempAddedHediffs;
            Scribe_Collections.Look(ref __tmpHediffs, "tempAddedHediffs", LookMode.Reference);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                tempAddedHediffs.Clear();
                __tmpHediffsLoad = __tmpHediffs;
            }

            // === prevApparels - ThingID 문자열 저장, CompTick에서 인벤토리 및 맵 검색으로 복원 (HashSet 최적화) ===

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                List<string> apIds = new List<string>(prevApparels.Count);
                for (int i = 0; i < prevApparels.Count; i++)
                {
                    if (prevApparels[i] != null) apIds.Add(prevApparels[i].ThingID);
                }
                Scribe_Collections.Look(ref apIds, "prevApparelIds", LookMode.Value);
            }
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                List<string> apIds = null;
                Scribe_Collections.Look(ref apIds, "prevApparelIds", LookMode.Value);
                prevApparels.Clear();
                // List를 HashSet으로 변환하여 O(1) 검색 캐싱
                __tmpPrevApIds = apIds != null ? new HashSet<string>(apIds) : null;
            }

            // === prevWeapons - 위와 동일 방식 (인벤토리 및 맵 검색) ===

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                List<string> wpIds = new List<string>(prevWeapons.Count);
                for (int i = 0; i < prevWeapons.Count; i++)
                {
                    if (prevWeapons[i] != null) wpIds.Add(prevWeapons[i].ThingID);
                }
                Scribe_Collections.Look(ref wpIds, "prevWeaponIds", LookMode.Value);
            }
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                List<string> wpIds = null;
                Scribe_Collections.Look(ref wpIds, "prevWeaponIds", LookMode.Value);
                prevWeapons.Clear();
                // List를 HashSet으로 변환하여 O(1) 검색 캐싱
                __tmpPrevWpIds = wpIds != null ? new HashSet<string>(wpIds) : null;
            }

            // === Deep 리스트 (LoadingVars에서 즉시 사용 가능) ===

            List<ShapeshiftPartRestoreRecord> __tmpRestore = null;
            if (Scribe.mode == LoadSaveMode.Saving) __tmpRestore = tempPartRestoreRecords;
            Scribe_Collections.Look(ref __tmpRestore, "tempPartRestoreRecords", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                tempPartRestoreRecords.Clear();
                if (__tmpRestore != null) tempPartRestoreRecords.AddRange(__tmpRestore);
            }

            // === verbAutoToggle 딕셔너리 ===

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                List<string> __keys = new List<string>(verbAutoToggle.Count);
                List<bool> __vals = new List<bool>(verbAutoToggle.Count);
                foreach (var kv in verbAutoToggle) { __keys.Add(kv.Key); __vals.Add(kv.Value); }
                Scribe_Collections.Look(ref __keys, "ssfVerbToggleKeys", LookMode.Value);
                Scribe_Collections.Look(ref __vals, "ssfVerbToggleVals", LookMode.Value);
            }
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                List<string> __keys = null; List<bool> __vals = null;
                Scribe_Collections.Look(ref __keys, "ssfVerbToggleKeys", LookMode.Value);
                Scribe_Collections.Look(ref __vals, "ssfVerbToggleVals", LookMode.Value);
                verbAutoToggle.Clear();
                if (__keys != null && __vals != null && __keys.Count == __vals.Count)
                {
                    for (int i = 0; i < __keys.Count; i++) verbAutoToggle[__keys[i]] = __vals[i];
                }
            }
            Scribe_Collections.Look(ref sourceItems, "sourceItems", LookMode.Reference);
            Scribe_Collections.Look(ref generatedApparel, "generatedApparel", LookMode.Reference);
            Scribe_Collections.Look(ref generatedWeapons, "generatedWeapons", LookMode.Reference);
            // === PostLoadInit ===

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (sourceItems != null) sourceItems.RemoveAll(x => x == null);
                else sourceItems = new List<Thing>();

                if (generatedApparel != null) generatedApparel.RemoveAll(x => x == null);
                else generatedApparel = new List<Apparel>();

                if (generatedWeapons != null) generatedWeapons.RemoveAll(x => x == null);
                else generatedWeapons = new List<ThingWithComps>();

                if (__tmpHediffsLoad != null)
                {
                    for (int i = 0; i < __tmpHediffsLoad.Count; i++)
                    {
                        if (__tmpHediffsLoad[i] != null)
                            tempAddedHediffs.Add(__tmpHediffsLoad[i]);
                    }
                    __tmpHediffsLoad = null;
                }
                else
                {
                    for (int i = tempAddedHediffs.Count - 1; i >= 0; i--)
                    {
                        if (tempAddedHediffs[i] == null)
                            tempAddedHediffs.RemoveAt(i);
                    }
                }

                needsGearResolve = true;

                Pawn pawn = parent as Pawn;
                if (pawn != null && pawn.Dead && isTransformed)
                {
                    RemoveForm();
                }
                else if (isTransformed && currentForm != null && pawn != null)
                {
                    ApplyRuntimeCaches(pawn, currentForm);
                }
            }
        }

        #endregion

        #region 기즈모 생성

        /// <summary>
        /// 변신/해제/전환 및 폼 전용 verb 지즈모를 생성한다.
        /// - 플레이어 조종 Pawn만 허용
        /// - 변신 중 여부에 따라 메뉴/인라인 버튼 구성
        /// - 드래프트 상태에서 원거리 verb용 토글/공격 버튼 노출
        /// </summary>
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            var pawn = parent as Pawn;
            if (pawn == null) yield break;

            // 플레이어가 직접 조종 가능한 Pawn(식민지인, 식민지노예, 퀘스트 손님 등)만 허용
            if (!pawn.IsColonistPlayerControlled)
                yield break;

            // (1) 변신/해제/전환 — 프레임워크 고유 지즈모
            int threshold = (ShapeshifterFrameworkMod.Settings != null)
                ? ShapeshifterFrameworkMod.Settings.maxInlineGizmoCount
                : 8;

            GetAvailableFormsCached(pawn);

            if (!isTransformed)
            {
                if (gizmoFormsCache.Count > threshold)
                {
                    yield return new Command_Action
                    {
                        defaultLabel = "SSF_Menu_ChooseLabel".Translate(),
                        defaultDesc = "SSF_Menu_ChooseDesc".Translate(),
                        action = delegate
                        {
                            var opts = new List<FloatMenuOption>(gizmoFormsCache.Count);
                            for (int i = 0; i < gizmoFormsCache.Count; i++)
                            {
                                var f = gizmoFormsCache[i]; if (f == null) continue;
                                var cap = f; // capture
                                opts.Add(new FloatMenuOption(f.LabelCap, delegate
                                {
                                    List<Thing> sources = FindSourceItemsForForm(cap);
                                    ApplyForm(cap, null, sources);
                                }));
                            }
                            if (opts.Count == 0) opts.Add(new FloatMenuOption("None".Translate(), null));
                            Find.WindowStack.Add(new FloatMenu(opts));
                        },
                        // 메뉴 버튼 자체는 기본 아이콘 사용
                        icon = ShapeshiftTextureUtility.DefaultEnterIcon
                    };
                }
                else
                {
                    for (int i = 0; i < gizmoFormsCache.Count; i++)
                    {
                        var form = gizmoFormsCache[i]; if (form == null) continue;

                        yield return new Command_Action
                        {
                            defaultLabel = "SSF_Command_TransformLabel".Translate(form.LabelCap),
                            defaultDesc = "SSF_Command_TransformDesc".Translate(form.description),
                            action = delegate
                            {
                                List<Thing> sources = FindSourceItemsForForm(form);
                                ApplyForm(form, null, sources);
                            },
                            icon = ShapeshiftTextureUtility.GetEnterIcon(form)
                        };
                    }
                }
            }
            else if (currentForm != null)
            {
                // 해제
                yield return new Command_Action
                {
                    defaultLabel = "SSF_Command_RevertLabel".Translate(),
                    defaultDesc = (currentForm.durationTicks.HasValue && currentForm.durationTicks.Value > 0)
                        ? "SSF_Command_RevertTime".Translate((float)RemainingShapeshiftTicks / 60f)
                        : "SSF_Command_RevertDesc".Translate(),
                    action = delegate { RemoveForm(); },
                    icon = ShapeshiftTextureUtility.GetRevertIcon(currentForm)
                };

                // 전환
                string prev = currentForm.defName;
                if (gizmoFormsCache.Count > threshold)
                {
                    yield return new Command_Action
                    {
                        defaultLabel = "SSF_Menu_SwitchLabel".Translate(),
                        defaultDesc = "SSF_Menu_SwitchDesc".Translate(),
                        action = delegate
                        {
                            var opts = new List<FloatMenuOption>(gizmoFormsCache.Count);
                            for (int i = 0; i < gizmoFormsCache.Count; i++)
                            {
                                var f = gizmoFormsCache[i]; if (f == null) continue;
                                var cap = f;
                                opts.Add(new FloatMenuOption(f.LabelCap, delegate
                                {
                                    List<Thing> sources = FindSourceItemsForForm(cap);
                                    ApplyForm(cap, prev, sources);
                                }));
                            }
                            if (opts.Count == 0) opts.Add(new FloatMenuOption("None".Translate(), null));
                            Find.WindowStack.Add(new FloatMenu(opts));
                        },
                        icon = ShapeshiftTextureUtility.DefaultEnterIcon
                    };
                }
                else
                {
                    for (int i = 0; i < gizmoFormsCache.Count; i++)
                    {
                        var form = gizmoFormsCache[i]; if (form == null) continue;

                        yield return new Command_Action
                        {
                            defaultLabel = "SSF_Command_SwitchLabel".Translate(form.LabelCap),
                            defaultDesc = "SSF_Command_SwitchDesc".Translate(form.description),
                            action = delegate
                            {
                                List<Thing> sources = FindSourceItemsForForm(form);
                                ApplyForm(form, prev, sources);
                            },
                            // [최적화 완료]
                            icon = ShapeshiftTextureUtility.GetEnterIcon(form)
                        };
                    }
                }
            }
            else
            {
                // 방어적 폴백
                yield return new Command_Action
                {
                    defaultLabel = "SSF_Command_RevertLabel".Translate(),
                    defaultDesc = "SSF_Command_RevertDesc".Translate(),
                    action = delegate { RemoveForm(); },
                    icon = ShapeshiftTextureUtility.DefaultRevertIcon
                };
            }

            // (2) 폼 전용 verb 토글/공격 — 바닐라 공격 지즈모 뒤에 배치
            //     (이하 기존 코드와 동일. Verb의 UIIcon은 바닐라에서 이미 최적화되어 있으므로 건드리지 않아도 됩니다.)

            if (!pawn.Drafted) yield break;

            var vt = ShapeshiftVerbTracker;
            if (vt == null) yield break;

            bool canViolent = !pawn.WorkTagIsDisabled(WorkTags.Violent);
            bool showToggle = ShapeshifterFrameworkMod.Settings?.showVerbAutoToggle ?? true;
            var seen = new HashSet<Verb>();

            var verbs = vt.AllVerbs;
            for (int i = 0; i < verbs.Count; i++)
            {
                var v = verbs[i];
                if (v == null || v.verbProps == null) continue;
                if (!v.verbProps.Ranged) continue;

                if (v.caster == null) v.caster = pawn;
                if (!seen.Add(v)) continue;

                int idx = i;

                if (showToggle)
                {
                    var tgl = new Command_Toggle
                    {
                        defaultLabel = GetVerbLabel(idx, v, preferToggleLabel: true),
                        defaultDesc = GetVerbDesc(idx, v, forToggle: true),
                        icon = v.UIIcon, // 바닐라 캐싱 이용
                        isActive = () => IsAutoAttackEnabled(idx, v),
                        toggleAction = () => ToggleAutoAttack(idx, v),
                        groupable = false,
                    };
                    if (!canViolent)
                        tgl.Disable("IsIncapableOfViolence".Translate());
                    yield return tgl;
                }
                else
                {
                    ForceAutoAttackOn(idx, v);
                }

                bool projectileOk = !(v is Verb_LaunchProjectile) || v.verbProps.defaultProjectile != null;

                var cmd = new Command_VerbTarget
                {
                    defaultLabel = GetVerbLabel(idx, v, preferToggleLabel: false),
                    defaultDesc = GetVerbDesc(idx, v, forToggle: false),
                    icon = v.UIIcon, // 바닐라 캐싱 이용
                    verb = v,
                    groupable = false,
                };
                if (!projectileOk)
                    cmd.Disable("SSF_Message_NoProjectile".Translate());
                if (!canViolent)
                    cmd.Disable("IsIncapableOfViolence".Translate());
                else if (!v.Available())
                    cmd.Disable("CommandCannotFire".Translate());

                yield return cmd;
            }
        }
        #endregion
    }
}