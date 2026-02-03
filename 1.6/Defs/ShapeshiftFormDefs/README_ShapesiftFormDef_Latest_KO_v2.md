# ShapeshiftFormDef 사용법 (최신판 / Detailed)

RimWorld 변신 모드의 변신 폼(Form) 정의용 Def: **`ShapeshiftFormDef`**  
이 문서는 **현재 코드 기준(ShapeshiftFormDef.cs 최신본)** 필드를 기준으로 작성했습니다.

핵심 설계:
- **입력한 것만 override**
- 입력하지 않은 것은 **바닐라/원본 유지**
- 수영 그래픽/그림자/셰이더는 바닐라 동작과 충돌하지 않도록 “안전 폴백”을 둠

---

## 0) 파일 위치

변신 폼 XML(`*.xml`)은 보통 아래 폴더에 둡니다.

```
/Defs/ShapeshiftForms/
```

---

## 1) 기본 구조

```xml
<Defs>
  <ShapeshiftFormDef>
    <defName>MyForm</defName>
    <label>My Form</label>
    <description>설명...</description>
    ...
  </ShapeshiftFormDef>
</Defs>
```

---

## 2) 필수/주요 필드

### `defName` (필수)
- 폼 고유 ID
- **중복 불가**
- 조건 필드(`allowedFromForms`)에서도 이 문자열을 참조합니다.

### `label` (선택)
- UI 표시명
- 생략 시 `defName`을 그대로 표시

### `description` (선택)
- 툴팁/설명 텍스트

---

## 3) 스케일/오프셋 (렌더 보정)

> 구 문서의 `customDrawSize / customHeadDrawSize`가 **현재는** 아래 필드로 바뀌었습니다.

### `bodyDrawScale` (선택, 기본 1)
- 몸 전체 스케일 배수
- 예: `5.0`이면 5배

### `headDrawScale` (선택, 기본 1)
- 헤드 추가 배수  
- 실제 헤드 스케일은 **(bodyDrawScale × headDrawScale)**

### `bodyOffset`, `headOffset` (선택, 기본 (0,0))
- 바디/헤드 위치 보정(Vector2)

### `portraitDrawScale` (선택, 기본 1)
- 포트레잇(정보창)에서만 적용되는 추가 스케일 배수

---

## 4) 파츠 그래픽 제어 (PartOverrideOption)

아래 파츠들은 동일한 구조를 사용합니다.

- `body`
- `head`
- `hair`
- `beard`
- `tattooBody`
- `tattooHead`

각 파츠 블록은 내부적으로 `PartOverrideOption`입니다.

---

### 4.1 `mode` (기본 `Default`)
| 값 | 의미 |
|---|---|
| `Default` | 바닐라 그대로 |
| `Hidden` | 숨김(렌더 제외) |
| `Replace` | 텍스처 교체 |

- `mode`를 생략하면 `Default`로 처리됩니다.

---

### 4.2 `replacementTexPath` (Replace에서 사용)
- `mode=Replace`일 때 교체 텍스처 경로
- 생략 시 교체가 일어나지 않거나(실제로는 Default처럼) 보일 수 있습니다.

---

### 4.3 `swimmingReplacementTexPath` (Body 전용)
- 수영 중(물 타일) 텍스처 경로
- 없으면 육지 텍스처를 그대로 사용합니다.

---

### 4.4 색상: `color` / `swimmingColor` (선택)
| 필드 | 설명 |
|---|---|
| `color` | 육지/기본 상태 색상 |
| `swimmingColor` | **수영 텍스처를 실제로 사용 중일 때** 색상 |

우선순위:
1) 수영 텍스처 사용 중 + `swimmingColor` 존재 → `swimmingColor`
2) 그 외 → `color` 있으면 `color`
3) 둘 다 없으면 → `Color.white`

---

### 4.5 셰이더: `shaderTypeDefName` / `swimmingShaderTypeDefName` (선택)
| 필드 | 설명 |
|---|---|
| `shaderTypeDefName` | 육지 셰이더(ShaderTypeDef의 defName) |
| `swimmingShaderTypeDefName` | 수영 텍스처 사용 시 셰이더 |

기본 동작(미입력 시):
- 육지: 파츠 기본 셰이더(바디/헤어 등 노드 기본값)
- 수영(바디 + 수영 텍스처 사용):  
  `swimmingShaderTypeDefName` → `shaderTypeDefName` → **Transparent 폴백**

---

