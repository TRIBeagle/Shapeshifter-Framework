# Shapeshifter Framework — FormDef 제작 가이드

> 변신 폼 제작을 위한 완전 레퍼런스.
> **HediffDef**가 진입점(스탯/severity), **ShapeshiftFormDef**가 비주얼/행동 데이터 시트입니다.
> 모든 필드는 별도 표기 없는 한 **선택사항**입니다. 생략하면 바닐라 기본값이 적용됩니다.

---

## 목차
1. [빠른 시작](#1-빠른-시작)
2. [추상 베이스 폼](#2-추상-베이스-폼)
3. [필드 레퍼런스](#3-필드-레퍼런스)
4. [HediffDef 설정](#4-hediffdef-설정)
5. [트리거 시스템](#5-트리거-시스템)
6. [이벤트 및 외부 연동](#6-이벤트-및-외부-연동)
7. [전체 예시](#7-전체-예시)
8. [Combat Extended 호환성](#8-combat-extended-호환성)

---

## 1. 빠른 시작

모든 변신에는 **ShapeshiftFormDef** (비주얼/행동)와 **HediffDef** (스탯/진입점) 두 가지 Def가 필요합니다.

### 최소 FormDef
```xml
<ShapeshifterFramework.ShapeshiftFormDef ParentName="SSF_BaseForm_Animal">
  <defName>MyMod_WolfForm</defName>
  <label>늑대 폼</label>
  <description>늑대로 변신합니다.</description>
  <body>
    <mode>Replace</mode>
    <replacementTexPath>MyMod/Pawn/Wolf</replacementTexPath>
  </body>
  <bodyDrawScale>0.8</bodyDrawScale>
  <durationTicks>30000</durationTicks>  <!-- 12시간 -->
</ShapeshifterFramework.ShapeshiftFormDef>
```

### 최소 HediffDef
```xml
<HediffDef ParentName="SSF_ShapeshiftFormBase">
  <defName>MyMod_Hediff_WolfForm</defName>
  <label>늑대 폼</label>
  <description>늑대로 변신 중입니다.</description>
  <stages>
    <li>
      <statOffsets>
        <MoveSpeed>1.5</MoveSpeed>
      </statOffsets>
    </li>
  </stages>
  <comps>
    <li Class="ShapeshifterFramework.Hediffs.HediffCompProperties_ShapeshiftCore">
      <formDef>MyMod_WolfForm</formDef>
    </li>
  </comps>
</HediffDef>
```

### 최소 AbilityDef (트리거)
```xml
<AbilityDef ParentName="SSF_BaseSelfShiftAbility">
  <defName>MyMod_Ability_Wolf</defName>
  <label>늑대 변신</label>
  <description>늑대로 변신합니다.</description>
  <comps>
    <li Class="ShapeshifterFramework.Comps.CompProperties_AbilityGiveHediff_Shapeshift">
      <hediffDef>MyMod_Hediff_WolfForm</hediffDef>
    </li>
  </comps>
</AbilityDef>
```

---

## 2. 추상 베이스 폼

세 가지 추상 부모를 제공합니다. 폼 유형에 맞게 `ParentName`으로 선택하세요.

### SSF_BaseForm_Animal
완전 변신. 모든 인간 파츠(머리, 헤어, 수염, 타투) 숨김, 모든 의류/무기/유전자/헤디프 그래픽 숨김. 의류와 무기는 인벤토리로 이동.

### SSF_BaseForm_Humanoid
반인간 변신. 몸, 머리, 헤어, 수염, 타투 유지. Overhead 레이어(헬멧)만 숨김. 장비 유지.

### SSF_BaseForm_Armored
파워 슈트/갑옷 변신. 인간 외형과 기존 장비 유지. 충돌 장비는 인벤토리로 이동. 의류 교체 잠금, 무기는 자유.

### 추상 어빌리티 부모

| 부모 | 유형 | 사거리 | 워밍업 | 적대 |
|------|------|--------|--------|------|
| `SSF_BaseSelfShiftAbility` | 자기 시전 | 0 | 0초 | 아니오 |
| `SSF_BaseTargetedShiftAbility` | 대상 지정 | 15 | 1.0초 | 아니오 |
| `SSF_BaseAoEShiftAbility` | 범위 시전 | 25 | 2.5초 | 예 |

### 추상 HediffDef 부모

`SSF_ShapeshiftFormBase` — `Hediff_ShapeshiftForm` 클래스, `HediffCompProperties_ShapeshiftCore` 컴프가 미리 설정됨. 폼 HediffDef는 반드시 이것을 부모로 사용하세요.

---

## 3. 필드 레퍼런스

### 3.1 종족 & 뮤턴트 필터
| 필드 | 타입 | 설명 |
|------|------|------|
| `formAllowedRaces` | `List<ThingDef>` | 이 종족만 사용 가능. 빈 목록 = 제한 없음. null 항목 시 ConfigError 출력. |
| `formDisallowedRaces` | `List<ThingDef>` | 이 종족은 차단. allow보다 우선. null 항목 시 ConfigError 출력. |
| `formAllowedMutants` | `List<MutantDef>` | [Anomaly] 이 뮤턴트만 허용. null 항목 시 ConfigError 출력. |
| `formDisallowedMutants` | `List<MutantDef>` | [Anomaly] 이 뮤턴트 차단. null 항목 시 ConfigError 출력. |

```xml
<formAllowedRaces><li>Human</li></formAllowedRaces>
<formDisallowedRaces><li>Thrumbo</li></formDisallowedRaces>
<formAllowedMutants MayRequire="Ludeon.RimWorld.Anomaly">
  <li>Ghoul</li>
</formAllowedMutants>
```

### 3.2 스케일 & 오프셋
| 필드 | 타입 | 기본값 | 설명 |
|------|------|--------|------|
| `bodyDrawScale` | `float?` | 1.0 | 몸체 렌더 스케일 |
| `headDrawScale` | `float?` | 1.0 | 머리 렌더 스케일 |
| `bodyOffset` | `Vector2?` | (0,0) | 몸체 위치 오프셋 (x, z) |
| `headOffset` | `Vector2?` | (0,0) | 머리 위치 오프셋 (x, z) |
| `portraitDrawScale` | `float?` | 1.0 | 초상화 UI 스케일 |

```xml
<bodyDrawScale>1.5</bodyDrawScale>
<headDrawScale>0.8</headDrawScale>
<bodyOffset>(0, -0.1)</bodyOffset>
<portraitDrawScale>1.2</portraitDrawScale>
```

### 3.3 파츠 오버라이드
각 파츠(`body`, `head`, `hair`, `beard`, `tattooBody`, `tattooHead`)는 `PartOverrideOption`을 받습니다:

**PartControlMode 동작:**
| 모드 | 효과 |
|------|------|
| `Default` | 바닐라 규칙대로 정상 렌더링. 커스텀 텍스처/색상/셰이더 미적용. |
| `Hidden` | 파츠가 완전히 보이지 않음 (그래픽 null). 동물 폼에서 인간 머리/헤어 등을 숨길 때 사용. |
| `Replace` | `replacementTexPath`의 커스텀 텍스처로 교체. 색상 틴트와 셰이더 오버라이드 가능. 수영 중이고 `swimmingReplacementTexPath`가 설정되면 수영 텍스처 사용. |

**동물 폰 지원:** `body` 오버라이드는 `PawnRenderNode_AnimalPart`를 통해 동물 폰에도 적용됩니다. AoE 폴리모프 등으로 동물이 변신하면 폼의 `replacementTexPath`로 body 텍스처가 교체됩니다.

**성별 오버라이드 우선순위:** `male` 또는 `female` 하위 옵션이 설정되면 해당 성별에서 공통 필드보다 우선. 미설정 시 공통 옵션으로 폴백.

| 필드 | 타입 | 설명 |
|------|------|------|
| `mode` | `PartControlMode` | `Default` / `Hidden` / `Replace` |
| `replacementTexPath` | `string` | 교체 텍스처 경로 (mode=Replace 시) |
| `swimmingReplacementTexPath` | `string` | 수영 시 텍스처 |
| `color` | `Color?` | 틴트 색상 |
| `swimmingColor` | `Color?` | 수영 시 색상 |
| `shaderTypeDefName` | `string` | 셰이더 오버라이드 (예: `Transparent`) |
| `swimmingShaderTypeDefName` | `string` | 수영 시 셰이더 |
| `shadowVolume` | `Vector3?` | 그림자 박스 크기 |
| `shadowOffset` | `Vector3?` | 그림자 위치 오프셋 |
| `male` | `PartOverrideOption` | 남성 전용 오버라이드 |
| `female` | `PartOverrideOption` | 여성 전용 오버라이드 |

```xml
<body>
  <mode>Replace</mode>
  <replacementTexPath>MyMod/Pawn/Bear</replacementTexPath>
  <swimmingReplacementTexPath>MyMod/Pawn/Bear_Swimming</swimmingReplacementTexPath>
  <shadowVolume>(0.5, 0.0, 0.6)</shadowVolume>
  <male>
    <replacementTexPath>MyMod/Pawn/Bear_Male</replacementTexPath>
  </male>
  <female>
    <replacementTexPath>MyMod/Pawn/Bear_Female</replacementTexPath>
  </female>
</body>
<head>
  <mode>Replace</mode>
  <replacementTexPath>MyMod/Pawn/BearHead</replacementTexPath>
  <shaderTypeDefName>Transparent</shaderTypeDefName>
</head>
```

### 3.4 그래픽 가시성 필터
변신 중 의류/무기/유전자/헤디프 렌더링 제어.

**필터 로직:**
- `renderHide*` = 매칭 항목 숨김 (블랙리스트)
- `renderShow*` = hide 무시하고 강제 표시 (화이트리스트, 우선)
- 특수값 `All` = 전체 매칭
- 와일드카드 `*` 접두사/접미사 지원 (예: `Flak*`, `*Jacket`)

| 필드 | 매칭 대상 |
|------|----------|
| `renderHideApparelLayers` / `renderShowApparelLayers` | 의류 레이어명 (`Overhead`, `Shell`, `Middle` 등) |
| `renderHideApparelDefNames` / `renderShowApparelDefNames` | 의류 defName |
| `renderHideWeaponTags` / `renderShowWeaponTags` | 무기 태그 |
| `renderHideWeaponDefNames` / `renderShowWeaponDefNames` | 무기 defName |
| `renderHideGeneExclusionTags` / `renderShowGeneExclusionTags` | 유전자 제외 태그 |
| `renderHideGeneDefNames` / `renderShowGeneDefNames` | 유전자 defName |
| `renderHideHediffDefNames` / `renderShowHediffDefNames` | 헤디프 defName |

```xml
<!-- 모든 의류 숨기되 파워 아머만 표시 -->
<renderHideApparelLayers><li>All</li></renderHideApparelLayers>
<renderShowApparelDefNames><li>Apparel_PowerArmor</li></renderShowApparelDefNames>
```

### 3.5 장비 처리
| 필드 | 타입 | 기본값 | 설명 |
|------|------|--------|------|
| `apparelOnTransform` | `GearHandling` | `Keep` | `Keep` / `Inventory` / `Drop` |
| `weaponsOnTransform` | `GearHandling` | `Keep` | 동일 |
| `apparelEquipLock` | `EquipLockMode` | `Auto` | `Auto` / `Locked` / `Unlocked` |
| `weaponEquipLock` | `EquipLockMode` | `Auto` | 동일 |
| `conflictingGearHandling` | `GearHandling` | `Inventory` | 스폰 장비 충돌 시 기존 장비 처리 |

**GearHandling 동작 (변신 시):**
| 모드 | 효과 |
|------|------|
| `Keep` | 기존 의류/무기를 착용/장비한 상태로 유지. 아무 조치 없음. |
| `Inventory` | 의류/무기를 벗겨서 폰의 인벤토리로 이동. 인벤토리가 가득 차면 바닥에 드랍. |
| `Drop` | 의류/무기를 폰 위치의 바닥에 드랍. |

변신 해제 시 프레임워크가 이전에 캡처한 장비를 자동으로 재장착 시도합니다.

**EquipLockMode 동작 (변신 중):**
| 모드 | 효과 |
|------|------|
| `Locked` | 변신 중 해당 슬롯의 장착/해제 불가. UI 메뉴 차단. |
| `Unlocked` | 변신 중 자유롭게 장착/해제 가능. |
| `Auto` (기본값) | GearHandling에 따라 결정: **Keep → Unlocked**, **Inventory 또는 Drop → Locked**. |

```xml
<apparelOnTransform>Inventory</apparelOnTransform>
<weaponsOnTransform>Drop</weaponsOnTransform>
<apparelEquipLock>Locked</apparelEquipLock>
<weaponEquipLock>Unlocked</weaponEquipLock>
```

### 3.6 스폰 장비
변신 시 장비 생성. 해제 시 자동 제거.

| 필드 | 타입 | 설명 |
|------|------|------|
| `spawnApparelOnTransform` | `List<ThingDef>` | 생성하여 착용할 의류. IsApparel이어야 함; 비의류 시 ConfigError 출력. |
| `spawnWeaponOnTransform` | `List<ThingDef>` | 생성하여 장비할 무기. IsWeapon이어야 함; 비무기 시 ConfigError 출력. |
| `spawnApparelStuff` | `ThingDef` | 의류 재질 |
| `spawnWeaponStuff` | `ThingDef` | 무기 재질 |

```xml
<spawnApparelOnTransform>
  <li>Apparel_PlateArmor</li>
</spawnApparelOnTransform>
<spawnApparelStuff>Steel</spawnApparelStuff>
<spawnWeaponOnTransform>
  <li>MeleeWeapon_LongSword</li>
</spawnWeaponOnTransform>
<spawnWeaponStuff>Plasteel</spawnWeaponStuff>
```

### 3.7 렌더 노드
추가 렌더 레이어 (귀, 꼬리, 날개 등). 바닐라 `PawnRenderNodeProperties` 사용.

```xml
<renderNodeProperties>
  <li>
    <nodeClass>PawnRenderNode_AttachmentHead</nodeClass>
    <workerClass>PawnRenderNodeWorker_FlipWhenCrawling</workerClass>
    <texPath>MyMod/Pawn/FloppyEar</texPath>
    <color>(0.8, 0.6, 0.4)</color>
    <drawSize>(0.8, 0.8)</drawSize>
    <parentTagDef>Head</parentTagDef>
    <rotDrawMode>FullRotation</rotDrawMode>
    <drawData>
      <dataNorth><offset>(0.0, 0.4, 0.15)</offset></dataNorth>
      <dataSouth><offset>(0.05, 0.003, -0.2)</offset></dataSouth>
      <dataEast><offset>(0.0, 0.4, 0.1)</offset></dataEast>
    </drawData>
  </li>
</renderNodeProperties>
```

### 3.8 타입 & 색상 오버라이드
| 필드 | 타입 | 설명 |
|------|------|------|
| `bodyType` | `BodyTypeDef` | 체형 오버라이드 (Thin, Fat, Hulk 등) |
| `headType` | `HeadTypeDef` | 머리형 오버라이드 |
| `hairColor` | `Color?` | 머리카락 색상 오버라이드 |
| `skinColor` | `Color?` | 피부 색상 오버라이드 |

```xml
<bodyType>Hulk</bodyType>
<headType>Stump</headType>
<hairColor>(0.2, 0.2, 0.2)</hairColor>
<skinColor>(0.6, 0.5, 0.4)</skinColor>
```

### 3.9 유지 조건
조건 미충족 시 자동 해제 (60틱/1초마다 검사).

**SustainMode 동작:**
| 모드 | 효과 |
|------|------|
| `All` (기본값) | 요구사항이 있는 **모든** 카테고리가 동시에 충족되어야 함. 하나라도 실패하면 해제. |
| `Any` | 요구사항이 있는 카테고리 중 **하나만** 충족되면 유지. 전부 실패해야 해제. |

| 필드 | 타입 | 설명 |
|------|------|------|
| `sustainApparels` | `List<ThingDef>` | 착용 필수 의류 |
| `sustainWeapons` | `List<ThingDef>` | 장비 필수 무기 |
| `sustainHediffs` | `List<HediffDef>` | 보유 필수 헤디프 |
| `sustainGenes` | `List<GeneDef>` | [Biotech] 보유 필수 유전자. null 항목 시 ConfigError 출력. |
| `sustainMode` | `SustainMode?` | `All` (기본값) / `Any` |

```xml
<sustainHediffs>
  <li>MyMod_GuardianMark</li>
</sustainHediffs>
<sustainApparels>
  <li>Apparel_PlateArmor</li>
</sustainApparels>
<sustainWeapons>
  <li>MeleeWeapon_LongSword</li>
</sustainWeapons>
<sustainGenes MayRequire="Ludeon.RimWorld.Biotech">
  <li>MyMod_Gene_BeastBlood</li>
</sustainGenes>
<sustainMode>Any</sustainMode>
```

### 3.10 부여 효과
| 필드 | 타입 | 설명 |
|------|------|------|
| `addHediffs` | `List<HediffAddEntry>` | 변신 시 부여, 해제 시 제거되는 헤디프 |
| `addAbilities` | `List<AbilityDef>` | 변신 시 부여, 해제 시 제거되는 어빌리티 |

**HediffAddEntry 필드:**
| 필드 | 타입 | 기본값 | 설명 |
|------|------|--------|------|
| `hediff` | `HediffDef` | **필수** | 부여할 헤디프 |
| `targetPart` | `BodyPartDef` | null | 특정 신체 부위 |
| `targetGroups` | `List<BodyPartGroupDef>` | null | 신체 부위 그룹 |
| `severity` | `float?` | null | 초기 심각도 |
| `addedPartPolicy` | `AddedPartPolicy` | `ForceAdd` | 인공 장기/의수 헤디프 적용 정책 (아래 참고) |

**AddedPartPolicy 동작** (`addedPartProps`가 있는 인공 장기/의수 헤디프에만 적용):
| 정책 | 결손 부위 | 기존 인공 장기 | 효과 |
|------|----------|--------------|------|
| `ForceAdd` | 부위 복원 후 부착 | 기존 인공 장기 제거 후 부착 | 가장 공격적 — 무조건 강제 부착. |
| `StrictFleshOnly` | **스킵** (헤디프 미부여) | **스킵** (헤디프 미부여) | 가장 제한적 — 자연 살점만 허용. |
| `RegrowFleshOnly` | 부위 복원 후 부착 | **스킵** (헤디프 미부여) | 중간 — 결손은 재생하지만 기존 인공 장기는 존중. |

```xml
<addHediffs>
  <li>
    <hediff>MyMod_ThickFur</hediff>
  </li>
  <li>
    <hediff>MyMod_BeastArm</hediff>
    <targetPart>Arm</targetPart>
    <addedPartPolicy>StrictFleshOnly</addedPartPolicy>
  </li>
</addHediffs>
<addAbilities>
  <li>MyMod_Ability_Howl</li>
</addAbilities>
```

### 3.11 버브 & 도구 (전투)
| 필드 | 타입 | 설명 |
|------|------|------|
| `verbs` | `List<VerbProperties>` | 커스텀 원거리/근접 버브 |
| `tools` | `List<Tool>` | 커스텀 근접 도구 (물기, 할퀴기 등) |
| `replaceNativeVerbs` | `bool?` | 바닐라 버브를 폼 버브로 교체 |
| `replaceNativeTools` | `bool?` | 바닐라 도구를 폼 도구로 교체 |
| `verbGizmoOptions` | `List<VerbGizmoOption>` | 버브별 기즈모 라벨/아이콘/토글 |

**VerbGizmoOption 필드:**
| 필드 | 타입 | 설명 |
|------|------|------|
| `verbLabel` | `string` | 매칭할 버브 라벨 |
| `label` | `string` | 명령 버튼 라벨 |
| `description` | `string` | 명령 버튼 설명 |
| `toggleLabel` | `string` | 자동공격 토글 라벨 |
| `toggleDescription` | `string` | 자동공격 토글 설명 |
| `iconPath` | `string` | 아이콘 오버라이드 |
| `durationCostTicks` | `int` | 사용 시 차감할 변신 잔여 틱 (0 = 무료). 버스트 무기는 버스트당 1회 차감. |
| `entropyCost` | `float` | 사용 시 추가할 신경열 (0 = 없음). 로열티 DLC 필요. 신경열 추적기가 없는 폰은 발사 불가. 버스트당 1회. |

```xml
<verbs>
  <li>
    <verbClass>Verb_MeleeAttackDamage</verbClass>
    <label>물기</label>
    <meleeDamageBaseAmount>20</meleeDamageBaseAmount>
    <meleeDamageDef>Bite</meleeDamageDef>
  </li>
</verbs>
<tools>
  <li>
    <label>발톱</label>
    <capacities><li>Scratch</li></capacities>
    <power>15</power>
    <cooldownTime>1.5</cooldownTime>
    <linkedBodyPartsGroup>FrontLeftPaw</linkedBodyPartsGroup>
  </li>
</tools>
<replaceNativeTools>true</replaceNativeTools>
<verbGizmoOptions>
  <li>
    <verbLabel>물기</verbLabel>
    <label>물기 공격</label>
    <description>강력한 턱 공격</description>
    <toggleLabel>자동 물기</toggleLabel>
    <toggleDescription>자동 물기 공격 토글</toggleDescription>
    <durationCostTicks>2500</durationCostTicks> <!-- 사용 시 변신 시간 ~1시간 차감 -->
    <entropyCost>12</entropyCost> <!-- 사용 시 신경열 12 추가 (로열티 DLC) -->
  </li>
</verbGizmoOptions>
```

### 3.12 근접 사운드
| 필드 | 타입 | 설명 |
|------|------|------|
| `soundMeleeHitPawn` | `SoundDef` | 폰 타격 사운드 |
| `soundMeleeHitBuilding` | `SoundDef` | 건물 타격 사운드 |
| `soundMeleeMiss` | `SoundDef` | 빗나감 사운드 |

```xml
<soundMeleeHitPawn>Pawn_Melee_BigBash_HitPawn</soundMeleeHitPawn>
<soundMeleeHitBuilding>Pawn_Melee_BigBash_HitBuilding</soundMeleeHitBuilding>
<soundMeleeMiss>Pawn_Melee_BigBash_Miss</soundMeleeMiss>
```

### 3.13 작업 제한
| 필드 | 타입 | 설명 |
|------|------|------|
| `disabledWorkTypesOnTransform` | `List<WorkTypeDef>` | 비활성화할 작업 유형 |
| `disabledWorkTagsOnTransform` | `WorkTags` | 비활성화할 작업 태그 (플래그) |

```xml
<disabledWorkTypesOnTransform>
  <li>Cooking</li>
  <li>Crafting</li>
</disabledWorkTypesOnTransform>
<disabledWorkTagsOnTransform>Intellectual</disabledWorkTagsOnTransform>
```

### 3.14 이데올로기
| 필드 | 타입 | 기본값 | 설명 |
|------|------|--------|------|
| `suppressIdeologyUncoveredThoughts` | `bool` | `true` | 변신 중 이데올로기 노출 관련 감정 페널티(하의/상의/머리/얼굴 노출) 전부 억제. 동물/몬스터 폼에서 "미개한 벌거벗음" 디버프 방지. |
| `linkedSacredAnimalDef` | `ThingDef` | `null` | 이 폼이 대표하는 동물 종족. 숭배 동물 일치 시 규율 단계별 기분 (-8 / -3 / +2 / +5 / +8) |

```xml
<suppressIdeologyUncoveredThoughts>true</suppressIdeologyUncoveredThoughts>
<linkedSacredAnimalDef>Bear_Grizzly</linkedSacredAnimalDef>
```

**변신 규율 (5단계):**

| 단계 | 라벨 | 기분 | 의견 | 기억 감정 | 특수 |
|------|------|------|------|-----------|------|
| 0 | 섭리에 대한 모독 | -10 | -20 | -10 (5일) | **자기 주도 변신 금지** |
| 1 | 부자연스러운 힘 | -5 | -10 | -5 (3일) | - |
| - | 신경쓰지 않음 | - | - | - | 효과 없음 |
| 2 | 특별한 재능 | +5 | +10 | - | - |
| 3 | 신이 내린 축복 | +10 | +20 | - | - |

> "섭리에 대한 모독" 단계에서는 **자기 주도** 변신이 차단됩니다: 어빌리티 기즈모 비활성, 약물 섭취 메뉴 비활성, 주문서/아이템 사용 불가. **타인에 의한 강제 변신**(타인 어빌리티, 투사체, 수술 약물 투여)은 허용되며, 변신은 정상 진행되고 감정 페널티만 적용됩니다.

### 3.15 VFX & 사운드 (진입/해제)
| 필드 | 타입 | 설명 |
|------|------|------|
| `transformEnterSound` | `SoundDef` | 변신 진입 사운드 |
| `transformExitSound` | `SoundDef` | 변신 해제 사운드 |
| `transformEnterEffecter` | `EffecterDef` | 변신 진입 이펙터 |
| `transformExitEffecter` | `EffecterDef` | 변신 해제 이펙터 |
| `transformEnterFleck` | `FleckDef` | 변신 진입 플렉 |
| `transformEnterFleckCount` | `int` | 진입 플렉 수 (기본 0) |
| `transformEnterFleckScale` | `float` | 진입 플렉 스케일 (기본 1.0) |
| `transformExitFleck` | `FleckDef` | 변신 해제 플렉 |
| `transformExitFleckCount` | `int` | 해제 플렉 수 (기본 0) |
| `transformExitFleckScale` | `float` | 해제 플렉 스케일 (기본 1.0) |
| `transformEnterFxDelayTicks` | `int` | 진입 FX 지연 (기본 0) |
| `transformExitFxDelayTicks` | `int` | 해제 FX 지연 (기본 0) |
| `transformFxCooldownTicks` | `int` | FX 재생 최소 간격 (기본 30) |

```xml
<transformEnterSound>SSFTest_Sound_DarkKnightEnter</transformEnterSound>
<transformEnterEffecter>SSFTest_Effecter_DarkKnightEnter</transformEnterEffecter>
<transformEnterFleck>FleckStatic_PsychicPulse</transformEnterFleck>
<transformEnterFleckCount>5</transformEnterFleckCount>
<transformEnterFleckScale>2.0</transformEnterFleckScale>
<transformEnterFxDelayTicks>10</transformEnterFxDelayTicks>
```

### 3.16 앰비언트 VFX
변신 중 지속되는 효과.

| 필드 | 타입 | 기본값 | 설명 |
|------|------|--------|------|
| `ambientEffecter` | `EffecterDef` | null | 지속 이펙터 |
| `ambientFleck` | `FleckDef` | null | 주기적 플렉 |
| `ambientFleckIntervalTicks` | `int` | 60 | 스폰 간격 (틱) |
| `ambientFleckScale` | `float` | 1.0 | 플렉 스케일 |

```xml
<ambientFleck>FleckStatic_PsychicEffect</ambientFleck>
<ambientFleckIntervalTicks>120</ambientFleckIntervalTicks>
<ambientFleckScale>0.5</ambientFleckScale>
```

### 3.17 해제 부산물
| 필드 | 타입 | 설명 |
|------|------|------|
| `revertDrops` | `List<ThingDefCountClass>` | 해제 시 드랍 아이템 |
| `revertAddHediffs` | `List<HediffAddEntry>` | 해제 시 부여 헤디프 |

```xml
<revertDrops>
  <li><thingDef>WoolMuffalo</thingDef><count>10</count></li>
</revertDrops>
<revertAddHediffs>
  <li><hediff>MyMod_Exhaustion</hediff><severity>0.5</severity></li>
</revertAddHediffs>
```

### 3.18 보이스 오버라이드
| 필드 | 타입 | 설명 |
|------|------|------|
| `soundCall` | `SoundDef` | 대기/호출 사운드 |
| `soundWounded` | `SoundDef` | 부상 사운드 |
| `soundDeath` | `SoundDef` | 사망 사운드 |
| `soundAngry` | `SoundDef` | 분노 사운드 |
| `soundEating` | `SoundDef` | 식사 사운드 |

```xml
<soundCall>Pawn_Furskin_Call</soundCall>
<soundWounded>Pawn_Furskin_Wounded</soundWounded>
<soundDeath>Pawn_Furskin_Death</soundDeath>
<soundAngry>Pawn_Bear_Angry</soundAngry>
<soundEating>PredatorLarge_Eat</soundEating>
```

### 3.19 혈흔 & 살점
| 필드 | 타입 | 설명 |
|------|------|------|
| `bloodDef` | `ThingDef` | 혈흔 오버라이드 |
| `bloodSmearDef` | `ThingDef` | 혈흔 번짐 오버라이드 |
| `fleshType` | `FleshTypeDef` | 살점 유형 오버라이드 |

```xml
<bloodDef>Filth_BloodInsect</bloodDef>
<bloodSmearDef>Filth_BloodInsectSmear</bloodSmearDef>
<fleshType>Insectoid</fleshType>
```

### 3.20 UI & 지속 시간
| 필드 | 타입 | 기본값 | 설명 |
|------|------|--------|------|
| `durationTicks` | `int?` | null (영구) | 폼 지속 시간. null = 타이머 없음. |
| `canRevertVoluntarily` | `bool` | `true` | 해제 기즈모 표시 |
| `revertOnDowned` | `bool` | `false` | 쓰러짐(의식 상실/무력화) 시 즉시 자동 해제. 매 틱 검사. |
| `gizmoIconPathEnter` | `string` | null | 변신 기즈모 아이콘 |
| `gizmoIconPathRevert` | `string` | null | 해제 기즈모 아이콘 |

```xml
<durationTicks>30000</durationTicks>    <!-- 12시간 -->
<canRevertVoluntarily>true</canRevertVoluntarily>
<revertOnDowned>true</revertOnDowned>
<gizmoIconPathEnter>UI/Commands/MyMod_Shift_Enter</gizmoIconPathEnter>
<gizmoIconPathRevert>UI/Commands/MyMod_Shift_Revert</gizmoIconPathRevert>
```

---

## 4. HediffDef 설정

### HediffCompProperties_ShapeshiftCore
이 컴프가 HediffDef를 FormDef에 연결하며, 헤디프별 행동 오버라이드를 허용합니다.

| 필드 | 타입 | 기본값 | 설명 |
|------|------|--------|------|
| `formDef` | `ShapeshiftFormDef` | null | 대상 폼. null = 런타임 `SetFormDef()`. |
| `durationTicks` | `int?` | null | FormDef.durationTicks 오버라이드 |
| `canRevertVoluntarily` | `bool?` | null | FormDef.canRevertVoluntarily 오버라이드 |
| `revertOnDowned` | `bool?` | null | FormDef.revertOnDowned 오버라이드 |
| `sustainApparels` | `List<ThingDef>` | null | FormDef.sustainApparels 오버라이드 |
| `sustainWeapons` | `List<ThingDef>` | null | FormDef.sustainWeapons 오버라이드 |
| `sustainHediffs` | `List<HediffDef>` | null | FormDef.sustainHediffs 오버라이드 |
| `sustainGenes` | `List<GeneDef>` | null | [Biotech] FormDef.sustainGenes 오버라이드 |
| `sustainMode` | `SustainMode?` | null | FormDef.sustainMode 오버라이드 |
| `revertDrops` | `List<ThingDefCountClass>` | null | FormDef.revertDrops 오버라이드 |
| `revertAddHediffs` | `List<HediffAddEntry>` | null | FormDef.revertAddHediffs 오버라이드 |

**null = FormDef 값 사용. 명시적 값 = 오버라이드.**

### N:1 매핑 예시
여러 HediffDef가 하나의 FormDef를 공유하되 스탯만 다르게:
```xml
<!-- 같은 늑대 비주얼, 다른 스탯 -->
<HediffDef ParentName="SSF_ShapeshiftFormBase">
  <defName>MyMod_Hediff_WolfAlpha</defName>
  <label>알파 늑대</label>
  <stages><li><statOffsets><MoveSpeed>2.0</MoveSpeed></statOffsets></li></stages>
  <comps><li Class="ShapeshifterFramework.Hediffs.HediffCompProperties_ShapeshiftCore">
    <formDef>MyMod_WolfForm</formDef>
    <durationTicks>60000</durationTicks>
  </li></comps>
</HediffDef>

<HediffDef ParentName="SSF_ShapeshiftFormBase">
  <defName>MyMod_Hediff_WolfPup</defName>
  <label>새끼 늑대</label>
  <stages><li><statOffsets><MoveSpeed>0.5</MoveSpeed></statOffsets></li></stages>
  <comps><li Class="ShapeshifterFramework.Hediffs.HediffCompProperties_ShapeshiftCore">
    <formDef>MyMod_WolfForm</formDef>
    <durationTicks>15000</durationTicks>
  </li></comps>
</HediffDef>
```

---

## 4b. 추가 HediffComp

### HediffComp_PermanentTransform
hediff severity가 임계값에 도달하면 폰을 동물 또는 Thing으로 영구 전환. 되돌릴 수 없음.

| 필드 | 타입 | 기본값 | 설명 |
|------|------|--------|------|
| `animalKind` | `PawnKindDef` | null | 스폰할 동물. 이름/관계 자동 이전. |
| `thingDef` | `ThingDef` | null | animalKind가 null일 때 스폰할 Thing (치즈, 조각상 등) |
| `thingCount` | `int` | 1 | thingDef 스폰 수량 |
| `severityThreshold` | `float` | 1.0 | 전환 발동 severity 임계값 |
| `sendLetter` | `bool` | true | 전환 시 알림 레터 발송 |
| `letterTitleKey` | `string` | `SSF_PermanentTransform_LetterTitle` | 레터 타이틀 번역 키 |
| `letterTextKey` | `string` | `SSF_PermanentTransform_LetterText` | 레터 본문 키. {0}=폰 이름, {1}=결과물 이름 |

```xml
<!-- 중독 severity 1.0 → 영구 곰 전환 -->
<li Class="ShapeshifterFramework.Hediffs.HediffCompProperties_PermanentTransform">
  <animalKind>Bear_Grizzly</animalKind>
  <severityThreshold>1.0</severityThreshold>
</li>

<!-- 저주 → 치즈 전환 -->
<li Class="ShapeshifterFramework.Hediffs.HediffCompProperties_PermanentTransform">
  <thingDef>CheeseWheel</thingDef>
  <thingCount>3</thingCount>
  <severityThreshold>1.0</severityThreshold>
</li>
```

### HediffComp_Harvestable
hediff 활성 중 자원 수확 가능. 바닐라 CompShearable/CompMilkable/CompEggLayer 3종을 HediffComp 하나로 통합.

| 필드 | 타입 | 기본값 | 설명 |
|------|------|--------|------|
| `resourceDef` | `ThingDef` | null | 수확 자원 |
| `resourceAmount` | `int` | 10 | 1회 수확 수량 |
| `intervalDays` | `int` | 10 | fullness 0→1 소요 일수 |
| `autoSpawn` | `bool` | false | true: 바닥에 자동 스폰 (알 패턴). false: WorkGiver 수확 (울/우유 패턴) |
| `requiredGender` | `Gender?` | null | null=무관, `Female`=암컷만, `Male`=수컷만 |
| `inspectStringKey` | `string` | `SSF_Harvestable_Fullness` | 인스펙터 표시 키. {0}=fullness% |
| `saveKey` | `string` | `ssfHarvestFullness` | Scribe 저장 키 |

```xml
<!-- 울: 수확 필요, 10일 주기 -->
<li Class="ShapeshifterFramework.Hediffs.HediffCompProperties_Harvestable">
  <resourceDef>WoolSheep</resourceDef>
  <resourceAmount>45</resourceAmount>
  <intervalDays>10</intervalDays>
</li>

<!-- 우유: 수확 필요, 암컷만, 1일 주기 -->
<li Class="ShapeshifterFramework.Hediffs.HediffCompProperties_Harvestable">
  <resourceDef>RawMilk</resourceDef>
  <resourceAmount>14</resourceAmount>
  <intervalDays>1</intervalDays>
  <requiredGender>Female</requiredGender>
</li>

<!-- 알: 자동 스폰, 암컷만, 1일 주기 -->
<li Class="ShapeshifterFramework.Hediffs.HediffCompProperties_Harvestable">
  <resourceDef>EggChickenUnfertilized</resourceDef>
  <resourceAmount>1</resourceAmount>
  <intervalDays>1</intervalDays>
  <autoSpawn>true</autoSpawn>
  <requiredGender>Female</requiredGender>
</li>
```

---

## 5. 트리거 시스템

### 5.1 어빌리티 (자기/대상/범위)
```xml
<!-- 자기 시전 -->
<AbilityDef ParentName="SSF_BaseSelfShiftAbility">
  <defName>MyMod_Ability_Wolf</defName>
  <label>늑대 변신</label>
  <comps>
    <li Class="ShapeshifterFramework.Comps.CompProperties_AbilityGiveHediff_Shapeshift">
      <hediffDef>MyMod_Hediff_WolfForm</hediffDef>
    </li>
  </comps>
</AbilityDef>

<!-- 대상 지정 (아군 버프) -->
<AbilityDef ParentName="SSF_BaseTargetedShiftAbility">
  <defName>MyMod_Ability_BuffAlly</defName>
  <label>늑대 폼 부여</label>
  <comps>
    <li Class="ShapeshifterFramework.Comps.CompProperties_AbilityGiveHediff_Shapeshift">
      <hediffDef>MyMod_Hediff_WolfForm</hediffDef>
    </li>
  </comps>
</AbilityDef>

<!-- AoE (적대만) -->
<AbilityDef ParentName="SSF_BaseAoEShiftAbility">
  <defName>MyMod_Ability_MassPolymorph</defName>
  <label>광역 변이</label>
  <comps>
    <li Class="ShapeshifterFramework.Comps.CompProperties_AbilityGiveHediff_Shapeshift">
      <hediffDef>MyMod_Hediff_SheepForm</hediffDef>
      <affectHostileOnly>true</affectHostileOnly>
    </li>
    <li Class="CompProperties_AbilityEffectRadius"><radius>5</radius></li>
  </comps>
</AbilityDef>
```

**CompProperties_AbilityGiveHediff_Shapeshift 추가 필드:**
| 필드 | 타입 | 설명 |
|------|------|------|
| `hediffDef` | `HediffDef` | **필수** — 바닐라 상속 |
| `allowedRaces` | `List<ThingDef>` | 시전자 종족 필터 |
| `disallowedRaces` | `List<ThingDef>` | 시전자 종족 차단 |
| `allowedMutants` | `List<MutantDef>` | [Anomaly] 시전자 뮤턴트 필터 |
| `disallowedMutants` | `List<MutantDef>` | [Anomaly] 시전자 뮤턴트 차단 |
| `affectHostileOnly` | `bool` | 적대 대상만 영향 (AoE) |
| `allowedFromForms` | `List<string>` | 변신 중 시전 허용 폼 defName 목록 |

> **변신 차단:** 변신 중에는 동일 폼 포함 **모든** 추가 변신이 차단됩니다. 현재 폼의 `defName`이 `allowedFromForms`에 있을 때만 예외적으로 시전 가능합니다. 이 규칙은 어빌리티, 약물, 아이템, 투사체 등 모든 트리거에 일관 적용됩니다.
>
> **자기 시전 기즈모 자동 숨김:** 자기 전용 어빌리티(`targetRequired = false`)는 변신 중 기즈모 바에서 자동으로 숨겨집니다 (현재 폼이 `allowedFromForms`에 있으면 예외). 대상 지정 어빌리티는 계속 표시됩니다.
>
> **어빌리티 툴팁:** 어빌리티 호버 툴팁에 대상 폼 이름과 지속시간(또는 "무제한")이 자동으로 표시됩니다 — 추가 설정 불필요.

#### 어빌리티 변신 시간 차감

`CompProperties_AbilityEffect_ShapeshiftDurationCost`를 추가 comp로 붙이면 어빌리티 사용 시 변신 시간을 차감합니다. 변신 어빌리티/비변신 어빌리티 모두 사용 가능.

```xml
<comps>
  <!-- 메인 효과 (예: 기절 부여) -->
  <li Class="CompProperties_AbilityGiveHediff">
    <compClass>CompAbilityEffect_GiveHediff</compClass>
    <hediffDef>Stunned</hediffDef>
  </li>
  <!-- 변신 시간 차감 -->
  <li Class="ShapeshifterFramework.Comps.CompProperties_AbilityEffect_ShapeshiftDurationCost">
    <durationCostTicks>7500</durationCostTicks>        <!-- 변신 시간 ~3시간 차감 -->
    <requireTransformed>true</requireTransformed>       <!-- true면 변신 중이 아닐 때 사용 차단 (기본 true) -->
  </li>
</comps>
```

| 필드 | 타입 | 설명 |
|------|------|------|
| `durationCostTicks` | `int` | 사용 시 차감할 변신 잔여 틱 |
| `requireTransformed` | `bool` | true(기본)면 변신 중이 아닐 때 기즈모 비활성화 |

### 5.2 약물 (섭취)
```xml
<ThingDef ParentName="MakeableDrugBase">
  <defName>MyMod_WolfElixir</defName>
  <label>늑대 엘릭서</label>
  <ingestible>
    <outcomeDoers>
      <li Class="ShapeshifterFramework.Comps.IngestionOutcomeDoer_Shapeshift">
        <hediffDef>MyMod_Hediff_WolfForm</hediffDef>
        <!-- 선택: 특정 폼에서 변신 중일 때도 섭취 허용 -->
        <!-- <allowedFromForms><li>MyMod_BeastkinForm</li></allowedFromForms> -->
      </li>
    </outcomeDoers>
  </ingestible>
</ThingDef>
```

| 필드 | 타입 | 설명 |
|------|------|------|
| `hediffDef` | `HediffDef` | **필수** — `HediffComp_ShapeshiftCore` 포함 HediffDef |
| `allowedFromForms` | `List<string>` | 변신 중 섭취를 허용할 소스 폼 defName 목록 |

### 5.3 소비 아이템 (스크롤 / 아티팩트)
```xml
<!-- 자기 사용 스크롤 -->
<ThingDef ParentName="ResourceBase">
  <defName>MyMod_WolfScroll</defName>
  <label>늑대 변신 스크롤</label>
  <comps>
    <li Class="CompProperties_Usable"><useJob>UseItem</useJob><useLabel>스크롤 사용</useLabel></li>
    <li Class="CompProperties_UseEffectDestroySelf"/>
    <li Class="ShapeshifterFramework.Comps.CompProperties_UseEffect_Shapeshift">
      <hediffDef>MyMod_Hediff_WolfForm</hediffDef>
      <!-- 선택: 특정 폼에서 변신 중일 때도 사용 허용 -->
      <!-- <allowedFromForms><li>MyMod_BeastkinForm</li></allowedFromForms> -->
    </li>
  </comps>
</ThingDef>

<!-- 대상 지정 스크롤 (사용자가 변신 중이어도 사용 가능) -->
<ThingDef ParentName="ResourceBase">
  <defName>MyMod_WolfScroll_Target</defName>
  <label>늑대 폼 부여 스크롤</label>
  <comps>
    <li Class="CompProperties_Usable"><useJob>UseItem</useJob><useLabel>대상에게 사용</useLabel></li>
    <li Class="CompProperties_UseEffectDestroySelf"/>
    <li Class="CompProperties_TargetablePawn">
      <fleshCorpsesOnly>false</fleshCorpsesOnly>
      <nonDownedPawnOnly>true</nonDownedPawnOnly>
    </li>
    <li Class="ShapeshifterFramework.Comps.CompProperties_UseEffect_Shapeshift">
      <hediffDef>MyMod_Hediff_WolfForm</hediffDef>
    </li>
  </comps>
</ThingDef>
```

| 필드 | 타입 | 설명 |
|------|------|------|
| `hediffDef` | `HediffDef` | **필수** — `HediffComp_ShapeshiftCore` 포함 HediffDef |
| `allowedFromForms` | `List<string>` | 변신 중 자기 사용을 허용할 소스 폼 defName 목록 |

> **대상 지정 아이템:** `CompTargetable`(예: `CompProperties_TargetablePawn`)이 있는 아이템은 사용자가 변신 중이어도 항상 사용 가능합니다. 효과가 대상에게 적용되므로, 대상의 변신 상태만 체크합니다.

### 5.3.1 지속 시간 연장 — 약물

현재 변신의 남은 시간을 연장합니다. `IngestionOutcomeDoer_Shapeshift`(새 변신 적용)와 분리된 전용 클래스입니다.

```xml
<ThingDef ParentName="MakeableDrugBase">
  <defName>MyMod_BearRefreshElixir</defName>
  <label>곰 연장 영약</label>
  <ingestible>
    <outcomeDoers>
      <li Class="ShapeshifterFramework.Comps.IngestionOutcomeDoer_ExtendShapeshift">
        <extendTicks>30000</extendTicks>
        <!-- 선택: 특정 폼에서만 연장. 생략하면 어떤 폼이든 연장 -->
        <targetFormDef>MyMod_BearForm</targetFormDef>
        <!-- 선택: 원래 최대 시간을 초과하여 연장 허용 (기본 false) -->
        <allowExtendBeyondMax>false</allowExtendBeyondMax>
      </li>
    </outcomeDoers>
  </ingestible>
</ThingDef>
```

| 필드 | 타입 | 설명 |
|------|------|------|
| `extendTicks` | `int` | **필수** — 연장(+) 또는 단축(−) 틱 수 |
| `targetFormDef` | `string` | 폼 defName. 지정 시 해당 폼에서만 연장. 생략 시 현재 폼 종류 무관 |
| `allowExtendBeyondMax` | `bool` | `true`면 원래 최대 시간을 초과하여 연장 가능. 기본 `false` |

### 5.3.2 지속 시간 연장 — 아이템

```xml
<ThingDef ParentName="ResourceBase">
  <defName>MyMod_RefreshScroll</defName>
  <label>연장 스크롤</label>
  <comps>
    <li Class="CompProperties_Usable"><useJob>UseItem</useJob><useLabel>스크롤 사용</useLabel></li>
    <li Class="CompProperties_UseEffectDestroySelf"/>
    <li Class="ShapeshifterFramework.Comps.CompProperties_UseEffect_ExtendShapeshift">
      <extendTicks>30000</extendTicks>
      <targetFormDef>MyMod_BearForm</targetFormDef>
      <allowExtendBeyondMax>false</allowExtendBeyondMax>
    </li>
  </comps>
</ThingDef>
```

| 필드 | 타입 | 설명 |
|------|------|------|
| `extendTicks` | `int` | **필수** — 연장(+) 또는 단축(−) 틱 수 |
| `targetFormDef` | `string` | 폼 defName. 지정 시 해당 폼에서만 연장. 생략 시 현재 폼 종류 무관 |
| `allowExtendBeyondMax` | `bool` | `true`면 원래 최대 시간을 초과하여 연장 가능. 기본 `false` |

> **변신 중이 아닐 때:** 변신 중이 아니거나 `targetFormDef`와 다른 폼이면 아이템/약물은 소비되지만 효과 없음 — 거부 메시지가 표시됩니다.

### 5.4 투사체
```xml
<ThingDef ParentName="BaseProjectileNeolithic">
  <defName>MyMod_Proj_CursedArrow</defName>
  <thingClass>ShapeshifterFramework.Projectiles.Projectile_GiveHediff_Shapeshift</thingClass>
  <label>저주받은 화살</label>
  <modExtensions>
    <li Class="ShapeshifterFramework.Projectiles.GiveHediffProjectileExtension_Shapeshift">
      <hediffDef>MyMod_Hediff_SheepForm</hediffDef>
      <aoeRadius>0</aoeRadius>
      <affectAllies>false</affectAllies>
    </li>
  </modExtensions>
</ThingDef>
```

### 5.5 장비 → 어빌리티 부여
```xml
<!-- 무기 장비 시 어빌리티 부여 -->
<ThingDef ParentName="BaseMeleeWeapon_Cool_MakeableMetallic">
  <defName>MyMod_DarkBlade</defName>
  <label>어둠의 검</label>
  <comps>
    <li Class="ShapeshifterFramework.Comps.CompProperties_GiveAbility_Shapeshift">
      <ability>MyMod_Ability_DarkKnight</ability>
    </li>
  </comps>
</ThingDef>

<!-- 의류 착용 시 어빌리티 부여 -->
<ThingDef ParentName="ApparelBase">
  <defName>MyMod_MagicCloak</defName>
  <label>마법 망토</label>
  <comps>
    <li Class="ShapeshifterFramework.Comps.CompProperties_GiveAbility_Shapeshift">
      <ability>MyMod_Ability_Phantom</ability>
    </li>
  </comps>
</ThingDef>
```

### 5.6 유전자 → 어빌리티
```xml
<GeneDef MayRequire="Ludeon.RimWorld.Biotech">
  <defName>MyMod_Gene_WolfBlood</defName>
  <label>늑대 혈통</label>
  <geneClass>Gene</geneClass>
  <biostatCpx>2</biostatCpx>
  <biostatMet>-1</biostatMet>
  <hediffDef>MyMod_Hediff_WolfGeneAbility</hediffDef>
</GeneDef>

<HediffDef>
  <defName>MyMod_Hediff_WolfGeneAbility</defName>
  <hediffClass>HediffWithComps</hediffClass>
  <isBad>false</isBad>
  <comps>
    <li Class="HediffCompProperties_GiveAbility">
      <abilityDef>MyMod_Ability_Wolf</abilityDef>
    </li>
  </comps>
</HediffDef>
```

### 5.7 AutoShift (조건부 자동 변신)
```xml
<HediffDef>
  <defName>MyMod_Hediff_WerewolfCurse</defName>
  <label>늑대인간 저주</label>
  <hediffClass>HediffWithComps</hediffClass>
  <comps>
    <li Class="ShapeshifterFramework.Hediffs.HediffCompProperties_AutoShift">
      <hediffDef>MyMod_Hediff_WolfForm</hediffDef>
      <triggerSunGlowBelow>0.3</triggerSunGlowBelow>  <!-- 깊은 밤 -->
      <healthThreshold>0.3</healthThreshold>           <!-- 체력 30% 미만 -->
      <triggerInCombat>true</triggerInCombat>
      <checkIntervalTicks>120</checkIntervalTicks>     <!-- 2초마다 -->
      <triggerOnce>false</triggerOnce>                 <!-- 반복 발동 -->
    </li>
  </comps>
</HediffDef>
```

**HediffCompProperties_AutoShift 필드:**
| 필드 | 타입 | 기본값 | 설명 |
|------|------|--------|------|
| `hediffDef` | `HediffDef` | null | **필수** — 적용할 폼 헤디프 |
| `healthThreshold` | `float?` | null | HP%가 이 값 미만이면 발동 |
| `severityThreshold` | `float?` | null | 이 hediff의 severity가 해당 값 이상이면 발동 |
| `triggerMentalStates` | `List<MentalStateDef>` | null | 이 정신 상태 발동 시 트리거 |
| `triggerSunGlowBelow` | `float?` | null | 햇빛이 이 값 미만이면 발동 |
| `triggerInCombat` | `bool` | false | 징집 + 근처 적 시 발동 |
| `checkIntervalTicks` | `int` | 120 | 검사 간격 (틱) |
| `triggerOnce` | `bool` | false | 발동 후 이 헤디프 제거 (1회성) |

조건은 **OR** 로직 — 하나만 충족해도 변신 발동.

### 5.8 다단계 폼 체인
`allowedFromForms`로 변신 중 시전 허용:

```xml
<!-- 1단계: 수인 (휴머노이드) -->
<AbilityDef ParentName="SSF_BaseSelfShiftAbility">
  <defName>MyMod_Ability_Beastkin</defName>
  <comps>
    <li Class="ShapeshifterFramework.Comps.CompProperties_AbilityGiveHediff_Shapeshift">
      <hediffDef>MyMod_Hediff_Beastkin</hediffDef>
    </li>
  </comps>
</AbilityDef>

<!-- 2단계: 완전수 (수인 폼 필수) -->
<AbilityDef ParentName="SSF_BaseSelfShiftAbility">
  <defName>MyMod_Ability_FullBeast</defName>
  <comps>
    <li Class="ShapeshifterFramework.Comps.CompProperties_AbilityGiveHediff_Shapeshift">
      <hediffDef>MyMod_Hediff_FullBeast</hediffDef>
      <allowedFromForms>
        <li>MyMod_BeastkinForm</li>
      </allowedFromForms>
    </li>
  </comps>
</AbilityDef>
```

---

## 6. 이벤트 및 외부 연동

### C# 이벤트
```csharp
using ShapeshifterFramework.Utilities;

// 변신 이벤트 구독
ShapeshiftCoreUtility.OnFormApplied += (pawn, form) => { /* ... */ };
ShapeshiftCoreUtility.OnFormRemoved += (pawn, form) => { /* ... */ };
```

> **주의:** 이벤트 핸들러는 게임 로드 시(`GameComponent.FinalizeInit`) 매번 초기화됩니다.
> `[StaticConstructorOnStartup]`에서 등록하면 로드 후 핸들러가 유실됩니다.
> 반드시 자체 `GameComponent.FinalizeInit()` 오버라이드에서 등록하세요:
> ```csharp
> public class MyModGameComponent : GameComponent
> {
>     public MyModGameComponent(Game game) : base(game) { }
>     public override void FinalizeInit()
>     {
>         ShapeshiftCoreUtility.OnFormApplied += MyHandler;
>     }
> }
> ```

### C# API
```csharp
// 변신 적용
ShapeshiftCoreUtility.GiveShiftHediff(pawn, hediffDef);

// 변신 해제
ShapeshiftCoreUtility.RemoveForm(pawn);

// 현재 폼 조회
if (ShapeshiftCoreUtility.TryGetCore(pawn, out var core))
{
    bool isShifted = core.isTransformed;
    ShapeshiftFormDef form = core.currentForm;
}

// 남은 시간 연장/단축 (시간제 변신만. 영구 변신은 무시)
core.ExtendDuration(2500);                // +1시간
core.ExtendDuration(-1250);               // -0.5시간 (0 이하 → 다음 틱에 자동 해제)
core.ExtendDuration(30000, false);        // +5시간, 원래 최대 시간 이내로 제한
core.ExtendDuration(30000, true);         // +5시간, 원래 최대 시간 초과 허용
```

---

## 7. 전체 예시

모든 기능을 포함한 늑대 폼:

```xml
<!-- FormDef -->
<ShapeshifterFramework.ShapeshiftFormDef ParentName="SSF_BaseForm_Animal">
  <defName>MyMod_WolfForm</defName>
  <label>늑대 폼</label>
  <description>사나운 늑대로 변신합니다.</description>

  <!-- 비주얼 -->
  <body>
    <mode>Replace</mode>
    <replacementTexPath>MyMod/Pawn/Wolf</replacementTexPath>
    <swimmingReplacementTexPath>MyMod/Pawn/Wolf_Swimming</swimmingReplacementTexPath>
    <shadowVolume>(0.4, 0.0, 0.5)</shadowVolume>
  </body>
  <bodyDrawScale>0.85</bodyDrawScale>

  <!-- 지속 & 해제 -->
  <durationTicks>30000</durationTicks>
  <canRevertVoluntarily>true</canRevertVoluntarily>
  <revertOnDowned>true</revertOnDowned>

  <!-- 전투 -->
  <tools>
    <li>
      <label>송곳니</label>
      <capacities><li>Bite</li></capacities>
      <power>18</power>
      <cooldownTime>2.0</cooldownTime>
    </li>
    <li>
      <label>발톱</label>
      <capacities><li>Scratch</li></capacities>
      <power>12</power>
      <cooldownTime>1.5</cooldownTime>
    </li>
  </tools>
  <replaceNativeTools>true</replaceNativeTools>

  <!-- VFX -->
  <transformEnterFleck>FleckStatic_PsychicPulse</transformEnterFleck>
  <transformEnterFleckCount>3</transformEnterFleckCount>
  <transformEnterFleckScale>1.5</transformEnterFleckScale>

  <!-- 보이스 -->
  <soundCall>Pawn_Wolf_Call</soundCall>
  <soundWounded>Pawn_Wolf_Wounded</soundWounded>
  <soundDeath>Pawn_Wolf_Death</soundDeath>

  <!-- 혈흔 -->
  <bloodDef>Filth_Blood</bloodDef>
  <fleshType>Normal</fleshType>

  <!-- 해제 드랍 -->
  <revertDrops>
    <li><thingDef>WoolMuffalo</thingDef><count>5</count></li>
  </revertDrops>

  <!-- 작업 제한 -->
  <disabledWorkTagsOnTransform>Intellectual</disabledWorkTagsOnTransform>
</ShapeshifterFramework.ShapeshiftFormDef>
```

---

## 8. Combat Extended 호환성

SSF 폼의 verb는 **NativeVerb** (`EquipmentSource=null`)이므로 CE 탄약 시스템(`CompAmmoUser`)에서 자연 면제됩니다. CE 환경에서 별도 코드 수정 없이 동작합니다.

다만 CE가 추가하는 melee 필드(`armorPenetrationSharp`, `armorPenetrationBlunt`)와 ranged 필드(`recoilAmount` 등)는 미지정 시 기본값(0)이 되어, CE 환경에서 관통력이 없는 공격이 됩니다.

### CE 지원 방법

`MayRequire="CETeam.CombatExtended"` XPath 패치로 바닐라 `Tool`을 `CombatExtended.ToolCE`로, `VerbProperties`를 `CombatExtended.VerbPropertiesCE`로 교체합니다.

**핵심:**
- `ToolCE`는 `Tool`을 상속 → SSF의 `List<Tool>`이 그대로 수용
- `VerbPropertiesCE`는 `VerbProperties`를 상속 → 동일 원리
- CE 어셈블리에 하드 참조 없음 — CE 활성 시에만 패치 적용
- SSF는 CE에 대한 코드 패치를 수행하지 않음 — 모더가 MayRequire XML 패치로 처리

### Tool 패치 예제

```xml
<Operation Class="PatchOperationReplace" MayRequire="CETeam.CombatExtended">
  <xpath>Defs/ShapeshifterFramework.ShapeshiftFormDef[defName="MyMod_WolfForm"]/tools</xpath>
  <value>
    <tools>
      <li Class="CombatExtended.ToolCE">
        <label>fangs</label>
        <capacities><li>Bite</li></capacities>
        <power>18</power>
        <cooldownTime>2.0</cooldownTime>
        <armorPenetrationSharp>5</armorPenetrationSharp>
        <armorPenetrationBlunt>10</armorPenetrationBlunt>
      </li>
    </tools>
  </value>
</Operation>
```

### Verb 패치 예제

```xml
<Operation Class="PatchOperationReplace" MayRequire="CETeam.CombatExtended">
  <xpath>Defs/ShapeshifterFramework.ShapeshiftFormDef[defName="MyMod_BeastForm"]/verbs/li[label="AssaultRifle"]</xpath>
  <value>
    <li Class="CombatExtended.VerbPropertiesCE">
      <label>AssaultRifle</label>
      <verbClass>CombatExtended.Verb_ShootCE</verbClass>
      <hasStandardCommand>true</hasStandardCommand>
      <defaultProjectile>Bullet_AssaultRifle</defaultProjectile>
      <warmupTime>1.0</warmupTime>
      <range>28</range>
      <burstShotCount>3</burstShotCount>
      <ticksBetweenBurstShots>10</ticksBetweenBurstShots>
      <recoilAmount>1.2</recoilAmount>
    </li>
  </value>
</Operation>
```

> **참고:** 실제 CE 환경에서는 projectile도 CE 전용 탄약 Def로 교체해야 합니다. 위 예제는 구조 시연용으로 바닐라 projectile을 사용합니다.

### 패치 파일 위치

CE 활성 시에만 로드되는 폴더에 배치:
```
MyMod/
  CombatExtended/
    Patches/
      MyMod_Forms_CE.xml
  LoadFolders.xml   ← MayRequire로 CombatExtended 폴더 추가
```

또는 다른 패치 파일과 같은 위치에 두고 각 `<Operation>` 요소에 `MayRequire`를 사용할 수 있습니다.

실제 동작 예제는 `TestMod_SSF/CombatExtended/Patches/SSF_TestForms_CE.xml`을 참고하세요.
