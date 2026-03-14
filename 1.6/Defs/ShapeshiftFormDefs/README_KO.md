# Shapeshifter Framework - FormDef 매뉴얼

이 문서는 `ShapeshifterFramework`를 사용하여 나만의 변신 폼(Form)을 만들 때 사용하는 XML 태그(`ShapeshiftFormDef`)의 모든 기능을 설명합니다.

모든 옵션은 **입력하지 않으면 바닐라(기본) 상태를 유지**하도록 설계되어 있으므로, 필요한 기능만 골라서 사용하시면 됩니다.

## 1. 기본 정보 (Basic Info)
* `<defName>` (필수): 폼의 고유 ID입니다. 절대 중복될 수 없습니다.
* `<label>`: 게임 내에 표시될 폼의 이름입니다.
* `<description>`: 변신 폼에 대한 설명 및 툴팁입니다.

## 2. 크기 및 위치 보정 (Scale & Offset)
렌더링되는 캐릭터의 크기와 위치를 조정합니다.
* `<bodyDrawScale>`: 몸 전체의 렌더링 크기 배수 (기본값: 1.0)
* `<headDrawScale>`: 머리의 추가 렌더링 배수 (몸 크기에 곱해집니다. 기본값: 1.0)
* `<portraitDrawScale>`: 하단 UI 포트레잇(초상화) 창에서만 적용되는 크기 배수입니다. (캐릭터를 거대하게 키웠을 때 초상화 창에 맞추기 위해 사용)
* `<bodyOffset>` / `<headOffset>`: 바디와 머리의 X, Z축 위치를 보정합니다. (예: `(0, 0.5)`)

## 3. 부위별 외형 제어 (Part Override Options)
특정 신체 부위의 텍스처, 색상, 셰이더를 교체하거나 숨길 수 있습니다.
지원하는 태그: `<body>`, `<head>`, `<hair>`, `<beard>`, `<tattooBody>`, `<tattooHead>`

**[내부 옵션]**
* `<mode>`: `Default`(바닐라 유지), `Hidden`(숨김), `Replace`(텍스처 교체) 중 택 1
* `<replacementTexPath>`: `Replace` 모드일 때 사용할 새 텍스처 경로
* `<swimmingReplacementTexPath>`: 수영 중일 때 사용할 전용 텍스처 경로
* `<color>` / `<swimmingColor>`: 텍스처에 덧씌울 색상 (예: `(255, 255, 255)`)
* `<shaderTypeDefName>`: 셰이더 변경 (예: `Cutout`, `Transparent` 등)
* `<swimmingShaderTypeDefName>`: 수영 중일 때 사용할 전용 셰이더
* `<shadowVolume>` / `<shadowOffset>`: 캐릭터 발밑의 그림자 크기와 위치 오버라이드 (`<body>`에서만 유효)
* `<male>` / `<female>`: 성별에 따라 완전히 다른 옵션을 주고 싶을 때 내부에 동일한 구조로 작성 가능

## 4. 그래픽 숨김 및 표시 규칙 (Render Hiding/Showing)
변신 시 착용 중인 장비나 유전자 등을 강제로 숨기거나 보여줍니다.
목록(`<li>`) 형태로 작성하며, **"All"** 이라고 적으면 해당 카테고리 전체에 적용됩니다.
* `<renderHideApparelLayers>` / `<renderHideApparelDefNames>`: 특정 레이어(예: `OnSkin`)나 특정 옷 숨김
* `<renderHideWeaponTags>` / `<renderHideWeaponDefNames>`: 특정 무기 숨김
* `<renderHideGeneExclusionTags>` / `<renderHideGeneDefNames>`: 특정 유전자 그래픽 숨김
* `<renderHideHediffDefNames>`: 특정 헤디프(예: 상처, 임플란트) 그래픽 숨김
* *참고: 위 태그들의 `Hide`를 `Show`로 바꾸면 예외적으로 보여줄 항목을 지정할 수 있습니다.*

## 5. 장비 처리 규칙 (Equipment Handling)
변신할 때 원래 입고 있던 옷과 무기를 어떻게 할지 결정합니다.
* `<apparelOnTransform>` / `<weaponsOnTransform>`: 변신 시 처리 방법. `Keep`(그대로 착용), `Inventory`(인벤토리에 넣음), `Drop`(바닥에 떨어뜨림) 중 택 1 (기본값: `Keep`)
* `<apparelEquipLock>` / `<weaponEquipLock>`: 변신 중 착용 변경 제한. `Auto`(위 옵션에 따라 자동 결정), `Locked`(무조건 변경 불가), `Unlocked`(자유롭게 변경 가능) (기본값: `Auto`)

