# Shapeshifter Framework — ShapeshiftFormDef 매뉴얼

> **소스 코드 기준 자동 작성** — `ShapeshiftFormDef.cs` 및 관련 컴포넌트의 실제 C# 필드를 반영합니다.

이 문서는 `ShapeshifterFramework`를 사용하여 나만의 변신 폼(Form)을 만들 때 사용하는 XML 태그의 모든 기능을 설명합니다.
모든 옵션은 **입력하지 않으면 바닐라(기본) 상태를 유지**하도록 설계되어 있으므로, 필요한 기능만 골라서 사용하시면 됩니다.

---

## 아키텍처 개요

Shapeshifter Framework는 역할별로 컴포넌트를 분리합니다:

| 컴포넌트 | 역할 |
|----------|------|
| **ShapeshiftFormDef** | 비주얼, 장비, 도구/Verb, 사운드, VFX, 지속시간, UI |
| **linkedHediff (HediffDef)** | 스탯 합연산, 곱연산, 능력치(Capacity) 보정 (바닐라 패턴) |
| **CompProperties_AbilityShiftTarget** | 캐스트 조건 (종족, 뮤턴트), 성공 확률 |
| **어빌리티 획득 소스** | 유전자, 헤디프, 아이템(CompGiveAbility_SSF), 약물, 투사체 |

스탯과 능력치 보정은 FormDef에 정의하지 **않습니다**. `linkedHediff`의 HediffDef stages에서 바닐라 HediffDef 패턴으로 정의합니다 (`statOffsets`, `statFactors`, `capMods`).

---

## 1. 기본 정보 (Basic Info)
* `<defName>` (필수): 폼의 고유 ID입니다. 절대 중복될 수 없습니다.
* `<label>`: 게임 내에 표시될 폼의 이름입니다.
* `<description>`: 변신 폼에 대한 설명 및 툴팁입니다.

## 2. 메인 헤디프 (스탯 & 능력치)
* `<linkedHediff>`: (필수) 변신 상태를 나타내는 `HediffDef`입니다. 이 헤디프가 제거되면 자동으로 변신이 해제됩니다.
* `<formAllowedRaces>`: (선택) 이 폼을 적용받을 수 있는 종족(`ThingDef`) 목록. 생략하거나 빈 목록이면 모든 종족 허용(기본 동작).
* `<formDisallowedRaces>`: (선택) 이 폼을 적용받을 수 **없는** 종족(`ThingDef`) 목록. `formAllowedRaces`보다 우선합니다.

> **참고**: 이 필드들은 **대상자**(폼을 받는 쪽) 필터입니다. `CompProperties_AbilityShiftTarget.allowedRaces`/`disallowedRaces`는 **시전자**(캐스트하는 쪽) 필터입니다. FormDef 수준 필터는 **모든** 변신 경로(어빌리티/약물/스크롤/투사체)에서 작동합니다.

```xml
<formAllowedRaces>
  <li>Human</li>
</formAllowedRaces>
```

* `<formAllowedMutants>`: (선택, `MayRequire: Ludeon.RimWorld.Anomaly`) 이 폼을 적용받을 수 있는 뮤턴트(`MutantDef`) 목록. 생략하거나 빈 목록이면 뮤턴트 제한 없음.
* `<formDisallowedMutants>`: (선택, `MayRequire: Ludeon.RimWorld.Anomaly`) 이 폼을 적용받을 수 **없는** 뮤턴트(`MutantDef`) 목록.

> **참고**: 이 필드들은 **대상자**(폼을 받는 쪽) 필터입니다. `CompProperties_AbilityShiftTarget.allowedMutants`/`disallowedMutants`는 **시전자**(캐스트하는 쪽) 필터입니다. FormDef 수준 필터는 **모든** 변신 경로(어빌리티/약물/스크롤/투사체)에서 작동합니다.

**스탯과 능력치 보정은 linkedHediff의 HediffDef에서 정의합니다. FormDef가 아닙니다.** 바닐라 HediffDef 패턴을 사용합니다:

