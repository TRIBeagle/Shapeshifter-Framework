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
using UnityEngine;
using Verse;
using Verse.AI;

namespace ShapeshifterFramework.Comps
{
    /// <summary>Pawn 변신 라이프사이클 관리 컴포넌트.</summary>
    public class CompShapeshifter : ThingComp
    {
        #region 상태 필드/캐시

        public ShapeshiftFormDef currentForm = null;
        public bool isTransformed { get { return currentForm != null; } }

        /// <summary>폼 소환 전용 무기 여부 확인.</summary>
        public bool IsGeneratedWeapon(ThingWithComps eq)
        {
            return generatedWeapons != null && generatedWeapons.Contains(eq);
        }

        private int transformTimer = 0;

        // 체형/머리형 백업
        private BodyTypeDef originalBodyType;
        private HeadTypeDef originalHeadType;

        // 기본 컬러 백업
        private Color? originalHairColor;
        private Color? originalSkinColor;
        private bool hasSavedColors;

        // 임시 부여 요소 추적
        private readonly List<AbilityDef> tempAddedAbilities = new List<AbilityDef>();
        private readonly List<Hediff> tempAddedHediffs = new List<Hediff>();
        private readonly List<HediffDef> tempAddedHediffsDefCache = new List<HediffDef>();

        // 변신 전 장비 스냅샷
        private readonly List<Apparel> prevApparels = new List<Apparel>();
        private readonly List<ThingWithComps> prevWeapons = new List<ThingWithComps>();

        // 변신을 유발한 원본 아이템 (예: 변신 반지) - 드랍 보호용
        public List<Thing> sourceItems = new List<Thing>();

        // 변신 시 소환된 폼 전용 장비 추적 (해제 시 삭제 및 복사 방지용)
        private List<Apparel> generatedApparel = new List<Apparel>();
        private List<ThingWithComps> generatedWeapons = new List<ThingWithComps>();

        // 폼 전용 VerbTracker (폼 verbs/tools용)
        private VerbTracker shapeshiftVerbTracker;

        // Gizmo 캐시 (변신 해제 시 갱신)
        private List<ShapeshiftFormDef> gizmoFormsCache = new List<ShapeshiftFormDef>();

        // 틱(Tick) 에러 스팸 방지용 플래그
        private bool verbTickErrorLogged = false;

        // 기즈모 verb 중복 방지용 재사용 HashSet (GC 할당 방지)
        private readonly HashSet<Verb> _tmpSeenVerbs = new HashSet<Verb>();

        // verb 자동공격 토글 상태 (키: formDefName#index)
        private readonly Dictionary<string, bool> verbAutoToggle = new Dictionary<string, bool>();

        public bool suppressEquipLock = false;

        // PostLoadInit에서 Reference 연결 완료 후 AddRange하기 위한 임시 보관 필드
        private List<Hediff> __tmpHediffsLoad = null;
        private HashSet<string> __tmpPrevApIds = null;
        private HashSet<string> __tmpPrevWpIds = null;
        private bool needsGearResolve = false;

        // 추가한 헤디프(인스턴스) 추적은 기존 tempAddedHediffs 사용
        private readonly List<ShapeshifterFramework.Utilities.ShapeshiftPartRestoreRecord> tempPartRestoreRecords
            = new List<ShapeshifterFramework.Utilities.ShapeshiftPartRestoreRecord>(8);

        // 앰비언트 VFX 런타임 상태 (저장 불필요 — 로드 후 CompTick에서 자동 재생성)
        private Effecter ambientEffecterInstance;
        private int ambientFleckNextTick;

        #endregion

        #region IVerbOwner 구현 (폼 verbs/tools → 전용 VerbTracker)

        /// <summary>현재 폼 verbs/tools IVerbOwner 구현.</summary>
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

            // 폼 VerbProperties 목록
            public List<VerbProperties> VerbProperties
            {
                get
                {
                    var f = comp.currentForm;
                    return (f != null && f.verbs != null) ? f.verbs : EmptyVerbProperties;
                }
            }

            // 폼 Tool 목록
            public List<Tool> Tools
            {
                get
                {
                    var f = comp.currentForm;
                    return (f != null && f.tools != null) ? f.tools : EmptyTools;
                }
            }
        }

        /// <summary>현재 폼 전용 VerbTracker. 없으면 null.</summary>
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

        /// <summary>verb에 대응하는 VerbGizmoOption 검색. verbLabel 매칭 우선, 미지정 시 인덱스 폴백.</summary>
        private VerbGizmoOption FindGizmoOption(int index, Verb v)
        {
            var opt = currentForm?.verbGizmoOptions;
            if (opt == null || opt.Count == 0) return null;

            // 1) verbLabel 매칭: verb의 label과 일치하는 옵션 검색
            string vLabel = v?.verbProps?.label;
            if (!string.IsNullOrEmpty(vLabel))
            {
                for (int i = 0; i < opt.Count; i++)
                {
                    var o = opt[i];
                    if (o != null && string.Equals(o.verbLabel, vLabel, System.StringComparison.OrdinalIgnoreCase))
                        return o;
                }
            }

            // 2) 인덱스 폴백: verbLabel 미지정 옵션 중 인덱스 순서 매칭 (하위호환)
            if (index >= 0 && index < opt.Count)
            {
                var o = opt[index];
                if (o != null && string.IsNullOrEmpty(o.verbLabel))
                    return o;
            }

            return null;
        }

        // 자동공격 토글 키(formDefName#index#verbName)
        string AutoKey(Verb v)
        {
            var f = currentForm?.defName ?? "None";
            string vName = v?.verbProps?.label ?? v?.GetType().Name ?? "UnknownVerb";
            // verb index를 포함하여 같은 label의 verb 간 충돌 방지
            int idx = 0;
            var vt = shapeshiftVerbTracker;
            if (vt != null)
            {
                var verbs = vt.AllVerbs;
                for (int i = 0; i < verbs.Count; i++)
                {
                    if (verbs[i] == v) { idx = i; break; }
                }
            }
            return f + "#" + idx + "#" + vName;
        }