### 4.6 그림자(바닥 타원): `shadowVolume` / `shadowOffset` (Body 전용, 선택)
- 바디의 바닥 그림자(ellipse) 오버라이드용 값(Vector3)

규칙:
- 폼에서 쉐도우 값을 **입력하면** → **폼 쉐도우만** 출력(바닐라 쉐도우는 차단)
- 쉐도우 값을 **미입력하면** → **바닐라 쉐도우만** 출력
- 수영 텍스처 사용 중이면 → **그림자 미출력**(바닐라 동작과 동일)

---

### 4.7 젠더 분기: `<male>` / `<female>` (선택)
- 파츠 블록 내부에서 젠더별로 옵션을 분기할 수 있습니다.

머지/폴백 개념(현재 구현):
- 젠더 블록에 값이 없으면 공통(base) 값으로 폴백
- “일부만 젠더로 바꾸고 나머지는 공통 유지”가 가능합니다.

---

## 5) 의상/무기/유전자 렌더 숨김/표시

> 특수값 `"All"` 지원 (전부 숨김/전부 표시 같은 목적)

### 5.1 의상(Apparel) 숨김/표시
- `renderHideApparelLayers`
- `renderHideApparelDefNames`
- `renderShowApparelLayers`
- `renderShowApparelDefNames`

**Layers**는 ApparelLayerDef 이름(또는 레이어 문자열)을 쓰는 쪽입니다.  
정확한 해석은 모드 로직에 따르며, 일반적으로는 “레이어/특정 의상 defName” 기준 필터로 이해하면 됩니다.

### 5.2 무기(Weapon) 숨김/표시
- `renderHideWeaponTags`
- `renderHideWeaponDefNames`
- `renderShowWeaponTags`
- `renderShowWeaponDefNames`

### 5.3 유전자(Genes) 그래픽 숨김/표시
- `renderHideGeneExclusionTags`
- `renderHideGeneDefNames`
- `renderShowGeneExclusionTags`
- `renderShowGeneDefNames`

> 구 문서의 `exclusionTags`는 현재는 위 “renderHideGeneExclusionTags” 계열로 발전된 구조입니다.  
> (숨김/표시를 각각 분리해서 더 제어 가능)

---

## 6) 변신 시 장비 처리 / 착용 금지 정책

### 6.1 변신 시 기존 장비 처리
- `apparelOnTransform` : 의복 처리
- `weaponsOnTransform` : 무기 처리

값:
- `None` : 아무 처리 안 함
- `Inventory` : 인벤토리로 이동
- `Drop` : 바닥에 드랍

### 6.2 착용/장착 금지 정책
- `apparelEquipLock`
- `weaponEquipLock`

값:
- `Auto` : GearHandling 정책에 묶어서 자동
- `Always` : 변신 중 항상 금지
- `Never` : 변신 중 금지하지 않음

---

## 7) 폼 전용 렌더 노드 추가 (renderNodeProperties)

### `renderNodeProperties` (선택)
- 타입: `List<PawnRenderNodeProperties>`
- 해당 폼이 활성일 때만 Pawn RenderTree에 추가되는 노드 정의입니다.
- 바닐라 `RaceDef.renderNodeProperties`에 넣는 것과 같은 형식으로 작성합니다.

예시 형태(대표 패턴):
```xml
<renderNodeProperties>
  <li>
	<nodeClass>PawnRenderNode_AttachmentHead</nodeClass>
    <!-- 여기 아래는 PawnRenderNodeProperties가 지원하는 필드들을 넣습니다 -->
  </li>
</renderNodeProperties>
```
> ⚠️ 주의: `PawnRenderNodeProperties`는 “노드 타입/필드 조합”이 다양합니다.  
> 사용하려는 노드 클래스/필드는 **바닐라 RenderNodeProperties 정의**를 참고해서 그대로 작성하는 방식입니다.

---

## 8) 타입/스탯/캐퍼 변경

### 8.1 타입 오버라이드
- `bodyType` : BodyTypeDef
- `headType` : HeadTypeDef

### 8.2 스탯
- `statOffsets` : 가산(StatModifier 리스트)
- `statFactors` : 배수(StatModifier 리스트)

### 8.3 캐퍼(능력치)
- `capMods` : PawnCapacityModifier 리스트

---

## 9) 변신 요건(Requirements) / 허용 필터(Allowed)

### 9.1 요구 조건(카테고리)
- `requiredGenes`
- `requiredItems`
- `requiredApparels`
- `requiredWeapons`
- `requiredAbilities`
- `requiredHediffs`

