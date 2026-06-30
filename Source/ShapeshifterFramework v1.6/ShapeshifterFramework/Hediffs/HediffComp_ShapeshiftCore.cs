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

namespace ShapeshifterFramework.Hediffs
{
    /// <summary>변신 라이프사이클 관리 HediffComp. HediffDef → FormDef 매핑 + 상태 관리.</summary>
    public partial class HediffComp_ShapeshiftCore : HediffComp
    {
        #region Properties 접근

        /// <summary>XML 속성 접근.</summary>
        public HediffCompProperties_ShapeshiftCore Props => (HediffCompProperties_ShapeshiftCore)props;

        #endregion

        /// <summary>초기화 중이거나 변신 중에는 바닐라 severity 기반 자동 소멸을 차단.
        /// 외부(약물/다른 모드)가 severity를 줄여도 RemoveForm 경유 없이 hediff가 사라지는 것을 방지.</summary>
        public override bool CompShouldRemove
        {
            get
            {
                if (needsInit) return false;
                if (isTransformed) return false; // 변신 중 severity 감소에 의한 자동 제거 차단
                return base.CompShouldRemove;
            }
        }

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
        private List<Hediff> tmpHediffsLoad = null;
        private HashSet<string> tmpPrevApIds = null;
        private HashSet<string> tmpPrevWpIds = null;
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

        /// <summary>변신 남은 시간을 틱 단위로 연장(양수) 또는 단축(음수).
        /// 단축 시 0 이하가 되면 다음 틱에 자동 해제됨.</summary>
        /// <param name="ticks">연장(+) 또는 단축(-) 틱 수.</param>
        /// <param name="allowBeyondMax">false면 원래 폼 최대 시간을 초과하지 않도록 제한.</param>
        public void ExtendDuration(int ticks, bool allowBeyondMax = true)
        {
            if (!isTransformed) return;
            var resolved = ResolvedDurationTicks;
            if (!resolved.HasValue || resolved.Value <= 0) return; // 영구 변신은 무시
            int newVal = transformTimer + ticks;
            if (!allowBeyondMax && newVal > resolved.Value)
                newVal = resolved.Value;
            transformTimer = newVal > 0 ? newVal : 0;
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

        /// <summary>Hediff 제거 시 정리. RemoveForm이 미호출된 경우 대비.
        /// 사망/외부 모드에 의해 hediff가 직접 제거될 수 있으므로 반드시 방어적으로 정리.</summary>
        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();

            var pawn = Pawn;
            if (isTransformed)
            {
                try { RemoveForm(); }
                catch (Exception ex)
                {
                    // RemoveForm 실패 시에도 핵심 상태 강제 정리 — 좀비 변신 방지
                    Log.Error($"[SSF] CompPostPostRemoved: RemoveForm failed, forcing cleanup: {ex}");
                    currentForm = null;
                    if (pawn != null) ShapeshiftRegistry.Unregister(pawn);
                    if (pawn != null) ShapeshiftRuntimeCaches.ClearFor(pawn);
                }
            }
            else
            {
                // 미등록 상태의 방어적 레지스트리 해제 (needsInit 중 제거 등)
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

                try { pawn.Drawer?.renderer?.SetAllGraphicsDirty(); }
                catch (Exception ex) { Log.Warning($"[SSF] OnPawnSpawned SetAllGraphicsDirty error: {ex.Message}"); }
                try { PortraitsCache.SetDirty(pawn); }
                catch (Exception ex) { Log.Warning($"[SSF] OnPawnSpawned PortraitsCache error: {ex.Message}"); }
                try { GlobalTextureAtlasManager.TryMarkPawnFrameSetDirty(pawn); }
                catch (Exception ex) { Log.Warning($"[SSF] OnPawnSpawned TextureAtlas error: {ex.Message}"); }
            }
        }

        #endregion

        // 매 틱 처리(CompPostTick), sustain 조건, 인스펙터 문자열 → HediffComp_ShapeshiftCore.Tick.cs