        // 기본 자동공격 상태(없으면 true)
        bool DefaultAutoOn(int index, Verb v)
        {
            var o = FindGizmoOption(index, v);
            if (o != null && o.autoAttackDefault.HasValue) return o.autoAttackDefault.Value;
            return true; // 기본 On
        }

        /// <summary>verb 자동공격 활성 여부.</summary>
        public bool IsAutoAttackEnabled(int index, Verb v)
        {
            if (v == null) return true;
            bool val;
            if (verbAutoToggle.TryGetValue(AutoKey(v), out val)) return val;
            return DefaultAutoOn(index, v);
        }

        /// <summary>자동공격 토글 전환 (배타적: ON 시 다른 ranged verb 전부 OFF).</summary>
        public void ToggleAutoAttack(int index, Verb v)
        {
            bool now = IsAutoAttackEnabled(index, v);
            if (now)
            {
                verbAutoToggle[AutoKey(v)] = false;
            }
            else
            {
                // 1) 모든 ranged verb OFF
                var vt = ShapeshiftVerbTracker;
                if (vt != null)
                {
                    var verbs = vt.AllVerbs;
                    for (int i = 0; i < verbs.Count; i++)
                    {
                        var other = verbs[i];
                        if (other == null || other.verbProps == null) continue;
                        if (!other.verbProps.Ranged) continue;
                        verbAutoToggle[AutoKey(other)] = false;
                    }
                }
                // 2) 선택한 verb만 ON
                verbAutoToggle[AutoKey(v)] = true;
            }
        }

        /// <summary>자동공격 강제 활성.</summary>
        public void ForceAutoAttackOn(int index, Verb v)
        {
            verbAutoToggle[AutoKey(v)] = true;
        }

        /// <summary>폼 적용 시 배타적 토글 초기화: 첫 번째 ranged verb만 ON.</summary>
        private void InitAutoToggleForForm()
        {
            var vt = ShapeshiftVerbTracker;
            if (vt == null) return;

            // verbGizmoOptions에 autoAttackDefault=true인 verb를 우선 탐색
            int defaultOnIndex = -1;
            var verbs = vt.AllVerbs;

            for (int i = 0; i < verbs.Count; i++)
            {
                var v = verbs[i];
                if (v == null || v.verbProps == null || !v.verbProps.Ranged) continue;
                var o = FindGizmoOption(i, v);
                if (o != null && o.autoAttackDefault == true)
                {
                    defaultOnIndex = i;
                    break;
                }
            }

            bool firstSet = false;
            for (int i = 0; i < verbs.Count; i++)
            {
                var v = verbs[i];
                if (v == null || v.verbProps == null) continue;
                if (!v.verbProps.Ranged) continue;

                bool on;
                if (defaultOnIndex >= 0)
                    on = (i == defaultOnIndex);
                else
                    on = !firstSet; // 명시 없으면 첫 번째만 ON

                verbAutoToggle[AutoKey(v)] = on;
                if (on) firstSet = true;
            }
        }

        /// <summary>verb 명령 라벨 반환.</summary>
        public string GetVerbLabel(int index, Verb v, bool preferToggleLabel)
        {
            var vp = v?.verbProps;
            var o = FindGizmoOption(index, v);
            if (o != null)
            {
                // toggleLabel → label 순 fallback
                string s = preferToggleLabel ? (o.toggleLabel ?? o.label) : o.label;
                if (!string.IsNullOrEmpty(s)) return s.Translate().CapitalizeFirst();
            }

            string __label = string.IsNullOrEmpty(vp?.label) ? "SSF_Verb_Attack".Translate() : vp.label.Translate();
            return __label.CapitalizeFirst();
        }

        /// <summary>verb 명령/토글 설명 반환.</summary>
        public string GetVerbDesc(int index, Verb v, bool forToggle)
        {
            var o = FindGizmoOption(index, v);
            if (o != null)
            {
                // toggleDesc → desc 순 fallback
                string s = forToggle ? (o.toggleDesc ?? o.desc) : o.desc;
                if (!string.IsNullOrEmpty(s)) return s.Translate();
            }

            if (forToggle) return "SSF_Verb_ToggleDesc".Translate();
            return "SSF_Verb_OrderDesc".Translate();
        }

        /// <summary>verbGizmoOptions의 iconPath에서 아이콘 로드. 없으면 null.</summary>
        private Texture2D GetVerbIcon(int index, Verb v)
        {
            var o = FindGizmoOption(index, v);
            if (o != null)
            {
                string path = o.iconPath;
                if (!string.IsNullOrEmpty(path))
                    return ContentFinder<Texture2D>.Get(path, reportFailure: false);
            }
            return null;
        }

        #endregion

        #region Ticking/Inspect

