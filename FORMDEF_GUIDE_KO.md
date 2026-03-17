# Shapeshifter Framework — FormDef 제작 가이드

> 변신 폼 제작을 위한 완전 레퍼런스. `ShapeshiftFormDef.cs` 및 `HediffCompProperties_ShapeshiftCore.cs`의 실제 C# 필드를 반영합니다.

모든 필드는 별도 표기 없는 한 **선택사항**입니다. 생략하면 바닐라 기본값이 적용됩니다.

---

## 아키텍처 개요

> **v2 HediffComp 마이그레이션**으로 변신 시스템의 진입점과 데이터 구조가 근본적으로 변경되었습니다.

### 핵심 변경 요약

| 항목 | 이전 | 현재 |
|------|------|------|
| 변신 진입점 | FormDef 직접 참조 | **HediffDef 부여** (HediffComp_ShapeshiftCore 포함) |
| FormDef 역할 | 데이터 + 일부 로직 | **순수 데이터 시트/템플릿** (비주얼, 장비, 도구, 사운드, VFX만) |
| 스탯/능력치 보정 | FormDef.linkedHediff → HediffDef stages | **HediffDef stages** (statOffsets, statFactors, capMods)에서 직접 정의 |
| 매핑 방향 | FormDef.linkedHediff → HediffDef (양방향) | **HediffDef → FormDef 단방향** (HediffCompProperties_ShapeshiftCore.formDef) |
| N:1 매핑 | 불가 | **지원** — 같은 FormDef를 여러 HediffDef가 참조 가능 |
| CompShapeshifter | ThingDef에 패치 필요 (HumanPatch.xml) | **삭제** — ThingDef 패치 불필요 |
| FA/HAR 필드 | FormDef 내 직접 필드 | **DefModExtension으로 분리** (FAFormExtension, HARFormExtension) |
| revertAddHediffs | List\<HediffDef\> | **List\<HediffAddEntry\>** (severity/부위 지원) |

### 데이터 흐름

```
[트리거] → HediffDef 부여 → HediffComp_ShapeshiftCore
                                    ↓
                              CompPostPostAdd → 지연 초기화(needsInit)
                                    ↓
                              첫 Tick → ApplyForm(formDef)
                                    ↓
                           FormDef에서 비주얼/장비/도구 적용
                           HediffDef stages에서 스탯/능력치 적용
```

### 바닐라 GiveHediff 호환

바닐라 방식으로 HediffDef를 부여하기만 하면 자동으로 변신이 트리거됩니다.
`CompPostPostAdd`에서 기존 변신 hediff가 있으면 자동 제거하므로 중첩 걱정은 없습니다:

> **권장:** 약물/프로젝타일 트리거에는 바닐라 `IngestionOutcomeDoer_GiveHediff` 대신 `IngestionOutcomeDoer_Shapeshift`와 `Projectile_Polymorph`를 사용하세요. SSF 전용 클래스는 `successChance`(저항 판정)와 `ApplyShift()` 전체 흐름을 제공합니다.

```csharp
// C# 코드에서
pawn.health.AddHediff(HediffMaker.MakeHediff(hediffDef, pawn));
// → CompPostPostAdd → needsInit = true → 첫 Tick에서 ApplyForm 자동 실행
```

```xml
<!-- 바닐라 XML 이벤트에서 GiveHediff로도 변신 가능 -->
<hediffDef>MyShapeshiftHediffDef</hediffDef>
```

### N:1 매핑 — 같은 비주얼, 다른 스탯

여러 HediffDef가 같은 FormDef를 참조할 수 있습니다. 비주얼은 동일하되 스탯만 다른 변형을 만들 때 유용합니다:

```xml
<!-- 늑대 폼 비주얼 (FormDef는 하나) -->
<ShapeshifterFramework.ShapeshiftFormDef ParentName="SSF_BaseForm_Animal">
  <defName>WolfForm</defName>
  <label>늑대 폼</label>
  <body>
    <mode>Replace</mode>
    <replacementTexPath>Things/Pawn/Animal/Wolf/Wolf</replacementTexPath>
  </body>
  <replaceNativeTools>true</replaceNativeTools>
  <tools>
    <li>
      <label>teeth</label>
      <capacities><li>Bite</li></capacities>
      <power>12</power>
      <cooldownTime>1.5</cooldownTime>
      <linkedBodyPartsGroup>Teeth</linkedBodyPartsGroup>
    </li>
  </tools>
</ShapeshifterFramework.ShapeshiftFormDef>

<!-- HediffDef A: 일반 늑대 (이동속도 +1.5) -->
<HediffDef ParentName="SSF_ShapeshiftFormBase">
  <defName>Wolf_Normal</defName>
  <label>늑대 (일반)</label>
  <stages>
    <li><statOffsets><MoveSpeed>1.5</MoveSpeed></statOffsets></li>
  </stages>
  <comps>
    <li Class="ShapeshifterFramework.Hediffs.HediffCompProperties_ShapeshiftCore">
      <formDef>WolfForm</formDef>
    </li>
  </comps>
</HediffDef>

<!-- HediffDef B: 알파 늑대 (이동속도 +3.0, 방어력 +0.5) — 같은 비주얼 -->
<HediffDef ParentName="SSF_ShapeshiftFormBase">
  <defName>Wolf_Alpha</defName>
  <label>알파 늑대</label>
  <stages>
    <li>
      <statOffsets>
        <MoveSpeed>3.0</MoveSpeed>
        <ArmorRating_Sharp>0.5</ArmorRating_Sharp>
      </statOffsets>
    </li>
  </stages>
  <comps>
    <li Class="ShapeshifterFramework.Hediffs.HediffCompProperties_ShapeshiftCore">
      <formDef>WolfForm</formDef>
    </li>
  </comps>
</HediffDef>
```

### 이벤트 시스템

외부 모드에서 변신 이벤트를 구독할 수 있습니다:

```csharp
// C# — 이벤트 구독
ShapeshiftCoreUtility.OnFormApplied += (pawn, formDef) => {
    Log.Message($"{pawn.LabelShort}이(가) {formDef.defName}으로 변신");
};

ShapeshiftCoreUtility.OnFormRemoved += (pawn, formDef) => {
    Log.Message($"{pawn.LabelShort}이(가) {formDef.defName}에서 해제");
};
```