        #region 변신 적용/해제

        /// <summary>폼 적용.</summary>
        public void ApplyForm(ShapeshiftFormDef form) { ApplyForm(form, null); }

        /// <summary>폼 적용. sources 지정 시 변신 유발 아이템 추적.</summary>
        public void ApplyForm(ShapeshiftFormDef form, List<Thing> sources)
        {
            var pawn = Pawn;
            if (pawn == null || form == null) return;
            if (_isApplyingOrRemoving) return; // 재진입 방지
            _isApplyingOrRemoving = true;
            try
            {

            string basicBlockReason = ShapeshiftEligibility.CanTransformBasicReason(pawn, form);
            if (basicBlockReason != null)
            {
                try { Messages.Message(basicBlockReason, pawn, MessageTypeDefOf.RejectInput, false); } catch (System.Exception ex) { Log.Warning("[SSF] ApplyForm message display failed: " + ex.Message); }
                // 좀비 hediff 방지: 변신 실패 시 hediff 자체를 제거
                if (parent != null) parent.Severity = 0f;
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

                // RemoveForm.finally가 severity=0으로 설정하여 바닐라 자동 제거를 유도하지만,
                // 여기서는 동일 hediff에 새 폼을 계속 적용하므로 severity를 복원해야 함.
                if (parent != null)
                    parent.Severity = parent.def.initialSeverity > 0f ? parent.def.initialSeverity : 1f;
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

            GrantAbilities(pawn, form);
            GrantHediffs(pawn, form);

            // 상태 적용
            currentForm = form;

            ShapeshiftTransformFxUtility.PlayEnterFx(pawn, form);

            var resolvedDuration = ResolvedDurationTicks;
            if (resolvedDuration.HasValue && resolvedDuration.Value > 0)
                transformTimer = resolvedDuration.Value;

            // 앰비언트 VFX 초기화
            ambientEffecterInstance = null;
            ambientFleckNextTick = 0;

            ApplyAppearanceOverrides(pawn, form);

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

        /// <summary>폼의 addAbilities를 폰에 부여하고 tempAddedAbilities에 기록.</summary>
        private void GrantAbilities(Pawn pawn, ShapeshiftFormDef form)
        {
            tempAddedAbilities.Clear();
            if (form.addAbilities == null || form.addAbilities.Count == 0 || pawn.abilities == null) return;
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

        /// <summary>폼의 addHediffs를 폰에 부여하고 임시 리스트에 기록.</summary>
        private void GrantHediffs(Pawn pawn, ShapeshiftFormDef form)
        {
            tempAddedHediffs.Clear();
            tempAddedHediffsDefCache.Clear();
            tempPartRestoreRecords.Clear();
            if (form.addHediffs == null || form.addHediffs.Count == 0 || pawn.health == null) return;
            ShapeshiftApplyHediffUtility.ApplyHediffEntries(
                pawn, form.addHediffs,
                tempAddedHediffs, tempAddedHediffsDefCache, tempPartRestoreRecords,
                prevDefCache: tempAddedHediffsDefCache
            );
        }

        /// <summary>폼의 체형/머리형/헤어색/피부색 오버라이드를 적용.</summary>
        private void ApplyAppearanceOverrides(Pawn pawn, ShapeshiftFormDef form)
        {
            if (pawn.story == null) return;
            if (form.bodyType != null) pawn.story.bodyType = form.bodyType;
            if (form.headType != null) pawn.story.headType = form.headType;
            if (form.hairColor.HasValue) pawn.story.HairColor = form.hairColor.Value;
            if (form.skinColor.HasValue) pawn.story.skinColorOverride = form.skinColor.Value;
        }

        /// <summary>현재 폼 해제.</summary>
        public void RemoveForm()
        {
            var pawn = Pawn;
            if (pawn == null) return;
            if (_isApplyingOrRemoving) return; // 재진입 방지
            _isApplyingOrRemoving = true;
            var oldForm = currentForm;
            try
            {
                DestroyGeneratedGear();
                if (this.sourceItems != null) this.sourceItems.Clear();

                RemoveGrantedAbilities(pawn);
                RemoveGrantedHediffs(pawn);
                RestoreBodyParts(pawn);

                ShapeshiftDiagnostics.Info($"Revert: restored {tempPartRestoreRecords.Count} part(s)");
                tempAddedHediffs.Clear();
                tempAddedHediffsDefCache.Clear();
                tempPartRestoreRecords.Clear();

                transformTimer = 0;

                RestoreAppearance(pawn);

                // 자동 재착용
                ShapeshifterFrameworkSettings st = ShapeshifterFrameworkMod.Settings;
                if (st == null || st.autoReequipFromInventory || st.autoReequipFromGround)
                    TryReequipPreviousGear(pawn);

                // VerbTracker 해제
                shapeshiftVerbTracker = null;
                _verbKeyCache = null;
                verbAutoToggle.Clear();

                if (oldForm != null)
                    ShapeshiftTransformFxUtility.PlayExitFx(pawn, oldForm);

                CleanupAmbientVfx();
                SpawnRevertDrops(pawn);
                ApplyRevertHediffs(pawn, oldForm);

                currentForm = null;

                // 레지스트리 해제
                ShapeshiftRegistry.Unregister(pawn);

                // 캐시 정리
                ShapeshiftRuntimeCaches.ClearFor(pawn);

                RefreshPawn(pawn, this);

                // 이벤트 발행
                if (oldForm != null)
                    ShapeshiftCoreUtility.FireFormRemoved(pawn, oldForm);
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

        /// <summary>폼 전용 소환 장비(apparel/weapon) 파괴.</summary>
        private void DestroyGeneratedGear()
        {
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
        }

        /// <summary>변신 시 부여한 능력(Ability)을 회수.</summary>
        private void RemoveGrantedAbilities(Pawn pawn)
        {
            if (pawn.abilities == null || tempAddedAbilities.Count == 0) return;
            for (int i = 0; i < tempAddedAbilities.Count; i++)
            {
                AbilityDef ad = tempAddedAbilities[i];
                if (ad != null) pawn.abilities.RemoveAbility(ad);
            }
            tempAddedAbilities.Clear();
        }

        /// <summary>변신 시 부여한 hediff를 회수 — 1차 참조 제거 + 2차 def 기준 카운팅 제거.</summary>
        private void RemoveGrantedHediffs(Pawn pawn)
        {
            if (pawn.health == null) return;

            // 1차: 참조 기반 직접 제거
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
            if (tempAddedHediffsDefCache == null || tempAddedHediffsDefCache.Count == 0) return;

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

        /// <summary>변신 전 원래 파츠 상태 복원 (죽은 폰은 건너뜀).</summary>
        private void RestoreBodyParts(Pawn pawn)
        {
            if (pawn.health == null || pawn.Dead) return;

            for (int i = 0; i < tempPartRestoreRecords.Count; i++)
            {
                var rec = tempPartRestoreRecords[i];
                if (rec == null || rec.Part == null) continue;

                if (!rec.WasMissingBefore)
                {
                    try { pawn.health.RestorePart(rec.Part); }
                    catch (Exception ex) { Log.Warning($"[SSF] RestorePart failed for '{rec.Part.Label}': {ex}"); }
                }

                if (rec.PreExistingAdded == null || rec.PreExistingAdded.Count == 0
                    || pawn.health.hediffSet.PartIsMissing(rec.Part))
                    continue;

                ReinstatePreExistingHediffs(pawn, rec);
            }
        }

        /// <summary>파트에 이전 존재했던 hediff(added part 등)를 복원.</summary>
        private void ReinstatePreExistingHediffs(Pawn pawn, ShapeshiftPartRestoreRecord rec)
        {
            for (int k = 0; k < rec.PreExistingAdded.Count; k++)
            {
                var prev = rec.PreExistingAdded[k];
                if (prev?.Def == null) continue;

                BodyPartRecord targetPart = ResolveTargetPart(pawn, rec.Part, prev.PartDef);
                if (targetPart == null) continue;

                var reinst = pawn.health.AddHediff(prev.Def, targetPart, null);
                if (reinst != null && prev.Severity.HasValue)
                {
                    try { reinst.Severity = prev.Severity.Value; }
                    catch (Exception ex) { Log.Warning($"[SSF] Restore Severity failed for '{prev.Def.defName}': {ex}"); }
                }
            }
        }

        /// <summary>복원 대상 파트 결정. partDef가 다르면 하위 파트에서 탐색.</summary>
        private BodyPartRecord ResolveTargetPart(Pawn pawn, BodyPartRecord basePart, BodyPartDef partDef)
        {
            if (partDef == null || partDef == basePart.def)
                return basePart;

            if (pawn.RaceProps?.body == null) return null;
            var allParts = pawn.RaceProps.body.AllParts;
            for (int i = 0; i < allParts.Count; i++)
            {
                var x = allParts[i];
                if (x.def == partDef && !pawn.health.hediffSet.PartIsMissing(x) && IsPartChildOf(x, basePart))
                    return x;
            }
            return null;
        }

        /// <summary>체형/머리형/컬러를 변신 전 상태로 복원.</summary>
        private void RestoreAppearance(Pawn pawn)
        {
            if (pawn.story == null) return;
            if (originalBodyType != null) pawn.story.bodyType = originalBodyType;
            if (originalHeadType != null) pawn.story.headType = originalHeadType;
            if (hasSavedColors)
            {
                if (originalHairColor.HasValue) pawn.story.HairColor = originalHairColor.Value;
                pawn.story.skinColorOverride = originalSkinColor;
                hasSavedColors = false;
            }
        }

        /// <summary>앰비언트 VFX 인스턴스 정리.</summary>
        private void CleanupAmbientVfx()
        {
            if (ambientEffecterInstance == null) return;
            try { ambientEffecterInstance.Cleanup(); }
            catch (Exception ex) { Log.Warning($"[SSF] Effecter.Cleanup failed: {ex}"); }
            ambientEffecterInstance = null;
        }

        /// <summary>해제 시 잔해(Thing) 드랍.</summary>
        private void SpawnRevertDrops(Pawn pawn)
        {
            var drops = ResolvedRevertDrops;
            if (drops == null || drops.Count == 0 || !pawn.Spawned || pawn.MapHeld == null) return;

            for (int i = 0; i < drops.Count; i++)
            {
                var entry = drops[i];
                if (entry?.thingDef == null || entry.count <= 0) continue;
                // stackLimit 초과 시 분할 배치 — 단일 Thing에 한계 초과 stackCount 설정은 바닐라 규약 위반
                int remaining = entry.count;
                int limit = System.Math.Max(1, entry.thingDef.stackLimit);
                while (remaining > 0)
                {
                    int batch = System.Math.Min(remaining, limit);
                    Thing thing = ThingMaker.MakeThing(entry.thingDef);
                    thing.stackCount = batch;
                    GenPlace.TryPlaceThing(thing, pawn.PositionHeld, pawn.MapHeld, ThingPlaceMode.Near);
                    remaining -= batch;
                }
            }
        }

        /// <summary>해제 시 hediff 부여 — Props 오버라이드 우선, 없으면 FormDef 폴백.</summary>
        private void ApplyRevertHediffs(Pawn pawn, ShapeshiftFormDef oldForm)
        {
            if (pawn.health == null || pawn.Dead) return;

            // Props 오버라이드 (HediffAddEntry) 우선
            var addHediffEntries = ResolvedRevertAddHediffs;
            List<HediffAddEntry> entries = null;

            if (addHediffEntries != null && addHediffEntries.Count > 0)
                entries = addHediffEntries;
            else if (oldForm?.revertAddHediffs != null && oldForm.revertAddHediffs.Count > 0)
                entries = oldForm.revertAddHediffs;

            if (entries == null) return;

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry?.hediff == null) continue;
                Hediff h = pawn.health.AddHediff(entry.hediff);
                if (h != null && entry.severity.HasValue)
                {
                    try { h.Severity = entry.severity.Value; }
                    catch (Exception ex) { Log.Warning($"[SSF] revertAddHediffs severity set failed: {ex}"); }
                }
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

        /// <summary>VerbTracker.AllVerbs getter 접근으로 lazy init을 강제 트리거.</summary>
        private static void ForceVerbInit(VerbTracker vt)
        {
            // AllVerbs getter는 내부적으로 verbsNeedReinitOnLoad 플래그 시 InitVerbsFromZero를 호출
            // discard 변수 대신 명시적 헬퍼로 의도를 명확히 표현
            if (vt.AllVerbs == null) { /* getter 사이드이펙트만 필요 */ }
        }


        /// <summary>런타임 캐시 등록(사운드/혈흔/FleshType).</summary>
        public static void ApplyRuntimeCaches(Pawn pawn, ShapeshiftFormDef form)
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

            // ── 바닐라 캐시/그래픽 갱신 ──
            try
            {
                pawn.health?.capacities?.Notify_CapacityLevelsDirty();
                pawn.health?.hediffSet?.DirtyCache();
                pawn.Drawer?.renderer?.SetAllGraphicsDirty();
                PortraitsCache.SetDirty(pawn);
                GlobalTextureAtlasManager.TryMarkPawnFrameSetDirty(pawn);
                pawn.Notify_DisabledWorkTypesChanged();
            }
            catch (Exception ex) { Log.Warning($"[SSF] RefreshPawn (cache/graphics) error: {ex}"); }

            // ── 바닐라 Verb 재초기화 ──
            try
            {
                if (forceReinitPawnVerbs && pawn.verbTracker != null)
                {
                    pawn.verbTracker.VerbsNeedReinitOnLoad();
                    // AllVerbs getter 접근으로 lazy init 트리거 (의도적 사이드이펙트)
                    ForceVerbInit(pawn.verbTracker);
                }
            }
            catch (Exception ex) { Log.Warning($"[SSF] RefreshPawn (vanilla verbs) error: {ex}"); }

            // ── 변신 전용 Verb 재초기화 ──
            try
            {
                if (comp != null && resetShapeshiftVerbs)
                {
                    comp.shapeshiftVerbTracker = null;
                    comp._verbKeyCache = null;
                    // ShapeshiftVerbTracker getter 접근으로 lazy init 트리거 (의도적 사이드이펙트)
                    var vt = comp.ShapeshiftVerbTracker;
                    if (vt != null) ForceVerbInit(vt);
                }

                if (comp != null && comp.isTransformed && comp.currentForm != null && forceReinitPawnVerbs)
                {
                    var form = comp.currentForm;
                    bool replaceNative = form.replaceNativeTools ?? false;

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
            catch (Exception ex) { Log.Warning($"[SSF] RefreshPawn (shapeshift verbs) error: {ex}"); }

            // ── UI 선택 갱신 ──
            try
            {
                if (refreshSelection && Find.Selector != null && Find.Selector.IsSelected(pawn))
                {
                    Find.Selector.Deselect(pawn);
                    Find.Selector.Select(pawn, playSound: false, forceDesignatorDeselect: false);
                }
            }
            catch (Exception ex) { Log.Warning($"[SSF] RefreshPawn (UI selection) error: {ex}"); }

            if (comp != null)
                comp.verbTickErrorLogged = false;
        }

        #endregion

        // 저장/로드(CompExposeData) → HediffComp_ShapeshiftCore.ExposeData.cs
        // 기즈모 생성(GetGizmosExtra) → HediffComp_ShapeshiftCore.Gizmos.cs
    }
}
