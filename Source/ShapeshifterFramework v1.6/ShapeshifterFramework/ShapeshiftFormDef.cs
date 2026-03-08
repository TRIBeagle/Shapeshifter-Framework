// ShapeshifterFramework | Root | ShapeshiftFormDef.cs
// 목적 : 변신 폼(Form)의 모든 동작을 정의하는 최상위 XML 데이터 구조체(Schema).
// 용도 : 외형 스케일/오프셋, 렌더링 숨김/대체 규칙, 스탯 및 능력치 보정, 변신 요구 조건, 부가 효과(능력/헤디프/FX/사운드), 타 모드(HAR/FA) 호환성 등 폼에 관한 모든 정적(Static) 데이터를 보관함.
// 주의 : 이 클래스의 데이터는 XML 로드 시 확정되며, 런타임 게임 플레이 도중 수정(오염)되지 않도록 철저히 읽기 전용(Read-only) 데이터로 취급해야 함.

using RimWorld;
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

    // 조건 컨디션 모드
    public enum RequirementMatchMode { All, Any }

    // Ability 생성 모드
    // Auto : 프레임워크가 AbilityDef를 자동 생성 (기본값)
    // Custom : 모더가 직접 작성한 AbilityDef를 linkedAbility로 연결
    // None : Ability를 생성하지 않음 (약물/아이템/외부 코드 전용)
    public enum AbilityMode { Auto, Custom, None }

    // 변신 시 기존 장비 처리 모드(의복/무기 각자 지정)
    public enum GearHandling { Keep, Inventory, Drop }

    // 착용/장착 금지 정책(기본 Auto = GearHandling에 묶음)
    public enum EquipLockMode { Auto, Locked, Unlocked }

    // verb별 UI 메타
    public class VerbGizmoOption
    {
        public string label;          // verb 명령 라벨(없으면 verbProps.label)
        public string desc;           // verb 명령 설명(없으면 기본)
        public string toggleLabel;    // 토글 버튼 라벨(없으면 label 사용)
        public string toggleDesc;     // 토글 버튼 설명(없으면 기본)
        public string iconPath;       // 지정하면 v.UIIcon 대신 이 아이콘 사용(선택)
        public bool? autoAttackDefault; // 자동공격 토글 초기값. null이면 첫 번째 ranged verb만 ON, 나머지 OFF
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

    /// <summary>변신 폼 Def. 외형/필터/요건/버튼/지속/부여물 일괄 관리.</summary>
    public class ShapeshiftFormDef : Def
    {
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

        // 변신 시 기존 장비 처리(폼별): 의복/무기 각각
        public GearHandling apparelOnTransform = GearHandling.Keep;
        public GearHandling weaponsOnTransform = GearHandling.Keep;

        // 착용/장착 금지 정책(기본 Auto = GearHandling에 묶음)
        public EquipLockMode apparelEquipLock = EquipLockMode.Auto;
        public EquipLockMode weaponEquipLock = EquipLockMode.Auto;

        // 변신 시 소환해서 강제로 입힐 전용 의류 목록 (해제 시 자동 파괴)
        public List<ThingDef> spawnApparelOnTransform = new List<ThingDef>();

        // 변신 시 소환해서 강제로 들려줄 전용 무기 목록 (해제 시 자동 파괴)
        public List<ThingDef> spawnWeaponOnTransform = new List<ThingDef>();

        // 소환되는 의류/무기의 재질 (예: ThingDefOf.Plasteel).
        public ThingDef spawnApparelStuff;
        public ThingDef spawnWeaponStuff;

        // 소환된 전용 의류/무기와 부위가 겹쳐서 강제로 벗어야 하는 의복 처리
        public GearHandling conflictingGearHandling = GearHandling.Inventory;


        // 폼 전용 렌더 노드(해당 폼 활성 시에만 추가)
        public List<PawnRenderNodeProperties> renderNodeProperties;

        // 타입 오버라이드(선택)
        public BodyTypeDef bodyType;
        public HeadTypeDef headType;

        // 수치 변경
        public List<StatModifier> statOffsets;
        public List<StatModifier> statFactors;
        public List<PawnCapacityModifier> capMods;

        // ── 변신 요건(카테고리 내부는 ALL-of, 카테고리 집계는 requirementsMode 적용)
        public List<ThingDef> requiredItems;
        public List<ThingDef> requiredApparels;
        public List<ThingDef> requiredWeapons;
        public List<HediffDef> requiredHediffs;

        // ──조건 집계와 무관, 항상 선행 필터
        public List<ThingDef> allowedRaces;
        public List<ThingDef> disallowedRaces;
        [MayRequire("Ludeon.RimWorld.Anomaly")] public List<MutantDef> allowedMutants;
        [MayRequire("Ludeon.RimWorld.Anomaly")] public List<MutantDef> disallowedMutants;
        [MayRequire("Ludeon.RimWorld.Biotech")] public List<XenotypeDef> allowedXenotypes;
        [MayRequire("Ludeon.RimWorld.Biotech")] public List<XenotypeDef> disallowedXenotypes;
        public List<string> allowedFromForms; // defName 리스트

        // ── 카테고리 집계 모드(기본 All)
        public RequirementMatchMode? requirementsMode; // 기본 All

        // 변신 중 부여
        public List<HediffAddEntry> addHediffs;
        public List<AbilityDef> addAbilities;

        // ── 추가 Verb/Tool 정의 및 대체 플래그 ──
        // verbs : 변신 폼에서 사용할 VerbProperties 목록(원거리/근접 모두 가능)
        // tools : 변신 폼에서 사용할 Tool 목록(근접툴)
        // replaceNativeVerbs : true면 원래 Pawn의 Verb들을 무시하고 이 폼의 verbs만 사용
        // replaceNativeTools : true면 Pawn의 ThingDef.tools를 임시 교체(해제 시 원복)
        public List<VerbProperties> verbs;
        public List<Tool> tools;
        public bool? replaceNativeVerbs;
        public bool? replaceNativeTools;

        public List<VerbGizmoOption> verbGizmoOptions; // verbs 순서에 맞춰 매칭

        // ── 변신 시 특정 작업 불가(폼별)
        public List<WorkTypeDef> disabledWorkTypesOnTransform;

        // WorkTags 기반 일괄 차단(예: Violent, Caring 등)
        public WorkTags disabledWorkTagsOnTransform = WorkTags.None;

        // ── 이념 관련 외모 노출 계열 억제(폼별)
        public bool suppressIdeologyUncoveredThoughts = true; // 기본 on: 하의/상의/머리/얼굴 노출 사상 비활성

        // ── [VFX/SFX: 변신 시작/해제] 폼별 이펙트·사운드 (원샷 중심)
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

        // Ability 모드 (Auto: 자동생성, Custom: 모더 AbilityDef 연결, None: 약물/아이템 전용)
        public AbilityMode abilityMode = AbilityMode.Auto;
        // Auto 모드: 자동생성된 AbilityDef 참조 / Custom 모드: 모더가 지정한 AbilityDef
        [Unsaved(false)]
        public AbilityDef linkedAbility;

        // 버튼/기타
        public string gizmoIconPathEnter;   // 변신 버튼 아이콘 (Auto 모드에서 Ability 아이콘으로도 사용)
        public string gizmoIconPathRevert;  // 해제 버튼 아이콘
        public int? durationTicks = null;      // 지속 틱(null=무제한)
        public bool canRevertVoluntarily = true; // false면 유저가 기즈모로 해제 불가(강제 변신용)

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

        // 혈흔/스미어
        public ThingDef bloodDef;
        public ThingDef bloodSmearDef;
        public FleshTypeDef fleshType;

        // 런타임 자동 생성된 스탯 헤디프 (세이브 불필요, 매 시작 재생성)
        [Unsaved(false)]
        public HediffDef generatedStatHediff;

        // HAR 옵션
        [MayRequire("erdelf.HumanoidAlienRaces")] public bool showHarAddons = false;

        // Facial Animation 옵션
        [MayRequire("Nals.FacialAnimation")] public string faHeadTypeDef;
        [MayRequire("Nals.FacialAnimation")] public string faEyeballTypeDef;
        [MayRequire("Nals.FacialAnimation")] public string faLidTypeDef;
        [MayRequire("Nals.FacialAnimation")] public string faBrowTypeDef;
        [MayRequire("Nals.FacialAnimation")] public string faMouthTypeDef;
        [MayRequire("Nals.FacialAnimation")] public string faSkinTypeDef;
        [MayRequire("Nals.FacialAnimation")] public ColorInt? faEyeColor;
        [MayRequire("Nals.FacialAnimation")] public ColorInt? faEyeColor2;

        /// <summary>Def 로드 완료 후 호출. 동적 HediffDef/AbilityDef를 DefDatabase에 등록.</summary>
        public override void ResolveReferences()
        {
            base.ResolveReferences();
            ShapeshifterFramework.Utilities.ShapeshiftStatHediffGenerator.TryGenerateFor(this);
            ShapeshifterFramework.Utilities.ShapeshiftAbilityGenerator.TryGenerateFor(this);

            // Custom 모드 검증
            if (abilityMode == AbilityMode.Custom && linkedAbility == null)
            {
                Log.Error($"[SSF] ShapeshiftFormDef '{defName}' has abilityMode=Custom but linkedAbility is null. Falling back to None.");
                abilityMode = AbilityMode.None;
            }
        }
    }
}
