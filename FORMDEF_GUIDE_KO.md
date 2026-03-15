# Shapeshifter Framework — FormDef 제작 가이드

> 변신 폼 제작을 위한 완전 레퍼런스. `ShapeshiftFormDef.cs`의 실제 C# 필드를 반영합니다.

모든 필드는 별도 표기 없는 한 **선택사항**입니다. 생략하면 바닐라 기본값이 적용됩니다.

---

## 빠른 시작

최소한의 폼은 `defName`과 비주얼 설정만 있으면 됩니다:

```xml
<ShapeshifterFramework.ShapeshiftFormDef>
  <defName>MyForm</defName>
  <label>나의 폼</label>
  <body>
    <mode>Replace</mode>
    <replacementTexPath>Things/Pawn/MyCreature/MyCreature</replacementTexPath>
  </body>
  <head><mode>Hidden</mode></head>
  <durationTicks>30000</durationTicks>
</ShapeshifterFramework.ShapeshiftFormDef>
```

스탯/능력치 보정이 필요하면 `linkedHediff`에 바닐라 HediffDef(stages에 `statOffsets`/`statFactors`/`capMods`)를 연결하면 됩니다.

---

## 추상 기본 폼

`SSF_BaseForms.xml`에 3가지 기본 부모 폼이 제공됩니다:

| 부모 | 장비 처리 | 그래픽 숨김 | 용도 |
|------|-----------|------------|------|
| `SSF_BaseForm_Animal` | Inventory | 모든 파츠·그래픽 숨김 | 동물형 완전 교체 |
| `SSF_BaseForm_Humanoid` | Keep | 오버헤드 의류만 숨김 | 인간형 + 추가 요소 |
| `SSF_BaseForm_Armored` | Keep | 숨김 없음 | 장비 중심 폼 |

사용법: `<ShapeshifterFramework.ShapeshiftFormDef ParentName="SSF_BaseForm_Animal">`

---

## 필드 레퍼런스

### 1. 기본 정보

| 필드 | 타입 | 설명 |
|------|------|------|
| `defName` | string | **필수.** 고유 ID. |
| `label` | string | 게임 내 표시 이름. |
| `description` | string | 툴팁 및 설명. |

### 2. 연동 헤디프

| 필드 | 타입 | 설명 |
|------|------|------|
| `linkedHediff` | HediffDef | 선택. 변신 중 부여되는 hediff. 외부에서 제거하면 변신 자동 해제. 스탯/능력치 보정은 이 HediffDef의 stages에 정의. |

```xml
<!-- 스탯이 필요하면 HediffDef에 정의 -->
<HediffDef>
  <defName>MyForm_Hediff</defName>
  <hediffClass>ShapeshifterFramework.Hediffs.Hediff_ShapeshiftForm</hediffClass>
  <label>나의 폼</label>
  <isBad>false</isBad>
  <stages>
    <li>
      <statOffsets><MoveSpeed>1.5</MoveSpeed></statOffsets>
      <capMods>
        <li><capacity>Moving</capacity><postFactor>1.30</postFactor></li>
      </capMods>
    </li>
  </stages>
</HediffDef>

<!-- 그 다음 연결 -->
<linkedHediff>MyForm_Hediff</linkedHediff>
```

### 3. 종족 / 뮤턴트 필터

**대상자**(폼을 받는 쪽) 필터. 모든 트리거 경로(어빌리티, 약물, 스크롤, 투사체)에서 적용됩니다.

| 필드 | 타입 | 설명 |
|------|------|------|
| `formAllowedRaces` | List\<ThingDef\> | 이 종족만 변신 가능. 비우면 제한 없음. |
| `formDisallowedRaces` | List\<ThingDef\> | 이 종족은 변신 불가. allowedRaces보다 우선. |
| `formAllowedMutants` | List\<MutantDef\> | 이 뮤턴트만 변신 가능. (`MayRequire: Ludeon.RimWorld.Anomaly`) |
| `formDisallowedMutants` | List\<MutantDef\> | 이 뮤턴트는 변신 불가. (`MayRequire: Ludeon.RimWorld.Anomaly`) |

> **시전자 측** 필터(`allowedRaces`, `disallowedRaces`)는 `CompProperties_AbilityShapeshift`에서 설정합니다.

### 4. 크기 및 위치

| 필드 | 타입 | 기본값 | 설명 |
|------|------|--------|------|
| `bodyDrawScale` | float? | 1.0 | 몸 전체 크기 배수. |
| `headDrawScale` | float? | 1.0 | 머리 추가 배수 (bodyDrawScale에 곱해짐). |
| `portraitDrawScale` | float? | 1.0 | 하단 UI 포트레잇에서만 적용되는 크기. |
| `bodyOffset` | Vector2? | (0,0) | 바디 위치 보정 (X, Z). |
| `headOffset` | Vector2? | (0,0) | 헤드 위치 보정 (X, Z). |