---

## 빠른 시작

변신을 정의하려면 **HediffDef** + **FormDef** 두 가지가 필요합니다.

### 최소 구성 예시

```xml
<Defs>
  <!-- 1단계: FormDef — 비주얼 정의 -->
  <ShapeshifterFramework.ShapeshiftFormDef>
    <defName>MyForm</defName>
    <label>나의 폼</label>
    <body>
      <mode>Replace</mode>
      <replacementTexPath>Things/Pawn/MyCreature/MyCreature</replacementTexPath>
    </body>
    <head><mode>Hidden</mode></head>
  </ShapeshifterFramework.ShapeshiftFormDef>

  <!-- 2단계: HediffDef — 변신 마커 + 스탯 + FormDef 연결 -->
  <HediffDef ParentName="SSF_ShapeshiftFormBase">
    <defName>MyForm_Hediff</defName>
    <label>나의 폼</label>
    <stages>
      <li>
        <statOffsets><MoveSpeed>1.5</MoveSpeed></statOffsets>
      </li>
    </stages>
    <comps>
      <li Class="ShapeshifterFramework.Hediffs.HediffCompProperties_ShapeshiftCore">
        <formDef>MyForm</formDef>
        <durationTicks>30000</durationTicks>
      </li>
    </comps>
  </HediffDef>
</Defs>
```

> **중요:** `linkedHediff` 필드는 삭제되었습니다. 매핑은 항상 HediffDef → FormDef 단방향입니다.

---

## 추상 부모 HediffDef

`SSF_BaseHediffs.xml`에 변신 HediffDef의 공통 설정을 담은 추상 부모가 제공됩니다:

### SSF_ShapeshiftFormBase

모든 변신 HediffDef의 최상위 추상 부모. 다음 설정을 공통 적용합니다:

| 속성 | 값 |
|------|-----|
| `hediffClass` | `ShapeshifterFramework.Hediffs.Hediff_ShapeshiftForm` |
| `isBad` | false |
| `tendable` | false |
| `makesAlert` | false |
| `makesSickThought` | false |
| `initialSeverity` / `maxSeverity` | 1 |
| `defaultLabelColor` | (0.8, 0.7, 1.0) |
| comps | HediffCompProperties_ShapeshiftCore (formDef 비워둠) |

사용법:
```xml
<HediffDef ParentName="SSF_ShapeshiftFormBase">
  <defName>MyTransform_Hediff</defName>
  <label>나의 변신</label>
  <stages>
    <li><statOffsets><MoveSpeed>2.0</MoveSpeed></statOffsets></li>
  </stages>
  <comps>
    <li Class="ShapeshifterFramework.Hediffs.HediffCompProperties_ShapeshiftCore">
      <formDef>MyTransformForm</formDef>
    </li>
  </comps>
</HediffDef>
```

### SSF_GenericShapeshiftForm

범용/디버그용 HediffDef. `formDef`를 미지정하고 런타임에 `comp.SetFormDef()`로 폼을 동적 지정합니다. 디버그 액션, 자동 검증 등에서 사용됩니다.

---

## 추상 기본 FormDef

`SSF_BaseForms.xml`에 3가지 기본 부모 폼이 제공됩니다:

| 부모 | 장비 처리 | 그래픽 숨김 | 용도 |
|------|-----------|------------|------|
| `SSF_BaseForm_Animal` | Inventory | 모든 파츠·그래픽 숨김 | 동물형 완전 교체 |
| `SSF_BaseForm_Humanoid` | Keep | 오버헤드 의류만 숨김 | 인간형 + 추가 요소 |
| `SSF_BaseForm_Armored` | Keep | 숨김 없음 | 장비 중심 폼 |

사용법: `<ShapeshifterFramework.ShapeshiftFormDef ParentName="SSF_BaseForm_Animal">`

---

## HediffDef 필드 레퍼런스

### 스탯/능력치 보정

스탯 보정은 **HediffDef의 stages**에서 정의합니다. FormDef에는 스탯 필드가 없습니다.

```xml
<HediffDef ParentName="SSF_ShapeshiftFormBase">
  <defName>MyForm_Hediff</defName>
  <label>나의 폼</label>
  <stages>
    <li>
      <!-- 스탯 절대값 보정 -->
      <statOffsets>
        <MoveSpeed>2.5</MoveSpeed>
        <ArmorRating_Sharp>0.4</ArmorRating_Sharp>
      </statOffsets>
      <!-- 스탯 배수 보정 -->
      <statFactors>
        <MeleeHitChance>1.20</MeleeHitChance>
        <IncomingDamageFactor>0.8</IncomingDamageFactor>
      </statFactors>
      <!-- 능력치 보정 -->
      <capMods>
        <li><capacity>Moving</capacity><postFactor>1.30</postFactor></li>
        <li><capacity>Manipulation</capacity><setMax>0.2</setMax></li>
        <li><capacity>Sight</capacity><postFactor>1.10</postFactor></li>
      </capMods>
    </li>
  </stages>
  <comps>
    <li Class="ShapeshifterFramework.Hediffs.HediffCompProperties_ShapeshiftCore">
      <formDef>MyForm</formDef>
    </li>
  </comps>
</HediffDef>
```

### HediffCompProperties_ShapeshiftCore — 행동 오버라이드

HediffDef의 `<comps>`에 `HediffCompProperties_ShapeshiftCore`를 부착합니다. `formDef`로 대상 FormDef를 지정하고, 나머지 행동 필드는 **null이면 FormDef 기본값을 사용**합니다.

