// ShapeshiftFormDef.cs
// 목적: 변신 폼(스케일/오프셋/부위 제어/숨김 규칙/스탯·캐퍼 보정/노드 추가) Def.
// 용도: XML에서 각 폼별 동작을 한 곳에서 정의.
// 주의: renderHideApparel*/renderHideGene* 특수값 "All" 지원. renderNodeProperties는 폼 활성 시에만 추가.

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
        public string replacementTexPath = null;

        public PartOverrideOption male = null;   // 선택: 남성 전용
        public PartOverrideOption female = null; // 선택: 여성 전용
    }

    // 조건 컨디션 모드
    public enum RequirementMatchMode { All, Any }

    // 변신 시 기존 장비 처리 모드(의복/무기 각자 지정)
    public enum GearHandling { None, Inventory, Drop }

    // 착용/장착 금지 정책(기본 Auto = GearHandling에 묶음)
    public enum EquipLockMode { Auto, Always, Never }

    // verb별 UI 메타
    public class VerbGizmoOption
    {
        public string label;          // verb 명령 라벨(없으면 verbProps.label)
        public string desc;           // verb 명령 설명(없으면 기본)
        public string toggleLabel;    // 토글 버튼 라벨(없으면 label 사용)
        public string toggleDesc;     // 토글 버튼 설명(없으면 기본)
        public string iconPath;       // 지정하면 v.UIIcon 대신 이 아이콘 사용(선택)
        public bool? autoAttackDefault; // 토글 기본값(null이면 On)
    }

    // AddedPart 정책
    public enum AddedPartPolicy
    {
        ForceAdd,     // 조건 무시하고 강제 설치
        OnlyIfMissing // 해당 파츠가 결손(Missing)일 때만 설치
    }

    // Hediff 엔트리(부위/그룹/전신 + Severity + 정책)
    public class HediffAddEntry
    {
        public HediffDef hediff;                    // 필수
        public BodyPartDef targetPart;              // 선택: 지정 시 그 BodyPartDef의 모든 파츠(좌우 포함)
        public List<BodyPartGroupDef> targetGroups; // 선택: 그룹들
        public float? severity;                     // 선택: 지정 시 SetSeverity
        public AddedPartPolicy addedPartPolicy = AddedPartPolicy.OnlyIfMissing;
    }

    /// <summary>
    /// 변신 폼 Def(외형/필터/요건/버튼/지속/부여물 일괄 관리)
    /// </summary>
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

        // ▼ 의상 숨김: layer/defName (특수값: "All")
        public List<string> renderHideApparelLayers;
        public List<string> renderHideApparelDefNames;
        public List<string> renderShowApparelLayers;
        public List<string> renderShowApparelDefNames;

        // ▼ 장비(무기) 숨김: weaponTags/defName (특수값: "All")
        public List<string> renderHideWeaponTags;
        public List<string> renderHideWeaponDefNames;
        public List<string> renderShowWeaponTags;
        public List<string> renderShowWeaponDefNames;

        // ▼ 유전자 그래픽 숨김: exclusionTags/defName 목록 (특수값: "All")
        public List<string> renderHideGeneExclusionTags;
        public List<string> renderHideGeneDefNames;
        public List<string> renderShowGeneExclusionTags;
        public List<string> renderShowGeneDefNames;

        // 변신 시 기존 장비 처리(폼별): 의복/무기 각각
        public GearHandling apparelOnTransform = GearHandling.None;
        public GearHandling weaponsOnTransform = GearHandling.None;

        // 착용/장착 금지 정책(기본 Auto = GearHandling에 묶음)
        public EquipLockMode apparelEquipLock = EquipLockMode.Auto;
        public EquipLockMode weaponEquipLock = EquipLockMode.Auto;

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
        public List<GeneDef> requiredGenes;
        public List<ThingDef> requiredItems;
        public List<ThingDef> requiredApparels;
        public List<ThingDef> requiredWeapons;
        public List<AbilityDef> requiredAbilities;
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

        // 버튼/기타
        public string gizmoIconPathEnter;   // 변신 버튼 아이콘
        public string gizmoIconPathRevert;  // 해제 버튼 아이콘
        public int? durationTicks = null;      // 지속 틱(null=무제한)

        // 보이스
        public SoundDef soundCall;
        public SoundDef soundWounded;
        public SoundDef soundDeath;

        // 혈흔/스미어
        public ThingDef bloodDef;
        public ThingDef bloodSmearDef;
        public FleshTypeDef fleshType;

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
    }
}