### 5. 부위별 외형 제어

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

### 6. 그래픽 숨김 / 표시

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

### 7. 장비 처리

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

### 8. 렌더 노드

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

### 9. 타입 및 컬러 오버라이드

| 필드 | 타입 | 설명 |
|------|------|------|
| `bodyType` | BodyTypeDef | 체형 강제 변경 (예: `Thin`, `Hulk`). |
| `headType` | HeadTypeDef | 머리 타입 강제 변경. |
| `hairColor` | Color? | 머리카락 색상. hair 모드가 `Replace`면 무시됨. |
| `skinColor` | Color? | 피부 색상. body 모드가 `Replace`면 무시됨. |

### 10. 변신 유지 조건

변신 상태를 유지하기 위해 계속 충족해야 하는 조건. 깨지면 자동 해제.

| 필드 | 타입 | 설명 |
|------|------|------|
| `sustainApparels` | List\<ThingDef\> | 계속 착용해야 함. |
| `sustainWeapons` | List\<ThingDef\> | 계속 장비해야 함. |
| `sustainHediffs` | List\<HediffDef\> | 유지되어야 함. |
| `sustainGenes` | List\<GeneDef\> | 유지되어야 함 (Biotech). |
| `sustainMode` | SustainMode? | `All` (모두 충족) 또는 `Any` (하나라도 충족). |

### 11. 부여물 (헤디프 & 어빌리티)

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

### 12. 전투

**Verb & Tool:**

| 필드 | 타입 | 설명 |
|------|------|------|
| `verbs` | List\<VerbProperties\> | 추가 공격 (원거리/근접). |
| `tools` | List\<Tool\> | 추가 근접 도구. |
| `replaceNativeVerbs` | bool? | `true` = 기존 verb 비활성화. |
| `replaceNativeTools` | bool? | `true` = 기존 ThingDef 도구 임시 교체 (해제 시 원복). |
| `damageSourceDef` | ThingDef | 상처 라벨에 표시할 종족 (예: `Warg` → "Warg teeth"). |

**Verb 기즈모 옵션** (`verbGizmoOptions`, `verbs` 인덱스와 1:1 매칭):

| 필드 | 설명 |
|------|------|
| `label` / `desc` | Verb 명령 라벨/설명. |
| `toggleLabel` / `toggleDesc` | 자동공격 토글 라벨/설명. |
| `iconPath` | 커스텀 아이콘 경로. |
| `autoAttackDefault` | 자동공격 초기값. `null` = 첫 번째 원거리 verb만 ON, 나머지 OFF. |

**작업 제한:**

| 필드 | 타입 | 기본값 | 설명 |
|------|------|--------|------|
| `disabledWorkTypesOnTransform` | List\<WorkTypeDef\> | — | 변신 중 비활성화할 작업 타입. |
| `disabledWorkTagsOnTransform` | WorkTags | None | 비활성화할 작업 태그 (예: `Violent`, `Crafting`). |
| `suppressIdeologyUncoveredThoughts` | bool | true | 장비 제거로 인한 '알몸' 무드 페널티 억제. |

### 13. 이펙트 및 사운드

**지속시간 & 해제:**

| 필드 | 타입 | 기본값 | 설명 |
|------|------|--------|------|
| `durationTicks` | int? | null (무제한) | 지속 틱. 60000 = 인게임 1일. |
| `canRevertVoluntarily` | bool | true | `false` = 해제 기즈모 없음 (강제 변신). |
| `revertOnDowned` | bool | false | 의식 상실 시 자동 해제. |
| `revertDrops` | List\<ThingDefCountClass\> | — | 해제 시 드랍 아이템 (허물, 결정 등). |
| `revertAddHediffs` | List\<HediffDef\> | — | 해제 시 부여 hediff (피로 등). **비추적** — 바닐라 수명. |

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

### 14. 보이스 & 혈액

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

### 15. 모드 호환성

**HAR (Humanoid Alien Races):**
- `showHarAddons` (bool, 기본 false) — 변신 후에도 BodyAddon 표시. `MayRequire: erdelf.HumanoidAlienRaces`

**Facial Animation:**
모든 필드 `MayRequire: Nals.FacialAnimation`:
- `faHeadTypeDef`, `faEyeballTypeDef`, `faLidTypeDef`, `faBrowTypeDef`, `faMouthTypeDef`, `faSkinTypeDef`
- `faEyeColor` / `faEyeColor2` (ColorInt)