| 필드 | 타입 | 기본값 | 설명 |
|------|------|--------|------|
| `formDef` | ShapeshiftFormDef | null | **핵심.** 이 HediffDef가 적용할 변신 폼. null이면 런타임에 SetFormDef()로 지정. |
| `defaultSuccessChance` | float | 1 | 변신 성공 확률 (0~1). 바닐라 GiveHediff 경로에서도 적용. `ApplyShift()` 호출 시 명시적 successChance가 우선. |
| `durationTicks` | int? | null | 변신 지속 틱. null이면 FormDef.durationTicks 사용. |
| `canRevertVoluntarily` | bool? | null | 기즈모로 해제 가능 여부. null이면 FormDef.canRevertVoluntarily 사용. |
| `revertOnDowned` | bool? | null | Downed 시 자동 해제. null이면 FormDef.revertOnDowned 사용. |
| `sustainApparels` | List\<ThingDef\> | null | 유지 필요 의류. null이면 FormDef.sustainApparels 사용. |
| `sustainWeapons` | List\<ThingDef\> | null | 유지 필요 무기. null이면 FormDef.sustainWeapons 사용. |
| `sustainHediffs` | List\<HediffDef\> | null | 유지 필요 hediff. null이면 FormDef.sustainHediffs 사용. |
| `sustainGenes` | List\<GeneDef\> | null | 유지 필요 유전자 (Biotech). null이면 FormDef.sustainGenes 사용. |
| `sustainMode` | SustainMode? | null | 유지 조건 집계 모드. null이면 FormDef.sustainMode 사용. |
| `revertDrops` | List\<ThingDefCountClass\> | null | 해제 시 드랍. null이면 FormDef.revertDrops 사용. |
| `revertAddHediffs` | List\<HediffAddEntry\> | null | 해제 시 부여 hediff. null이면 FormDef.revertAddHediffs 사용. |

> **오버라이드 패턴:** CompProperties에 값을 명시하면 FormDef의 같은 필드를 **오버라이드**합니다. null(미지정)이면 FormDef 기본값을 사용합니다. 이를 통해 같은 FormDef를 사용하면서 HediffDef별로 지속시간, 해제 조건 등을 다르게 설정할 수 있습니다.

```xml
<!-- 같은 WolfForm이지만 HediffDef마다 다른 지속시간 -->
<HediffDef ParentName="SSF_ShapeshiftFormBase">
  <defName>Wolf_Short</defName>
  <label>단시간 늑대</label>
  <stages><li><statOffsets><MoveSpeed>1.0</MoveSpeed></statOffsets></li></stages>
  <comps>
    <li Class="ShapeshifterFramework.Hediffs.HediffCompProperties_ShapeshiftCore">
      <formDef>WolfForm</formDef>
      <durationTicks>5000</durationTicks>       <!-- 오버라이드: 짧은 지속 -->
      <canRevertVoluntarily>false</canRevertVoluntarily> <!-- 오버라이드: 해제 불가 -->
    </li>
  </comps>
</HediffDef>

<HediffDef ParentName="SSF_ShapeshiftFormBase">
  <defName>Wolf_Long</defName>
  <label>장시간 늑대</label>
  <stages><li><statOffsets><MoveSpeed>2.0</MoveSpeed></statOffsets></li></stages>
  <comps>
    <li Class="ShapeshifterFramework.Hediffs.HediffCompProperties_ShapeshiftCore">
      <formDef>WolfForm</formDef>
      <durationTicks>60000</durationTicks>      <!-- 오버라이드: 1일 지속 -->
      <revertOnDowned>true</revertOnDowned>     <!-- 오버라이드: downed 시 해제 -->
    </li>
  </comps>
</HediffDef>
```

---

## FormDef 필드 레퍼런스

FormDef는 변신의 **비주얼, 장비, 도구, 사운드, VFX** 데이터를 정의하는 순수 데이터 시트입니다. 스탯 보정은 포함하지 않습니다.

### 1. 기본 정보

| 필드 | 타입 | 설명 |
|------|------|------|
| `defName` | string | **필수.** 고유 ID. |
| `label` | string | 게임 내 표시 이름. |
| `description` | string | 툴팁 및 설명. |

### 2. 종족 / 뮤턴트 필터

**대상자**(폼을 받는 쪽) 필터. 모든 트리거 경로(어빌리티, 약물, 스크롤, 투사체)에서 적용됩니다.

| 필드 | 타입 | 설명 |
|------|------|------|
| `formAllowedRaces` | List\<ThingDef\> | 이 종족만 변신 가능. 비우면 제한 없음. |
| `formDisallowedRaces` | List\<ThingDef\> | 이 종족은 변신 불가. allowedRaces보다 우선. |
| `formAllowedMutants` | List\<MutantDef\> | 이 뮤턴트만 변신 가능. (`MayRequire: Ludeon.RimWorld.Anomaly`) |
| `formDisallowedMutants` | List\<MutantDef\> | 이 뮤턴트는 변신 불가. (`MayRequire: Ludeon.RimWorld.Anomaly`) |

> **시전자 측** 필터(`allowedRaces`, `disallowedRaces`)는 `CompProperties_AbilityShapeshift`에서 설정합니다.

### 3. 크기 및 위치

| 필드 | 타입 | 기본값 | 설명 |
|------|------|--------|------|
| `bodyDrawScale` | float? | 1.0 | 몸 전체 크기 배수. |
| `headDrawScale` | float? | 1.0 | 머리 추가 배수 (bodyDrawScale에 곱해짐). |
| `portraitDrawScale` | float? | 1.0 | 하단 UI 포트레잇에서만 적용되는 크기. |
| `bodyOffset` | Vector2? | (0,0) | 바디 위치 보정 (X, Z). |
| `headOffset` | Vector2? | (0,0) | 헤드 위치 보정 (X, Z). |

### 4. 부위별 외형 제어

`<body>`, `<head>`, `<hair>`, `<beard>`, `<tattooBody>`, `<tattooHead>` 각각에 적용합니다.

각 태그는 `PartOverrideOption` 블록을 받습니다:

| 필드 | 설명 |
|------|------|
| `mode` | `Default` / `Hidden` / `Replace` |
| `replacementTexPath` | 교체 텍스처 경로 (`Replace` 모드 필요). |
| `swimmingReplacementTexPath` | 수영 중 사용할 텍스처. |
| `color` | 색상 틴트: `(R,G,B)` 또는 `(R,G,B,A)`. |
| `swimmingColor` | 수영 중 색상. `color`로 폴백. |
| `shaderTypeDefName` | 셰이더 오버라이드 (예: `Cutout`, `Transparent`). |
| `swimmingShaderTypeDefName` | 수영 중 셰이더. `shaderTypeDefName`으로 폴백. |
| `shadowVolume` | 그림자 타원 크기 (Vector3). **body에서만 유효.** |
| `shadowOffset` | 그림자 위치 오프셋 (Vector3). **body에서만 유효.** |
| `male` / `female` | 성별별 `PartOverrideOption` (동일 구조). |