```xml
<HediffDef>
  <defName>MyForm_Hediff</defName>
  <hediffClass>ShapeshifterFramework.Hediffs.Hediff_ShapeshiftForm</hediffClass>
  <label>나의 폼</label>
  <isBad>false</isBad>
  <stages>
    <li>
      <statOffsets>
        <MoveSpeed>1.5</MoveSpeed>
      </statOffsets>
      <statFactors>
        <MeleeHitChance>1.20</MeleeHitChance>
      </statFactors>
      <capMods>
        <li><capacity>Moving</capacity><postFactor>1.30</postFactor></li>
        <li><capacity>Manipulation</capacity><setMax>0.2</setMax></li>
      </capMods>
    </li>
  </stages>
</HediffDef>
```

## 3. 크기 및 위치 보정 (Scale & Offset)
렌더링되는 캐릭터의 크기와 위치를 조정합니다.
* `<bodyDrawScale>`: 몸 전체의 렌더링 크기 배수 (기본값: 1.0)
* `<headDrawScale>`: 머리의 추가 렌더링 배수 (몸 크기에 곱해집니다. 기본값: 1.0)
* `<portraitDrawScale>`: 하단 UI 포트레잇(초상화) 창에서만 적용되는 크기 배수입니다. (캐릭터를 거대하게 키웠을 때 초상화 창에 맞추기 위해 사용)
* `<bodyOffset>` / `<headOffset>`: 바디와 머리의 X, Z축 위치를 보정합니다. (예: `(0, 0.5)`)

## 4. 부위별 외형 제어 (Part Override Options)
특정 신체 부위의 텍스처, 색상, 셰이더를 교체하거나 숨길 수 있습니다.
지원하는 태그: `<body>`, `<head>`, `<hair>`, `<beard>`, `<tattooBody>`, `<tattooHead>`

**[내부 옵션]**
* `<mode>`: `Default`(바닐라 유지), `Hidden`(숨김), `Replace`(텍스처 교체) 중 택 1
* `<replacementTexPath>`: `Replace` 모드일 때 사용할 새 텍스처 경로
* `<swimmingReplacementTexPath>`: 수영 중일 때 사용할 전용 텍스처 경로
* `<color>` / `<swimmingColor>`: 텍스처에 덧씌울 색상 (예: `(112,82,65)` 또는 알파 포함 `(0.7, 0.8, 1.0, 0.5)`)
* `<shaderTypeDefName>`: 셰이더 변경 (예: `Cutout`, `Transparent` 등)
* `<swimmingShaderTypeDefName>`: 수영 중일 때 사용할 전용 셰이더. `shaderTypeDefName`으로 폴백, 둘 다 없으면 노드 기본값 사용.
* `<shadowVolume>` / `<shadowOffset>`: 캐릭터 발밑의 그림자 크기와 위치 오버라이드 (**`<body>`에서만 유효**). 예: `(0.6, 1.0, 0.6)`
* `<male>` / `<female>`: 성별에 따라 완전히 다른 옵션을 주고 싶을 때 내부에 동일한 구조로 작성 가능

## 5. 그래픽 숨김 및 표시 규칙 (Render Hiding / Showing)
변신 시 착용 중인 장비나 유전자 등을 강제로 숨기거나 보여줍니다.
목록(`<li>`) 형태로 작성하며, **"All"** 이라고 적으면 해당 카테고리 전체에 적용됩니다.

**의류:**
* `<renderHideApparelLayers>` / `<renderHideApparelDefNames>`: 특정 레이어(예: `OnSkin`, `Overhead`)나 특정 옷 숨김
* `<renderShowApparelLayers>` / `<renderShowApparelDefNames>`: 예외 허용 — 숨김 규칙에도 불구하고 **계속 표시**할 항목

**무기:**
* `<renderHideWeaponTags>` / `<renderHideWeaponDefNames>`: 특정 무기 태그나 defName 숨김
* `<renderShowWeaponTags>` / `<renderShowWeaponDefNames>`: 예외 허용

**유전자:**
* `<renderHideGeneExclusionTags>` / `<renderHideGeneDefNames>`: 특정 유전자 그래픽 숨김
* `<renderShowGeneExclusionTags>` / `<renderShowGeneDefNames>`: 예외 허용

**헤디프:**
* `<renderHideHediffDefNames>`: 특정 헤디프(예: 상처, 임플란트) 그래픽 숨김
* `<renderShowHediffDefNames>`: 예외 허용

