// ShapeshifterFramework | Hediffs | HediffComp_ShapeshiftCore.cs
// 목적 : 변신(Shapeshift) 라이프사이클 전체를 관장하는 핵심 HediffComp.
// 용도 : HediffDef 부여 → CompPostPostAdd → 지연 초기화(needsInit) → 첫 Tick에서 ApplyForm 실행.
//        모든 상태 필드(장비 스냅샷, VerbTracker, 타이머 등)를 Hediff 수명과 동기화.
// 주의 : CompPostPostAdd에서 ApplyForm을 직접 호출하지 않음 (RefreshPawn 재진입 방지).
//        needsInit == true 구간에서 ShouldRemove는 false를 반환하여 자동 소멸 방지.

using RimWorld;
using ShapeshifterFramework.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ShapeshifterFramework.Hediffs
{
    /// <summary>변신 라이프사이클 관리 HediffComp. HediffDef → FormDef 매핑 + 상태 관리.</summary>
    public class HediffComp_ShapeshiftCore : HediffComp
    {
        #region Properties 접근

        /// <summary>XML 속성 접근.</summary>
        public HediffCompProperties_ShapeshiftCore Props => (HediffCompProperties_ShapeshiftCore)props;

        /// <summary>소유 Pawn 접근 (parent.pawn).</summary>
        public Pawn Pawn => parent?.pawn;

        #endregion

        #region 상태 필드/캐시

        public ShapeshiftFormDef currentForm = null;
        public bool isTransformed { get { return currentForm != null; } }

        /// <summary>지연 초기화 플래그. CompPostPostAdd에서 true, 첫 CompPostTick에서 ApplyForm 실행.</summary>
        public bool needsInit = false;

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

        // 틱(Tick) 에러 스팸 방지용 플래그
        private bool verbTickErrorLogged = false;

        // 기즈모 verb 중복 방지용 재사용 HashSet (GC 할당 방지)
        private readonly HashSet<Verb> _tmpSeenVerbs = new HashSet<Verb>();

        // verb 자동공격 토글 상태 (키: formDefName#index#verbName)
        private readonly Dictionary<string, bool> verbAutoToggle = new Dictionary<string, bool>();

        public bool suppressEquipLock = false;

        // ApplyForm/RemoveForm 재진입 방지 플래그 (이벤트 콜백으로 인한 중첩 호출 차단)
        private bool _isApplyingOrRemoving = false;

        // PostLoadInit에서 Reference 연결 완료 후 AddRange하기 위한 임시 보관 필드
        private List<Hediff> __tmpHediffsLoad = null;
        private HashSet<string> __tmpPrevApIds = null;
        private HashSet<string> __tmpPrevWpIds = null;
        private bool needsGearResolve = false;

        // 파츠 복원 추적
        private readonly List<ShapeshiftPartRestoreRecord> tempPartRestoreRecords
            = new List<ShapeshiftPartRestoreRecord>(8);

        // 앰비언트 VFX 런타임 상태 (저장 불필요 — 로드 후 CompPostTick에서 자동 재생성)
        private Effecter ambientEffecterInstance;
        private int ambientFleckNextTick;

        #endregion

        #region 행동값 해석 (Props 오버라이드 ?? FormDef 기본값)

        /// <summary>해석된 지속 틱. Props 오버라이드 우선.</summary>
        public int? ResolvedDurationTicks
        {
            get { return Props.durationTicks ?? currentForm?.durationTicks; }
        }

        /// <summary>해석된 자발적 해제 가능 여부.</summary>
        public bool ResolvedCanRevertVoluntarily
        {
            get { return Props.canRevertVoluntarily ?? currentForm?.canRevertVoluntarily ?? true; }
        }

        /// <summary>해석된 Downed 시 자동 해제 여부.</summary>
        public bool ResolvedRevertOnDowned
        {
            get { return Props.revertOnDowned ?? currentForm?.revertOnDowned ?? false; }
        }

        /// <summary>해석된 sustain 모드.</summary>
        public SustainMode ResolvedSustainMode
        {
            get { return Props.sustainMode ?? currentForm?.sustainMode ?? SustainMode.All; }
        }

        // sustain 조건 리스트 해석 헬퍼
        private List<ThingDef> ResolvedSustainApparels => Props.sustainApparels ?? currentForm?.sustainApparels;
        private List<ThingDef> ResolvedSustainWeapons => Props.sustainWeapons ?? currentForm?.sustainWeapons;
        private List<HediffDef> ResolvedSustainHediffs => Props.sustainHediffs ?? currentForm?.sustainHediffs;
        private List<GeneDef> ResolvedSustainGenes => Props.sustainGenes ?? currentForm?.sustainGenes;

        // revert 부산물 해석
        private List<ThingDefCountClass> ResolvedRevertDrops => Props.revertDrops ?? currentForm?.revertDrops;
        // Props.revertAddHediffs (List<HediffAddEntry>)가 우선. null이면 FormDef는 별도 경로로 처리.
        private List<HediffAddEntry> ResolvedRevertAddHediffs => Props.revertAddHediffs;

        #endregion

        #region IVerbOwner 구현 (폼 verbs/tools → 전용 VerbTracker)

        /// <summary>현재 폼 verbs/tools IVerbOwner 구현.</summary>
        private class ShapeshiftVerbOwner : IVerbOwner
        {
            private readonly HediffComp_ShapeshiftCore comp;
            private static readonly List<VerbProperties> EmptyVerbProperties = new List<VerbProperties>(0);
            private static readonly List<Tool> EmptyTools = new List<Tool>(0);
            public ShapeshiftVerbOwner(HediffComp_ShapeshiftCore c) { comp = c; }

            VerbTracker IVerbOwner.VerbTracker => comp.shapeshiftVerbTracker;

            ImplementOwnerTypeDef IVerbOwner.ImplementOwnerTypeDef => ImplementOwnerTypeDefOf.NativeVerb;

            string IVerbOwner.UniqueVerbOwnerID()
            {
                var p = comp.Pawn;
                return p != null ? "Shapeshift_" + p.ThingID : "Shapeshift_Unknown";
            }

            bool IVerbOwner.VerbsStillUsableBy(Pawn p)
            {
                return comp.isTransformed && comp.Pawn == p;
            }

            Thing IVerbOwner.ConstantCaster => comp.Pawn;

            public List<VerbProperties> VerbProperties
            {
                get
                {
                    var f = comp.currentForm;
                    return (f != null && f.verbs != null) ? f.verbs : EmptyVerbProperties;
                }
            }

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
                    var pawn = Pawn;
                    if (pawn != null)
                    {
                        try
                        {
                            var verbs = shapeshiftVerbTracker.AllVerbs;
                            for (int i = 0; i < verbs.Count; i++)
                            {
                                var v = verbs[i];
                                if (v != null) v.caster = pawn;
                            }
                        }
                        catch (Exception ex) { Log.Error($"[SSF] VerbTracker init error: {ex}"); }
                    }
                }
                return shapeshiftVerbTracker;
            }
        }

        #endregion

        #region Verb 자동공격 토글 유틸/라벨·설명 헬퍼

        /// <summary>verb에 대응하는 VerbGizmoOption 검색.</summary>
        private VerbGizmoOption FindGizmoOption(int index, Verb v)
        {
            var opt = currentForm?.verbGizmoOptions;
            if (opt == null || opt.Count == 0) return null;

            string vLabel = v?.verbProps?.label;
            if (!string.IsNullOrEmpty(vLabel))
            {
                for (int i = 0; i < opt.Count; i++)
                {
                    var o = opt[i];
                    if (o != null && string.Equals(o.verbLabel, vLabel, StringComparison.OrdinalIgnoreCase))
                        return o;
                }
            }

            if (index >= 0 && index < opt.Count)
            {
                var o = opt[index];
                if (o != null && string.IsNullOrEmpty(o.verbLabel))
                    return o;
            }

            return null;
        }

        string AutoKey(Verb v)
        {
            var f = currentForm?.defName ?? "None";
            string vName = v?.verbProps?.label ?? v?.GetType().Name ?? "UnknownVerb";
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

        /// <summary>verb 자동공격 활성 여부.</summary>
        public bool IsAutoAttackEnabled(int index, Verb v)
        {
            if (v == null) return true;
            bool val;
            if (verbAutoToggle.TryGetValue(AutoKey(v), out val)) return val;
            return true;
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
                verbAutoToggle[AutoKey(v)] = true;
            }
        }

        /// <summary>폼 적용 시 배타적 토글 초기화.</summary>
        private void InitAutoToggleForForm()
        {
            var vt = ShapeshiftVerbTracker;
            if (vt == null) return;

            bool toggleEnabled = ShapeshifterFrameworkMod.Settings?.showVerbAutoToggle ?? true;

            bool firstSet = false;
            var verbs = vt.AllVerbs;
            for (int i = 0; i < verbs.Count; i++)
            {
                var v = verbs[i];
                if (v == null || v.verbProps == null) continue;
                if (!v.verbProps.Ranged) continue;

                bool on = toggleEnabled && !firstSet;
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
                string s = forToggle ? (o.toggleDesc ?? o.desc) : o.desc;
                if (!string.IsNullOrEmpty(s)) return s.Translate();
            }

            if (forToggle) return "SSF_Verb_ToggleDesc".Translate();
            return "SSF_Verb_OrderDesc".Translate();
        }

        /// <summary>verbGizmoOptions의 iconPath에서 아이콘 로드.</summary>
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

        #region 생명주기

        /// <summary>Hediff 부여 직후 — needsInit 플래그만 설정. 실제 ApplyForm은 첫 Tick에서 실행.</summary>
        public override void CompPostPostAdd(DamageInfo? dinfo, float amount)
        {
            base.CompPostPostAdd(dinfo, amount);
            needsInit = true;
        }

        /// <summary>Hediff 제거 시 정리. RemoveForm → Registry 해제.</summary>
        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            var pawn = Pawn;

            if (isTransformed)
            {
                RemoveForm();
            }

            // 방어적 레지스트리 해제
            if (pawn != null)
                ShapeshiftRegistry.Unregister(pawn);
        }

        /// <summary>Pawn 스폰 시 레지스트리 재등록 + 그래픽 복원. Patch_Pawn_SpawnSetup에서 호출.</summary>
        public void OnPawnSpawned(bool respawningAfterLoad)
        {
            var pawn = Pawn;
            if (pawn == null || !isTransformed || currentForm == null) return;

            ShapeshiftRegistry.Register(pawn, this);

            if (respawningAfterLoad)
            {
                var form = currentForm;
                if (pawn.story != null)
                {
                    if (form.bodyType != null) pawn.story.bodyType = form.bodyType;
                    if (form.headType != null) pawn.story.headType = form.headType;
                    if (form.hairColor.HasValue) pawn.story.HairColor = form.hairColor.Value;
                    if (form.skinColor.HasValue) pawn.story.skinColorOverride = form.skinColor.Value;
                }

                try { pawn.Drawer?.renderer?.SetAllGraphicsDirty(); } catch (Exception) { }
                try { PortraitsCache.SetDirty(pawn); } catch (Exception) { }
                try { GlobalTextureAtlasManager.TryMarkPawnFrameSetDirty(pawn); } catch (Exception) { }
            }
        }

        #endregion

        #region Ticking/Inspect

        /// <summary>매 틱: 지연 초기화 + 타이머/사망/downed/sustain 검사/VerbTracker 처리.</summary>
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            var pawn = Pawn;
            if (pawn == null) return;

            // 지연 초기화: CompPostPostAdd에서 needsInit=true → 첫 Tick에서 ApplyForm 실행
            if (needsInit)
            {
                needsInit = false;
                var formDef = currentForm ?? Props.formDef;
                if (formDef != null)
                {
                    ApplyForm(formDef);
                }
                else
                {
                    ShapeshiftDiagnostics.Info("HediffComp_ShapeshiftCore: needsInit but no formDef. Use SetFormDef() for dynamic forms.");
                }
                return;
            }

            // 로드 후 장비 참조 복원
            if (needsGearResolve)
            {
                needsGearResolve = false;
                if (__tmpPrevApIds != null || __tmpPrevWpIds != null)
                {
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

                    if (__tmpPrevApIds != null && __tmpPrevApIds.Count > 0)
                        Log.Warning($"[SSF] {__tmpPrevApIds.Count} prev apparel(s) could not be resolved for {pawn?.Name}. Items may be lost on revert.");
                    if (__tmpPrevWpIds != null && __tmpPrevWpIds.Count > 0)
                        Log.Warning($"[SSF] {__tmpPrevWpIds.Count} prev weapon(s) could not be resolved for {pawn?.Name}. Items may be lost on revert.");
                    __tmpPrevApIds = null;
                    __tmpPrevWpIds = null;
                }
            }

            if (isTransformed && currentForm != null)
            {
                if (pawn.Dead)
                {
                    RemoveForm();
                    return;
                }
                if (ResolvedRevertOnDowned && pawn.Downed)
                {
                    RemoveForm();
                    return;
                }

                var resolvedDuration = ResolvedDurationTicks;
                if (resolvedDuration.HasValue && resolvedDuration.Value > 0)
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

                    // sustain 조건 검사 (Props 오버라이드 반영)
                    if (!CheckSustainConditions(pawn)
                        && !(pawn.stances?.curStance is Stance_Warmup))
                    {
                        Messages.Message("SSF_Message_RevertDueToConditionLost".Translate(pawn.LabelShortCap), pawn, MessageTypeDefOf.NegativeEvent, false);
                        RemoveForm();
                        return;
                    }
                }

                // 앰비언트 VFX
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
                catch (Exception ex)
                {
                    if (!verbTickErrorLogged)
                    {
                        Log.Error($"[SSF] VerbsTick error on pawn {pawn?.Name} (Logging once to prevent spam): {ex}");
                        verbTickErrorLogged = true;
                    }
                }
            }
        }

        /// <summary>sustain 조건 충족 여부 검사. Props 오버라이드 반영.</summary>
        private bool CheckSustainConditions(Pawn pawn)
        {
            var apparels = ResolvedSustainApparels;
            var weapons = ResolvedSustainWeapons;
            var hediffs = ResolvedSustainHediffs;
            var genes = ResolvedSustainGenes;

            bool hasApparels = apparels != null && apparels.Count > 0;
            bool hasWeapons = weapons != null && weapons.Count > 0;
            bool hasHediffs = hediffs != null && hediffs.Count > 0;
            bool hasGenes = ModsConfig.BiotechActive && genes != null && genes.Count > 0;

            if (!hasApparels && !hasWeapons && !hasHediffs && !hasGenes) return true;

            var mode = ResolvedSustainMode;

            bool apparelMet = !hasApparels || CheckSustainApparels(pawn, apparels);
            bool weaponMet = !hasWeapons || CheckSustainWeapons(pawn, weapons);
            bool hediffMet = !hasHediffs || CheckSustainHediffs(pawn, hediffs);
            bool geneMet = !hasGenes || CheckSustainGenes(pawn, genes);

            if (mode == SustainMode.All)
                return apparelMet && weaponMet && hediffMet && geneMet;
            else
                return apparelMet || weaponMet || hediffMet || geneMet;
        }

        // sustain 체크용 재활용 HashSet — 재진입 안전을 위해 apparel/weapon 분리
        private static readonly HashSet<ThingDef> _tmpSustainApparelDefs = new HashSet<ThingDef>();
        private static readonly HashSet<ThingDef> _tmpSustainWeaponDefs = new HashSet<ThingDef>();

        private static bool CheckSustainApparels(Pawn pawn, List<ThingDef> required)
        {
            if (pawn.apparel == null) return false;
            var worn = pawn.apparel.WornApparel;
            _tmpSustainApparelDefs.Clear();
            for (int j = 0; j < worn.Count; j++)
                _tmpSustainApparelDefs.Add(worn[j].def);
            for (int i = 0; i < required.Count; i++)
            {
                if (!_tmpSustainApparelDefs.Contains(required[i])) return false;
            }
            return true;
        }

        private static bool CheckSustainWeapons(Pawn pawn, List<ThingDef> required)
        {
            if (pawn.equipment == null) return false;
            var eqs = pawn.equipment.AllEquipmentListForReading;
            _tmpSustainWeaponDefs.Clear();
            for (int j = 0; j < eqs.Count; j++)
                _tmpSustainWeaponDefs.Add(eqs[j].def);
            for (int i = 0; i < required.Count; i++)
            {
                if (!_tmpSustainWeaponDefs.Contains(required[i])) return false;
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

            var resolvedDuration = ResolvedDurationTicks;
            if (!resolvedDuration.HasValue || resolvedDuration.Value <= 0)
                return "SSF_Inspect_Permanent".Translate();

            int remain = transformTimer;
            if (remain <= 0) return null;

            string timeStr = GenDate.ToStringTicksToPeriod(remain, allowSeconds: false, shortForm: false);
            return "SSF_Inspect_Remaining".Translate(timeStr);
        }

        #endregion

        #region 변신 가능 판정

        /// <summary>기본 변신 가능 여부 판정.</summary>
        public bool CanTransform(ShapeshiftFormDef form)
        {
            var pawn = Pawn;
            if (pawn == null || form == null) return false;
            string prev = (isTransformed && currentForm != null) ? currentForm.defName : null;
            return ShapeshiftEligibility.CanTransformBasic(pawn, form, prev);
        }

        #endregion

        #region 런타임 FormDef 설정

        /// <summary>런타임 동적 formDef 설정 (디버그/SSF_GenericShapeshiftForm용).</summary>
        public void SetFormDef(ShapeshiftFormDef form)
        {
            if (form == null) return;
            // needsInit가 true인 상태에서 호출되면 해당 formDef를 사용
            // needsInit가 false이고 아직 미변신이면 즉시 ApplyForm
            if (needsInit)
            {
                // currentForm에 임시 저장 — 다음 Tick의 needsInit 처리에서 사용
                currentForm = form;
            }
            else if (!isTransformed)
            {
                ApplyForm(form);
            }
        }

        #endregion

        #region 변신 적용/해제

        /// <summary>폼 적용.</summary>
        public void ApplyForm(ShapeshiftFormDef form) { ApplyForm(form, null, null); }

        /// <summary>폼 적용. prevOverride 지정 시 해제 후 전환.</summary>
        public void ApplyForm(ShapeshiftFormDef form, string prevOverride, List<Thing> sources = null)
        {
            var pawn = Pawn;
            if (pawn == null || form == null) return;
            if (_isApplyingOrRemoving) return; // 재진입 방지
            _isApplyingOrRemoving = true;
            try
            {

            string prev = prevOverride ?? ((isTransformed && currentForm != null) ? currentForm.defName : null);

            if (!ShapeshiftEligibility.CanTransformBasic(pawn, form, prev))
            {
                try { Messages.Message("SSF_Message_CannotTransform".Translate(form.LabelCap), MessageTypeDefOf.RejectInput, false); } catch { }
                return;
            }

            // 전환 시 기존 폼 먼저 해제 — RemoveForm 내부 재진입 검사를 우회해야 하므로 플래그 일시 해제
            if (isTransformed)
            {
                _isApplyingOrRemoving = false;
                RemoveForm();
                if (_isApplyingOrRemoving) return; // RemoveForm 중 재진입 발생 시 중단
                _isApplyingOrRemoving = true;
            }

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
                ShapeshiftApplyHediffUtility.ApplyHediffEntries(
                    pawn,
                    form.addHediffs,
                    tempAddedHediffs,
                    tempAddedHediffsDefCache,
                    tempPartRestoreRecords,
                    prevDefCache: tempAddedHediffsDefCache
                );
            }

            // 상태 적용
            currentForm = form;

            ShapeshiftTransformFxUtility.PlayEnterFx(pawn, form);

            var resolvedDuration = ResolvedDurationTicks;
            if (resolvedDuration.HasValue && resolvedDuration.Value > 0)
                transformTimer = resolvedDuration.Value;

            // 앰비언트 VFX 초기화
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

            // 레지스트리 등록
            ShapeshiftRegistry.Register(pawn, this);

            // VerbTracker 리셋
            shapeshiftVerbTracker = null;
            InitAutoToggleForForm();

            RefreshPawn(pawn, this);

            // 이벤트 발행
            ShapeshiftCoreUtility.FireFormApplied(pawn, form);

            }
            catch (Exception ex)
            {
                Log.Error($"[SSF] ApplyForm failed for {pawn?.Name}: {ex}");
            }
            finally
            {
                _isApplyingOrRemoving = false;
            }
        }

        /// <summary>현재 폼 해제.</summary>
        public void RemoveForm()
        {
            var pawn = Pawn;
            if (pawn == null) return;
            if (_isApplyingOrRemoving) return; // 재진입 방지
            _isApplyingOrRemoving = true;
            var __oldForm = currentForm;
            try
            {

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
                for (int i = 0; i < tempAddedHediffs.Count; i++)
                {
                    Hediff h = tempAddedHediffs[i];
                    if (h != null && pawn.health.hediffSet.hediffs.Contains(h))
                    {
                        pawn.health.RemoveHediff(h);
                        if (h.def != null) tempAddedHediffsDefCache.Remove(h.def);
                    }
                }

                // 2차: 참조 실패분 → def 기준 카운팅 제거 (동일 def 복수 부여 대응)
                if (tempAddedHediffsDefCache != null && tempAddedHediffsDefCache.Count > 0)
                {
                    var remaining = new Dictionary<HediffDef, int>();
                    for (int i = 0; i < tempAddedHediffsDefCache.Count; i++)
                    {
                        var d = tempAddedHediffsDefCache[i];
                        if (d == null) continue;
                        if (remaining.ContainsKey(d)) remaining[d]++;
                        else remaining[d] = 1;
                    }
                    List<Hediff> list = pawn.health.hediffSet.hediffs;
                    for (int j = list.Count - 1; j >= 0; j--)
                    {
                        if (remaining.Count == 0) break;
                        if (list[j] == null) continue;
                        var hd = list[j].def;
                        if (hd == null) continue;
                        if (remaining.TryGetValue(hd, out int cnt) && cnt > 0)
                        {
                            remaining[hd] = cnt - 1;
                            if (cnt <= 1) remaining.Remove(hd);
                            pawn.health.RemoveHediff(list[j]);
                        }
                    }
                }

                // 파츠 원상 복원
                for (int i = 0; i < tempPartRestoreRecords.Count; i++)
                {
                    var rec = tempPartRestoreRecords[i];
                    if (rec == null || rec.Part == null) continue;

                    if (!rec.WasMissingBefore)
                    {
                        try { pawn.health.RestorePart(rec.Part); }
                        catch (Exception ex) { Log.Warning($"[SSF] RestorePart failed for '{rec.Part.Label}': {ex}"); }
                    }

                    if (rec.PreExistingAdded != null && rec.PreExistingAdded.Count > 0
                        && !pawn.health.hediffSet.PartIsMissing(rec.Part))
                    {
                        for (int k = 0; k < rec.PreExistingAdded.Count; k++)
                        {
                            var prev = rec.PreExistingAdded[k];
                            if (prev?.Def == null) continue;

                            BodyPartRecord targetPart = null;
                            if (prev.PartDef == null || prev.PartDef == rec.Part.def)
                            {
                                targetPart = rec.Part;
                            }
                            else
                            {
                                if (pawn.RaceProps?.body == null) continue;
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
                                    catch (Exception ex) { Log.Warning($"[SSF] Restore Severity failed for '{prev.Def.defName}': {ex}"); }
                                }
                            }
                        }
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
                    pawn.story.skinColorOverride = originalSkinColor;
                    hasSavedColors = false;
                }
            }

            // 자동 재착용
            ShapeshifterFrameworkSettings st = ShapeshifterFrameworkMod.Settings;
            if (st == null || st.autoReequipFromInventory || st.autoReequipFromGround)
                TryReequipPreviousGear(pawn);

            // VerbTracker 해제
            shapeshiftVerbTracker = null;

            if (__oldForm != null)
                ShapeshiftTransformFxUtility.PlayExitFx(pawn, __oldForm);

            // 앰비언트 VFX 정리
            if (ambientEffecterInstance != null)
            {
                ambientEffecterInstance.Cleanup();
                ambientEffecterInstance = null;
            }

            // 해제 시 잔해 드랍
            var drops = ResolvedRevertDrops;
            if (drops != null && drops.Count > 0
                && pawn.Spawned && pawn.MapHeld != null)
            {
                for (int i = 0; i < drops.Count; i++)
                {
                    var entry = drops[i];
                    if (entry?.thingDef == null || entry.count <= 0) continue;
                    Thing thing = ThingMaker.MakeThing(entry.thingDef);
                    thing.stackCount = entry.count;
                    GenPlace.TryPlaceThing(thing, pawn.PositionHeld, pawn.MapHeld, ThingPlaceMode.Near);
                }
            }

            // 해제 시 hediff 부여 — Props 오버라이드 (HediffAddEntry) 우선, 없으면 FormDef 폴백 (List<HediffDef>)
            var addHediffEntries = ResolvedRevertAddHediffs;
            if (addHediffEntries != null && addHediffEntries.Count > 0
                && pawn.health != null && !pawn.Dead)
            {
                for (int i = 0; i < addHediffEntries.Count; i++)
                {
                    var entry = addHediffEntries[i];
                    if (entry?.hediff == null) continue;
                    Hediff h = pawn.health.AddHediff(entry.hediff);
                    if (h != null && entry.severity.HasValue)
                    {
                        try { h.Severity = entry.severity.Value; }
                        catch (Exception ex) { Log.Warning($"[SSF] revertAddHediffs severity set failed: {ex}"); }
                    }
                }
            }
            else if (__oldForm != null && __oldForm.revertAddHediffs != null && __oldForm.revertAddHediffs.Count > 0
                && pawn.health != null && !pawn.Dead)
            {
                // FormDef 폴백 (Phase 3에서 List<HediffAddEntry>로 통일 예정)
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

            // 이벤트 발행
            if (__oldForm != null)
                ShapeshiftCoreUtility.FireFormRemoved(pawn, __oldForm);
            }
            catch (Exception ex)
            {
                Log.Error($"[SSF] RemoveForm failed for {pawn?.Name}: {ex}");
                // 부분 복원 상태 방지 — 핵심 상태를 강제 정리하여 좀비 변신 상태를 차단
                currentForm = null;
                if (pawn != null) ShapeshiftRegistry.Unregister(pawn);
                if (pawn != null) ShapeshiftRuntimeCaches.ClearFor(pawn);
            }
            finally
            {
                _isApplyingOrRemoving = false;
            }
        }

        /// <summary>하위 부위 여부 확인.</summary>
        private bool IsPartChildOf(BodyPartRecord child, BodyPartRecord parentPart)
        {
            if (child == null || parentPart == null) return false;

            BodyPartRecord current = child.parent;
            while (current != null)
            {
                if (current == parentPart) return true;
                current = current.parent;
            }
            return false;
        }

        #endregion

        #region 외부 알림

        /// <summary>사망 시 변신 해제 및 캐시 정리.</summary>
        public void Notify_Killed(DamageInfo? dinfo, Hediff exactCulprit)
        {
            var pawn = Pawn;
            if (pawn == null) return;

            bool wasTransformed = isTransformed;

            if (isTransformed)
            {
                RemoveForm();
            }

            ShapeshiftRuntimeCaches.ClearFor(pawn);

            if (wasTransformed)
            {
                ShapeshiftDiagnostics.Info($"{pawn.LabelShort} killed, shapeshift forcibly deactivated.");
            }
        }

        #endregion

        #region 내부: 장비 스냅샷/처리/재착용/드랍 유틸

        void CaptureCurrentGear(Pawn pawn)
        {
            if (pawn == null) return;

            if (pawn.apparel != null)
            {
                List<Apparel> worn = pawn.apparel.WornApparel;
                for (int i = 0; i < worn.Count; i++)
                {
                    var a = worn[i];
                    if (a != null) prevApparels.Add(a);
                }
            }

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
                    else
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
                    else
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

        void SpawnAndEquipFormGear(Pawn pawn, ShapeshiftFormDef form)
        {
            if (pawn == null || form == null) return;

            using (new ShapeshiftEquipLockScope(this))
            {
                if (pawn.apparel != null && form.spawnApparelOnTransform != null && form.spawnApparelOnTransform.Count > 0)
                {
                    for (int i = 0; i < form.spawnApparelOnTransform.Count; i++)
                    {
                        ThingDef apparelDef = form.spawnApparelOnTransform[i];
                        if (apparelDef == null || !apparelDef.IsApparel) continue;

                        if (pawn.apparel != null)
                        {
                            List<Apparel> worn = pawn.apparel.WornApparel;
                            for (int j = worn.Count - 1; j >= 0; j--)
                            {
                                Apparel existingAp = worn[j];
                                if ((sourceItems != null && sourceItems.Contains(existingAp)) || generatedApparel.Contains(existingAp)) continue;

                                if (pawn.RaceProps?.body != null && !ApparelUtility.CanWearTogether(apparelDef, existingAp.def, pawn.RaceProps.body))
                                {
                                    pawn.apparel.Remove(existingAp);
                                    if (form.conflictingGearHandling == GearHandling.Drop)
                                    {
                                        TryDropThing(existingAp, pawn.PositionHeld, pawn.MapHeld);
                                    }
                                    else
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

                        ThingDef stuff = null;
                        if (apparelDef.MadeFromStuff)
                        {
                            stuff = form.spawnApparelStuff;
                            if (stuff == null || stuff.stuffProps == null || !stuff.stuffProps.CanMake(apparelDef))
                            {
                                stuff = GenStuff.DefaultStuffFor(apparelDef);
                            }
                        }

                        Apparel newApparel = (Apparel)ThingMaker.MakeThing(apparelDef, stuff);

                        if (pawn.apparel != null)
                        {
                            pawn.apparel.Wear(newApparel, dropReplacedApparel: false);
                            pawn.apparel.Lock(newApparel);
                            generatedApparel.Add(newApparel);
                        }
                    }
                }

                if (pawn.equipment != null && form.spawnWeaponOnTransform != null && form.spawnWeaponOnTransform.Count > 0)
                {
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
                            else
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

                        ThingDef stuff = null;
                        if (weaponDef.MadeFromStuff)
                        {
                            stuff = form.spawnWeaponStuff;
                            if (stuff == null || stuff.stuffProps == null || !stuff.stuffProps.CanMake(weaponDef))
                            {
                                stuff = GenStuff.DefaultStuffFor(weaponDef);
                            }
                        }

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
            catch (Exception ex)
            {
                Log.Error($"[SSF] TryDropThing failed for '{t.Label}': {ex}");
            }
        }

        void TryReequipPreviousGear(Pawn pawn)
        {
            ShapeshiftDiagnostics.Info($"TryReequip: weapons={prevWeapons.Count}, apparels={prevApparels.Count}");
            if (pawn == null || pawn.Dead) return;

            ShapeshifterFrameworkSettings st = ShapeshifterFrameworkMod.Settings;
            bool allowInv = (st == null) ? true : st.autoReequipFromInventory;
            bool allowGround = (st == null) ? true : st.autoReequipFromGround;

            var toQueue = new List<Job>(prevWeapons.Count + prevApparels.Count);

            using (new ShapeshiftEquipLockScope(this))
            {
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

                        if (allowInv && pawn.inventory?.innerContainer?.Contains(w) == true)
                        {
                            ShapeshiftInventoryReequipUtility.SafeEquipFromInventory(pawn, w);
                        }
                    }
                }

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

        /// <summary>런타임 캐시 재등록.</summary>
        public static void ReapplyRuntimeCaches(Pawn pawn, ShapeshiftFormDef form)
        {
            ApplyRuntimeCaches(pawn, form);
        }

        /// <summary>런타임 캐시 등록(사운드/혈흔/FleshType).</summary>
        private static void ApplyRuntimeCaches(Pawn pawn, ShapeshiftFormDef form)
        {
            if (pawn == null || form == null) return;

            if (form.soundCall != null) ShapeshiftRuntimeCaches.SetCache(ShapeshiftRuntimeCaches.CallByPawn, pawn, form.soundCall);
            if (form.soundWounded != null) ShapeshiftRuntimeCaches.SetCache(ShapeshiftRuntimeCaches.WoundedByPawn, pawn, form.soundWounded);
            if (form.soundDeath != null) ShapeshiftRuntimeCaches.SetCache(ShapeshiftRuntimeCaches.DeathByPawn, pawn, form.soundDeath);
            if (form.soundAngry != null) ShapeshiftRuntimeCaches.SetCache(ShapeshiftRuntimeCaches.AngryByPawn, pawn, form.soundAngry);

            if (form.bloodDef != null) ShapeshiftRuntimeCaches.SetCache(ShapeshiftRuntimeCaches.BloodByPawn, pawn, form.bloodDef);
            if (form.bloodSmearDef != null) ShapeshiftRuntimeCaches.SetCache(ShapeshiftRuntimeCaches.SmearByPawn, pawn, form.bloodSmearDef);

            if (form.fleshType != null) ShapeshiftRuntimeCaches.SetCache(ShapeshiftRuntimeCaches.FleshTypeByPawn, pawn, form.fleshType);
        }

        /// <summary>캐시/그래픽/Verb 재초기화.</summary>
        public static void RefreshPawn(Pawn pawn, HediffComp_ShapeshiftCore compHint = null, bool forceReinitPawnVerbs = true, bool resetShapeshiftVerbs = true, bool refreshSelection = true)
        {
            if (pawn == null) return;

            var comp = compHint;
            if (comp == null)
            {
                // 레지스트리에서 조회
                ShapeshiftRegistry.TryGet(pawn, out comp, out _);
            }

            try { pawn.health?.capacities?.Notify_CapacityLevelsDirty(); } catch (Exception ex) { Log.Warning($"[SSF] RefreshPawn (Capacity) error: {ex}"); }
            try { pawn.health?.hediffSet?.DirtyCache(); } catch (Exception ex) { Log.Warning($"[SSF] RefreshPawn (Hediff) error: {ex}"); }
            try { pawn.Drawer?.renderer?.SetAllGraphicsDirty(); } catch (Exception ex) { Log.Warning($"[SSF] RefreshPawn (Graphics) error: {ex}"); }
            try { PortraitsCache.SetDirty(pawn); } catch (Exception ex) { Log.Warning($"[SSF] RefreshPawn (Portrait) error: {ex}"); }
            try { GlobalTextureAtlasManager.TryMarkPawnFrameSetDirty(pawn); } catch (Exception ex) { Log.Warning($"[SSF] RefreshPawn (Atlas) error: {ex}"); }
            try { pawn.Notify_DisabledWorkTypesChanged(); } catch (Exception ex) { Log.Warning($"[SSF] RefreshPawn (WorkTypes) error: {ex}"); }

            try
            {
                if (forceReinitPawnVerbs)
                {
                    pawn.verbTracker?.VerbsNeedReinitOnLoad();
                    var _ = pawn.verbTracker?.AllVerbs;
                }
            }
            catch (Exception ex) { Log.Warning($"[SSF] RefreshPawn (Vanilla Verbs) error: {ex}"); }

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
            catch (Exception ex) { Log.Warning($"[SSF] RefreshPawn (Shapeshift Verbs) error: {ex}"); }

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
            catch (Exception ex) { Log.Warning($"[SSF] RefreshPawn (Remove Native Melee) error: {ex}"); }

            try
            {
                if (refreshSelection && Find.Selector != null && Find.Selector.IsSelected(pawn))
                {
                    Find.Selector.Deselect(pawn);
                    Find.Selector.Select(pawn, playSound: false, forceDesignatorDeselect: false);
                }
            }
            catch (Exception ex) { Log.Warning($"[SSF] RefreshPawn (UI Selection) error: {ex}"); }

            try
            {
                if (comp != null)
                {
                    comp.verbTickErrorLogged = false;
                }
            }
            catch (Exception ex) { Log.Warning($"[SSF] RefreshPawn (Error Reset) error: {ex}"); }
        }

        #endregion

        #region 저장/로드

        /// <summary>저장/로드 처리.</summary>
        public override void CompExposeData()
        {
            base.CompExposeData();

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
                    int lostCount = 0;
                    for (int i = 0; i < __tmpHediffsLoad.Count; i++)
                    {
                        if (__tmpHediffsLoad[i] != null)
                            tempAddedHediffs.Add(__tmpHediffsLoad[i]);
                        else
                            lostCount++;
                    }
                    if (lostCount > 0)
                        Log.Warning($"[SSF] {lostCount} hediff reference(s) lost during load for pawn {pawn?.Name}. Revert may leave orphaned hediffs.");
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

                var pawn = Pawn;
                if (pawn != null && pawn.Dead && isTransformed)
                {
                    RemoveForm();
                }
                else if (isTransformed && currentForm != null && pawn != null)
                {
                    ApplyRuntimeCaches(pawn, currentForm);
                    ShapeshiftRegistry.Register(pawn, this);
                }
                else if (pawn != null && !pawn.Dead && currentForm == null
                    && (tempAddedHediffs.Count > 0 || tempAddedAbilities.Count > 0
                        || generatedApparel.Count > 0 || generatedWeapons.Count > 0))
                {
                    // FormDef 삭제 등으로 currentForm이 null이지만 변신 잔여 데이터 존재 → 정리
                    Log.Warning($"[SSF] Pawn {pawn.Name}: orphaned transform data found (FormDef removed?). Forcing cleanup.");

                    // hediff 잔여 제거
                    if (pawn.health != null)
                    {
                        for (int i = 0; i < tempAddedHediffs.Count; i++)
                        {
                            var h = tempAddedHediffs[i];
                            if (h != null && pawn.health.hediffSet.hediffs.Contains(h))
                                pawn.health.RemoveHediff(h);
                        }
                    }
                    tempAddedHediffs.Clear();
                    tempAddedHediffsDefCache.Clear();

                    // 능력 잔여 제거
                    if (pawn.abilities != null)
                    {
                        for (int i = 0; i < tempAddedAbilities.Count; i++)
                        {
                            if (tempAddedAbilities[i] != null)
                                pawn.abilities.RemoveAbility(tempAddedAbilities[i]);
                        }
                    }
                    tempAddedAbilities.Clear();

                    // 생성 장비 파괴
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

                    // 체형 원복
                    if (pawn.story != null)
                    {
                        if (originalBodyType != null) pawn.story.bodyType = originalBodyType;
                        if (originalHeadType != null) pawn.story.headType = originalHeadType;
                        if (hasSavedColors)
                        {
                            if (originalHairColor.HasValue) pawn.story.HairColor = originalHairColor.Value;
                            pawn.story.skinColorOverride = originalSkinColor;
                            hasSavedColors = false;
                        }
                    }

                    tempPartRestoreRecords.Clear();
                    shapeshiftVerbTracker = null;

                    try { RefreshPawn(pawn, this); } catch (Exception ex) { Log.Warning($"[SSF] Orphan cleanup RefreshPawn error: {ex}"); }
                }
            }
        }

        #endregion

        #region 기즈모 생성

        /// <summary>해제 및 verb 기즈모 생성. hediff의 GetGizmos()에서 호출.</summary>
        public IEnumerable<Gizmo> GetGizmosExtra()
        {
            var pawn = Pawn;
            if (pawn == null) yield break;

            if (!pawn.IsColonistPlayerControlled)
                yield break;

            if (isTransformed && currentForm != null)
            {
                if (ResolvedCanRevertVoluntarily)
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

            if (!pawn.Drafted) yield break;

            var vt = ShapeshiftVerbTracker;
            if (vt == null) yield break;

            bool canViolent = !pawn.WorkTagIsDisabled(WorkTags.Violent);
            bool showToggle = ShapeshifterFrameworkMod.Settings?.showVerbAutoToggle ?? true;
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
            }
        }

        #endregion
    }
}
