// .NET Framework 4.8 / C# 7.3
// CompShapeshifter.cs
// 목적
//  - 변신 상태 관리(요건 판정은 ShapeshiftEligibility.cs 사용)
//  - 변신 시 의복/무기 처리(Inventory/Drop/None), 드랍 자동 금지(단일 옵션)
//  - 해제 시 자동 재착용(인벤토리/드랍 별도 옵션)
//  - 폼 verbs/tools 제공 + 공격 기즈모 노출(변신 폼 전용 VerbTracker)
//
// 구현 주의
//  - enum 충돌 방지: 루트 네임스페이스의 GearHandling/MergeMode를 강제 사용
//  - RenderTickOnGUI 등 고빈도 경로에 LINQ/박싱 회피, 반복 캐싱
//  - IExposable: 역호환 불필요(초기 배포 전) — 단, 참조/값 분리 저장 처리
//  - Verb 재초기화: RefreshPawn 내부에서 VerbTracker.VerbsNeedReinitOnLoad() 호출
//  - 자동 재착용: 바닥은 잡 큐(자연스럽게), 인벤토리는 holder.Remove 후 즉시 Wear/AddEquipment

using RimWorld;
using ShapeshifterFramework.Utilities;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace ShapeshifterFramework.Comps
{
    public class CompShapeshifter : ThingComp
    {
        // ─────────────────────────────────────────────────────────────
        // 상태(호환을 위해 public 유지)
        public ShapeshiftFormDef currentForm = null;
        public bool isTransformed { get { return currentForm != null; } }
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

        // 폼 전용 VerbTracker (폼 verbs/tools용)
        private VerbTracker shapeshiftVerbTracker;

        // Form 선택용 Gizmo 캐시
        private const int GizmoCacheInterval = 90;
        private int gizmoCacheTick = -9999;
        private List<ShapeshiftFormDef> gizmoFormsCache = new List<ShapeshiftFormDef>();

        // NEW: verb 자동공격 토글 상태 (키: formDefName#index)
        private readonly Dictionary<string, bool> verbAutoToggle = new Dictionary<string, bool>();

        // 변신 복귀 중 내부 재장착 허용 플래그 (세이브 불필요, 런타임 전용)
        public bool suppressEquipLock = false;

        // 우리가 추가한 헤디프(인스턴스) 추적은 기존 tempAddedHediffs 사용
        private readonly List<ShapeshifterFramework.Utilities.ShapeshiftPartRestoreRecord> tempPartRestoreRecords
            = new List<ShapeshifterFramework.Utilities.ShapeshiftPartRestoreRecord>(8);

        // ─────────────────────────────────────────────────────────────
        // shapeshift VerbOwner: 폼의 verbs/tools를 VerbTracker로 노출
        private class ShapeshiftVerbOwner : IVerbOwner
        {
            private readonly CompShapeshifter comp;
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

            // 폼에서 정의한 VerbProperties/Tools를 그대로 노출
            public List<VerbProperties> VerbProperties
            {
                get
                {
                    var f = comp.currentForm;
                    return (f != null && f.verbs != null) ? f.verbs : new List<VerbProperties>();
                }
            }

            public List<Tool> Tools
            {
                get
                {
                    var f = comp.currentForm;
                    return (f != null && f.tools != null) ? f.tools : new List<Tool>();
                }
            }
        }

        /// <summary>
        /// 현재 폼의 verbs/tools를 제공하는 전용 VerbTracker.
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
                        catch { }
                    }
                }
                return shapeshiftVerbTracker;
            }
        }
        // ─────────────────────────────────────────────────────────────
        // NEW: Verb 자동공격 토글 유틸리티 + 라벨/설명 헬퍼

        string AutoKey(int index)
        {
            var f = currentForm?.defName ?? "None";
            return f + "#" + index.ToString();
        }

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

        public bool IsAutoAttackEnabled(int index, Verb v)
        {
            if (v == null) return true;
            bool val;
            if (verbAutoToggle.TryGetValue(AutoKey(index), out val)) return val;
            return DefaultAutoOn(index);
        }

        public void ToggleAutoAttack(int index, Verb v)
        {
            bool now = IsAutoAttackEnabled(index, v);
            verbAutoToggle[AutoKey(index)] = !now;
        }

        public void ForceAutoAttackOn(int index, Verb v)
        {
            verbAutoToggle[AutoKey(index)] = true;
        }

        /// <summary>verb 명령 라벨(Def verbGizmoOptions 우선, 없으면 verbProps.label/Attack)</summary>
        public string GetVerbLabel(int index, Verb v, bool preferToggleLabel)
        {
            var vp = v?.verbProps;
            var opt = currentForm?.verbGizmoOptions;
            if (opt != null && index >= 0 && index < opt.Count && opt[index] != null)
            {
                string s = preferToggleLabel ? opt[index].toggleLabel : opt[index].label;
                if (!string.IsNullOrEmpty(s)) return s.Translate().CapitalizeFirst();
            }

            string __label = string.IsNullOrEmpty(vp?.label) ? "Shapeshift.Verb.Attack".Translate() : vp.label.Translate();
            return __label.CapitalizeFirst();
        }

        /// <summary>verb 명령/토글 설명(Def verbGizmoOptions 우선, 없으면 기본)</summary>
        public string GetVerbDesc(int index, Verb v, bool forToggle)
        {
            var opt = currentForm?.verbGizmoOptions;
            if (opt != null && index >= 0 && index < opt.Count && opt[index] != null)
            {
                string s = forToggle ? opt[index].toggleDesc : opt[index].desc;
                if (!string.IsNullOrEmpty(s)) return s.Translate();
            }

            if (forToggle) return "Shapeshift.Verb.ToggleDesc".Translate();
            return "Shapeshift.Verb.OrderDesc".Translate();
        }

        // ─────────────────────────────────────────────────────────────
        // Ticking

        public override void CompTick()
        {
            base.CompTick();
            var pawn = parent as Pawn;
            if (isTransformed && currentForm != null)
            {
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

                // 전용 VerbTracker 틱 업데이트
                try { ShapeshiftVerbTracker?.VerbsTick(); } catch { }
            }
        }

        // 바닐라 틱 접근: transformTimer가 남은 틱을 의미(0 이하이면 무시)
        private int RemainingShapeshiftTicks
        {
            get
            {
                int t = transformTimer; // 기존 카운트다운 필드 사용
                return t > 0 ? t : 0;
            }
        }
        public override string CompInspectStringExtra()
        {
            if (!isTransformed || currentForm == null)
                return null;

            // durationTicks가 없거나 <=0 이면 영구 변신
            if (!currentForm.durationTicks.HasValue || currentForm.durationTicks.Value <= 0)
                return "ShapeshiftInspect_Permanent".Translate();

            int remain = transformTimer; // 남은 틱(CompTick에서 감소하는 기존 필드)
            if (remain <= 0) return null;

            // 바닐라 포맷(다국어 대응)
            string timeStr = GenDate.ToStringTicksToPeriod(remain, allowSeconds: false, shortForm: true);

            // "변신: 남은 시간 {0}" / "Shapeshift: {0} remaining"
            return "ShapeshiftInspect_Remaining".Translate(timeStr);
        }

        // ─────────────────────────────────────────────────────────────
        // 변신 가능 판정

        public bool CanTransform(Pawn pawn, ShapeshiftFormDef form)
        {
            if (pawn == null || form == null) return false;
            string prev = (isTransformed && currentForm != null) ? currentForm.defName : null;
            if (!ShapeshiftEligibility.PassBasicFilters(pawn, form, prev)) return false; // allow/disallow* 게이트
            return ShapeshiftEligibility.PassConditional(pawn, form);                    // required* 집계(Mode 적용)
        }

        private void InvalidateGizmoCache()
        {
            gizmoCacheTick = -9999;
            gizmoFormsCache = null;
        }

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
                    if (CanTransform(pawn, f)) list.Add(f);
                }
                gizmoFormsCache = list;
                gizmoCacheTick = now;
            }
            return gizmoFormsCache;
        }

        // ─────────────────────────────────────────────────────────────
        // 적용/해제

        public void ApplyForm(ShapeshiftFormDef form) { ApplyForm(form, null); }

        public void ApplyForm(ShapeshiftFormDef form, string prevOverride)
        {
            var pawn = parent as Pawn;
            if (pawn == null || form == null) return;

            string prev = prevOverride ?? ((isTransformed && currentForm != null) ? currentForm.defName : null);

            // 실시간 재검증
            if (!ShapeshiftEligibility.PassBasicFilters(pawn, form, prev) ||
                !ShapeshiftEligibility.PassConditional(pawn, form))
            {
                try { Messages.Message("Shapeshift_CannotTransform".Translate(form.LabelCap), MessageTypeDefOf.RejectInput, false); } catch { }
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
            HandleGearOnTransform(pawn, form);

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
                    tempPartRestoreRecords
                );
            }

            // 상태 적용
            currentForm = form;
            ShapeshiftTransformFxUtility.PlayEnterFx(pawn, form); // 변신 시작 FX
            if (form.durationTicks.HasValue && form.durationTicks.Value > 0)
                transformTimer = form.durationTicks.Value;

            // 체형/머리형 적용(인간형만)
            if (pawn.story != null)
            {
                if (form.bodyType != null) pawn.story.bodyType = form.bodyType;
                if (form.headType != null) pawn.story.headType = form.headType;
            }

            // 보이스 캐시 등록
            if (form.soundCall != null)
                ShapeshiftRuntimeCaches.CallByPawn[pawn] = form.soundCall;
            if (form.soundWounded != null)
                ShapeshiftRuntimeCaches.WoundedByPawn[pawn] = form.soundWounded;
            if (form.soundDeath != null)
                ShapeshiftRuntimeCaches.DeathByPawn[pawn] = form.soundDeath;

            // 혈흔, 스미어 캐시 등록
            if (form.bloodDef != null)
                ShapeshiftRuntimeCaches.BloodByPawn[pawn] = form.bloodDef;
            if (form.bloodSmearDef != null)
                ShapeshiftRuntimeCaches.SmearByPawn[pawn] = form.bloodSmearDef;

            // FleshType 캐시 등록
            if (form.fleshType != null)
                ShapeshiftRuntimeCaches.FleshTypeByPawn[pawn] = form.fleshType;

            // 전용 VerbTracker는 프로퍼티 접근 시 생성 → Refresh에서 Verb 리셋 포함
            shapeshiftVerbTracker = null;

            RefreshPawn(pawn);
            InvalidateGizmoCache();
        }

        public void RemoveForm()
        {
            var pawn = parent as Pawn;
            if (pawn == null) return;
            var __oldForm = currentForm;

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
                // 1) 우리가 추가한 헤디프 제거
                for (int i = 0; i < tempAddedHediffs.Count; i++)
                {
                    Hediff h = tempAddedHediffs[i];
                    if (h != null && pawn.health.hediffSet.hediffs.Contains(h))
                        pawn.health.RemoveHediff(h);
                }

                // 2) Def 캐시 기반 방어적 정리 (혹시 남은 동일 Def)
                if (tempAddedHediffsDefCache != null && tempAddedHediffsDefCache.Count > 0)
                {
                    List<Hediff> list = pawn.health.hediffSet.hediffs;
                    for (int i = 0; i < tempAddedHediffsDefCache.Count; i++)
                    {
                        HediffDef def = tempAddedHediffsDefCache[i]; if (def == null) continue;
                        for (int j = list.Count - 1; j >= 0; j--)
                            if (list[j].def == def) pawn.health.RemoveHediff(list[j]);
                    }
                }

                // 3) 파츠 단위 원상 복원
                //    - 변신 전 결손이 아니었던 파츠: RestorePart로 자연복원(무출혈)
                //    - 변신 전 기존 AddedPart가 있었던 파츠: 다시 재설치(복구)
                for (int i = 0; i < tempPartRestoreRecords.Count; i++)
                {
                    var rec = tempPartRestoreRecords[i];
                    if (rec == null || rec.Part == null) continue;

                    // 변신 전 결손이 아니었다면 자연 파츠 복원(우리가 AddedPart 제거하면서 MissingPart가 생겼을 수 있음)
                    if (!rec.WasMissingBefore)
                    {
                        try { pawn.health.RestorePart(rec.Part); } catch { }
                    }

                    // 변신 전 설치되어 있던 AddedPart들을 다시 설치(복구)
                    if (rec.PreExistingAdded != null && rec.PreExistingAdded.Count > 0)
                    {
                        for (int k = 0; k < rec.PreExistingAdded.Count; k++)
                        {
                            var prev = rec.PreExistingAdded[k];
                            if (prev?.Def == null) continue;
                            var reinst = pawn.health.AddHediff(prev.Def, rec.Part, null);
                            if (reinst != null && prev.Severity.HasValue)
                            {
                                try { reinst.Severity = prev.Severity.Value; } catch { }
                            }
                        }
                    }
                    else
                    {
                        // 변신 전 결손이었으면(=자연 파츠가 원래 없었으면) 자연 파츠를 다시 제거해야 하나?
                        // 설계: WasMissingBefore==true인 경우는 "결손 상태를 유지"하도록 RestorePart를 호출하지 않았으므로,
                        //       현재 상태는 MissingPart 그대로 유지됨. 별도 조치 불필요.
                    }
                }

                if (ShapeshifterFramework.Utilities.ShapeshiftApplyHediffUtility.DebugLog)
                    Log.Message($"SSF Revert: restored {tempPartRestoreRecords.Count} part(s)");

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

        // ─────────────────────────────────────────────────────────────
        // 외부 알림 (예: Pawn.Kill Postfix에서 호출)

        /// <summary>
        /// Pawn이 사망했을 때 호출됨.
        /// 변신 해제 및 런타임 캐시 정리를 강제로 실행.
        /// </summary>
        public void Notify_Killed(DamageInfo? dinfo, Hediff exactCulprit)
        {
            var pawn = parent as Pawn;
            if (pawn == null) return;

            if (isTransformed)
            {
                RemoveForm();
            }

            ShapeshiftRuntimeCaches.ClearFor(pawn);
            Log.Message($"[SSF] {pawn} killed, shapeshift forcibly deactivated.");
        }

        // ─────────────────────────────────────────────────────────────
        // 내부: 장비 스냅샷/처리/재착용/리프레시

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

        void HandleGearOnTransform(Pawn pawn, ShapeshiftFormDef form)
        {
            if (pawn == null || form == null) return;

            IntVec3 pos = pawn.PositionHeld;
            Map map = pawn.MapHeld;
            ShapeshifterFrameworkSettings st = ShapeshifterFrameworkMod.Settings;

            // 의복
            if (form.apparelOnTransform != GearHandling.None && pawn.apparel != null)
            {
                List<Apparel> worn = pawn.apparel.WornApparel;
                List<Apparel> copy = new List<Apparel>(worn.Count);
                for (int i = 0; i < worn.Count; i++) { if (worn[i] != null) copy.Add(worn[i]); }

                for (int i = 0; i < copy.Count; i++)
                {
                    Apparel ap = copy[i];
                    if (ap == null) continue;

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
            if (form.weaponsOnTransform != GearHandling.None && pawn.equipment != null)
            {
                List<ThingWithComps> list = pawn.equipment.AllEquipmentListForReading;
                List<ThingWithComps> copy = new List<ThingWithComps>(list.Count);
                for (int i = 0; i < list.Count; i++) { if (list[i] != null) copy.Add(list[i]); }

                for (int i = 0; i < copy.Count; i++)
                {
                    ThingWithComps eq = copy[i];
                    if (eq == null) continue;

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
            catch { }
        }

        void TryReequipPreviousGear(Pawn pawn)
        {
            if (pawn == null || pawn.Dead) return;

            ShapeshifterFrameworkSettings st = ShapeshifterFrameworkMod.Settings;
            bool allowInv = (st == null) ? true : st.autoReequipFromInventory;
            bool allowGround = (st == null) ? true : st.autoReequipFromGround;

            var toQueue = new List<Job>(prevWeapons.Count + prevApparels.Count);

            // ★ 변신 해제 중 내부 재장착은 착용락을 임시 해제
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
                            if (w.Map == pawn.MapHeld)
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
                            ShapeshiftInventoryReequipUtil.SafeEquipFromInventory(pawn, w);
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
                            if (ap.Map == pawn.MapHeld)
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
                            ShapeshiftInventoryReequipUtil.SafeWearFromInventory(pawn, ap, dropReplaced: true);
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
            try { pawn.health?.capacities?.Notify_CapacityLevelsDirty(); } catch { }
            try { pawn.health?.hediffSet?.DirtyCache(); } catch { }
            try { pawn.Drawer?.renderer?.SetAllGraphicsDirty(); } catch { }
            try { PortraitsCache.SetDirty(pawn); } catch { }
            try { GlobalTextureAtlasManager.TryMarkPawnFrameSetDirty(pawn); } catch { }
            try { pawn.Notify_DisabledWorkTypesChanged(); } catch { }

            // 1) 바닐라 쪽 VerbTracker 재초기화 (InitVerbsFromPawn 없음)
            //    VerbsNeedReinitOnLoad()가 내부 verbs를 null로 만들어 다음 접근에서 재구성되게 함.
            try
            {
                if (forceReinitPawnVerbs)
                {
                    pawn.verbTracker?.VerbsNeedReinitOnLoad();
                    // 재빌드를 지금 당장 유도: AllVerbs 접근 시 InitVerbsFromZero→InitVerbs 경로로 빌드됨  :contentReference[oaicite:3]{index=3}
                    var _ = pawn.verbTracker?.AllVerbs;
                }
            }
            catch { }

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
            catch { }

            // 3) UI 갱신: 선택 토글로 지즈모 강제 새로고침
            try
            {
                if (refreshSelection && Find.Selector != null && Find.Selector.IsSelected(pawn))
                {
                    // 시그니처는 (Thing obj, bool playSound = true, bool forceDesignatorDeselect = true)
                    Find.Selector.Deselect(pawn);
                    Find.Selector.Select(pawn, playSound: false, forceDesignatorDeselect: false);
                }
            }
            catch { }
        }

        // ─────────────────────────────────────────────────────────────
        // 저장/로드 (단일 메서드로 통합, 세이브 호환 주석 포함)

        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Defs.Look(ref currentForm, "currentForm");
            Scribe_Values.Look(ref transformTimer, "transformTimer", 0, true);
            Scribe_Defs.Look(ref originalBodyType, "originalBodyType");
            Scribe_Defs.Look(ref originalHeadType, "originalHeadType");

            // readonly 리스트 ref 불가 → 임시 변수 경유
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

            // 변신 전 장비 스냅샷(레퍼런스 저장)
            List<Apparel> __tmpPrevAp = null;
            if (Scribe.mode == LoadSaveMode.Saving) __tmpPrevAp = prevApparels;
            Scribe_Collections.Look(ref __tmpPrevAp, "prevApparels", LookMode.Reference);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                prevApparels.Clear();
                if (__tmpPrevAp != null) prevApparels.AddRange(__tmpPrevAp);
            }

            List<ThingWithComps> __tmpPrevWp = null;
            if (Scribe.mode == LoadSaveMode.Saving) __tmpPrevWp = prevWeapons;
            Scribe_Collections.Look(ref __tmpPrevWp, "prevWeapons", LookMode.Reference);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                prevWeapons.Clear();
                if (__tmpPrevWp != null) prevWeapons.AddRange(__tmpPrevWp);
            }

            // NEW: verbAutoToggle 저장/로드
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
            // BackCompatibility: 키가 없으면 기본값(On)으로 동작

            Pawn pawn = parent as Pawn;
            if (Scribe.mode == LoadSaveMode.PostLoadInit && pawn != null)
            {
                if (pawn.Dead && isTransformed) RemoveForm();
            }
        }

        // ─────────────────────────────────────────────────────────────
        // 기즈모

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            var pawn = parent as Pawn;
            if (pawn == null) yield break;

            // ─────────────────────────────────────────────────────────────
            // (1) 여기부터는 "변신/해제/전환" 등 프레임워크 고유 기즈모 — 기존 코드 그대로 유지
            int threshold = (ShapeshifterFrameworkMod.Settings != null)
                ? ShapeshifterFrameworkMod.Settings.maxInlineGizmoCount
                : 8;

            // 사용 가능 폼 캐시 가져오기
            List<ShapeshiftFormDef> available;
            {
                var cached = GetAvailableFormsCached(pawn);
                available = new List<ShapeshiftFormDef>();
                foreach (var f in cached) if (f != null) available.Add(f);
            }

            if (!isTransformed)
            {
                if (available.Count > threshold)
                {
                    yield return new Command_Action
                    {
                        defaultLabel = "ShapeshiftMenuChooseLabel".Translate(),
                        defaultDesc = "ShapeshiftMenuChooseDesc".Translate(),
                        action = delegate
                        {
                            var opts = new List<FloatMenuOption>(available.Count);
                            for (int i = 0; i < available.Count; i++)
                            {
                                var f = available[i]; if (f == null) continue;
                                var cap = f; // capture
                                opts.Add(new FloatMenuOption(f.LabelCap, delegate { ApplyForm(cap); }));
                            }
                            if (opts.Count == 0) opts.Add(new FloatMenuOption("None".Translate(), null));
                            Find.WindowStack.Add(new FloatMenu(opts));
                        },
                        icon = ContentFinder<UnityEngine.Texture2D>.Get("UI/Commands/SSF_Shift_Enter", true)
                    };
                }
                else
                {
                    for (int i = 0; i < available.Count; i++)
                    {
                        var form = available[i]; if (form == null) continue;
                        string path = form.gizmoIconPathEnter;
                        if (string.IsNullOrEmpty(path)) path = "UI/Commands/SSF_Shift_Enter";
                        yield return new Command_Action
                        {
                            defaultLabel = "ShapeshiftCommandLabel".Translate(form.LabelCap),
                            defaultDesc = "ShapeshiftCommandDesc".Translate(form.description),
                            action = delegate { ApplyForm(form); },
                            icon = ContentFinder<UnityEngine.Texture2D>.Get(path, true)
                        };
                    }
                }
            }
            else if (currentForm != null)
            {
                // 해제
                string path = currentForm.gizmoIconPathRevert;
                if (string.IsNullOrEmpty(path)) path = "UI/Commands/SSF_Shift_Revert";
                yield return new Command_Action
                {
                    defaultLabel = "ShapeshiftRevertLabel".Translate(),
                    defaultDesc = (currentForm.durationTicks.HasValue && currentForm.durationTicks.Value > 0)
                        ? "ShapeshiftRevertDesc_WithTime".Translate((float)0 / 60f)
                        : "ShapeshiftRevertDesc".Translate(),
                    action = delegate { RemoveForm(); },
                    icon = ContentFinder<UnityEngine.Texture2D>.Get(path, true)
                };

                // 전환
                string prev = currentForm.defName;
                if (available.Count > threshold)
                {
                    yield return new Command_Action
                    {
                        defaultLabel = "ShapeshiftMenuSwitchLabel".Translate(),
                        defaultDesc = "ShapeshiftMenuSwitchDesc".Translate(),
                        action = delegate
                        {
                            var opts = new List<FloatMenuOption>(available.Count);
                            for (int i = 0; i < available.Count; i++)
                            {
                                var f = available[i]; if (f == null) continue;
                                var cap = f;
                                opts.Add(new FloatMenuOption(f.LabelCap, delegate { ApplyForm(cap, prev); }));
                            }
                            if (opts.Count == 0) opts.Add(new FloatMenuOption("None".Translate(), null));
                            Find.WindowStack.Add(new FloatMenu(opts));
                        },
                        icon = ContentFinder<UnityEngine.Texture2D>.Get("UI/Commands/SSF_Shift_Enter", true)
                    };
                }
                else
                {
                    for (int i = 0; i < available.Count; i++)
                    {
                        var form = available[i]; if (form == null) continue;
                        string path2 = form.gizmoIconPathEnter;
                        if (string.IsNullOrEmpty(path2)) path2 = "UI/Commands/SSF_Shift_Enter";
                        yield return new Command_Action
                        {
                            defaultLabel = "ShapeshiftSwitchLabel".Translate(form.LabelCap),
                            defaultDesc = "ShapeshiftSwitchDesc".Translate(form.description),
                            action = delegate { ApplyForm(form, prev); },
                            icon = ContentFinder<UnityEngine.Texture2D>.Get(path2, true)
                        };
                    }
                }
            }
            else
            {
                // 방어적 폴백
                yield return new Command_Action
                {
                    defaultLabel = "ShapeshiftRevertLabel".Translate(),
                    defaultDesc = "ShapeshiftRevertDesc".Translate(),
                    action = delegate { RemoveForm(); },
                    icon = ContentFinder<UnityEngine.Texture2D>.Get("UI/Commands/SSF_Shift_Revert", true)
                };
            }

            // ─────────────────────────────────────────────────────────────
            // (2) 여기서부터 "verb 토글/공격" — 바닐라 근접/원거리 뒤에 나오게 Comp에서만 생성
            //     (바닐라: PawnAttackGizmoUtility가 먼저, 그 다음 CompGetGizmosExtra가 호출되므로 자연히 뒤에 위치함)

            // 드래프트시에만 노출(바닐라 규칙 유지)
            if (!pawn.Drafted) yield break;

            var vt = ShapeshiftVerbTracker;
            if (vt == null) yield break;

            // 비폭력 Pawn: 버튼은 노출되지만 Disabled
            bool canViolent = !pawn.WorkTagIsDisabled(WorkTags.Violent);

            // 모드 설정: 토글 노출 여부 (Off면 자동공격 강제 On)
            bool showToggle = ShapeshifterFrameworkMod.Settings?.showVerbAutoToggle ?? true;

            // 같은 프레임 UI 중복 방지: 동일 verb 중복 생성 회피
            var seen = new HashSet<Verb>();

            var verbs = vt.AllVerbs;
            for (int i = 0; i < verbs.Count; i++)
            {
                var v = verbs[i];
                if (v == null || v.verbProps == null) continue;
                if (!v.verbProps.Ranged) continue; // 원거리만 기즈모 생성

                // 캐스터 보정
                if (v.caster == null) v.caster = pawn;

                // 같은 Verb 인스턴스 중복 방지
                if (!seen.Add(v)) continue;

                int idx = i; // ★ 클로저 안전 복사(중요)

                // ── 자동공격 토글(옵션 허용 시에만 표시)
                if (showToggle)
                {
                    var tgl = new Command_Toggle
                    {
                        defaultLabel = GetVerbLabel(idx, v, preferToggleLabel: true),
                        defaultDesc = GetVerbDesc(idx, v, forToggle: true),
                        icon = v.UIIcon,
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
                    // 토글 숨김이면 자동공격은 항상 On
                    ForceAutoAttackOn(idx, v);
                }

                // LaunchProjectile은 defaultProjectile 없으면 클릭 금지
                bool projectileOk = !(v is Verb_LaunchProjectile) || v.verbProps.defaultProjectile != null;

                var cmd = new Command_VerbTarget
                {
                    defaultLabel = GetVerbLabel(idx, v, preferToggleLabel: false),
                    defaultDesc = GetVerbDesc(idx, v, forToggle: false),
                    icon = v.UIIcon,
                    verb = v,
                    groupable = false, // 중복 병합 방지
                };
                if (!projectileOk)
                    cmd.Disable("ShapeshiftNoProjectileForVerb".Translate());
                if (!canViolent)
                    cmd.Disable("IsIncapableOfViolence".Translate());
                else if (!v.Available())
                    cmd.Disable("CommandCannotFire".Translate());

                yield return cmd;
            }
        }
    }
}