> **팁:** `<renderHideApparelLayers><li>All</li></renderHideApparelLayers>`로 모든 의류 그래픽을 숨기고, `<renderShowApparelDefNames>`로 특정 아이템(예: 망토)만 선택적으로 표시할 수 있습니다.

## 6. 장비 처리 규칙 (Equipment Handling)
변신할 때 원래 입고 있던 옷과 무기를 어떻게 할지 결정합니다.

**기본 처리:**
* `<apparelOnTransform>` / `<weaponsOnTransform>`: 변신 시 처리 방법. `Keep`(그대로 착용), `Inventory`(인벤토리에 넣음), `Drop`(바닥에 떨어뜨림) 중 택 1 (기본값: `Keep`)
* `<apparelEquipLock>` / `<weaponEquipLock>`: 변신 중 착용 변경 제한. `Auto`(위 옵션에 따라 자동 결정), `Locked`(무조건 변경 불가), `Unlocked`(자유롭게 변경 가능) (기본값: `Auto`)

**소환 장비 (변신 시 생성, 해제 시 파괴):**
* `<spawnApparelOnTransform>`: 변신 시 소환하여 강제 착용할 `ThingDef` 의류 목록
* `<spawnWeaponOnTransform>`: 변신 시 소환하여 강제 장비할 `ThingDef` 무기 목록
* `<spawnApparelStuff>` / `<spawnWeaponStuff>`: 소환 장비의 재질 `ThingDef` (예: `Plasteel`)
* `<conflictingGearHandling>`: 소환 장비와 겹치는 기존 장비 처리 방법. `Keep`, `Inventory`, `Drop` 중 택 1 (기본값: `Inventory`)

## 7. 렌더 노드 (Render Nodes)
이 폼이 활성화된 동안에만 추가되는 커스텀 렌더 노드 (예: 귀, 꼬리).
* `<renderNodeProperties>`: `PawnRenderNodeProperties` 목록. 림월드 표준 렌더 노드 시스템을 사용합니다.

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

## 8. 타입 및 컬러 오버라이드 (Type & Color Overrides)
* `<bodyType>`: 특정 `BodyTypeDef` 강제 변경 (예: `Thin`, `Fat`, `Hulk`)
* `<headType>`: 특정 `HeadTypeDef` 강제 변경 (예: `Male_AverageNormal`)
* `<hairColor>`: 머리카락 색상 오버라이드 (예: `(0.85, 0.85, 0.95)`). 텍스처 Replace 모드 사용 시 무시됨.
* `<skinColor>`: 피부 색상 오버라이드 (예: `(0.7, 0.8, 1.0)`). 텍스처 Replace 모드 사용 시 무시됨.

## 9. 변신 유지 조건 (Sustain Conditions)
변신 상태를 유지하기 위해 계속 충족되어야 하는 조건입니다. 조건이 깨지면 자동으로 변신이 해제됩니다.
* `<sustainApparels>`: 계속 착용하고 있어야 하는 `ThingDef` 의류 목록
* `<sustainWeapons>`: 계속 장비하고 있어야 하는 `ThingDef` 무기 목록
* `<sustainHediffs>`: 유지되어야 하는 `HediffDef` 목록
* `<sustainGenes>`: 유지되어야 하는 `GeneDef` 목록 (Biotech DLC, `MayRequire`)
* `<sustainMode>`: `All` (모든 조건 충족 필요) 또는 `Any` (하나라도 충족하면 유지)

## 10. 부여물 (Additions — Hediffs & Abilities)
변신하는 동안 초능력이나 헤디프(상태이상/버프)를 부여합니다.
* `<addAbilities>`: 부여할 `AbilityDef` 목록. DLC 조건부 어빌리티는 `MayRequire` 지원.
* `<addHediffs>`: `HediffAddEntry` 목록:
    * `<hediff>`: 부여할 상태이상 Def
    * `<targetPart>`: 특정 `BodyPartDef` (일치하는 모든 파츠에 적용, 예: 양쪽 팔)
    * `<targetGroups>`: `BodyPartGroupDef` 목록
    * `<severity>`: 부여할 초기 수치
    * `<addedPartPolicy>`: 인공장기나 결손 부위 처리 정책:
        * `ForceAdd` — 무조건 덮어씀 (인공장기 파괴, 결손 복원 후 강제 부착)
        * `StrictFleshOnly` — 생살에만 적용 (결손이거나 인공장기가 있으면 실패)
        * `RegrowFleshOnly` — 결손은 복구하되 인공장기는 건드리지 않음

