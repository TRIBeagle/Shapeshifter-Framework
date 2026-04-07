// ShapeshifterFramework | Root | ShapeshiftFormDef.cs
// 목적 : 변신 폼(Form)의 비주얼·장비·도구·사운드·VFX 등 정적 데이터를 정의하는 최상위 XML Def.
// 용도 : 모더가 XML로 폼을 정의하면, HediffComp_ShapeshiftCore가 이 데이터를 읽어 변신을 적용/해제.
//        스탯 보정은 HediffDef의 stages(statOffsets/statFactors/capMods)에서 정의.
//        HediffDef → FormDef 매핑은 HediffCompProperties_ShapeshiftCore.formDef로 설정 (단방향).
// 주의 : XML 로드 시 확정되는 읽기 전용 데이터 시트/템플릿. 런타임에 수정 금지.

using RimWorld;
using ShapeshifterFramework.Utilities;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ShapeshifterFramework
{
    // 파츠 제어 모드(기본/숨김/텍스처 대체)
    public enum PartControlMode { Default, Hidden, Replace }

    // 파츠 교체 옵션: male/female가 있으면 공통보다 우선, null이면 폴백
    public class PartOverrideOption
    {
        public PartControlMode mode = PartControlMode.Default;

        // 텍스처 교체
        public string replacementTexPath = null;
        public string swimmingReplacementTexPath = null; // 선택: 물/수영 시 텍스처 교체용(미사용이면 null)

        // 색상
        public Color? color = null; // 선택: 교체 시 색상 보정(없으면 흰색)
        public Color? swimmingColor = null; // 선택: 수영 텍스처 사용 시 색상 보정(없으면 color 폴백)

        // 셰이더 타입(DefName). 예: Cutout, CutoutComplex, CutoutWithOverlay, CutoutSkin, Transparent 등
        // - null이면 노드 기본 셰이더(바디=Cutout, 헤어=CutoutHair 등) 사용
        public string shaderTypeDefName = null;

        // 수영 텍스처를 실제로 선택했을 때 사용할 셰이더 타입(DefName)
        // - null이면 shaderTypeDefName 폴백
        // - 둘 다 null이고 바디 수영 텍스처면 Utility에서 Transparent로 안전 폴백(바닐라 수영 에셋 대응)
        public string swimmingShaderTypeDefName = null;

        // 바닥 그림자(ellipse) 오버라이드: 바디에만 의미 있음.
        // - 입력하면 해당 ShadowData로 대체
        // - null이면 바닐라(종족 bodyGraphicData.shadowData / specialShadowData) 유지
        public Vector3? shadowVolume = null;
        public Vector3? shadowOffset = null;

        // 성별 전용
        public PartOverrideOption male = null;
        public PartOverrideOption female = null;
    }

    // sustain 조건 집계 모드
    public enum SustainMode { All, Any }

    // 변신 시 기존 장비 처리 모드(의복/무기 각자 지정)
    public enum GearHandling { Keep, Inventory, Drop }

    // 착용/장착 금지 정책(기본 Auto = GearHandling에 묶음)
    public enum EquipLockMode { Auto, Locked, Unlocked }

    // verb별 UI 메타 — verbLabel로 매칭 (미지정 시 인덱스 폴백)
    public class VerbGizmoOption
    {
        public string verbLabel;      // 매칭할 verb의 label (필수 권장). 미지정 시 리스트 인덱스로 폴백
        public string label;          // verb 명령 라벨(없으면 verbProps.label)
        public string description;     // verb 명령 설명(없으면 기본)
        public string toggleLabel;    // 토글 버튼 라벨(없으면 label 사용)
        public string toggleDescription; // 토글 버튼 설명(없으면 기본)
        public string iconPath;       // 지정하면 v.UIIcon 대신 이 아이콘 사용(선택)

        /// <summary>이 verb 사용 시 차감할 변신 잔여 틱. 0이면 비용 없음. 버스트 무기는 버스트당 1회 차감.</summary>
        public int durationCostTicks;

        /// <summary>이 verb 사용 시 추가할 신경열(Neural Heat). 0이면 비용 없음. 로열티 DLC 비활성 시 무시. 버스트당 1회.</summary>
        public float entropyCost;
    }

    // AddedPart 정책
    public enum AddedPartPolicy
    {
        ForceAdd,         // 완전히 덮어쓰기 (기계 파괴, 결손 복원 후 강제 부착)
        StrictFleshOnly,  // 생살만 변신 (결손이거나 인공장기가 있으면 변신 안 함)
        RegrowFleshOnly   // 재생하며 변신 (결손은 복원 후 부착하지만, 인공장기가 있으면 냅둠)
    }

    // Hediff 엔트리(부위/그룹/전신 + Severity + 정책)
    public class HediffAddEntry
    {
        public HediffDef hediff;                    // 필수
        public BodyPartDef targetPart;              // 선택: 지정 시 그 BodyPartDef의 모든 파츠(좌우 포함)
        public List<BodyPartGroupDef> targetGroups; // 선택: 그룹들
        public float? severity;                     // 선택: 지정 시 SetSeverity
        public AddedPartPolicy addedPartPolicy = AddedPartPolicy.ForceAdd;
    }

    /// <summary>변신 폼 Def. 비주얼/장비/도구/사운드/VFX 등 폼 전체 정의.</summary>
    public class ShapeshiftFormDef : Def
    {
        // ── linkedHediff 삭제됨: HediffDef → FormDef 매핑은 HediffCompProperties_ShapeshiftCore.formDef로 단방향 설정.
        //    FormDef는 순수 데이터 시트이며, 어떤 HediffDef와 연결될지는 HediffDef 쪽에서 결정.

        #region 적용 대상 제한 — 카테고리 bool (바닐라 TargetingParameters 패턴)

        /// <summary>인간형(Humanlike) 폰 허용. HAR 종족 포함. 기본 true.</summary>
        public bool allowHumanlike = true;

        /// <summary>동물(Animal) 폰 허용. 기본 true.</summary>
        public bool allowAnimals = true;

        /// <summary>메카노이드 폰 허용. 기본 false (차단).</summary>
        public bool allowMechanoids = false;

        /// <summary>뮤턴트 폰 허용. 기본 false (차단). true 시 formAllowedMutants로 세부 제한 가능.</summary>
        public bool allowMutants = false;

        #endregion

        #region 적용 대상 제한 — 세부 리스트 (카테고리 허용 범위 안에서 추가 제한)

        /// <summary>이 폼을 적용받을 수 있는 종족(ThingDef) 화이트리스트. null/빈 목록이면 카테고리 bool 범위 전부 허용.</summary>
        public List<ThingDef> formAllowedRaces;

        /// <summary>이 폼을 적용받을 수 없는 종족(ThingDef) 블랙리스트. 화이트리스트보다 우선.</summary>
        public List<ThingDef> formDisallowedRaces;

        /// <summary>허용할 뮤턴트(MutantDef) 화이트리스트. allowMutants=true일 때만 의미. null/빈 목록이면 모든 뮤턴트 허용.</summary>
        [MayRequire("Ludeon.RimWorld.Anomaly")]
        public List<MutantDef> formAllowedMutants;

        /// <summary>차단할 뮤턴트(MutantDef) 블랙리스트. 화이트리스트보다 우선.</summary>
        [MayRequire("Ludeon.RimWorld.Anomaly")]
        public List<MutantDef> formDisallowedMutants;

        #endregion

        #region 렌더링 — 스케일/오프셋/파츠

        // 스케일/오프셋(렌더 보정용)
        public float? bodyDrawScale;   // 몸 전체 스케일 배수 (예: 5.0이면 5배) 비우면 1
        public float? headDrawScale;   // 헤드 추가 배수 (바디 스케일에 곱해짐) 비우면 1
        public Vector2? bodyOffset = null;          // 바디 위치 보정
        public Vector2? headOffset = null;          // 헤드 위치 보정

        // 포트레잇(정보창)에서만 전체 크기 배수. 비우면 1
        public float? portraitDrawScale;

        // 부위별 제어
        public PartOverrideOption body = new PartOverrideOption();
        public PartOverrideOption head = new PartOverrideOption();
        public PartOverrideOption hair = new PartOverrideOption();
        public PartOverrideOption beard = new PartOverrideOption();
        public PartOverrideOption tattooBody = new PartOverrideOption();
        public PartOverrideOption tattooHead = new PartOverrideOption();

        // 폼 전용 렌더 노드(해당 폼 활성 시에만 추가)
        public List<PawnRenderNodeProperties> renderNodeProperties;

        // 타입 오버라이드(선택)
        public BodyTypeDef bodyType;
        public HeadTypeDef headType;

        // 기본 컬러 오버라이드(선택, 텍스처 Replace 시 무시됨)
        public Color? hairColor;
        public Color? skinColor;

        #endregion

        #region 렌더링 — 숨김/표시 필터 (의상/무기/유전자/헤디프)

        // 의상 숨김: layer/defName (특수값: "All")
        public List<string> renderHideApparelLayers;
        public List<string> renderHideApparelDefNames;
        public List<string> renderShowApparelLayers;
        public List<string> renderShowApparelDefNames;

        // 장비(무기) 숨김: weaponTags/defName (특수값: "All")
        public List<string> renderHideWeaponTags;
        public List<string> renderHideWeaponDefNames;
        public List<string> renderShowWeaponTags;
        public List<string> renderShowWeaponDefNames;

        // 유전자 그래픽 숨김: exclusionTags/defName 목록 (특수값: "All")
        public List<string> renderHideGeneExclusionTags;
        public List<string> renderHideGeneDefNames;
        public List<string> renderShowGeneExclusionTags;
        public List<string> renderShowGeneDefNames;

        // 헤디프 그래픽 숨김: defName 목록 (특수값: "All")
        public List<string> renderHideHediffDefNames;
        public List<string> renderShowHediffDefNames;

        #endregion

        #region 장비 처리 (의복/무기 관리, 소환, 잠금)

        // 변신 시 기존 장비 처리(폼별): 의복/무기 각각
        public GearHandling apparelOnTransform = GearHandling.Keep;
        public GearHandling weaponsOnTransform = GearHandling.Keep;

        // 착용/장착 금지 정책(기본 Auto = GearHandling에 묶음)
        public EquipLockMode apparelEquipLock = EquipLockMode.Auto;
        public EquipLockMode weaponEquipLock = EquipLockMode.Auto;

        // 변신 시 소환해서 강제로 입힐 전용 의류 목록 (해제 시 자동 파괴)
        public List<ThingDef> spawnApparelOnTransform;

        // 변신 시 소환해서 강제로 들려줄 전용 무기 목록 (해제 시 자동 파괴)
        public List<ThingDef> spawnWeaponOnTransform;

        // 소환되는 의류/무기의 재질 (예: ThingDefOf.Plasteel).
        public ThingDef spawnApparelStuff;
        public ThingDef spawnWeaponStuff;

        // 소환된 전용 의류/무기와 부위가 겹쳐서 강제로 벗어야 하는 의복 처리
        public GearHandling conflictingGearHandling = GearHandling.Inventory;

        #endregion

        #region 유지 조건 (sustain) — 조건 깨지면 자동 해제

        public List<ThingDef> sustainApparels;   // 이 의류를 착용 유지해야 함
        public List<ThingDef> sustainWeapons;    // 이 무기를 장비 유지해야 함
        public List<HediffDef> sustainHediffs;   // 이 헤디프가 유지되어야 함
        [MayRequire("Ludeon.RimWorld.Biotech")]
        public List<GeneDef> sustainGenes;       // 이 유전자가 유지되어야 함 (Biotech)
        public SustainMode? sustainMode;         // 기본 All: 모두 충족 / Any: 하나라도 충족

        #endregion

        #region 변신 중 부여 (Hediff/능력)

        public List<HediffAddEntry> addHediffs;
        public List<AbilityDef> addAbilities;

        #endregion

        #region 전투 — Verb/Tool/데미지소스

        // verbs : 변신 폼에서 사용할 VerbProperties 목록(원거리/근접 모두 가능)
        // tools : 변신 폼에서 사용할 Tool 목록(근접툴)
        // replaceNativeVerbs : true면 원래 Pawn의 Verb들을 무시하고 이 폼의 verbs만 사용
        // replaceNativeTools : true면 Pawn의 ThingDef.tools를 임시 교체(해제 시 원복)
        //
        // [Verb 선택 흐름] — 아래 3개 Harmony 패치가 협력:
        //   1. Patch_VerbTracker_InitVerbsFromZero  : 폼 교체 시 tools를 NativeVerb 풀에 주입/제거 (구성 시점)
        //   2. Patch_Pawn_TryGetAttackVerb           : 공격 시 원거리 우선 → 근접 폴백으로 최적 verb 선정 (선택 시점)
        //   3. Patch_Pawn_MeleeVerbs_TryGetMeleeVerb : 바닐라 근접 경로 안전망 + power 비교 (폴백 시점)
        //   공유 헬퍼: Patch_Pawn_TryGetAttackVerb.FindBestFormMelee()
        public List<VerbProperties> verbs;
        public List<Tool> tools;
        public bool? replaceNativeVerbs;
        public bool? replaceNativeTools;

        public List<VerbGizmoOption> verbGizmoOptions; // verbs 순서에 맞춰 매칭

        #endregion

        #region 작업 제한

        public List<WorkTypeDef> disabledWorkTypesOnTransform;

        // WorkTags 기반 일괄 차단(예: Violent, Caring 등) — XML에서 콤마로 OR 결합: <disabledWorkTagsOnTransform>Violent, Crafting</disabledWorkTagsOnTransform>
        public WorkTags disabledWorkTagsOnTransform = WorkTags.None;

        #endregion

        #region 이념 연동

        public bool suppressIdeologyUncoveredThoughts = true; // 기본 on: 하의/상의/머리/얼굴 노출 사상 비활성

        /// <summary>이 폼이 대표하는 동물 종족. 이데올로기 숭배(성스러운) 동물과 매칭하여 기분 보너스 부여.</summary>
        [MayRequire("Ludeon.RimWorld.Ideology")]
        public ThingDef linkedSacredAnimalDef;

        #endregion

        #region VFX/SFX — 변신 시작/해제 (원샷)

        public SoundDef transformEnterSound;     // 변신 시작
        public SoundDef transformExitSound;      // 변신 해제
        public EffecterDef transformEnterEffecter;
        public EffecterDef transformExitEffecter;

        // Fleck(초경량 파티클): count==0이면 미사용
        public FleckDef transformEnterFleck;
        public int transformEnterFleckCount = 0;
        public float transformEnterFleckScale = 1f;

        public FleckDef transformExitFleck;
        public int transformExitFleckCount = 0;
        public float transformExitFleckScale = 1f;

        // 타이밍·스팸 방지
        public int transformEnterFxDelayTicks = 0;  // Enter FX 재생 지연
        public int transformExitFxDelayTicks = 0;   // Exit  FX 재생 지연
        public int transformFxCooldownTicks = 30;   // 동일 단계 쿨다운(틱)

        #endregion

        #region VFX — 앰비언트 (변신 중 지속)

        /// <summary>변신 중 매 틱 EffectTick으로 유지되는 지속형 Effecter (오라, 연기 등).</summary>
        public EffecterDef ambientEffecter;
        /// <summary>변신 중 주기적으로 스폰되는 일회성 Fleck (스파크, 불꽃 등).</summary>
        public FleckDef ambientFleck;
        /// <summary>ambientFleck 스폰 간격 (틱). 기본 60 = 1초.</summary>
        public int ambientFleckIntervalTicks = 60;
        /// <summary>ambientFleck 스케일. 기본 1.0.</summary>
        public float ambientFleckScale = 1f;

        /// <summary>변신 중 주기적으로 재생되는 사운드. ambientFleck과 같은 간격(ambientFleckIntervalTicks)으로 재생.</summary>
        public SoundDef ambientSound;

        #endregion

        #region 변신 해제 시 부산물

        /// <summary>변신 해제 시 드랍할 아이템 목록 (허물, 결정 등).</summary>
        public List<ThingDefCountClass> revertDrops;
        /// <summary>변신 해제 시 부여할 hediff 목록. HediffAddEntry로 부위/severity/정책 지정 가능. 프레임워크가 추적/제거하지 않음 (바닐라 수명).</summary>
        public List<HediffAddEntry> revertAddHediffs;

        #endregion

        #region UI/기즈모/지속시간

        public string gizmoIconPathRevert;  // 해제 버튼 아이콘
        public int? durationTicks = null;      // 지속 틱(null=무제한)
        public bool canRevertVoluntarily = true; // false면 유저가 기즈모로 해제 불가(강제 변신용)
        public bool revertOnDowned = false;      // true면 의식 상실(Downed) 시 변신 자동 해제

        #endregion

        #region 사운드 — 보이스/근접 전투

        // 보이스
        public SoundDef soundCall;
        public SoundDef soundWounded;
        public SoundDef soundDeath;
        public SoundDef soundAngry;
        public SoundDef soundEating;

        // 근접 전투 사운드
        public SoundDef soundMeleeHitPawn;
        public SoundDef soundMeleeHitBuilding;
        public SoundDef soundMeleeMiss;

        #endregion

        #region 혈흔/살점

        public ThingDef bloodDef;
        public ThingDef bloodSmearDef;
        public FleshTypeDef fleshType;

        #endregion

        #region 내부 — 사전 컴파일 렌더 필터 (ResolveReferences에서 빌드, 세이브 제외)
        [Unsaved] internal CompiledFilterSet _hideApparelLayers;
        [Unsaved] internal CompiledFilterSet _hideApparelDefNames;
        [Unsaved] internal CompiledFilterSet _showApparelLayers;
        [Unsaved] internal CompiledFilterSet _showApparelDefNames;
        [Unsaved] internal CompiledFilterSet _hideWeaponTags;
        [Unsaved] internal CompiledFilterSet _hideWeaponDefNames;
        [Unsaved] internal CompiledFilterSet _showWeaponTags;
        [Unsaved] internal CompiledFilterSet _showWeaponDefNames;
        [Unsaved] internal CompiledFilterSet _hideGeneExclusionTags;
        [Unsaved] internal CompiledFilterSet _hideGeneDefNames;
        [Unsaved] internal CompiledFilterSet _showGeneExclusionTags;
        [Unsaved] internal CompiledFilterSet _showGeneDefNames;
        [Unsaved] internal CompiledFilterSet _hideHediffDefNames;
        [Unsaved] internal CompiledFilterSet _showHediffDefNames;

        #endregion

        #region ResolveReferences / ConfigErrors

        public override void ResolveReferences()
        {
            base.ResolveReferences();

            // 렌더 필터 와일드카드 사전 컴파일 — 렌더 루프에서 문자열 파싱 제거
            _hideApparelLayers = CompiledFilterSet.Compile(renderHideApparelLayers);
            _hideApparelDefNames = CompiledFilterSet.Compile(renderHideApparelDefNames);
            _showApparelLayers = CompiledFilterSet.Compile(renderShowApparelLayers);
            _showApparelDefNames = CompiledFilterSet.Compile(renderShowApparelDefNames);
            _hideWeaponTags = CompiledFilterSet.Compile(renderHideWeaponTags);
            _hideWeaponDefNames = CompiledFilterSet.Compile(renderHideWeaponDefNames);
            _showWeaponTags = CompiledFilterSet.Compile(renderShowWeaponTags);
            _showWeaponDefNames = CompiledFilterSet.Compile(renderShowWeaponDefNames);
            _hideGeneExclusionTags = CompiledFilterSet.Compile(renderHideGeneExclusionTags);
            _hideGeneDefNames = CompiledFilterSet.Compile(renderHideGeneDefNames);
            _showGeneExclusionTags = CompiledFilterSet.Compile(renderShowGeneExclusionTags);
            _showGeneDefNames = CompiledFilterSet.Compile(renderShowGeneDefNames);
            _hideHediffDefNames = CompiledFilterSet.Compile(renderHideHediffDefNames);
            _showHediffDefNames = CompiledFilterSet.Compile(renderShowHediffDefNames);
        }

        /// <summary>Def 참조 유효성 검증. 로드 시 깨진 참조 조기 검출.</summary>
        public override IEnumerable<string> ConfigErrors()
        {
            __warnings = null; // 다중 호출 시 중복 방지
            foreach (var e in base.ConfigErrors())
                yield return e;

            // 능력 참조
            if (addAbilities != null)
            {
                for (int i = 0; i < addAbilities.Count; i++)
                    if (addAbilities[i] == null)
                        yield return $"addAbilities[{i}]: null AbilityDef reference";
            }

            // hediff 참조
            if (addHediffs != null)
            {
                for (int i = 0; i < addHediffs.Count; i++)
                {
                    var entry = addHediffs[i];
                    if (entry == null) { yield return $"addHediffs[{i}]: null entry"; continue; }
                    if (entry.hediff == null) yield return $"addHediffs[{i}]: null HediffDef reference";
                }
            }

            // sustain 참조
            foreach (var e in CheckNullRefs(sustainApparels, "sustainApparels")) yield return e;
            foreach (var e in CheckNullRefs(sustainWeapons, "sustainWeapons")) yield return e;
            foreach (var e in CheckNullRefs(sustainHediffs, "sustainHediffs")) yield return e;
            foreach (var e in CheckNullRefs(sustainGenes, "sustainGenes")) yield return e;

            // 종족/뮤턴트 제한 참조
            foreach (var e in CheckNullRefs(formAllowedRaces, "formAllowedRaces")) yield return e;
            foreach (var e in CheckNullRefs(formDisallowedRaces, "formDisallowedRaces")) yield return e;
            foreach (var e in CheckNullRefs(formAllowedMutants, "formAllowedMutants")) yield return e;
            foreach (var e in CheckNullRefs(formDisallowedMutants, "formDisallowedMutants")) yield return e;

            // 소환 장비 참조 + 카테고리 검증
            if (spawnApparelOnTransform != null)
                for (int i = 0; i < spawnApparelOnTransform.Count; i++)
                {
                    if (spawnApparelOnTransform[i] == null) yield return $"spawnApparelOnTransform[{i}]: null ThingDef reference";
                    else if (!spawnApparelOnTransform[i].IsApparel) yield return $"spawnApparelOnTransform[{i}]: {spawnApparelOnTransform[i].defName} is not apparel";
                }
            if (spawnWeaponOnTransform != null)
                for (int i = 0; i < spawnWeaponOnTransform.Count; i++)
                {
                    if (spawnWeaponOnTransform[i] == null) yield return $"spawnWeaponOnTransform[{i}]: null ThingDef reference";
                    else if (!spawnWeaponOnTransform[i].IsWeapon) yield return $"spawnWeaponOnTransform[{i}]: {spawnWeaponOnTransform[i].defName} is not a weapon";
                }

            // 작업 제한 참조
            foreach (var e in CheckNullRefs(disabledWorkTypesOnTransform, "disabledWorkTypesOnTransform")) yield return e;

            // 스케일 범위 검증
            if (bodyDrawScale.HasValue && bodyDrawScale.Value <= 0f)
                yield return $"bodyDrawScale must be > 0, got {bodyDrawScale.Value}";
            if (headDrawScale.HasValue && headDrawScale.Value <= 0f)
                yield return $"headDrawScale must be > 0, got {headDrawScale.Value}";

            // 전투 검증: 바닐라 verb/tool 대체 시 대체물 없으면 경고
            if (replaceNativeVerbs == true && (verbs == null || verbs.Count == 0)
                && (spawnWeaponOnTransform == null || spawnWeaponOnTransform.Count == 0))
                yield return "replaceNativeVerbs=true but no verbs or spawnWeaponOnTransform defined — pawn will have no ranged attack";
            if (replaceNativeTools == true && (tools == null || tools.Count == 0))
                yield return "replaceNativeTools=true but no tools defined — pawn will have no melee attack";

            // 무기 스폰 2개 이상 경고 (듀얼 윌드 모드 없으면 마지막만 장착)
            if (spawnWeaponOnTransform != null && spawnWeaponOnTransform.Count > 1)
                yield return $"spawnWeaponOnTransform has {spawnWeaponOnTransform.Count} weapons — vanilla only supports 1 primary weapon (extras will be dropped)";

            // PartOverride: Replace 모드인데 텍스처 없으면 경고
            CheckPartReplace(body, "body", ref __warnings);
            CheckPartReplace(head, "head", ref __warnings);
            CheckPartReplace(hair, "hair", ref __warnings);
            CheckPartReplace(beard, "beard", ref __warnings);
            CheckPartReplace(tattooBody, "tattooBody", ref __warnings);
            CheckPartReplace(tattooHead, "tattooHead", ref __warnings);
            if (__warnings != null)
                foreach (var w in __warnings) yield return w;

            // 뮤턴트 필터: allowMutants=false인데 formAllowedMutants 설정됨
            if (!allowMutants && formAllowedMutants != null && formAllowedMutants.Count > 0)
                yield return "formAllowedMutants is set but allowMutants=false — mutant list will be ignored";

            // 해제 부산물 참조
            if (revertDrops != null)
                for (int i = 0; i < revertDrops.Count; i++)
                    if (revertDrops[i]?.thingDef == null) yield return $"revertDrops[{i}]: null ThingDef reference";
            if (revertAddHediffs != null)
                for (int i = 0; i < revertAddHediffs.Count; i++)
                {
                    var entry = revertAddHediffs[i];
                    if (entry == null) { yield return $"revertAddHediffs[{i}]: null entry"; continue; }
                    if (entry.hediff == null) yield return $"revertAddHediffs[{i}]: null HediffDef reference";
                }
        }

        // ConfigErrors 임시 경고 리스트 (yield return 내 ref 사용 불가로 우회)
        [Unsaved] private List<string> __warnings;

        /// <summary>PartOverride가 Replace인데 텍스처 없으면 경고.</summary>
        private void CheckPartReplace(PartOverrideOption opt, string name, ref List<string> warnings)
        {
            if (opt == null || opt.mode != PartControlMode.Replace) return;
            if (string.IsNullOrEmpty(opt.replacementTexPath))
            {
                if (warnings == null) warnings = new List<string>();
                warnings.Add($"{name}.mode=Replace but replacementTexPath is empty — will render vanilla texture, not hidden");
            }
        }

        /// <summary>리스트 내 null 참조 검증 헬퍼.</summary>
        private static IEnumerable<string> CheckNullRefs<T>(List<T> list, string fieldName) where T : class
        {
            if (list == null) yield break;
            for (int i = 0; i < list.Count; i++)
                if (list[i] == null) yield return $"{fieldName}[{i}]: null reference";
        }

        #endregion
    }
}