```xml
<body>
  <mode>Replace</mode>
  <replacementTexPath>Things/Pawn/Animal/Wolf/Wolf</replacementTexPath>
  <color>(112, 82, 65)</color>
  <male>
    <replacementTexPath>Things/Pawn/Animal/Wolf/WolfMale</replacementTexPath>
  </male>
</body>
```

### 5. 그래픽 숨김 / 표시

변신 중 그래픽을 숨기거나 강제 표시합니다. `<li>All</li>`로 전체 카테고리에 적용 가능.

**의류:**
- `renderHideApparelLayers` / `renderShowApparelLayers` — 레이어별 (예: `OnSkin`, `Overhead`)
- `renderHideApparelDefNames` / `renderShowApparelDefNames` — defName별

**무기:**
- `renderHideWeaponTags` / `renderShowWeaponTags` — 무기 태그별
- `renderHideWeaponDefNames` / `renderShowWeaponDefNames` — defName별

**유전자:**
- `renderHideGeneExclusionTags` / `renderShowGeneExclusionTags`
- `renderHideGeneDefNames` / `renderShowGeneDefNames`

**헤디프:**
- `renderHideHediffDefNames` / `renderShowHediffDefNames`

> **팁:** 전부 숨기고 예외만 표시: `<renderHideApparelLayers><li>All</li></renderHideApparelLayers>` + `<renderShowApparelDefNames><li>Apparel_Cape</li></renderShowApparelDefNames>`

### 6. 장비 처리

| 필드 | 타입 | 기본값 | 설명 |
|------|------|--------|------|
| `apparelOnTransform` | GearHandling | Keep | `Keep` / `Inventory` / `Drop` |
| `weaponsOnTransform` | GearHandling | Keep | `Keep` / `Inventory` / `Drop` |
| `apparelEquipLock` | EquipLockMode | Auto | `Auto` / `Locked` / `Unlocked` — 변신 중 장비 교체 제한. |
| `weaponEquipLock` | EquipLockMode | Auto | 위와 동일 (무기). |

**소환 장비** (변신 시 생성, 해제 시 파괴):

| 필드 | 타입 | 설명 |
|------|------|------|
| `spawnApparelOnTransform` | List\<ThingDef\> | 소환 후 강제 착용할 의류. |
| `spawnWeaponOnTransform` | List\<ThingDef\> | 소환 후 강제 장비할 무기. |
| `spawnApparelStuff` | ThingDef | 소환 의류 재질 (예: `Plasteel`). |
| `spawnWeaponStuff` | ThingDef | 소환 무기 재질. |
| `conflictingGearHandling` | GearHandling | `Inventory` | 소환 장비와 겹치는 기존 장비 처리. |

### 7. 렌더 노드

이 폼 활성 중에만 표시되는 커스텀 렌더 노드 (귀, 꼬리, 날개 등). 림월드 표준 `PawnRenderNodeProperties` 사용.

```xml
<renderNodeProperties>
  <li>
    <nodeClass>PawnRenderNode_AttachmentHead</nodeClass>
    <workerClass>PawnRenderNodeWorker_FlipWhenCrawling</workerClass>
    <texPath>Things/Pawn/Humanlike/HeadAttachments/FloppyEars/FloppyEars</texPath>
    <colorType>Skin</colorType>
    <parentTagDef>Head</parentTagDef>
    <drawData>
      <defaultData><layer>70</layer></defaultData>
    </drawData>
  </li>
</renderNodeProperties>
```

### 8. 타입 및 컬러 오버라이드

| 필드 | 타입 | 설명 |
|------|------|------|
| `bodyType` | BodyTypeDef | 체형 강제 변경 (예: `Thin`, `Hulk`). |
| `headType` | HeadTypeDef | 머리 타입 강제 변경. |
| `hairColor` | Color? | 머리카락 색상. hair 모드가 `Replace`면 무시됨. |
| `skinColor` | Color? | 피부 색상. body 모드가 `Replace`면 무시됨. |

### 9. 변신 유지 조건

변신 상태를 유지하기 위해 계속 충족해야 하는 조건. 깨지면 자동 해제.

> **참고:** 이 필드들은 FormDef에도 정의할 수 있고, HediffCompProperties_ShapeshiftCore에서도 오버라이드할 수 있습니다. CompProperties에 명시하면 FormDef 값을 덮어씁니다.

| 필드 | 타입 | 설명 |
|------|------|------|
| `sustainApparels` | List\<ThingDef\> | 계속 착용해야 함. |
| `sustainWeapons` | List\<ThingDef\> | 계속 장비해야 함. |
| `sustainHediffs` | List\<HediffDef\> | 유지되어야 함. |
| `sustainGenes` | List\<GeneDef\> | 유지되어야 함 (Biotech). |
| `sustainMode` | SustainMode? | `All` (모두 충족) 또는 `Any` (하나라도 충족). |

### 10. 부여물 (헤디프 & 어빌리티)

변신 중 부여. 해제 시 자동 제거.

| 필드 | 타입 | 설명 |
|------|------|------|
| `addAbilities` | List\<AbilityDef\> | 변신 중 부여할 어빌리티. `MayRequire` 지원. |
| `addHediffs` | List\<HediffAddEntry\> | 변신 중 부여할 hediff (추적 — 해제 시 자동 제거). |

**HediffAddEntry 필드:**

| 필드 | 타입 | 설명 |
|------|------|------|
| `hediff` | HediffDef | 부여할 hediff. |
| `targetPart` | BodyPartDef | 대상 신체 부위 (일치하는 모든 파츠, 예: 양쪽 팔). |
| `targetGroups` | List\<BodyPartGroupDef\> | 대상 그룹. |
| `severity` | float? | 초기 심각도. |
| `addedPartPolicy` | AddedPartPolicy | `ForceAdd` (인공장기 파괴/결손 복원 후 부착), `StrictFleshOnly` (인공장기/결손 시 실패), `RegrowFleshOnly` (결손 복원, 인공장기 유지). |

### 11. 전투

**Verb & Tool:**