카테고리 내부는 기본적으로 “전부 만족(ALL-of)” 개념이며,  
카테고리 집계는 `requirementsMode`로 결정됩니다.

### 9.2 집계 모드: `requirementsMode` (선택, 기본 All)
- `All` : 각 카테고리 요구를 모두 만족해야 함
- `Any` : 카테고리들 중 하나라도 만족하면 됨

### 9.3 항상 선행 필터(최우선)
- `allowedRaces` / `disallowedRaces` (ThingDef)
- (DLC 있을 때) `allowedMutants` / `disallowedMutants`
- (Biotech 있을 때) `allowedXenotypes` / `disallowedXenotypes`

### 9.4 이전 폼 제한
- `allowedFromForms` : 문자열 리스트(defName)
- `"None"`을 넣으면 “무변신 상태”를 의미

> 구 문서의 `allowedPreviousForms`와 동일한 역할을 이 필드가 맡습니다.

---

## 10) 변신 중 부여(임시 효과)

- `addHediffs` : `HediffAddEntry` 리스트
- `addAbilities` : AbilityDef 리스트

---

## 11) Verb/Tool 추가 및 대체

- `verbs` : VerbProperties 리스트
- `tools` : Tool 리스트
- `replaceNativeVerbs` : 기존 Verb를 대체할지 여부
- `replaceNativeTools` : 기존 Tool을 대체할지 여부
- `verbGizmoOptions` : verbs 순서에 맞춰 UI/사용성 옵션 부여

---

## 12) 작업(Work) 제한 / 사상(노출) 억제

- `disabledWorkTypesOnTransform` : WorkTypeDef 리스트
- `disabledWorkTagsOnTransform` : WorkTags 플래그
- `suppressIdeologyUncoveredThoughts` (기본 true)  
  변신 중 하의/상의/머리/얼굴 노출 관련 사상(Ideology)을 억제

---

## 13) 이펙트/사운드/혈흔/육질

### 13.1 변신 진입/해제 FX
- `transformEnterSound` / `transformExitSound`
- `transformEnterEffecter` / `transformExitEffecter`
- `transformEnterFleck` / `transformExitFleck`
- `transformEnterFleckCount` / `transformExitFleckCount`
- `transformEnterFleckScale` / `transformExitFleckScale`
- `transformEnterFxDelayTicks` / `transformExitFxDelayTicks`
- `transformFxCooldownTicks` (기본 30)

### 13.2 보이스/행동 사운드 오버라이드(선택)
- `soundCall`, `soundWounded`, `soundDeath`, `soundAngry`, `soundEating`,
- `soundMeleeHitPawn`, `soundMeleeHitBuilding`, `soundMeleeMiss`

### 13.3 피/혈흔/육질(선택)
- `bloodDef`
- `bloodSmearDef`
- `fleshType`

---

## 14) Gizmo / Duration

### 14.1 버튼 숨김
- `hideGizmo` (기본 false)

### 14.2 버튼 아이콘
- `gizmoIconPathEnter` : 변신 버튼
- `gizmoIconPathRevert` : 해제 버튼

### 14.3 지속 시간
- `durationTicks` (기본 null=무한)
  - 양수: 그 시간 후 자동 해제
  - null: 무한

---

## 15) HAR (Humanoid Alien Races) 옵션

- `showHarAddons` (기본 false)  
  HAR 바디 애드온을 렌더할지 여부  
  (HAR 모드가 있을 때만 의미)

---

# 16) 풀 기능 예제 XML (모든 기능 활용)

> 아래 예제는 “기능 설명용”으로 가능한 한 많은 필드를 채운 예시입니다.  
> 실제 폼 제작 시에는 필요한 옵션만 골라 쓰는 걸 권장합니다.