## 11. 전투 및 행동 (Combat & Work)
**Verb & Tool:**
* `<verbs>`: 추가할 `VerbProperties` 목록 (원거리/근접 공격)
* `<tools>`: 추가할 `Tool` 목록 (근접 도구)
* `<replaceNativeVerbs>`: `true`로 설정하면 바닐라 종족이 가진 기본 Verb를 지우고 폼 Verb만 사용
* `<replaceNativeTools>`: `true`로 설정하면 ThingDef의 기본 도구를 임시 교체 (해제 시 원복)
* `<verbGizmoOptions>`: `<verbs>` 순서에 맞춰 매칭되는 `VerbGizmoOption` 목록:
    * `<label>`: Verb 명령 라벨
    * `<desc>`: Verb 명령 설명
    * `<toggleLabel>` / `<toggleDesc>`: 자동공격 토글 버튼 라벨/설명
    * `<iconPath>`: 커스텀 아이콘 경로
    * `<autoAttackDefault>`: 자동공격 토글 초기값. `null`이면 첫 번째 원거리 verb만 ON, 나머지 OFF
* `<damageSourceDef>`: 상처 라벨에 표시할 종족 `ThingDef` (예: `Warg` → "Warg teeth"). `null`이면 기본 폰 라벨 사용.

**작업 제한:**
* `<disabledWorkTypesOnTransform>`: 변신 중 비활성화할 `WorkTypeDef` 목록 (예: `Firefighter`)
* `<disabledWorkTagsOnTransform>`: 비활성화할 `WorkTags` 플래그 (예: `Violent`, `Crafting`). 여러 값을 `<li>` 태그로 나열 가능.
* `<suppressIdeologyUncoveredThoughts>`: 변신 시 옷을 벗어서 생기는 '알몸' 관련 무드 페널티를 막아줍니다. (기본값: `true`)

## 12. 이펙트, 사운드, UI (VFX, SFX & UI)

**지속시간 & 해제:**
* `<durationTicks>`: 변신 지속 시간. 비워두면 무제한입니다. (60,000틱 = 인게임 1일)
* `<canRevertVoluntarily>`: `false`면 유저가 기즈모로 해제 불가 (강제 변신/디버프용). (기본값: `true`)
* `<revertOnDowned>`: `true`면 의식 상실(Downed) 시 변신 자동 해제. (기본값: `false`)

**기즈모 아이콘:**
* `<gizmoIconPathEnter>` / `<gizmoIconPathRevert>`: 변신/해제 버튼의 아이콘 경로

**변신 사운드:**
* `<transformEnterSound>` / `<transformExitSound>`: 변신 시작/종료 시 사운드

**변신 이펙터:**
* `<transformEnterEffecter>` / `<transformExitEffecter>`: 변신/해제 시 이펙터 VFX

**변신 플렉(경량 파티클):**
* `<transformEnterFleck>` / `<transformExitFleck>`: 생성할 `FleckDef`
* `<transformEnterFleckCount>` / `<transformExitFleckCount>`: 플렉 파티클 수 (0 = 비활성)
* `<transformEnterFleckScale>` / `<transformExitFleckScale>`: 플렉 파티클 크기 (기본값: 1.0)

**타이밍 & 스팸 방지:**
* `<transformEnterFxDelayTicks>` / `<transformExitFxDelayTicks>`: FX 재생 전 지연 (틱 단위)
* `<transformFxCooldownTicks>`: 동일 FX 재생 쿨다운 (기본값: 30틱)

## 13. 보이스 & 혈액 (Voice & Blood)
**보이스 오버라이드 (변신 중 폰 음성 교체):**
* `<soundCall>`: 평시 울음소리
* `<soundWounded>`: 부상 시 소리
* `<soundDeath>`: 사망 시 소리
* `<soundAngry>`: 분노 시 소리
* `<soundEating>`: 식사 시 소리