        /// <summary>매 틱: 타이머/사망/linkedHediff 모니터링/sustain 검사/VerbTracker 처리.</summary>
        public override void CompTick()
        {
            base.CompTick();
            Pawn pawn = parent as Pawn;
            if (pawn == null) return;

            // 로드 후 장비 참조 복원
            if (needsGearResolve)
            {
                needsGearResolve = false;
                if (pawn != null && (__tmpPrevApIds != null || __tmpPrevWpIds != null))
                {
                    // 인벤토리 검색
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

                    // 맵 바닥 검색
                    if (pawn.Map != null)
                    {
                        var allThings = pawn.Map.listerThings.AllThings;
                        for (int i = 0; i < allThings.Count; i++)
                        {
                            var t = allThings[i];
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

                    __tmpPrevApIds = null;
                    __tmpPrevWpIds = null;
                }
            }

            if (isTransformed && currentForm != null)
            {
                // linkedHediff 외부 삭제 시 변신 해제
                if (currentForm.linkedHediff != null
                    && pawn.health?.hediffSet?.GetFirstHediffOfDef(currentForm.linkedHediff) == null)
                {
                    RemoveForm();
                    return;
                }
                if (pawn.Dead)
                {
                    RemoveForm();
                    return;
                }
                if (currentForm.revertOnDowned && pawn.Downed)
                {
                    RemoveForm();
                    return;
                }
                if (currentForm.durationTicks.HasValue && currentForm.durationTicks.Value > 0)
                {
                    if (transformTimer <= 0) { RemoveForm(); return; }
                    transformTimer--;
                }
                // 60틱마다 유지 요건(sustain) 검사
                if (pawn.IsHashIntervalTick(60))
                {
                    // 코어 아이템 유실 검사
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
                                Messages.Message("SSF_Message_RevertDueToItemLost".Translate(pawn.LabelShortCap, item.Label), pawn, MessageTypeDefOf.NegativeEvent, false);
                                RemoveForm();
                                return;
                            }
                        }
                    }

                    // sustain 조건 검사: sustainApparels/sustainWeapons/sustainHediffs
                    // 어빌리티 시전 중(warmup)에는 sustain 해제를 유예하여 중단 방지
                    if (!CheckSustainConditions(pawn, currentForm)
                        && !(pawn.stances?.curStance is Stance_Warmup))
                    {
                        Messages.Message("SSF_Message_RevertDueToConditionLost".Translate(pawn.LabelShortCap), pawn, MessageTypeDefOf.NegativeEvent, false);
                        RemoveForm();
                        return;
                    }
                }
                // ── 앰비언트 VFX (지속형 이펙트) ──
                // 앰비언트 필드가 없는 폼(대다수)에서는 Spawned 체크 자체를 건너뛰어 틱 부하 최소화
                if ((currentForm.ambientEffecter != null || currentForm.ambientFleck != null)
                    && pawn.Spawned)
                {
                    if (currentForm.ambientEffecter != null)
                    {
                        if (ambientEffecterInstance == null)
                            ambientEffecterInstance = currentForm.ambientEffecter.Spawn();
                        ambientEffecterInstance.EffectTick(pawn, pawn);
                    }
                    if (currentForm.ambientFleck != null
                        && Find.TickManager.TicksGame >= ambientFleckNextTick)
                    {
                        FleckMaker.Static(pawn.DrawPos, pawn.Map, currentForm.ambientFleck,
                            Mathf.Max(0.01f, currentForm.ambientFleckScale));
                        ambientFleckNextTick = Find.TickManager.TicksGame
                            + Mathf.Max(1, currentForm.ambientFleckIntervalTicks);
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

        /// <summary>sustain 조건 충족 여부 검사. 조건이 없으면 true.</summary>
        private static bool CheckSustainConditions(Pawn pawn, ShapeshiftFormDef form)
        {
            bool hasApparels = form.sustainApparels != null && form.sustainApparels.Count > 0;
            bool hasWeapons = form.sustainWeapons != null && form.sustainWeapons.Count > 0;
            bool hasHediffs = form.sustainHediffs != null && form.sustainHediffs.Count > 0;
            bool hasGenes = ModsConfig.BiotechActive && form.sustainGenes != null && form.sustainGenes.Count > 0;

            if (!hasApparels && !hasWeapons && !hasHediffs && !hasGenes) return true;

            var mode = form.sustainMode ?? SustainMode.All;

            bool apparelMet = !hasApparels || CheckSustainApparels(pawn, form.sustainApparels);
            bool weaponMet = !hasWeapons || CheckSustainWeapons(pawn, form.sustainWeapons);
            bool hediffMet = !hasHediffs || CheckSustainHediffs(pawn, form.sustainHediffs);
            bool geneMet = !hasGenes || CheckSustainGenes(pawn, form.sustainGenes);

            if (mode == SustainMode.All)
                return apparelMet && weaponMet && hediffMet && geneMet;
            else // Any
                return apparelMet || weaponMet || hediffMet || geneMet;
        }

        private static bool CheckSustainApparels(Pawn pawn, List<ThingDef> required)
        {
            if (pawn.apparel == null) return false;
            var worn = pawn.apparel.WornApparel;
            for (int i = 0; i < required.Count; i++)
            {
                bool found = false;
                for (int j = 0; j < worn.Count; j++)
                {
                    if (worn[j].def == required[i]) { found = true; break; }
                }
                if (!found) return false;
            }
            return true;
        }

        private static bool CheckSustainWeapons(Pawn pawn, List<ThingDef> required)
        {
            if (pawn.equipment == null) return false;
            var eqs = pawn.equipment.AllEquipmentListForReading;
            for (int i = 0; i < required.Count; i++)
            {
                bool found = false;
                for (int j = 0; j < eqs.Count; j++)
                {
                    if (eqs[j].def == required[i]) { found = true; break; }
                }
                if (!found) return false;
            }
            return true;
        }

        private static bool CheckSustainHediffs(Pawn pawn, List<HediffDef> required)
        {
            if (pawn.health == null) return false;
            for (int i = 0; i < required.Count; i++)
            {
                if (pawn.health.hediffSet.GetFirstHediffOfDef(required[i]) == null)
                    return false;
            }
            return true;
        }

        private static bool CheckSustainGenes(Pawn pawn, List<GeneDef> required)
        {
            if (pawn.genes == null) return false;
            for (int i = 0; i < required.Count; i++)
            {
                if (pawn.genes.GetGene(required[i]) == null)
                    return false;
            }
            return true;
        }

        // 남은 변신 틱
        public int RemainingShapeshiftTicks
        {
            get
            {
                int t = transformTimer;
                return t > 0 ? t : 0;
            }
        }

        /// <summary>인스펙트 추가 문자열.</summary>
        public override string CompInspectStringExtra()
        {
            if (!isTransformed || currentForm == null)
                return null;

            // 영구 변신
            if (!currentForm.durationTicks.HasValue || currentForm.durationTicks.Value <= 0)
                return "SSF_Inspect_Permanent".Translate();

            int remain = transformTimer;
            if (remain <= 0) return null;

            // 바닐라 시간 포맷
            string timeStr = GenDate.ToStringTicksToPeriod(remain, allowSeconds: false, shortForm: false);

            return "SSF_Inspect_Remaining".Translate(timeStr);
        }

        #endregion

        #region 변신 가능 판정

        /// <summary>기본 변신 가능 여부 판정 (같은 폼 재변신·사망 차단).</summary>
        public bool CanTransform(Pawn pawn, ShapeshiftFormDef form)
        {
            if (pawn == null || form == null) return false;
            string prev = (isTransformed && currentForm != null) ? currentForm.defName : null;
            return ShapeshiftEligibility.CanTransformBasic(pawn, form, prev);
        }

        // 지즈모 캐시 무효화
        private void InvalidateGizmoCache()
        {
            gizmoFormsCache = null;
        }

        #endregion

        #region 변신 적용/해제

        /// <summary>폼 적용.</summary>
        public void ApplyForm(ShapeshiftFormDef form) { ApplyForm(form, null, null); }

        /// <summary>폼 적용. prevOverride 지정 시 해제 후 전환.</summary>
        public void ApplyForm(ShapeshiftFormDef form, string prevOverride, List<Thing> sources = null)
        {
            var pawn = parent as Pawn;
            if (pawn == null || form == null) return;

            string prev = prevOverride ?? ((isTransformed && currentForm != null) ? currentForm.defName : null);

            // 실시간 재검증 (기본: 같은 폼 재변신·사망 차단)
            if (!ShapeshiftEligibility.CanTransformBasic(pawn, form, prev))
            {
                try { Messages.Message("SSF_Message_CannotTransform".Translate(form.LabelCap), MessageTypeDefOf.RejectInput, false); } catch { }
                return;
            }

            // 전환 시 기존 폼 먼저 해제 (sourceItems 덮어쓰기 전에 수행)
            if (isTransformed)
                RemoveForm();

            this.sourceItems = sources ?? new List<Thing>();

            // 장비 스냅샷 캡처
            prevApparels.Clear();
            prevWeapons.Clear();
            CaptureCurrentGear(pawn);

            // 장비 처리
            try
            {
                HandleGearOnTransform(pawn, form);
                SpawnAndEquipFormGear(pawn, form);
            }
            catch (Exception ex)
            {
                // 장비 처리 실패해도 변신 진행
                Log.Error($"[SSF] Error handling gear during transform for {pawn.Name}: {ex}");
            }

            // 체형/컬러 백업
            if (!isTransformed && pawn.story != null)
            {
                originalBodyType = pawn.story.bodyType;
                originalHeadType = pawn.story.headType;
                originalHairColor = pawn.story.HairColor;
                originalSkinColor = pawn.story.skinColorOverride;
                hasSavedColors = true;
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

            // 메인 헤디프 부여 (변신 상태 마커 + 스탯/능력치)
            if (form.linkedHediff != null && pawn.health != null)
            {
                if (pawn.health.hediffSet.GetFirstHediffOfDef(form.linkedHediff) == null)
                {
                    Hediff mainH = pawn.health.AddHediff(form.linkedHediff);
                    if (mainH != null)
                    {
                        tempAddedHediffs.Add(mainH);
                        tempAddedHediffsDefCache.Add(form.linkedHediff);
                    }
                }
            }

            ShapeshiftTransformFxUtility.PlayEnterFx(pawn, form);
            if (form.durationTicks.HasValue && form.durationTicks.Value > 0)
                transformTimer = form.durationTicks.Value;

            // 앰비언트 VFX 초기화 (이전 폼 잔존 인스턴스 방지)
            ambientEffecterInstance = null;
            ambientFleckNextTick = 0;

            // 체형/머리형/컬러 적용
            if (pawn.story != null)
            {
                if (form.bodyType != null) pawn.story.bodyType = form.bodyType;
                if (form.headType != null) pawn.story.headType = form.headType;
                if (form.hairColor.HasValue) pawn.story.HairColor = form.hairColor.Value;
                if (form.skinColor.HasValue) pawn.story.skinColorOverride = form.skinColor.Value;
            }

            // 런타임 캐시 등록
            ApplyRuntimeCaches(pawn, form);

            // 레지스트리 등록 — O(1) 조회 활성화
            ShapeshiftRegistry.Register(pawn, this);

            // VerbTracker 리셋
            shapeshiftVerbTracker = null;

            // 배타적 토글 초기화: 첫 번째 ranged verb만 ON
            InitAutoToggleForForm();

            RefreshPawn(pawn, this);
            InvalidateGizmoCache();
        }

        /// <summary>현재 폼 해제.</summary>
        public void RemoveForm()
        {
            var pawn = parent as Pawn;
            if (pawn == null) return;
            var __oldForm = currentForm;

            // 전용 장비 파괴
            using (new ShapeshiftEquipLockScope(this))
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
            if (this.sourceItems != null) this.sourceItems.Clear();

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
                // 인스턴스 기반 헤디프 제거
                for (int i = 0; i < tempAddedHediffs.Count; i++)
                {
                    Hediff h = tempAddedHediffs[i];
                    if (h != null && pawn.health.hediffSet.hediffs.Contains(h))
                    {
                        pawn.health.RemoveHediff(h);
                        // DefCache에서도 제외
                        if (h.def != null) tempAddedHediffsDefCache.Remove(h.def);
                    }
                }

                // Def 기반 방어적 정리
                if (tempAddedHediffsDefCache != null && tempAddedHediffsDefCache.Count > 0)
                {
                    List<Hediff> list = pawn.health.hediffSet.hediffs;
                    for (int i = 0; i < tempAddedHediffsDefCache.Count; i++)
                    {
                        HediffDef def = tempAddedHediffsDefCache[i];
                        if (def == null) continue;

                        // 1개만 제거 후 중단
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

                // 파츠 원상 복원
                for (int i = 0; i < tempPartRestoreRecords.Count; i++)
                {
                    var rec = tempPartRestoreRecords[i];
                    if (rec == null || rec.Part == null) continue;

                    // 자연 파츠 복원
                    if (!rec.WasMissingBefore)
                    {
                        try { pawn.health.RestorePart(rec.Part); }
                        catch (System.Exception ex) { Log.Warning($"[SSF] RestorePart failed for '{rec.Part.Label}': {ex}"); }
                    }

                    // AddedPart 복구
                    if (rec.PreExistingAdded != null && rec.PreExistingAdded.Count > 0)
                    {
                        for (int k = 0; k < rec.PreExistingAdded.Count; k++)
                        {
                            var prev = rec.PreExistingAdded[k];
                            if (prev?.Def == null) continue;

                            BodyPartRecord targetPart = null;
                            // 본체 부착
                            if (prev.PartDef == null || prev.PartDef == rec.Part.def)
                            {
                                targetPart = rec.Part;
                            }
                            else
                            {
                                // 하위 파츠 중 일치 부위 탐색
                                var allParts = pawn.RaceProps.body.AllParts;
                                for (int pIdx = 0; pIdx < allParts.Count; pIdx++)
                                {
                                    var x = allParts[pIdx];
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
                        // WasMissing 유지
                    }
                }

                ShapeshiftDiagnostics.Info($"Revert: restored {tempPartRestoreRecords.Count} part(s)");

                tempAddedHediffs.Clear();
                tempAddedHediffsDefCache.Clear();
                tempPartRestoreRecords.Clear();
            }

            transformTimer = 0;

            // 체형/머리형/컬러 원복
            if (pawn.story != null)
            {
                if (originalBodyType != null) pawn.story.bodyType = originalBodyType;
                if (originalHeadType != null) pawn.story.headType = originalHeadType;
                if (hasSavedColors)
                {
                    if (originalHairColor.HasValue) pawn.story.HairColor = originalHairColor.Value;
                    pawn.story.skinColorOverride = originalSkinColor; // null 복원 포함
                    hasSavedColors = false;
                }
            }

            // 자동 재착용
            ShapeshifterFrameworkSettings st = ShapeshifterFrameworkMod.Settings;
            if (st == null || st.autoReequipFromInventory || st.autoReequipFromGround)
                TryReequipPreviousGear(pawn);

            // VerbTracker 해제
            shapeshiftVerbTracker = null;

            ShapeshiftTransformFxUtility.PlayExitFx(pawn, __oldForm);

            // 앰비언트 VFX 정리
            if (ambientEffecterInstance != null)
            {
                ambientEffecterInstance.Cleanup();
                ambientEffecterInstance = null;
            }

            // 해제 시 잔해 드랍
            if (__oldForm.revertDrops != null && __oldForm.revertDrops.Count > 0
                && pawn.Spawned && pawn.MapHeld != null)
            {
                for (int i = 0; i < __oldForm.revertDrops.Count; i++)
                {
                    var entry = __oldForm.revertDrops[i];
                    if (entry?.thingDef == null || entry.count <= 0) continue;
                    Thing thing = ThingMaker.MakeThing(entry.thingDef);
                    thing.stackCount = entry.count;
                    GenPlace.TryPlaceThing(thing, pawn.PositionHeld, pawn.MapHeld, ThingPlaceMode.Near);
                }
            }

            // 해제 시 hediff 부여 (비추적 — 바닐라 라이프사이클)
            if (__oldForm.revertAddHediffs != null && __oldForm.revertAddHediffs.Count > 0
                && pawn.health != null && !pawn.Dead)
            {
                for (int i = 0; i < __oldForm.revertAddHediffs.Count; i++)
                {
                    HediffDef hd = __oldForm.revertAddHediffs[i];
                    if (hd != null)
                        pawn.health.AddHediff(hd);
                }
            }

            currentForm = null;

            // 레지스트리 해제
            ShapeshiftRegistry.Unregister(pawn);

            // 캐시 정리
            ShapeshiftRuntimeCaches.ClearFor(pawn);

            RefreshPawn(pawn, this);
            InvalidateGizmoCache();
        }

        /// <summary>하위 부위 여부 확인.</summary>
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

        /// <summary>사망 시 변신 해제 및 캐시 정리.</summary>
        public void Notify_Killed(DamageInfo? dinfo, Hediff exactCulprit)
        {
            var pawn = parent as Pawn;
            if (pawn == null) return;

            bool wasTransformed = isTransformed;

            if (isTransformed)
            {
                RemoveForm();
            }

            // 잔여 캐시 방어적 정리
            ShapeshiftRuntimeCaches.ClearFor(pawn);

            if (wasTransformed)
            {
                ShapeshiftDiagnostics.Info($"{pawn.LabelShort} killed, shapeshift forcibly deactivated.");
            }
        }

        #endregion

        #region 생명주기 오버라이드

        /// <summary>맵 스폰(상단 복귀, 포드 하차, 동면관 해제 등) 시 변신 중이면 레지스트리 재등록.</summary>
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            var pawn = parent as Pawn;
            if (pawn != null && isTransformed && currentForm != null)
                ShapeshiftRegistry.Register(pawn, this);
        }

        /// <summary>폰 완전 파괴 시 레지스트리 방어적 해제. PostDeSpawn은 사용하지 않음 (상단/동면관/포드 진입 시 레지스트리 누락 방지).</summary>
        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            var pawn = parent as Pawn;
            ShapeshiftRegistry.Unregister(pawn);
            base.PostDestroy(mode, previousMap);
        }

        #endregion

        #region 내부: 장비 스냅샷/처리/재착용/드랍 유틸

        /// <summary>현재 장비 스냅샷 저장.</summary>
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

        /// <summary>변신 시 장비 이동/드랍 처리.</summary>
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

                for (int i = worn.Count - 1; i >= 0; i--)
                {
                    Apparel ap = worn[i];
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
                            dropped.SetForbidden(true);
                        }
                    }
                }
            }

            // 무기
            if (form.weaponsOnTransform != GearHandling.Keep && pawn.equipment != null)
            {
                List<ThingWithComps> list = pawn.equipment.AllEquipmentListForReading;

                for (int i = list.Count - 1; i >= 0; i--)
                {
                    ThingWithComps eq = list[i];
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

        /// <summary>폼 전용 장비 소환 및 장착.</summary>
        void SpawnAndEquipFormGear(Pawn pawn, ShapeshiftFormDef form)
        {
            if (pawn == null || form == null) return;

            using (new ShapeshiftEquipLockScope(this))
            {
                // 1. 전용 의류 소환 (pawn.apparel null이면 건너뜀 — 동물/메카노이드 방어)
                if (pawn.apparel != null && form.spawnApparelOnTransform != null && form.spawnApparelOnTransform.Count > 0)
                {
                    for (int i = 0; i < form.spawnApparelOnTransform.Count; i++)
                    {
                        ThingDef apparelDef = form.spawnApparelOnTransform[i];
                        if (apparelDef == null || !apparelDef.IsApparel) continue;

                        // 부위 겹치는 기존 장비 처리
                        if (pawn.apparel != null)
                        {
                            List<Apparel> worn = pawn.apparel.WornApparel;
                            for (int j = worn.Count - 1; j >= 0; j--)
                            {
                                Apparel existingAp = worn[j];
                                if ((sourceItems != null && sourceItems.Contains(existingAp)) || generatedApparel.Contains(existingAp)) continue;

                                if (!ApparelUtility.CanWearTogether(apparelDef, existingAp.def, pawn.RaceProps.body))
                                {
                                    pawn.apparel.Remove(existingAp);
                                    if (form.conflictingGearHandling == GearHandling.Drop)
                                    {
                                        TryDropThing(existingAp, pawn.PositionHeld, pawn.MapHeld);
                                    }
                                    else // Inventory (또는 Keep)
                                    {
                                        if (pawn.inventory?.innerContainer != null && pawn.inventory.innerContainer.TryAdd(existingAp, false)) { }
                                        else TryDropThing(existingAp, pawn.PositionHeld, pawn.MapHeld);
                                    }

                                    if (existingAp.Spawned && ShapeshifterFrameworkMod.Settings != null && ShapeshifterFrameworkMod.Settings.forbidDroppedItemsOnTransform)
                                    {
                                        existingAp.SetForbidden(true);
                                    }

                                    if (!prevApparels.Contains(existingAp)) prevApparels.Add(existingAp);
                                }
                            }
                        }

                        // 재료 안전망
                        ThingDef stuff = null;
                        if (apparelDef.MadeFromStuff)
                        {
                            stuff = form.spawnApparelStuff;
                            if (stuff == null || stuff.stuffProps == null || !stuff.stuffProps.CanMake(apparelDef))
                            {
                                stuff = GenStuff.DefaultStuffFor(apparelDef);
                            }
                        }

                        // 생성 및 착용
                        Apparel newApparel = (Apparel)ThingMaker.MakeThing(apparelDef, stuff);

                        if (pawn.apparel != null)
                        {
                            pawn.apparel.Wear(newApparel, dropReplacedApparel: false);
                            pawn.apparel.Lock(newApparel);
                            generatedApparel.Add(newApparel);
                        }
                    }
                }

                // 2. 전용 무기 소환 (pawn.equipment null이면 건너뜀 — 동물/메카노이드 방어)
                if (pawn.equipment != null && form.spawnWeaponOnTransform != null && form.spawnWeaponOnTransform.Count > 0)
                {
                    // 기존 무기 처리
                    if (pawn.equipment != null && pawn.equipment.Primary != null)
                    {
                        ThingWithComps existingWep = pawn.equipment.Primary;
                        if ((sourceItems == null || !sourceItems.Contains(existingWep)) && !generatedWeapons.Contains(existingWep))
                        {
                            pawn.equipment.Remove(existingWep);

                            if (form.conflictingGearHandling == GearHandling.Drop)
                            {
                                TryDropThing(existingWep, pawn.PositionHeld, pawn.MapHeld);
                            }
                            else // Inventory (또는 Keep)
                            {
                                if (pawn.inventory?.innerContainer != null && pawn.inventory.innerContainer.TryAdd(existingWep, false)) { }
                                else TryDropThing(existingWep, pawn.PositionHeld, pawn.MapHeld);
                            }

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

                        // 재료 안전망
                        ThingDef stuff = null;
                        if (weaponDef.MadeFromStuff)
                        {
                            stuff = form.spawnWeaponStuff;
                            if (stuff == null || stuff.stuffProps == null || !stuff.stuffProps.CanMake(weaponDef))
                            {
                                stuff = GenStuff.DefaultStuffFor(weaponDef);
                            }
                        }

                        // 생성 및 장착
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

        /// <summary>안전 드랍 유틸.</summary>
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

        /// <summary>해제 후 이전 장비 재착용.</summary>
        void TryReequipPreviousGear(Pawn pawn)
        {
            ShapeshiftDiagnostics.Info($"TryReequip: weapons={prevWeapons.Count}, apparels={prevApparels.Count}");
            if (pawn == null || pawn.Dead) return;

            ShapeshifterFrameworkSettings st = ShapeshifterFrameworkMod.Settings;
            bool allowInv = (st == null) ? true : st.autoReequipFromInventory;
            bool allowGround = (st == null) ? true : st.autoReequipFromGround;

            var toQueue = new List<Job>(prevWeapons.Count + prevApparels.Count);

            // 착용락 임시 해제
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
                            if (!allowGround) continue;

                            if (w.Map == pawn.MapHeld && pawn.CanReach(w, PathEndMode.ClosestTouch, Danger.Deadly))
                            {
                                if (w.IsForbidden(pawn)) w.SetForbidden(false);
                                Job job = JobMaker.MakeJob(JobDefOf.Equip, w);
                                job.playerForced = true;
                                toQueue.Add(job);
                            }
                            continue;
                        }

                        // 인벤 즉시 장착
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
            }

            // 잡 큐 실행
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

        /// <summary>런타임 캐시 등록(사운드/혈흔/FleshType).</summary>
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

        /// <summary>캐시/그래픽/Verb 재초기화. compHint를 전달하면 TryGetComp 호출을 생략.</summary>
        public static void RefreshPawn(Pawn pawn, CompShapeshifter compHint = null, bool forceReinitPawnVerbs = true, bool resetShapeshiftVerbs = true, bool refreshSelection = true)
        {
            if (pawn == null) return;

            // comp 한 번만 조회 (호출자가 전달하면 재사용, 아니면 바닐라 TryGetComp 폴백)
            var comp = compHint ?? pawn.TryGetComp<CompShapeshifter>();

            // 캐시 더럽히기
            try { pawn.health?.capacities?.Notify_CapacityLevelsDirty(); } catch (System.Exception ex) { Log.Warning($"[SSF] RefreshPawn (Capacity) error: {ex}"); }
            try { pawn.health?.hediffSet?.DirtyCache(); } catch (System.Exception ex) { Log.Warning($"[SSF] RefreshPawn (Hediff) error: {ex}"); }
            try { pawn.Drawer?.renderer?.SetAllGraphicsDirty(); } catch (System.Exception ex) { Log.Warning($"[SSF] RefreshPawn (Graphics) error: {ex}"); }
            try { PortraitsCache.SetDirty(pawn); } catch (System.Exception ex) { Log.Warning($"[SSF] RefreshPawn (Portrait) error: {ex}"); }
            try { GlobalTextureAtlasManager.TryMarkPawnFrameSetDirty(pawn); } catch (System.Exception ex) { Log.Warning($"[SSF] RefreshPawn (Atlas) error: {ex}"); }
            try { pawn.Notify_DisabledWorkTypesChanged(); } catch (System.Exception ex) { Log.Warning($"[SSF] RefreshPawn (WorkTypes) error: {ex}"); }

            // 바닐라 VerbTracker 재초기화
            try
            {
                if (forceReinitPawnVerbs)
                {
                    pawn.verbTracker?.VerbsNeedReinitOnLoad();
                    // 즉시 재빌드 유도
                    var _ = pawn.verbTracker?.AllVerbs;
                }
            }
            catch (System.Exception ex) { Log.Warning($"[SSF] RefreshPawn (Vanilla Verbs) error: {ex}"); }

            // 폼 전용 VerbTracker 재초기화
            try
            {
                if (comp != null && resetShapeshiftVerbs)
                {
                    comp.shapeshiftVerbTracker = null;

                    var vt = comp.ShapeshiftVerbTracker;
                    if (vt != null)
                    {
                        var __ = vt.AllVerbs;
                    }
                }
            }
            catch (System.Exception ex) { Log.Warning($"[SSF] RefreshPawn (Shapeshift Verbs) error: {ex}"); }

            // replaceNativeTools=true → pawn.verbTracker에서 네이티브 근접 verb만 제거
            // 폼 도구는 shapeshiftVerbTracker에만 두고, TryGetMeleeVerb Postfix에서 공급
            // (pawn.verbTracker에 직접 주입하면 MVCF 등 타 모드와 충돌)
            try
            {
                if (comp != null && comp.isTransformed && comp.currentForm != null && forceReinitPawnVerbs)
                {
                    var form = comp.currentForm;
                    bool replaceNative = form.replaceNativeTools.HasValue && form.replaceNativeTools.Value;

                    if (replaceNative && form.tools != null && form.tools.Count > 0 && pawn.verbTracker != null)
                    {
                        var verbList = pawn.verbTracker.AllVerbs;
                        for (int i = verbList.Count - 1; i >= 0; i--)
                        {
                            var v = verbList[i];
                            if (v != null && v.verbProps != null && v.verbProps.IsMeleeAttack)
                                verbList.RemoveAt(i);
                        }
                    }
                }
            }
            catch (System.Exception ex) { Log.Warning($"[SSF] RefreshPawn (Remove Native Melee) error: {ex}"); }

            // UI 갱신
            try
            {
                if (refreshSelection && Find.Selector != null && Find.Selector.IsSelected(pawn))
                {
                    Find.Selector.Deselect(pawn);
                    Find.Selector.Select(pawn, playSound: false, forceDesignatorDeselect: false);
                }
            }
            catch (System.Exception ex) { Log.Warning($"[SSF] RefreshPawn (UI Selection) error: {ex}"); }

            // 에러 로그 플래그 리셋
            try
            {
                if (comp != null)
                {
                    comp.verbTickErrorLogged = false;
                }
            }
            catch (System.Exception ex) { Log.Warning($"[SSF] RefreshPawn (Error Reset) error: {ex}"); }
        }

        #endregion

        #region 저장/로드

        /// <summary>저장/로드 처리.</summary>
        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Defs.Look(ref currentForm, "currentForm");
            Scribe_Values.Look(ref transformTimer, "transformTimer", 0, true);
            Scribe_Defs.Look(ref originalBodyType, "originalBodyType");
            Scribe_Defs.Look(ref originalHeadType, "originalHeadType");

            Scribe_Values.Look(ref hasSavedColors, "hasSavedColors", false);

            Color __tmpHairColor = originalHairColor ?? default;
            bool __hasHairColor = originalHairColor.HasValue;
            Scribe_Values.Look(ref __hasHairColor, "hasOriginalHairColor", false);
            Scribe_Values.Look(ref __tmpHairColor, "originalHairColor");
            if (Scribe.mode == LoadSaveMode.LoadingVars)
                originalHairColor = __hasHairColor ? __tmpHairColor : (Color?)null;

            Color __tmpSkinColor = originalSkinColor ?? default;
            bool __hasSkinColor = originalSkinColor.HasValue;
            Scribe_Values.Look(ref __hasSkinColor, "hasOriginalSkinColor", false);
            Scribe_Values.Look(ref __tmpSkinColor, "originalSkinColor");
            if (Scribe.mode == LoadSaveMode.LoadingVars)
                originalSkinColor = __hasSkinColor ? __tmpSkinColor : (Color?)null;

            // Def 리스트

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

            // Reference 리스트 - hediff

            List<Hediff> __tmpHediffs = null;
            if (Scribe.mode == LoadSaveMode.Saving) __tmpHediffs = tempAddedHediffs;
            Scribe_Collections.Look(ref __tmpHediffs, "tempAddedHediffs", LookMode.Reference);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                tempAddedHediffs.Clear();
                __tmpHediffsLoad = __tmpHediffs;
            }

            // prevApparels - ThingID 문자열 저장

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
                __tmpPrevApIds = apIds != null ? new HashSet<string>(apIds) : null;
            }

            // prevWeapons - ThingID 문자열 저장

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
                __tmpPrevWpIds = wpIds != null ? new HashSet<string>(wpIds) : null;
            }

            // Deep 리스트

            List<ShapeshiftPartRestoreRecord> __tmpRestore = null;
            if (Scribe.mode == LoadSaveMode.Saving) __tmpRestore = tempPartRestoreRecords;
            Scribe_Collections.Look(ref __tmpRestore, "tempPartRestoreRecords", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                tempPartRestoreRecords.Clear();
                if (__tmpRestore != null) tempPartRestoreRecords.AddRange(__tmpRestore);
            }

            // verbAutoToggle 딕셔너리

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

            // PostLoadInit

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
                    ShapeshiftRegistry.Register(pawn, this);
                }
            }
        }

        #endregion

        #region 기즈모 생성

        /// <summary>해제 및 verb 지즈모 생성. 변신/전환은 Ability 바에서 처리.</summary>
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            var pawn = parent as Pawn;
            if (pawn == null) yield break;

            // 플레이어 조종 Pawn만
            if (!pawn.IsColonistPlayerControlled)
                yield break;

            // 변신 중: 해제 기즈모
            if (isTransformed && currentForm != null)
            {
                if (currentForm.canRevertVoluntarily)
                {
                    yield return new Command_Action
                    {
                        defaultLabel = "SSF_Command_RevertLabel".Translate(),
                        defaultDesc = "SSF_Command_RevertDesc".Translate(),
                        action = delegate { RemoveForm(); },
                        icon = ShapeshiftTextureUtility.GetRevertIcon(currentForm)
                    };
                }
            }

            // 폼 전용 verb 토글/공격

            if (!pawn.Drafted) yield break;

            var vt = ShapeshiftVerbTracker;
            if (vt == null) yield break;

            bool canViolent = !pawn.WorkTagIsDisabled(WorkTags.Violent);
            bool showToggle = ShapeshifterFrameworkMod.Settings?.showVerbAutoToggle ?? true;
            // 다중 선택 시 토글 숨김 — 토글 상태가 폰마다 다를 수 있어 병합 불가
            bool multiSelected = Find.Selector != null && Find.Selector.NumSelected > 1;
            _tmpSeenVerbs.Clear();
            var seen = _tmpSeenVerbs;

            var verbs = vt.AllVerbs;
            for (int i = 0; i < verbs.Count; i++)
            {
                var v = verbs[i];
                if (v == null || v.verbProps == null) continue;
                if (!v.verbProps.Ranged) continue;

                if (v.caster == null) v.caster = pawn;
                if (!seen.Add(v)) continue;

                int idx = i;

                bool projectileOk = !(v is Verb_LaunchProjectile) || v.verbProps.defaultProjectile != null;

                // 다중 선택 시 같은 폼+verb끼리 병합 (바닐라 무기와 동일 동작)
                var cmd = new Command_VerbTarget
                {
                    defaultLabel = GetVerbLabel(idx, v, preferToggleLabel: false),
                    defaultDesc = GetVerbDesc(idx, v, forToggle: false),
                    icon = GetVerbIcon(idx, v) ?? v.UIIcon,
                    verb = v,
                };
                if (!projectileOk)
                    cmd.Disable("SSF_Message_NoProjectile".Translate());
                if (!canViolent)
                    cmd.Disable("IsIncapableOfViolenceLower".Translate(pawn.LabelShort, pawn));
                else if (!v.Available())
                    cmd.Disable("CommandCannotFire".Translate());

                yield return cmd;

                // 다중 선택 시 토글 숨김 — 개별 폰 선택에서만 설정 가능
                if (multiSelected) continue;

                if (showToggle)
                {
                    var tgl = new Command_Toggle
                    {
                        defaultLabel = GetVerbLabel(idx, v, preferToggleLabel: true),
                        defaultDesc = GetVerbDesc(idx, v, forToggle: true),
                        icon = GetVerbIcon(idx, v) ?? v.UIIcon,
                        isActive = () => IsAutoAttackEnabled(idx, v),
                        toggleAction = () => ToggleAutoAttack(idx, v),
                        groupable = false,
                    };
                    if (!canViolent)
                        tgl.Disable("IsIncapableOfViolenceLower".Translate(pawn.LabelShort, pawn));
                    yield return tgl;
                }
                else
                {
                    ForceAutoAttackOn(idx, v);
                }
            }
        }
        #endregion
    }
}