| 필드 | 타입 | 설명 |
|------|------|------|
| `verbs` | List\<VerbProperties\> | 추가 공격 (원거리/근접). |
| `tools` | List\<Tool\> | 추가 근접 도구. |
| `replaceNativeVerbs` | bool? | `true` = 기존 verb 비활성화. |
| `replaceNativeTools` | bool? | `true` = 기존 ThingDef 도구 임시 교체 (해제 시 원복). |
| `damageSourceDef` | ThingDef | 상처 라벨에 표시할 종족 (예: `Warg` → "Warg teeth"). |

**Verb 기즈모 옵션** (`verbGizmoOptions`, `verbLabel`로 verb의 `label`과 매칭):

| 필드 | 타입 | 기본값 | 설명 |
|------|------|--------|------|
| `verbLabel` | string | null | **권장.** verb의 `<label>`과 매칭 (대소문자 무시). 순서 무관. 미지정 시 인덱스 폴백. |
| `label` | string | null | Verb 명령 기즈모 라벨. 미지정 시 `verbProps.label` 사용. |
| `desc` | string | null | Verb 명령 기즈모 설명. 미지정 시 기본값 사용. |
| `toggleLabel` | string | null | 자동공격 토글 버튼 라벨. 미지정 시 `label` 사용. |
| `toggleDesc` | string | null | 자동공격 토글 버튼 설명. 미지정 시 기본값 사용. |
| `iconPath` | string | null | 커스텀 아이콘 텍스처 경로. 지정 시 verb의 `UIIcon` 대신 사용. |
자동공격 기본값: 첫 번째 원거리 verb만 ON, 나머지 OFF. 하나를 ON하면 나머지는 자동 OFF (배타적).

> **다중 선택 동작:** 여러 폰 선택 시 같은 폼+verb의 사격 기즈모(`Command_VerbTarget`)는 병합됩니다. 자동사격 토글은 다중 선택 시 숨김 — 개별 폰 선택에서 설정하세요.
>
> **모드옵션:** `showVerbAutoToggle` 비활성 시 토글 기즈모 숨김 + 자동사격 전부 OFF. 수동 명령으로만 폼 verb 사격 가능.

**작업 제한:**

| 필드 | 타입 | 기본값 | 설명 |
|------|------|--------|------|
| `disabledWorkTypesOnTransform` | List\<WorkTypeDef\> | — | 변신 중 비활성화할 작업 타입. |
| `disabledWorkTagsOnTransform` | WorkTags | None | 비활성화할 작업 태그 (예: `Violent`, `Crafting`). |
| `suppressIdeologyUncoveredThoughts` | bool | true | 장비 제거로 인한 '알몸' 무드 페널티 억제. |

### 12. 이펙트 및 사운드

**지속시간 & 해제 (FormDef 기본값):**

> **참고:** 아래 필드는 FormDef에 기본값으로 정의하되, HediffCompProperties_ShapeshiftCore에서 오버라이드할 수 있습니다. CompProperties 값이 null이 아니면 FormDef 값을 덮어씁니다.

| 필드 | 타입 | 기본값 | 설명 |
|------|------|--------|------|
| `durationTicks` | int? | null (무제한) | 지속 틱. 60000 = 인게임 1일. |
| `canRevertVoluntarily` | bool | true | `false` = 해제 기즈모 없음 (강제 변신). |
| `revertOnDowned` | bool | false | 의식 상실 시 자동 해제. |
| `revertDrops` | List\<ThingDefCountClass\> | — | 해제 시 드랍 아이템 (허물, 결정 등). |
| `revertAddHediffs` | List\<HediffAddEntry\> | — | 해제 시 부여 hediff. **비추적** — 바닐라 수명. HediffAddEntry 형식 (severity/부위 지정 가능). |

> **revertAddHediffs 변경사항:** 이전에는 `List<HediffDef>`였으나, 현재는 `List<HediffAddEntry>`로 변경되어 severity와 대상 부위를 지정할 수 있습니다.

```xml
<!-- 이전 방식 (더 이상 사용 불가) -->
<revertAddHediffs>
  <li>FibrousMechanites</li>
</revertAddHediffs>

<!-- 현재 방식 (HediffAddEntry 형식) -->
<revertAddHediffs>
  <li>
    <hediff>FibrousMechanites</hediff>
    <severity>0.5</severity>          <!-- 선택: 초기 심각도 -->
  </li>
  <li>
    <hediff>Burn</hediff>
    <targetPart>Arm</targetPart>      <!-- 선택: 대상 부위 -->
    <severity>0.3</severity>
  </li>
</revertAddHediffs>
```

**기즈모 아이콘:**
- `gizmoIconPathEnter` / `gizmoIconPathRevert` — 변신/해제 버튼 아이콘.

**변신 FX (진입/해제 시 원샷):**

| 필드 | 설명 |
|------|------|
| `transformEnterSound` / `transformExitSound` | 변신/해제 시 사운드. |
| `transformEnterEffecter` / `transformExitEffecter` | 변신/해제 시 이펙터. |
| `transformEnterFleck` / `transformExitFleck` | FleckDef 파티클. |
| `transformEnterFleckCount` / `transformExitFleckCount` | 파티클 수 (0 = 비활성). |
| `transformEnterFleckScale` / `transformExitFleckScale` | 파티클 크기 (기본 1.0). |
| `transformEnterFxDelayTicks` / `transformExitFxDelayTicks` | FX 재생 딜레이. |
| `transformFxCooldownTicks` | 동일 FX 쿨다운 (기본 30). |

**앰비언트 VFX (변신 중 지속):**

| 필드 | 타입 | 기본값 | 설명 |
|------|------|--------|------|
| `ambientEffecter` | EffecterDef | — | 매 틱 유지되는 지속형 이펙터 (오라, 연기). 해제 시 자동 정리. |
| `ambientFleck` | FleckDef | — | 주기적 스폰 플렉 (스파크, 불꽃). |
| `ambientFleckIntervalTicks` | int | 60 | 스폰 간격 (틱). |
| `ambientFleckScale` | float | 1.0 | 플렉 크기. |

### 13. 보이스 & 혈액

**보이스 (폰 음성 교체):**
- `soundCall`, `soundWounded`, `soundDeath`, `soundAngry`, `soundEating`

**근접 전투 사운드:**
- `soundMeleeHitPawn`, `soundMeleeHitBuilding`, `soundMeleeMiss`

**혈액/살점:**

