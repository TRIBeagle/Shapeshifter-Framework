# Shapeshifter Framework — 빠른 시작 가이드

XML 파일 3개로 첫 변신 폼을 만들어보세요. C# 코딩 불필요.

> 전체 필드 레퍼런스: [FORMDEF_GUIDE_KO.md](FORMDEF_GUIDE_KO.md)

---

## 사전 준비

1. RimWorld 1.6
2. [Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077)
3. Shapeshifter Framework (이 모드)

## 폴더 구조

모드 폴더는 이렇게 구성합니다:
```
YourMod/
  About/
    About.xml
  1.6/
    Defs/
      MyFormDef.xml      <-- 1단계: 폼 비주얼 & 동작
      MyHediffDef.xml    <-- 2단계: 스탯 & 진입점
      MyAbilityDef.xml   <-- 3단계: 트리거 방법
```

---

## 1단계: FormDef (비주얼 & 동작)

FormDef는 변신 중 폰이 **어떻게 보이고 행동하는지** 정의합니다.

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>

  <ShapeshifterFramework.ShapeshiftFormDef ParentName="SSF_BaseForm_Animal">
    <defName>MyMod_WolfForm</defName>
    <label>늑대 변신</label>
    <description>맹렬한 늑대로 변신합니다.</description>

    <!-- 몸체 텍스처 교체 -->
    <body>
      <mode>Replace</mode>
      <replacementTexPath>Things/Pawn/Animal/Wolf_Timber/Wolf_Timber</replacementTexPath>
    </body>

    <!-- 시각 스케일 (2배 크기) -->
    <bodyDrawScale>2.0</bodyDrawScale>

    <!-- 지속시간: 30000틱 = 12 게임 시간 (2500틱/시간). 삭제하면 영구. -->
    <durationTicks>30000</durationTicks>

    <!-- 플레이어가 수동 해제 가능 -->
    <canRevertVoluntarily>true</canRevertVoluntarily>

    <!-- 근접 도구 (발톱 & 물기) -->
    <tools>
      <li>
        <label>발톱</label>
        <capacities><li>Scratch</li></capacities>
        <power>12</power>
        <cooldownTime>1.5</cooldownTime>
      </li>
      <li>
        <label>물기</label>
        <capacities><li>Bite</li></capacities>
        <power>15</power>
        <cooldownTime>2.0</cooldownTime>
      </li>
    </tools>
    <replaceNativeTools>true</replaceNativeTools>
  </ShapeshifterFramework.ShapeshiftFormDef>

</Defs>
```

**기본 폼** (`ParentName`으로 사용):
| 기본 폼 | 장비 | 의류 숨김 |
|---------|------|----------|
| `SSF_BaseForm_Animal` | 전부 드랍 | 전부 숨김 |
| `SSF_BaseForm_Humanoid` | 전부 유지 | 오버헤드만 숨김 |
| `SSF_BaseForm_Armored` | 전부 유지 | 숨김 없음 |

---

## 2단계: HediffDef (스탯 & 진입점)

HediffDef는 트리거와 폼을 연결하는 **다리** 역할입니다. 스탯은 FormDef가 아니라 여기에 정의합니다.

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>

  <HediffDef ParentName="SSF_ShapeshiftFormBase">
    <defName>MyMod_WolfFormHediff</defName>
    <label>늑대 변신</label>
    <description>늑대로 변신 중. 이동속도 증가, 근접 강화.</description>
    <isBad>false</isBad>
    <defaultLabelColor>(0.6, 0.8, 0.4)</defaultLabelColor>

    <!-- 스탯: Offsets는 덧셈, Factors는 곱셈 -->
    <stages>
      <li>
        <statOffsets>
          <MoveSpeed>1.5</MoveSpeed>
        </statOffsets>
        <statFactors>
          <MeleeHitChance>1.3</MeleeHitChance>
          <MeleeDodgeChance>1.2</MeleeDodgeChance>
        </statFactors>
      </li>
    </stages>

    <!-- FormDef 연결 -->
    <comps>
      <li Class="ShapeshifterFramework.Hediffs.HediffCompProperties_ShapeshiftCore">
        <formDef>MyMod_WolfForm</formDef>
        <!-- 선택적 오버라이드 (주석 해제하여 사용):
        <durationTicks>60000</durationTicks>
        <canRevertVoluntarily>false</canRevertVoluntarily>
        -->
      </li>
    </comps>
  </HediffDef>

</Defs>
```