## 6. 능력치 및 능력 (Stats & Capacities)
* `<statOffsets>`: 스탯에 합연산(+, -) 적용 (예: 이동 속도 +2)
* `<statFactors>`: 스탯에 곱연산(%, x) 적용 (예: 방어력 x1.5)
* `<capMods>`: PawnCapacity(시각, 청각, 혈액 순환 등) 보정
* `<bodyType>` / `<headType>`: 체형이나 머리 모양 강제 변경

## 7. 부여물 (Additions)
변신하는 동안 초능력이나 헤디프(상태이상/버프)를 부여합니다.
* `<addAbilities>`: 부여할 `AbilityDef` 목록
* `<addHediffs>`: `HediffAddEntry` 목록.
    * `<hediff>`: 부여할 상태이상 Def
    * `<targetPart>` / `<targetGroups>`: 부여할 특정 신체 부위
    * `<severity>`: 부여할 초기 수치
    * `<addedPartPolicy>`: 인공장기나 결손 부위 처리 정책. `ForceAdd`(무조건 덮어씀), `StrictFleshOnly`(생살에만 적용, 인공장기 불가), `RegrowFleshOnly`(결손은 복구하되 인공장기는 건드리지 않음)

## 8. 변신 조건 및 필터 (Requirements & Filters)
누가 이 폼으로 변신할 수 있는지 결정합니다.
* **필터 (항상 엄격하게 적용됨):**
    * `<allowedRaces>` / `<disallowedRaces>`: 허용/차단할 종족
    * `<allowedMutants>`: 허용할 돌연변이
    * `<allowedFromForms>`: 이 폼으로 변환 가능한 이전 폼 제한
* **조건 (요구사항):**
    * `<requiredGenes>`, `<requiredItems>`, `<requiredApparels>`, `<requiredWeapons>`, `<requiredAbilities>`, `<requiredHediffs>`
    * `<requirementsMode>`: `All`(위 조건 모두 만족해야 함), `Any`(위 조건 중 하나라도 카테고리를 만족하면 됨)

## 9. 전투 및 행동 (Combat & Work)
* `<verbs>` / `<tools>`: 변신 시 추가할 원거리/근접 공격 스킬.
* `<replaceNativeVerbs>` / `<replaceNativeTools>`: `true`로 설정하면 바닐라 종족이 가진 기본 공격을 지우고 폼 공격만 사용합니다.
* `<verbGizmoOptions>`: 스킬별 자동 공격(Toggle) UI 설정 (`label`, `iconPath`, `autoAttackDefault` 등)
* `<disabledWorkTypesOnTransform>` / `<disabledWorkTagsOnTransform>`: 변신 시 특정 작업(예: 소방, 수술 등) 금지
* `<suppressIdeologyUncoveredThoughts>`: 변신 시 옷을 벗어서 생기는 '알몸' 관련 무드 페널티를 막아줍니다. (기본값: `true`)

## 10. 이펙트, 사운드, UI (VFX, SFX & UI)
* `<durationTicks>`: 변신 지속 시간. 비워두면 무제한입니다. (60,000틱 = 인게임 1일)
* `<gizmoIconPathEnter>` / `<gizmoIconPathRevert>`: 변신/해제 버튼의 아이콘 경로
* `<transformEnterSound>` / `<transformExitSound>`: 변신 시작/종료 시 사운드
* `<transformEnterEffecter>` / `<transformEnterFleck>`: 변신 파티클 및 이펙트 설정
* `<soundCall>`, `<soundWounded>`, `<soundDeath>`, `<soundMeleeHitPawn>` 등: 폼 전용 울음소리와 타격음
* `<bloodDef>` / `<bloodSmearDef>`: 피 흘릴 때 나오는 오물/혈흔 텍스처 변경

## 11. 호환성 (Compatibility)
* `<showHarAddons>`: `true` 시 HAR(Humanoid Alien Races) 모드의 BodyAddon을 변신 후에도 보여줍니다. (기본값: `false`)
* `<faHeadTypeDef>` 외 다수: Facial Animation 모드 적용 시 변신 폼 전용 표정 에셋으로 교체합니다.

---

### 📝 XML 작성 예시 (Example)

```xml
<Defs>
  <ShapeshifterFramework.ShapeshiftFormDef>
    <defName>SSF_WolfForm</defName>
    <label>Wolf Form</label>
    <description>A powerful wolf form. Drops weapons and runs fast.</description>
    
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
    
    <statOffsets>
      <li><stat>MoveSpeed</stat><value>2.5</value></li>
      <li><stat>ArmorRating_Sharp</stat><value>0.4</value></li>
    </statOffsets>
    
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
    <durationTicks>30000</durationTicks> </ShapeshifterFramework.ShapeshiftFormDef>
</Defs>