| 필드 | 타입 | 설명 |
|------|------|------|
| `bloodDef` | ThingDef | 피격 시 혈흔. |
| `bloodSmearDef` | ThingDef | 기어갈 때 혈흔 스미어. |
| `fleshType` | FleshTypeDef | 살점 타입 오버라이드 (예: `Insectoid`). |

### 14. 모드 호환성 — DefModExtension

FA 및 HAR 관련 필드는 FormDef에서 분리되어 **DefModExtension**으로 이동했습니다. FormDef의 `<modExtensions>`에 추가합니다.

**HAR (Humanoid Alien Races) — HARFormExtension:**

```xml
<ShapeshifterFramework.ShapeshiftFormDef>
  <defName>MyForm</defName>
  <!-- ... 기본 필드 ... -->
  <modExtensions>
    <li Class="ShapeshifterFramework.Compat.HARFormExtension" MayRequire="erdelf.HumanoidAlienRaces">
      <showHarAddons>true</showHarAddons>  <!-- 변신 후에도 BodyAddon 표시. 기본 false -->
    </li>
  </modExtensions>
</ShapeshifterFramework.ShapeshiftFormDef>
```

**Facial Animation — FAFormExtension:**

```xml
<ShapeshifterFramework.ShapeshiftFormDef>
  <defName>MyForm</defName>
  <!-- ... 기본 필드 ... -->
  <modExtensions>
    <li Class="ShapeshifterFramework.Compat.FAFormExtension" MayRequire="Nals.FacialAnimation">
      <faHeadTypeDef>MyFA_HeadType</faHeadTypeDef>
      <faEyeballTypeDef>MyFA_Eyeball</faEyeballTypeDef>
      <faLidTypeDef>MyFA_Lid</faLidTypeDef>
      <faBrowTypeDef>MyFA_Brow</faBrowTypeDef>
      <faMouthTypeDef>MyFA_Mouth</faMouthTypeDef>
      <faSkinTypeDef>MyFA_Skin</faSkinTypeDef>
      <faEyeColor>(255, 0, 0, 255)</faEyeColor>   <!-- ColorInt -->
      <faEyeColor2>(0, 255, 0, 255)</faEyeColor2>  <!-- ColorInt -->
    </li>
  </modExtensions>
</ShapeshifterFramework.ShapeshiftFormDef>
```

**Simple Sidearms:** 자동 호환 — XML 설정 불필요. 변신 시 무기 메모리 백업, 해제 시 복원.

---

## 트리거 시스템

FormDef는 폼의 **모습**을 정의합니다. **언제/어떻게** 발동되는지는 별도 컴포넌트에서 처리합니다.

### 트리거 클래스의 hediffDef / formDefName 우선순위

모든 트리거 클래스(`CompProperties_AbilityShapeshift`, `CompProperties_UseEffect_Shapeshift`, `IngestionOutcomeDoer_Shapeshift`, `PolymorphProjectileExtension`)에서 두 가지 필드를 지원합니다:

| 필드 | 우선순위 | 설명 |
|------|----------|------|
| `hediffDef` | **우선** | HediffDef를 직접 지정. `ShapeshiftCoreUtility.ApplyShift(pawn, hediffDef)`로 변신. |
| `formDefName` | 폴백 | FormDef defName을 문자열로 지정. `hediffDef`가 null일 때만 사용. 레거시 호환용. |

> **권장:** 새 콘텐츠는 `hediffDef` 경로를 사용하세요. `formDefName`은 하위 호환을 위해 유지됩니다.

```xml
<!-- 권장: hediffDef 경로 -->
<li Class="ShapeshifterFramework.Comps.CompProperties_AbilityShapeshift">
  <hediffDef>MyForm_Hediff</hediffDef>
  <successChance>1.0</successChance>
</li>

<!-- 레거시: formDefName 폴백 -->
<li Class="ShapeshifterFramework.Comps.CompProperties_AbilityShapeshift">
  <formDefName>MyForm</formDefName>
  <successChance>1.0</successChance>
</li>
```

### 베이스 AbilityDef (추상 부모)

`ParentName`으로 지정하여 공통 설정을 상속받을 수 있습니다:

| 베이스 | 용도 | 주요 기본값 |
|--------|------|-------------|
| `SSF_BaseSelfShiftAbility` | 자기 변신 (타겟 없음) | `hostile=false`, `targetRequired=false`, `range=0`, `warmupTime=0` |
| `SSF_BaseTargetedShiftAbility` | 타인 대상 변신 | `hostile=false`, `range=15`, `warmupTime=1.0`, `canTargetPawns=true` |
| `SSF_BaseAoEShiftAbility` | 범위 변신 (바닥/폰 타겟) | `hostile=true`, `range=25`, `warmupTime=2.5`, `canTargetLocations=true` |

세 가지 모두 공통: `category=SSF_Shapeshift`, `iconPath=UI/Commands/SSF_Shift_Enter`, `casterMustBeCapableOfViolence=false`.

### CompProperties_AbilityShapeshift

`AbilityDef`의 `<comps>`에 부착합니다:

| 필드 | 타입 | 설명 |
|------|------|------|
| `hediffDef` | HediffDef | **권장.** 적용할 HediffDef. 지정 시 formDefName보다 우선. |
| `formDefName` | string | 적용할 FormDef defName. hediffDef 미지정 시 폴백. |
| `successChance` | float | 성공 확률 (기본 1.0). |
| `allowedRaces` / `disallowedRaces` | List\<ThingDef\> | 시전자 종족 필터. |
| `allowedMutants` / `disallowedMutants` | List\<MutantDef\> | 시전자 뮤턴트 필터 (Anomaly). |
| `allowedFromForms` | List\<string\> | 변신 중 시전 허용 폼 목록. 비우면 변신 중 비활성(회색). |
| `affectHostileOnly` | bool | true 시 AoE 어빌리티에서 캐스터에 적대인 폰만 적용. 기본 false. |

### 획득 경로