**근접 전투 사운드:**
* `<soundMeleeHitPawn>`: 근접 히트(폰) 사운드
* `<soundMeleeHitBuilding>`: 근접 히트(건물) 사운드
* `<soundMeleeMiss>`: 근접 미스 사운드

**혈액/살점:**
* `<bloodDef>`: 피격 시 생성되는 혈흔 `ThingDef`
* `<bloodSmearDef>`: 기어갈 때 생성되는 혈흔 스미어 `ThingDef`
* `<fleshType>`: `FleshTypeDef` 오버라이드 (예: `Insectoid`). 상처 텍스처 등 관련 동작 변경.

## 14. 호환성 (Compatibility)

**Humanoid Alien Races (HAR):**
* `<showHarAddons>`: `true` 시 HAR 모드의 BodyAddon을 변신 후에도 보여줍니다. (기본값: `false`). `MayRequire: erdelf.HumanoidAlienRaces`

**Facial Animation:**
아래 필드는 모두 `MayRequire: Nals.FacialAnimation`:
* `<faHeadTypeDef>`: 얼굴 머리 타입 교체
* `<faEyeballTypeDef>`: 눈알 타입 교체
* `<faLidTypeDef>`: 눈꺼풀 타입 교체
* `<faBrowTypeDef>`: 눈썹 타입 교체
* `<faMouthTypeDef>`: 입 타입 교체
* `<faSkinTypeDef>`: 피부 타입 교체
* `<faEyeColor>` / `<faEyeColor2>`: 눈 색상 오버라이드 (`ColorInt`)

**Simple Sidearms:**
* XML 필드 불필요. 자동으로 호환됩니다.
* 변신 시: 폰의 사이드암 메모리를 백업 후 클리어하여, Simple Sidearms가 무기 교체 로직에 간섭하지 않도록 합니다.
* 해제 시: 원래 사이드암 메모리를 복원하여, 변신 전과 동일한 무기를 기억합니다.

---

## 어빌리티 & 트리거 시스템

FormDef 자체에는 캐스트 조건이나 트리거 로직이 **없습니다**. 별도의 컴포넌트에서 처리합니다:

### CompProperties_AbilityShiftTarget
`AbilityDef`의 `<comps>`에 부착하여 변신 효과를 정의합니다:
* `<formDefName>`: 적용할 `ShapeshiftFormDef`의 defName
* `<successChance>`: 변신 성공 확률 (0.0–1.0, 기본값: 1.0)
* `<allowedRaces>` / `<disallowedRaces>`: 캐스터 종족 제한 (`ThingDef` 목록)
* `<allowedMutants>` / `<disallowedMutants>`: 캐스터 뮤턴트 제한 (`MutantDef` 목록, Anomaly DLC)
* `<allowedFromForms>`: 변신 중 시전 허용 폼 목록 (`string` — FormDef defName). null/비어있으면 변신 중 기즈모 **비활성(회색)** 처리. 같은 폼 재시전은 항상 숨김.

### 어빌리티 획득 경로

| 경로 | 컴포넌트 | 설명 |
|------|----------|------|
| **유전자** | `GeneDef.abilities` | Biotech DLC. 유전자가 어빌리티를 자동 부여. |
| **헤디프** | `HediffCompProperties_GiveAbility` | 바닐라 패턴. 헤디프 보유 시 어빌리티 자동 부여. |
| **아이템 (소지/장비)** | `CompProperties_GiveAbility_SSF` | 커스텀. `requireEquipped=true`면 장비 시에만, `false`면 인벤토리 소지만으로 부여. |
| **약물** | `IngestionOutcomeDoer_Shapeshift` | 약물 복용 시 직접 변신 (어빌리티 없이). 필드: `formDefName`, `successChance`. |
| **스크롤/사용 아이템** | `CompProperties_UseEffect_ShiftTarget` | 아이템 사용 시 직접 변신. 필드: `formDefName`, `successChance`. |
| **투사체** | `PolymorphProjectileExtension` | 투사체 명중 시 변신. 필드: `formDefName`, `successChance`, `aoeRadius`, `affectAllies`. |

