# Shapeshifter Framework — ShapeshiftFormDef 빠른 참조

> 전체 매뉴얼은 프로젝트 루트의 `/FORMDEF_GUIDE_KO.md`를 참조하세요.

## 핵심 아키텍처

- **HediffDef** → 변신 진입점. `stages`에서 스탯 보정 (`statOffsets`, `statFactors`, `capMods`) 정의
- **HediffComp_ShapeshiftCore** → HediffDef의 `comps`에 포함. `formDef`로 FormDef를 참조하여 변신 로직 실행
- **ShapeshiftFormDef** → 순수 데이터 시트. 비주얼, 장비, 도구, 사운드, VFX 등 외형/연출 정보
- **CompProperties_AbilityGiveHediff_Shapeshift** → 캐스트 조건 (종족, 뮤턴트), 성공 확률
- **어빌리티 소스** → 유전자, 헤디프, 아이템, 약물, 스크롤, 투사체

## FormDef 필드 요약

| 카테고리 | 주요 필드 |
|----------|----------|
| 기본 | `defName`, `label`, `description`, `formAllowedRaces`, `formDisallowedRaces`, `formAllowedMutants`, `formDisallowedMutants` |
| 스케일 | `bodyDrawScale`, `headDrawScale`, `portraitDrawScale`, `bodyOffset`, `headOffset` |
| 부위 외형 | `body`, `head`, `hair`, `beard`, `tattooBody`, `tattooHead` (각각 `mode`, `replacementTexPath`, `color`, `shaderTypeDefName`, `male`/`female` 등) |
| 렌더 숨김/표시 | `renderHide*`/`renderShow*` — 의류(Layers/DefNames), 무기(Tags/DefNames), 유전자(ExclusionTags/DefNames), 헤디프(DefNames) |
| 장비 처리 | `apparelOnTransform`, `weaponsOnTransform` (Keep/Inventory/Drop), `apparelEquipLock`, `weaponEquipLock` (Auto/Locked/Unlocked) |
| 소환 장비 | `spawnApparelOnTransform`, `spawnWeaponOnTransform`, `spawnApparelStuff`, `spawnWeaponStuff`, `conflictingGearHandling` |
| 렌더 노드 | `renderNodeProperties` (PawnRenderNodeProperties 목록) |
| 타입/컬러 | `bodyType`, `headType`, `hairColor`, `skinColor` |
| 유지 조건 | `sustainApparels`, `sustainWeapons`, `sustainHediffs`, `sustainGenes`, `sustainMode` (All/Any) |
| 부여물 | `addHediffs` (HediffAddEntry 목록), `addAbilities` (AbilityDef 목록) |
| 전투 | `verbs`, `tools`, `replaceNativeVerbs`, `replaceNativeTools`, `verbGizmoOptions` |
| 작업 | `disabledWorkTypesOnTransform`, `disabledWorkTagsOnTransform`, `suppressIdeologyUncoveredThoughts` |
| 이데올로기 | `linkedSacredAnimalDef` — 숭배 동물 일치 시 규율 단계별 기분 (-8/-3/+2/+5/+8). 규율 `SSF_Shapeshifting` (혐오/못마땅/무관심/존중/숭고) |
| VFX/SFX | `transformEnterSound`/`ExitSound`, `transformEnterEffecter`/`ExitEffecter`, `transformEnterFleck`/`ExitFleck` (+Count, +Scale), FX 지연/쿨다운 틱 |
| 앰비언트 VFX | `ambientEffecter`, `ambientFleck`, `ambientFleckIntervalTicks`, `ambientFleckScale` |
| 해제 부산물 | `revertDrops` (ThingDefCountClass 목록), `revertAddHediffs` (HediffDef 목록) |
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
<!-- 1. FormDef — 비주얼/장비/도구 -->
<ShapeshifterFramework.ShapeshiftFormDef ParentName="SSF_BaseForm_Animal">
  <defName>MyForm</defName>
  <label>나의 폼</label>
  <body><mode>Replace</mode><replacementTexPath>MyMod/Textures/MyForm</replacementTexPath></body>
  <durationTicks>30000</durationTicks>
</ShapeshifterFramework.ShapeshiftFormDef>

<!-- 2. HediffDef — 진입점 + 스탯 보정 -->
<HediffDef ParentName="SSF_ShapeshiftFormBase">
  <defName>MyFormHediff</defName>
  <label>나의 폼</label>
  <stages><li><statOffsets><MoveSpeed>1.0</MoveSpeed></statOffsets></li></stages>
  <comps>
    <li Class="ShapeshifterFramework.Hediffs.HediffCompProperties_ShapeshiftCore">
      <formDef>MyForm</formDef>
    </li>
  </comps>
</HediffDef>

<!-- 3. AbilityDef — 트리거 -->
<AbilityDef ParentName="SSF_BaseSelfShiftAbility">
  <defName>MyAbility</defName>
  <label>나의 변신</label>
  <comps>
    <li Class="ShapeshifterFramework.Comps.CompProperties_AbilityGiveHediff_Shapeshift">
      <hediffDef>MyFormHediff</hediffDef>
    </li>
  </comps>
</AbilityDef>
```