---

## 3단계: 트리거 (AbilityDef)

변신을 어떻게 발동할지 선택합니다. 어빌리티가 가장 일반적입니다.

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>

  <!-- 자기 시전 어빌리티 -->
  <AbilityDef ParentName="SSF_BaseSelfShiftAbility">
    <defName>MyMod_Ability_WolfShift</defName>
    <label>늑대 변신</label>
    <description>늑대로 변신합니다.</description>
    <iconPath>UI/Abilities/MyWolfIcon</iconPath>  <!-- 아이콘 경로, 없으면 삭제 -->
    <comps>
      <li Class="ShapeshifterFramework.Comps.CompProperties_AbilityGiveHediff_Shapeshift">
        <hediffDef>MyMod_WolfFormHediff</hediffDef>
      </li>
    </comps>
  </AbilityDef>

</Defs>
```

**어빌리티 부여 방법:**
- 유전자: `<abilities><li>MyMod_Ability_WolfShift</li></abilities>`
- 헤디프: `<comps><li Class="HediffCompProperties_GiveAbility"><ability>MyMod_Ability_WolfShift</ability></li></comps>`
- 개발자 모드: Debug Actions > "Give ability..."

---

## 다른 트리거 방법

| 방법 | AbilityDef 대신 사용할 것 |
|------|--------------------------|
| **약물** | ThingDef.ingestible.outcomeDoers에 `IngestionOutcomeDoer_Shapeshift` |
| **아이템(스크롤)** | ThingDef.comps에 `CompProperties_UseEffect_Shapeshift` |
| **투사체** | projectile 클래스에 `Projectile_GiveHediff_Shapeshift` |
| **장비 부여** | 무기/의류 comps에 `CompProperties_GiveAbility_Shapeshift` |
| **자동 변신** | hediff comps에 `HediffCompProperties_AutoShift` (체력/전투/밝기 조건) |

자세한 내용: [FORMDEF_GUIDE_KO.md](FORMDEF_GUIDE_KO.md) 5장 참조.

---

## 테스트

1. 모드 + Shapeshifter Framework 활성화
2. 새 게임 시작 또는 기존 세이브 로드
3. 개발자 모드 > Debug Actions > "Give ability..." > 어빌리티 선택
4. 콜로니스트 선택 > 어빌리티 기즈모 클릭

---

## 여기서 커스텀하세요

기본 변신이 작동하면 다음 기능을 추가해보세요:

| 하고 싶은 것 | FormDef에 추가할 필드 |
|-------------|---------------------|
| 머리 숨기기 | `<head><mode>Hidden</mode></head>` |
| 커스텀 사운드 | `<soundCall>...</soundCall>`, `<soundMeleeHitPawn>...</soundMeleeHitPawn>` |
| 변신 VFX | `<transformEnterFleck>PsycastSkipFlashEntry</transformEnterFleck>` |
| 유지 조건 | `<sustainApparels><li>Apparel_PowerArmor</li></sustainApparels>` |
| hediff로 금지 | `<forbiddenHediffs><li>Flu</li></forbiddenHediffs>` |
| 커스텀 혈흔 | `<bloodDef>Filth_BloodInsect</bloodDef>` |
| 장비 소환 | `<spawnWeaponOnTransform><li>MeleeWeapon_LongSword</li></spawnWeaponOnTransform>` |

전체 필드 레퍼런스: [FORMDEF_GUIDE_KO.md](FORMDEF_GUIDE_KO.md)