| 경로 | 컴포넌트 | 트리거 |
|------|----------|--------|
| 유전자 | `GeneDef.abilities` | 유전자가 어빌리티 자동 부여 (Biotech). |
| 헤디프 | `HediffCompProperties_GiveAbility` | hediff 보유 시 어빌리티 부여. |
| 아이템 (장비) | `CompProperties_GiveAbility_Shapeshift` (`requireEquipped=true`) | 장비 시 어빌리티 부여. 어빌리티로 변신 시 해당 아이템이 `sourceItem`으로 등록 — 장비 해제 시 변신 해제. |
| 아이템 (소지) | `CompProperties_GiveAbility_Shapeshift` (`requireEquipped=false`) | 인벤토리 소지 시 부여. 아이템은 `sourceItem`으로 추적되지 **않음** — 변신은 독립 유지, 아이템 드롭 시 어빌리티만 회수. |
| 약물 | `IngestionOutcomeDoer_Shapeshift` | 복용 시 직접 변신. `hediffDef` / `formDefName` 지원. |
| 스크롤/사용 | `CompProperties_UseEffect_Shapeshift` | 사용 시 직접 변신. `hediffDef` / `formDefName` 지원. |
| 투사체 | `PolymorphProjectileExtension` | 명중 시 변신. `hediffDef` / `formDefName`, `aoeRadius`, `affectAllies` 지원. |

### HediffComp_AutoShift (조건부 자동 변신)

아무 HediffDef에 `HediffCompProperties_AutoShift`를 부착합니다. 조건 충족 시 자동 변신.

| 필드 | 타입 | 기본값 | 설명 |
|------|------|--------|------|
| `formDefName` | string | — | 변신할 FormDef. hediffDef 미지정 시 사용. |
| `hediffDef` | HediffDef | — | **권장.** 변신할 HediffDef. 지정 시 formDefName보다 우선. |
| `healthThreshold` | float | 0 (미사용) | 이 체력 % 미만 시 트리거. 예: `0.3` = 30%. |
| `triggerMentalStates` | List\<MentalStateDef\> | — | 이 정신상태 진입 시 트리거. |
| `triggerSunGlowBelow` | float | 0 (미사용) | 밝기가 이 값 미만이면 트리거. `0.5` = 밤. |
| `triggerInCombat` | bool | false | 징집/피격 + 적 근처 시 트리거. |
| `checkIntervalTicks` | int | 120 | 검사 간격 (120 = 2초). |
| `successChance` | float | 1.0 | 검사당 성공 확률. |
| `triggerOnce` | bool | false | 발동 후 hediff 자체 제거. |

**로직:** 조건은 OR — 하나라도 충족하면 트리거. 이미 변신 중이면 건너뜀.

```xml
<!-- formDefName 경로 (레거시) -->
<HediffDef>
  <defName>Curse_Werewolf</defName>
  <hediffClass>HediffWithComps</hediffClass>
  <label>werewolf curse</label>
  <isBad>false</isBad>
  <comps>
    <li Class="ShapeshifterFramework.Hediffs.HediffCompProperties_AutoShift">
      <formDefName>WerewolfForm</formDefName>
      <healthThreshold>0.3</healthThreshold>
      <triggerSunGlowBelow>0.5</triggerSunGlowBelow>
      <successChance>0.8</successChance>
    </li>
  </comps>
</HediffDef>

<!-- hediffDef 경로 (권장) -->
<HediffDef>
  <defName>CombatInstinct</defName>
  <hediffClass>HediffWithComps</hediffClass>
  <label>전투 본능</label>
  <isBad>false</isBad>
  <comps>
    <li Class="ShapeshifterFramework.Hediffs.HediffCompProperties_AutoShift">
      <hediffDef>BearWarrior_Hediff</hediffDef>
      <triggerInCombat>true</triggerInCombat>
      <checkIntervalTicks>60</checkIntervalTicks>
      <triggerOnce>true</triggerOnce>
    </li>
  </comps>
</HediffDef>
```

### 다단 변신 체인

`addAbilities`로 1단계 폼에서만 2단계 어빌리티를 부여합니다. 2단계 어빌리티에는 `allowedFromForms`로 1단계 폼을 명시해야 합니다.

```
1단계 (BeastkinForm) → addAbilities로 [FullBeast 어빌리티] 부여
  → FullBeast 어빌리티에 allowedFromForms: [BeastkinForm] 설정
  → FullBeast 사용 → FullBeastForm 진입
  → BeastkinForm 해제 → FullBeast 어빌리티 제거
```

---

## 전체 XML 예시

### 기본 예시 — 늑대 변신

```xml
<Defs>
  <!-- 1. FormDef — 비주얼/장비/도구/사운드 정의 -->
  <ShapeshifterFramework.ShapeshiftFormDef ParentName="SSF_BaseForm_Animal">
    <defName>SSF_WolfForm</defName>
    <label>늑대 폼</label>
    <description>강력한 늑대 변신.</description>

    <bodyDrawScale>1.5</bodyDrawScale>
    <body>
      <mode>Replace</mode>
      <replacementTexPath>Things/Pawn/Animal/Wolf/Wolf</replacementTexPath>
    </body>

    <replaceNativeTools>true</replaceNativeTools>
    <tools>
      <li>
        <label>teeth</label>
        <capacities><li>Bite</li></capacities>
        <power>15</power>
        <cooldownTime>1.5</cooldownTime>
        <linkedBodyPartsGroup>Teeth</linkedBodyPartsGroup>
      </li>
    </tools>

    <soundWounded>Pawn_Dog_Injured</soundWounded>
    <bloodDef>Filth_Blood</bloodDef>
    <durationTicks>30000</durationTicks>
    <gizmoIconPathEnter>UI/Commands/TransformWolf</gizmoIconPathEnter>
  </ShapeshifterFramework.ShapeshiftFormDef>

  <!-- 2. HediffDef — 스탯/능력치 + FormDef 매핑 -->
  <HediffDef ParentName="SSF_ShapeshiftFormBase">
    <defName>SSF_WolfFormHediff</defName>
    <label>늑대 폼</label>
    <stages>
      <li>
        <statOffsets>
          <MoveSpeed>2.5</MoveSpeed>
          <ArmorRating_Sharp>0.4</ArmorRating_Sharp>
        </statOffsets>
      </li>
    </stages>
    <comps>
      <li Class="ShapeshifterFramework.Hediffs.HediffCompProperties_ShapeshiftCore">
        <formDef>SSF_WolfForm</formDef>
      </li>
    </comps>
  </HediffDef>

  <!-- 3. 어빌리티 — hediffDef 경로 (권장) -->
  <AbilityDef ParentName="SSF_BaseSelfShiftAbility">
    <defName>SSF_Ability_Wolf</defName>
    <label>늑대 변신</label>
    <description>늑대로 변신한다.</description>
    <cooldownTicksRange>3000</cooldownTicksRange>
    <comps>
      <li Class="ShapeshifterFramework.Comps.CompProperties_AbilityShapeshift">
        <hediffDef>SSF_WolfFormHediff</hediffDef>
      </li>
    </comps>
  </AbilityDef>
</Defs>
```