**Simple Sidearms:** 자동 호환 — XML 설정 불필요. 변신 시 무기 메모리 백업, 해제 시 복원.

---

## 트리거 시스템

FormDef는 폼의 **모습**을 정의합니다. **언제/어떻게** 발동되는지는 별도 컴포넌트에서 처리합니다.

### CompProperties_AbilityShapeshift

`AbilityDef`의 `<comps>`에 부착합니다:

| 필드 | 타입 | 설명 |
|------|------|------|
| `formDefName` | string | 적용할 FormDef defName. |
| `successChance` | float | 성공 확률 (기본 1.0). |
| `allowedRaces` / `disallowedRaces` | List\<ThingDef\> | 시전자 종족 필터. |
| `allowedMutants` / `disallowedMutants` | List\<MutantDef\> | 시전자 뮤턴트 필터 (Anomaly). |
| `allowedFromForms` | List\<string\> | 변신 중 시전 허용 폼 목록. 비우면 변신 중 비활성(회색). |

### 획득 경로

| 경로 | 컴포넌트 | 트리거 |
|------|----------|--------|
| 유전자 | `GeneDef.abilities` | 유전자가 어빌리티 자동 부여 (Biotech). |
| 헤디프 | `HediffCompProperties_GiveAbility` | hediff 보유 시 어빌리티 부여. |
| 아이템 (장비) | `CompProperties_GiveAbility_Shapeshift` (`requireEquipped=true`) | 장비 시 어빌리티 부여. |
| 아이템 (소지) | `CompProperties_GiveAbility_Shapeshift` (`requireEquipped=false`) | 인벤토리 소지 시 부여. |
| 약물 | `IngestionOutcomeDoer_Shapeshift` | 복용 시 직접 변신. |
| 스크롤/사용 | `CompProperties_UseEffect_Shapeshift` | 사용 시 직접 변신. |
| 투사체 | `PolymorphProjectileExtension` | 명중 시 변신. `aoeRadius`, `affectAllies` 지원. |

### HediffComp_AutoShift (조건부 자동 변신)

아무 HediffDef에 `HediffCompProperties_AutoShift`를 부착합니다. 조건 충족 시 자동 변신.

| 필드 | 타입 | 기본값 | 설명 |
|------|------|--------|------|
| `formDefName` | string | — | 변신할 FormDef. |
| `healthThreshold` | float | 0 (미사용) | 이 체력 % 미만 시 트리거. 예: `0.3` = 30%. |
| `triggerMentalStates` | List\<MentalStateDef\> | — | 이 정신상태 진입 시 트리거. |
| `triggerSunGlowBelow` | float | 0 (미사용) | 밝기가 이 값 미만이면 트리거. `0.5` = 밤. |
| `triggerInCombat` | bool | false | 징집/피격 + 적 근처 시 트리거. |
| `checkIntervalTicks` | int | 120 | 검사 간격 (120 = 2초). |
| `successChance` | float | 1.0 | 검사당 성공 확률. |
| `triggerOnce` | bool | false | 발동 후 hediff 자체 제거. |

**로직:** 조건은 OR — 하나라도 충족하면 트리거. 이미 변신 중이면 건너뜀.

```xml
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

```xml
<Defs>
  <!-- 1. 스탯용 헤디프 (선택) -->
  <HediffDef>
    <defName>SSF_WolfFormHediff</defName>
    <hediffClass>ShapeshifterFramework.Hediffs.Hediff_ShapeshiftForm</hediffClass>
    <label>늑대 폼</label>
    <isBad>false</isBad>
    <stages>
      <li>
        <statOffsets>
          <MoveSpeed>2.5</MoveSpeed>
          <ArmorRating_Sharp>0.4</ArmorRating_Sharp>
        </statOffsets>
      </li>
    </stages>
  </HediffDef>

  <!-- 2. 폼 정의 -->
  <ShapeshifterFramework.ShapeshiftFormDef ParentName="SSF_BaseForm_Animal">
    <defName>SSF_WolfForm</defName>
    <label>늑대 폼</label>
    <description>강력한 늑대 변신.</description>
    <linkedHediff>SSF_WolfFormHediff</linkedHediff>

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

  <!-- 3. 어빌리티 -->
  <AbilityDef ParentName="SSF_BaseSelfShiftAbility">
    <defName>SSF_Ability_Wolf</defName>
    <label>늑대 변신</label>
    <description>늑대로 변신한다.</description>
    <cooldownTicksRange>3000</cooldownTicksRange>
    <comps>
      <li Class="ShapeshifterFramework.Comps.CompProperties_AbilityShapeshift">
        <formDefName>SSF_WolfForm</formDefName>
      </li>
    </comps>
  </AbilityDef>
</Defs>
```
