# Shapeshifter Framework — ShapeshiftFormDef 빠른 참조

> 전체 매뉴얼은 프로젝트 루트의 `/FORMDEF_GUIDE_KO.md`를 참조하세요.

## 핵심 아키텍처

- **ShapeshiftFormDef** → 비주얼, 장비, 도구, 사운드, VFX, 지속시간
- **linkedHediff (HediffDef)** → 스탯 (`statOffsets`, `statFactors`, `capMods`) — 바닐라 패턴
- **CompProperties_AbilityShiftTarget** → 캐스트 조건 (종족, 뮤턴트), 성공 확률
- **어빌리티 소스** → 유전자, 헤디프, 아이템, 약물, 스크롤, 투사체

## FormDef 필드 요약

| 카테고리 | 주요 필드 |
|----------|----------|
| 기본 | `defName`, `label`, `description`, `linkedHediff`, `formAllowedRaces`, `formDisallowedRaces`, `formAllowedMutants`, `formDisallowedMutants` |
| 스케일 | `bodyDrawScale`, `headDrawScale`, `portraitDrawScale`, `bodyOffset`, `headOffset` |
| 부위 외형 | `body`, `head`, `hair`, `beard`, `tattooBody`, `tattooHead` (각각 `mode`, `replacementTexPath`, `color`, `shaderTypeDefName`, `male`/`female` 등) |
| 렌더 숨김/표시 | `renderHide*`/`renderShow*` — 의류(Layers/DefNames), 무기(Tags/DefNames), 유전자(ExclusionTags/DefNames), 헤디프(DefNames) |
| 장비 처리 | `apparelOnTransform`, `weaponsOnTransform` (Keep/Inventory/Drop), `apparelEquipLock`, `weaponEquipLock` (Auto/Locked/Unlocked) |
| 소환 장비 | `spawnApparelOnTransform`, `spawnWeaponOnTransform`, `spawnApparelStuff`, `spawnWeaponStuff`, `conflictingGearHandling` |
| 렌더 노드 | `renderNodeProperties` (PawnRenderNodeProperties 목록) |
| 타입/컬러 | `bodyType`, `headType`, `hairColor`, `skinColor` |
| 유지 조건 | `sustainApparels`, `sustainWeapons`, `sustainHediffs`, `sustainGenes`, `sustainMode` (All/Any) |
| 부여물 | `addHediffs` (HediffAddEntry 목록), `addAbilities` (AbilityDef 목록) |
| 전투 | `verbs`, `tools`, `replaceNativeVerbs`, `replaceNativeTools`, `verbGizmoOptions`, `damageSourceDef` |
| 작업 | `disabledWorkTypesOnTransform`, `disabledWorkTagsOnTransform`, `suppressIdeologyUncoveredThoughts` |
| VFX/SFX | `transformEnterSound`/`ExitSound`, `transformEnterEffecter`/`ExitEffecter`, `transformEnterFleck`/`ExitFleck` (+Count, +Scale), FX 지연/쿨다운 틱 |
| UI | `gizmoIconPathEnter`/`Revert`, `durationTicks`, `canRevertVoluntarily` |
| 보이스 | `soundCall`, `soundWounded`, `soundDeath`, `soundAngry`, `soundEating` |
| 근접 SFX | `soundMeleeHitPawn`, `soundMeleeHitBuilding`, `soundMeleeMiss` |
| 혈액 | `bloodDef`, `bloodSmearDef`, `fleshType` |
| HAR | `showHarAddons` |
| Facial Anim | `faHeadTypeDef`, `faEyeballTypeDef`, `faLidTypeDef`, `faBrowTypeDef`, `faMouthTypeDef`, `faSkinTypeDef`, `faEyeColor`, `faEyeColor2` |

## 추상 기본 폼

| 기본 폼 | 장비 처리 | 의류 숨김 |
|---------|-----------|----------|
| `SSF_BaseForm_Animal` | 모두 Drop | 전부 숨김 |
| `SSF_BaseForm_Humanoid` | 모두 Keep | 오버헤드만 |
| `SSF_BaseForm_Armored` | 모두 Keep | 없음 |

## 최소 예시

```xml
<HediffDef>
  <defName>MyFormHediff</defName>
  <hediffClass>ShapeshifterFramework.Hediffs.Hediff_ShapeshiftForm</hediffClass>
  <label>나의 폼</label>
  <isBad>false</isBad>
  <stages><li><statOffsets><MoveSpeed>1.0</MoveSpeed></statOffsets></li></stages>
</HediffDef>

<ShapeshifterFramework.ShapeshiftFormDef ParentName="SSF_BaseForm_Animal">
  <defName>MyForm</defName>
  <label>나의 폼</label>
  <linkedHediff>MyFormHediff</linkedHediff>
  <body><mode>Replace</mode><replacementTexPath>MyMod/Textures/MyForm</replacementTexPath></body>
  <durationTicks>30000</durationTicks>
</ShapeshifterFramework.ShapeshiftFormDef>

<AbilityDef ParentName="SSF_BaseSelfShiftAbility">
  <defName>MyAbility</defName>
  <label>나의 변신</label>
  <comps>
    <li Class="ShapeshifterFramework.Comps.CompProperties_AbilityShiftTarget">
      <formDefName>MyForm</formDefName>
    </li>
  </comps>
</AbilityDef>
```