### 고급 예시 — CompProperties 오버라이드 + DefModExtension

```xml
<Defs>
  <!-- FormDef: 비주얼 공유 -->
  <ShapeshifterFramework.ShapeshiftFormDef ParentName="SSF_BaseForm_Humanoid">
    <defName>SSF_DragonkinForm</defName>
    <label>용인 폼</label>
    <description>용인 변신. 뿔과 꼬리가 생기며 인간형 유지.</description>
    <bodyDrawScale>1.3</bodyDrawScale>
    <headDrawScale>1.05</headDrawScale>
    <durationTicks>30000</durationTicks>
    <canRevertVoluntarily>true</canRevertVoluntarily>
    <renderNodeProperties>
      <li>
        <nodeClass>PawnRenderNode_AttachmentHead</nodeClass>
        <texPath>Things/Pawn/Humanlike/HeadAttachments/Horns/Horns</texPath>
        <parentTagDef>Head</parentTagDef>
        <drawData>
          <defaultData><layer>70</layer></defaultData>
        </drawData>
      </li>
    </renderNodeProperties>
    <skinColor>(0.7, 0.6, 0.5)</skinColor>
    <!-- FA 확장 -->
    <modExtensions>
      <li Class="ShapeshifterFramework.Compat.FAFormExtension" MayRequire="Nals.FacialAnimation">
        <faEyeColor>(255, 200, 0, 255)</faEyeColor>
      </li>
      <li Class="ShapeshifterFramework.Compat.HARFormExtension" MayRequire="erdelf.HumanoidAlienRaces">
        <showHarAddons>true</showHarAddons>
      </li>
    </modExtensions>
  </ShapeshifterFramework.ShapeshiftFormDef>

  <!-- HediffDef A: 일반 용인 -->
  <HediffDef ParentName="SSF_ShapeshiftFormBase">
    <defName>SSF_DragonkinNormal</defName>
    <label>용인 (일반)</label>
    <stages>
      <li>
        <statOffsets><MoveSpeed>0.5</MoveSpeed></statOffsets>
        <statFactors><ArmorRating_Heat>1.5</ArmorRating_Heat></statFactors>
      </li>
    </stages>
    <comps>
      <li Class="ShapeshifterFramework.Hediffs.HediffCompProperties_ShapeshiftCore">
        <formDef>SSF_DragonkinForm</formDef>
      </li>
    </comps>
  </HediffDef>

  <!-- HediffDef B: 분노 용인 — 같은 비주얼, 다른 스탯 + 오버라이드 -->
  <HediffDef ParentName="SSF_ShapeshiftFormBase">
    <defName>SSF_DragonkinRage</defName>
    <label>용인 (분노)</label>
    <stages>
      <li>
        <statOffsets><MoveSpeed>2.0</MoveSpeed></statOffsets>
        <statFactors>
          <ArmorRating_Heat>2.0</ArmorRating_Heat>
          <IncomingDamageFactor>0.7</IncomingDamageFactor>
        </statFactors>
      </li>
    </stages>
    <comps>
      <li Class="ShapeshifterFramework.Hediffs.HediffCompProperties_ShapeshiftCore">
        <formDef>SSF_DragonkinForm</formDef>
        <!-- CompProperties 오버라이드: FormDef 기본값 대신 사용 -->
        <durationTicks>10000</durationTicks>            <!-- 짧은 지속 -->
        <canRevertVoluntarily>false</canRevertVoluntarily>  <!-- 해제 불가 -->
        <revertOnDowned>true</revertOnDowned>               <!-- downed 시 해제 -->
        <revertAddHediffs>
          <li>
            <hediff>FibrousMechanites</hediff>              <!-- 해제 시 피로 -->
            <severity>0.3</severity>
          </li>
        </revertAddHediffs>
      </li>
    </comps>
  </HediffDef>

  <!-- 어빌리티: hediffDef 경로 -->
  <AbilityDef ParentName="SSF_BaseSelfShiftAbility">
    <defName>SSF_Ability_DragonkinRage</defName>
    <label>분노 용인 변신</label>
    <description>분노한 용인으로 변신. 강력하지만 짧고 해제 불가.</description>
    <cooldownTicksRange>6000</cooldownTicksRange>
    <comps>
      <li Class="ShapeshifterFramework.Comps.CompProperties_AbilityShapeshift">
        <hediffDef>SSF_DragonkinRage</hediffDef>
      </li>
    </comps>
  </AbilityDef>
</Defs>
```

---

## 마이그레이션 체크리스트

기존 모드를 HediffComp 아키텍처로 마이그레이션할 때 확인할 사항:

1. **FormDef에서 `linkedHediff` 제거** — 이 필드는 더 이상 존재하지 않습니다.
2. **HediffDef 생성** — 각 변신 폼에 대응하는 HediffDef를 만들고, `HediffCompProperties_ShapeshiftCore.formDef`로 FormDef를 참조합니다.
3. **스탯을 HediffDef stages로 이동** — 이전에 별도 HediffDef에 정의했던 스탯은 동일하게 유지하되, `formDef` 연결을 `comps`에 추가합니다.
4. **CompShapeshifter 참조 제거** — ThingDef 패치(HumanPatch.xml 등)를 삭제합니다.
5. **revertAddHediffs 형식 변경** — `List<HediffDef>` → `List<HediffAddEntry>` 형식으로 수정합니다.
6. **FA/HAR 필드 이동** — `showHarAddons` → `HARFormExtension`, FA 필드 → `FAFormExtension`으로 `<modExtensions>`에 배치합니다.
7. **트리거에 hediffDef 추가** — 기존 `formDefName`은 계속 동작하지만, 새 `hediffDef` 필드 사용을 권장합니다.
8. **추상 부모 활용** — `SSF_ShapeshiftFormBase`를 `ParentName`으로 사용하면 HediffDef 보일러플레이트를 줄일 수 있습니다.
