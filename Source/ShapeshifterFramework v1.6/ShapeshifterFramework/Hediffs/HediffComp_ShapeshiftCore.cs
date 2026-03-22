// ShapeshifterFramework | Hediffs | HediffComp_ShapeshiftCore.cs
// 목적 : 변신(Shapeshift) 라이프사이클 전체를 관장하는 핵심 HediffComp.
// 용도 : HediffDef 부여 → CompPostPostAdd → 지연 초기화(needsInit) → 첫 Tick에서 ApplyForm 실행.
//        모든 상태 필드(장비 스냅샷, VerbTracker, 타이머 등)를 Hediff 수명과 동기화.
// 주의 : CompPostPostAdd에서 ApplyForm을 직접 호출하지 않음 (바닐라 AddHediff 스택 내 재진입 방지).
//        needsInit == true 구간에서 CompShouldRemove가 false를 반환하여 자동 소멸 방지.

using RimWorld;
using ShapeshifterFramework.Utilities;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ShapeshifterFramework.Hediffs
{
    /// <summary>변신 라이프사이클 관리 HediffComp. HediffDef → FormDef 매핑 + 상태 관리.</summary>
    public partial class HediffComp_ShapeshiftCore : HediffComp
    {
        #region Properties 접근

        /// <summary>XML 속성 접근.</summary>
        public HediffCompProperties_ShapeshiftCore Props => (HediffCompProperties_ShapeshiftCore)props;

        #endregion

        /// <summary>초기화 진행 중에는 바닐라 severity 기반 자동 소멸을 차단.</summary>
        public override bool CompShouldRemove => needsInit ? false : base.CompShouldRemove;

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

        private int transformTimer;

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

        // 변신을 유발한 원본 아이템 (예: 변신 반지) - 장착 해제/파괴/드랍 시 변신 해제
        public List<Thing> sourceItems = new List<Thing>();

        // 변신 시 소환된 폼 전용 장비 추적 (해제 시 삭제 및 복사 방지용)
        private List<Apparel> generatedApparel = new List<Apparel>();
        private List<ThingWithComps> generatedWeapons = new List<ThingWithComps>();

        // 폼 전용 VerbTracker (폼 verbs/tools용)
        private VerbTracker shapeshiftVerbTracker;

        // 틱(Tick) 에러 스팸 방지용 플래그
        private bool verbTickErrorLogged;

        // 기즈모 verb 중복 방지용 재사용 HashSet (GC 할당 방지)
        private readonly HashSet<Verb> _tmpSeenVerbs = new HashSet<Verb>();

        // verb 자동공격 토글 상태 (키: formDefName#index#verbName)
        private Dictionary<string, bool> verbAutoToggle = new Dictionary<string, bool>();
        // Scribe Dict 직렬화용 tmp
        private List<string> tmpVerbToggleKeys;
        private List<bool> tmpVerbToggleVals;

        public bool suppressEquipLock = false;

        // ApplyForm/RemoveForm 재진입 방지 플래그 (이벤트 콜백으로 인한 중첩 호출 차단)
        private bool _isApplyingOrRemoving;

        // PostLoadInit에서 Reference 연결 완료 후 AddRange하기 위한 임시 보관 필드
        private List<Hediff> __tmpHediffsLoad = null;
        private HashSet<string> __tmpPrevApIds = null;
        private HashSet<string> __tmpPrevWpIds = null;
        private bool needsGearResolve;

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

        // IVerbOwner 구현, VerbTracker, 자동공격 토글 → HediffComp_ShapeshiftCore.Verbs.cs

        #region 생명주기

        /// <summary>Hediff 부여 직후 — 기존 변신 hediff 제거 + needsInit 플래그 설정. 실제 ApplyForm은 첫 Tick에서 실행.</summary>
        /// <remarks>바닐라 GiveHediff 경로(데브 도구, 외부 모드 등)에서도 변신 중첩을 방지하기 위해
        /// 자기 자신을 제외한 기존 변신 hediff를 자동 제거합니다.</remarks>
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);

            // 기존 변신 hediff 제거 (동시 적용 방지) — 자기 자신(parent)은 제외
            var pawn = Pawn;
            if (pawn?.health?.hediffSet != null)
            {
                var hediffs = pawn.health.hediffSet.hediffs;
                for (int i = hediffs.Count - 1; i >= 0; i--)
                {
                    if (hediffs[i] == parent) continue; // 자기 자신 스킵
                    var existingCore = (hediffs[i] as HediffWithComps)?.TryGetComp<HediffComp_ShapeshiftCore>();
                    if (existingCore != null)
                    {
                        if (existingCore.isTransformed)
                        {
                            try { existingCore.RemoveForm(); }
                            catch (Exception ex) { Log.Error($"[SSF] RemoveForm failed during CompPostPostAdd for {pawn.Name}: {ex}"); }
                        }
                        pawn.health.RemoveHediff(hediffs[i]);
                        break; // 동시에 1개만
                    }
                }
            }

            needsInit = true;
        }

        /// <summary>Hediff 제거 시 정리. RemoveForm이 미호출된 경우 대비.</summary>
        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();

            if (isTransformed)
            {
                RemoveForm();
            }
            else
            {
                // 미등록 상태의 방어적 레지스트리 해제 (needsInit 중 제거 등)
                var pawn = Pawn;
                if (pawn != null)
                    ShapeshiftRegistry.Unregister(pawn);
            }
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

                try { pawn.Drawer?.renderer?.SetAllGraphicsDirty(); } catch (Exception) { /* 호환성 방어 — 타 모드 충돌 시 무시 */ }
                try { PortraitsCache.SetDirty(pawn); } catch (Exception) { /* 호환성 방어 — 타 모드 충돌 시 무시 */ }
                try { GlobalTextureAtlasManager.TryMarkPawnFrameSetDirty(pawn); } catch (Exception) { /* 호환성 방어 — 타 모드 충돌 시 무시 */ }
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

            // 로드 후 장비 참조 복원 (상세 로직은 ExposeData.cs의 ResolveGearFromIds)
            if (needsGearResolve)
            {
                ResolveGearFromIds(pawn);
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
                    transformTimer--;
                    if (transformTimer <= 0) { RemoveForm(); return; }
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

                            bool isEquipped =
                                (item is Apparel ap && pawn.apparel != null && pawn.apparel.Contains(ap)) ||
                                (item is ThingWithComps tc && pawn.equipment != null && pawn.equipment.Contains(tc));

                            if (item.Destroyed || item.Spawned || !isEquipped)
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

            if (mode == SustainMode.All)
            {
                // All 모드: 모든 카테고리 충족 필요
                bool apparelMet = !hasApparels || CheckSustainApparels(pawn, apparels);
                bool weaponMet = !hasWeapons || CheckSustainWeapons(pawn, weapons);
                bool hediffMet = !hasHediffs || CheckSustainHediffs(pawn, hediffs);
                bool geneMet = !hasGenes || CheckSustainGenes(pawn, genes);
                return apparelMet && weaponMet && hediffMet && geneMet;
            }

            // Any 모드: 요구사항이 있는 카테고리 중 하나라도 충족하면 유지
            if (hasApparels && CheckSustainApparels(pawn, apparels)) return true;
            if (hasWeapons && CheckSustainWeapons(pawn, weapons)) return true;
            if (hasHediffs && CheckSustainHediffs(pawn, hediffs)) return true;
            if (hasGenes && CheckSustainGenes(pawn, genes)) return true;
            return false;
        }

        // sustain 체크용 재활용 HashSet — 재진입 안전을 위해 ThreadStatic
        [ThreadStatic] private static HashSet<ThingDef> _tmpSustainDefs;

        private static bool CheckSustainApparels(Pawn pawn, List<ThingDef> required)
        {
            if (pawn.apparel == null) return false;
            if (_tmpSustainDefs == null) _tmpSustainDefs = new HashSet<ThingDef>();
            var worn = pawn.apparel.WornApparel;
            _tmpSustainDefs.Clear();
            for (int j = 0; j < worn.Count; j++)
            {
                if (worn[j] == null) continue;
                _tmpSustainDefs.Add(worn[j].def);
            }
            for (int i = 0; i < required.Count; i++)
            {
                if (!_tmpSustainDefs.Contains(required[i])) return false;
            }
            return true;
        }

        private static bool CheckSustainWeapons(Pawn pawn, List<ThingDef> required)
        {
            if (pawn.equipment == null) return false;
            if (_tmpSustainDefs == null) _tmpSustainDefs = new HashSet<ThingDef>();
            var eqs = pawn.equipment.AllEquipmentListForReading;
            _tmpSustainDefs.Clear();
            for (int j = 0; j < eqs.Count; j++)
            {
                if (eqs[j] == null) continue;
                _tmpSustainDefs.Add(eqs[j].def);
            }
            for (int i = 0; i < required.Count; i++)
            {
                if (!_tmpSustainDefs.Contains(required[i])) return false;
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

        /// <summary>헤디프 툴팁 추가 문자열 (1.6: CompTipStringExtra 프로퍼티).</summary>
        public override string CompTipStringExtra
        {
            get
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
            // 안전성: RemoveForm 예외 시 외부 finally(하단)가 _isApplyingOrRemoving = false를 보장.
            //         RemoveForm 정상 완료 시 그 내부 finally가 false 설정 → 여기서 true로 복원.
            if (isTransformed)
            {
                _isApplyingOrRemoving = false;
                RemoveForm();
                if (_isApplyingOrRemoving) return; // RemoveForm 중 재진입 발생 시 중단
                _isApplyingOrRemoving = true;
            }

            // sources가 명시적으로 전달되면 교체, 아니면 기존 sourceItems 보존 (GiveShiftHediff에서 사전 설정 가능)
            if (sources != null)
                this.sourceItems = sources;
            else if (this.sourceItems == null)
                this.sourceItems = new List<Thing>();

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

            // 체형/컬러 백업 — hasSavedColors로 중복 백업 방지 (RemoveForm 예외 시 isTransformed 오판 방어)
            if (!hasSavedColors && pawn.story != null)
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

                // 파츠 원상 복원 (죽은 폰은 건너뜀)
                if (!pawn.Dead)
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
            _verbKeyCache = null;
            verbAutoToggle.Clear();

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
                // FormDef 폴백 — revertAddHediffs는 List<HediffAddEntry>
                for (int i = 0; i < __oldForm.revertAddHediffs.Count; i++)
                {
                    var entry = __oldForm.revertAddHediffs[i];
                    if (entry?.hediff == null) continue;
                    Hediff h = pawn.health.AddHediff(entry.hediff);
                    if (h != null && entry.severity.HasValue)
                    {
                        try { h.Severity = entry.severity.Value; }
                        catch (Exception ex) { Log.Warning($"[SSF] revertAddHediffs (FormDef) severity set failed: {ex}"); }
                    }
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

                // severity를 0으로 설정 → 바닐라가 다음 틱에 hediff 자동 제거
                if (parent != null)
                    parent.Severity = 0f;
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

        // 장비 스냅샷/처리/재착용/드랍 유틸 → HediffComp_ShapeshiftCore.Gear.cs

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
                    comp._verbKeyCache = null;

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

        // 저장/로드(CompExposeData) → HediffComp_ShapeshiftCore.ExposeData.cs
        // 기즈모 생성(GetGizmosExtra) → HediffComp_ShapeshiftCore.Gizmos.cs
    }
}