```xml
<Defs>

  <ShapeshiftFormDef>
    <!-- 기본 -->
    <defName>Example_AllFeatures_Form</defName>
    <label>Example: All Features</label>
    <description>모든 기능을 사용하는 예제 폼.</description>

    <!-- 스케일/오프셋 -->
    <bodyDrawScale>1.25</bodyDrawScale>
    <headDrawScale>1.08</headDrawScale>
    <bodyOffset>(0, 0)</bodyOffset>
    <headOffset>(0, 0.04)</headOffset>
    <portraitDrawScale>1.10</portraitDrawScale>

    <!-- 파츠: body (수영/색/셰이더/쉐도우/젠더) -->
    <body>
      <mode>Replace</mode>
      <replacementTexPath>Things/Pawn/Example/Body_Common</replacementTexPath>
      <swimmingReplacementTexPath>Things/Pawn/Example/Body_Swim</swimmingReplacementTexPath>

      <color>(200,200,200)</color>
      <swimmingColor>(160,160,160)</swimmingColor>

      <shaderTypeDefName>CutoutComplex</shaderTypeDefName>
      <swimmingShaderTypeDefName>Transparent</swimmingShaderTypeDefName>

      <shadowVolume>(0.70, 0.55, 0.55)</shadowVolume>
      <shadowOffset>(0,0,-0.22)</shadowOffset>

      <male>
        <replacementTexPath>Things/Pawn/Example/Body_Male</replacementTexPath>
      </male>
      <female>
        <replacementTexPath>Things/Pawn/Example/Body_Female</replacementTexPath>
        <swimmingColor>(140,140,180)</swimmingColor>
      </female>
    </body>

    <!-- 파츠: head -->
    <head>
      <mode>Replace</mode>
      <replacementTexPath>Things/Pawn/Example/Head_Common</replacementTexPath>
    </head>

    <!-- 파츠: hair / beard / tattoos -->
    <hair><mode>Default</mode></hair>
    <beard><mode>Hidden</mode></beard>
    <tattooBody><mode>Default</mode></tattooBody>
    <tattooHead><mode>Default</mode></tattooHead>

    <!-- 의상/무기/유전자 렌더 필터 (특수값 "All" 가능) -->
    <renderHideApparelLayers>
      <li>All</li>
    </renderHideApparelLayers>
    <renderShowWeaponTags>
      <li>Gun</li>
    </renderShowWeaponTags>
    <renderHideGeneExclusionTags>
      <li>Hair</li>
      <li>Beard</li>
      <li>Tail</li>
      <li>Voice</li>
    </renderHideGeneExclusionTags>

    <!-- 변신 시 장비 처리 -->
    <apparelOnTransform>Inventory</apparelOnTransform>
    <weaponsOnTransform>Drop</weaponsOnTransform>
    <apparelEquipLock>Auto</apparelEquipLock>
    <weaponEquipLock>Always</weaponEquipLock>

    <!-- 폼 전용 렌더 노드(바닐라 PawnRenderNodeProperties 형식) -->
    <renderNodeProperties>
      <li>
        <nodeClass>PawnRenderNode_AttachmentHead</nodeClass>
        <workerClass>PawnRenderNodeWorker_FlipWhenCrawling</workerClass>
        <texPath>Things/Pawn/Humanlike/HeadAttachments/FloppyEars/FloppyEars</texPath>
        <colorType>Skin</colorType>
        <parentTagDef>Head</parentTagDef>
        <useSkinShader>true</useSkinShader>
        <useRottenColor>true</useRottenColor>
        <rotDrawMode>Fresh, Rotting</rotDrawMode>
        <drawData>
          <defaultData>
            <layer>70</layer>
          </defaultData>
        </drawData>
      </li>
      <li>
        <workerClass>PawnRenderNodeWorker_AttachmentBody</workerClass>
        <texPath>Things/Pawn/Humanlike/BodyAttachments/FurryTail/FurryTail</texPath>
        <colorType>Hair</colorType>
        <overrideMeshSize>(1, 1)</overrideMeshSize>
        <parentTagDef>Body</parentTagDef>
        <rotDrawMode>Fresh, Rotting</rotDrawMode>
        <drawData>
          <scaleOffsetByBodySize>true</scaleOffsetByBodySize>
          <defaultData>
            <layer>-2</layer>
          </defaultData>
          <dataNorth>
            <offset>(0.1, 0, -0.25)</offset>
            <layer>90</layer>
          </dataNorth>
          <dataSouth>
            <offset>(-0.1, 0, -0.25)</offset>
          </dataSouth>
          <dataEast>
            <offset>(-0.5, 0, -0.15)</offset>
          </dataEast>
          <dataWest>
            <offset>(0.5, 0, -0.15)</offset>
          </dataWest>
        </drawData>
      </li>
    </renderNodeProperties>
	
    <!-- 타입 -->
    <bodyType>Male</bodyType>
    <headType>Male_AverageNormal</headType>

    <!-- 스탯/캐퍼 -->
    <statOffsets>
      <li><stat>MoveSpeed</stat><value>0.30</value></li>
      <li><stat>Sight</stat><value>0.10</value></li>
    </statOffsets>
    <statFactors>
      <li><stat>MeleeDPS</stat><value>1.20</value></li>
      <li><stat>IncomingDamageFactor</stat><value>0.90</value></li>
    </statFactors>
    <capMods>
      <li><capacity>Manipulation</capacity><offset>0.10</offset></li>
    </capMods>

    <!-- 요구조건 -->
    <requiredGenes><li>Gene_Wolfkin</li></requiredGenes>
    <requiredItems><li>Example_TransformToken</li></requiredItems>
    <requiredApparels><li>Apparel_PsychicHat</li></requiredApparels>
    <requiredWeapons><li>Weapon_Gun_Pistol</li></requiredWeapons>
    <requiredAbilities><li>Example_Ability_UnlockForm</li></requiredAbilities>
    <requiredHediffs><li>Example_Hediff_Ready</li></requiredHediffs>

    <!-- 허용/차단 필터 -->
    <allowedRaces>
      <li>Human</li>
      <li>Yttakin</li>
    </allowedRaces>
    <disallowedRaces>
      <li>Mechanoid</li>
    </disallowedRaces>

    <!-- 이전 폼 제한 -->
    <allowedFromForms>
      <li>None</li>
      <li>Example_PreviousFormA</li>
    </allowedFromForms>

    <requirementsMode>All</requirementsMode>

    <!-- 변신 중 부여 -->
    <addHediffs>
      <li>
        <hediff>Example_Hediff_Buff</hediff>
        <severity>0.50</severity>
      </li>
    </addHediffs>
    <addAbilities><li>Example_Ability_Howl</li></addAbilities>

    <!-- Verb/Tool (예시 형태) -->
    <replaceNativeVerbs>true</replaceNativeVerbs>
    <verbs>
      <li>
        <verbClass>Verb_MeleeAttack</verbClass>
        <warmupTime>0</warmupTime>
      </li>
    </verbs>

    <replaceNativeTools>false</replaceNativeTools>
    <tools>
      <li>
        <label>claw</label>
        <power>12</power>
        <cooldownTime>1.5</cooldownTime>
      </li>
    </tools>

    <!-- Work 제한 / 사상 억제 -->
    <disabledWorkTypesOnTransform>
      <li>Research</li>
    </disabledWorkTypesOnTransform>
    <disabledWorkTagsOnTransform>Violent</disabledWorkTagsOnTransform>
    <suppressIdeologyUncoveredThoughts>true</suppressIdeologyUncoveredThoughts>

    <!-- FX/사운드 -->
    <transformEnterSound>Example_Sound_Enter</transformEnterSound>
    <transformExitSound>Example_Sound_Exit</transformExitSound>
    <transformEnterFleck>Example_Fleck_Enter</transformEnterFleck>
    <transformEnterFleckCount>6</transformEnterFleckCount>
    <transformEnterFleckScale>1.0</transformEnterFleckScale>
    <transformExitFleck>Example_Fleck_Exit</transformExitFleck>
    <transformExitFleckCount>6</transformExitFleckCount>
    <transformExitFleckScale>1.0</transformExitFleckScale>
    <transformEnterFxDelayTicks>0</transformEnterFxDelayTicks>
    <transformExitFxDelayTicks>0</transformExitFxDelayTicks>
    <transformFxCooldownTicks>30</transformFxCooldownTicks>

    <!-- 보이스/행동 사운드 -->
    <soundCall>Example_Sound_Call</soundCall>
    <soundWounded>Example_Sound_Wounded</soundWounded>
    <soundDeath>Example_Sound_Death</soundDeath>

    <!-- 피/혈흔/육질 -->
    <bloodDef>Filth_Blood</bloodDef>
    <bloodSmearDef>Filth_BloodSmear</bloodSmearDef>
    <fleshType>Normal</fleshType>

    <!-- Gizmo / Duration -->
    <hideGizmo>false</hideGizmo>
    <gizmoIconPathEnter>UI/Commands/ExampleTransform</gizmoIconPathEnter>
    <gizmoIconPathRevert>UI/Commands/ExampleRevert</gizmoIconPathRevert>
    <durationTicks>3600</durationTicks>

    <!-- HAR -->
    <showHarAddons>false</showHarAddons>

  </ShapeshiftFormDef>

</Defs>
```

---

## 17) 구 문서와 필드명 대응표

| 구 문서 | 현재 필드 |
|---|---|
| customDrawSize | bodyDrawScale (배수) |
| customHeadDrawSize | headDrawScale (배수) |
| duration | durationTicks |
| gizmoIconPath | gizmoIconPathEnter / gizmoIconPathRevert |
| exclusionTags | renderHideGeneExclusionTags (+ show/hide 분리) |
| allowedPreviousForms | allowedFromForms |