### 다단 변신 (addAbilities 체인)
`<addAbilities>`를 사용하여 1단계 폼 상태에서만 2단계 변신 어빌리티를 부여할 수 있습니다.
**중요**: 2단계 어빌리티의 comp에 `<allowedFromForms>`로 1단계 폼을 명시해야 합니다. 그렇지 않으면 변신 중 기즈모가 비활성(회색) 처리됩니다.
```
1단계 (BeastkinForm) → addAbilities: [FullBeast 어빌리티]
  → 폰이 수인 상태에서 FullBeast 어빌리티 획득
  → FullBeast 어빌리티에 allowedFromForms: [BeastkinForm] 설정
  → FullBeast 어빌리티 사용 → FullBeastForm 진입
  → BeastkinForm 해제 시 → FullBeast 어빌리티 제거
```

---

## 추상 기본 폼 (Abstract Base Forms)

프레임워크는 `SSF_BaseForms.xml`에 3가지 추상 기본 폼을 제공합니다:

| 기본 폼 | 장비 처리 | 그래픽 숨김 | 용도 |
|---------|-----------|------------|------|
| `SSF_BaseForm_Animal` | 모두 Drop | 모든 의류/무기/머리/헤어/수염 숨김 | 동물형 완전 변신 |
| `SSF_BaseForm_Humanoid` | 모두 Keep | 오버헤드 의류만 숨김 | 인간형 + 추가 요소 |
| `SSF_BaseForm_Armored` | 모두 Keep | 숨김 없음 (모두 표시) | 장비 중심 폼 |

---

## 전체 XML 예시

```xml
<Defs>
  <!-- 1단계: 스탯용 메인 헤디프 정의 -->
  <HediffDef>
    <defName>SSF_WolfFormHediff</defName>
    <hediffClass>ShapeshifterFramework.Hediffs.Hediff_ShapeshiftForm</hediffClass>
    <label>늑대 폼</label>
    <description>늑대로 변신한 상태.</description>
    <isBad>false</isBad>
    <stages>
      <li>
        <statOffsets>
          <MoveSpeed>2.5</MoveSpeed>
          <ArmorRating_Sharp>0.4</ArmorRating_Sharp>
        </statOffsets>
        <capMods>
          <li><capacity>Moving</capacity><postFactor>1.30</postFactor></li>
        </capMods>
      </li>
    </stages>
  </HediffDef>

  <!-- 2단계: 폼 정의 -->
  <ShapeshifterFramework.ShapeshiftFormDef ParentName="SSF_BaseForm_Animal">
    <defName>SSF_WolfForm</defName>
    <label>늑대 폼</label>
    <description>강력한 늑대 변신. 무기를 떨어뜨리고 빠르게 달린다.</description>
    <linkedHediff>SSF_WolfFormHediff</linkedHediff>

    <bodyDrawScale>1.5</bodyDrawScale>
    <portraitDrawScale>0.8</portraitDrawScale>

    <apparelOnTransform>Drop</apparelOnTransform>
    <weaponsOnTransform>Drop</weaponsOnTransform>

    <body>
      <mode>Replace</mode>
      <replacementTexPath>Things/Pawn/Animal/Wolf/Wolf</replacementTexPath>
    </body>
    <head><mode>Hidden</mode></head>
    <hair><mode>Hidden</mode></hair>

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

    <transformEnterFleck>ExplosionFlash</transformEnterFleck>
    <transformEnterFleckCount>3</transformEnterFleckCount>
    <soundWounded>Pawn_Dog_Injured</soundWounded>
    <bloodDef>Filth_Blood</bloodDef>

    <gizmoIconPathEnter>UI/Commands/TransformWolf</gizmoIconPathEnter>
    <durationTicks>30000</durationTicks>
  </ShapeshifterFramework.ShapeshiftFormDef>

  <!-- 3단계: 어빌리티 정의 -->
  <AbilityDef ParentName="SSF_BaseSelfShiftAbility">
    <defName>SSF_Ability_Wolf</defName>
    <label>늑대 변신</label>
    <description>늑대로 변신한다.</description>
    <cooldownTicksRange>3000</cooldownTicksRange>
    <comps>
      <li Class="ShapeshifterFramework.Comps.CompProperties_AbilityShiftTarget">
        <formDefName>SSF_WolfForm</formDefName>
        <successChance>1.0</successChance>
      </li>
    </comps>
  </AbilityDef>
</Defs>
```
