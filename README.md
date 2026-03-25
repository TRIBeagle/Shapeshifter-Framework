# Shapeshifter Framework

## Status
> **WIP** — Steam Workshop 미출시. 기능 및 API가 변경될 수 있습니다.

## Overview
RimWorld 1.6 전용 변신(Shapeshift) 모드 프레임워크.
C# (.NET Framework 4.8) + Harmony 패치 기반으로, 모더가 **XML만으로** 다양한 변신 폼을 정의할 수 있습니다.

## Architecture

```
HediffDef (진입점 — 스탯/severity)
  └─ HediffCompProperties_ShapeshiftCore
       └─ formDef ──▶ ShapeshiftFormDef (비주얼/장비/사운드/VFX 데이터)
```

- **HediffDef**: 변신 상태의 스탯 보정(statOffsets/statFactors/capMods) 및 severity를 정의
- **ShapeshiftFormDef**: 비주얼, 장비 처리, 사운드, VFX 등 읽기 전용 데이터 시트
- **N:1 매핑**: 여러 HediffDef가 하나의 FormDef를 공유 가능 (같은 외형, 다른 스탯)

### Trigger Methods
| 방법 | 설명 |
|------|------|
| **AbilityDef** | 자기/타인/AoE 시전. `CompProperties_AbilityGiveHediff_Shapeshift` 사용 |
| **Drug** | `IngestionOutcomeDoer_Shapeshift`로 약물 복용 시 변신 |
| **Item (Use)** | `CompProperties_UseEffect_Shapeshift`로 소비 아이템 사용 |
| **Item (Target)** | 위 + `CompTargetable`로 대상 지정 가능 |
| **Projectile** | `Projectile_GiveHediff_Shapeshift` + AoE 반경 지원 |
| **Equipment Grant** | `CompProperties_GiveAbility_Shapeshift`로 장비 착용 시 어빌리티 부여 |
| **Gene** | Biotech `GeneDef` → `HediffCompProperties_GiveAbility`로 유전자 기반 어빌리티 |
| **AutoShift** | `HediffComp_AutoShift`로 체력/정신/밝기/전투 조건 자동 변신 |

## Features

### Transformation System
- Timed / permanent forms with optional voluntary revert
- Revert on downed, sustain conditions (apparel/weapon/hediff/gene), auto-shift triggers
- Multi-stage form chains (`allowedFromForms`)
- Revert byproducts (item drops, hediff apply)

### Visuals
- Body/head/hair/beard/tattoo per-part control (Default / Hidden / Replace)
- Gender-specific textures and swimming variants
- Scale, offset, portrait scale, shadow override
- BodyType / HeadType / hair color / skin color override
- Shader override per part
- PawnRenderNode support (ears, tails, wings, etc.)
- Apparel/weapon/gene/hediff layer visibility filtering (with wildcard `All` and `*` patterns)

### Stats & Combat
- Stats via HediffDef stages (statOffsets, statFactors, capMods)
- Custom verbs & tools with per-verb auto-attack toggle gizmos
- `replaceNativeVerbs` / `replaceNativeTools` for full override
- Melee sound override (hit pawn / hit building / miss)
- Work type / work tag restrictions

### Equipment
- Per-slot gear handling: Keep / Inventory / Drop
- Equipment lock: Auto / Locked / Unlocked (apparel/weapon 독립)
- Spawn gear on transform (with stuff material)
- Previous gear auto-reequip on revert

### VFX & Sound
- Enter/exit sound, effecter, fleck (with count, scale, delay, cooldown)
- Ambient effecter & fleck (sustained during transform)
- Voice override (call, wounded, death, angry, eating)
- Blood / blood smear / flesh type override

### Integration
- **Events**: `ShapeshiftCoreUtility.OnFormApplied` / `OnFormRemoved` callbacks
- **API**: `GiveShiftHediff()`, `RemoveForm()`, `TryGetCore()`, `ExtendDuration()` for external mod access

## Documentation

| Document | Language |
|----------|----------|
| [FormDef Creation Guide (EN)](FORMDEF_GUIDE_EN.md) | English |
| [FormDef 제작 가이드 (KO)](FORMDEF_GUIDE_KO.md) | 한국어 |

## Compatibility

| Mod | Status |
|-----|--------|
| **RimWorld 1.6** | Required |
| **Harmony** | Required |
| **HAR (Humanoid Alien Races)** | Compatible — body addon scaling, head addon detection |
| **Facial Animation** | Compatible — face component backup/restore |
| **Simple Sidearms** | Compatible — memory backup/restore on transform |

## Build
```bash
cd "Source/ShapeshifterFramework v1.6/ShapeshifterFramework"
dotnet build ShapeshifterFramework.csproj
```

## Credits
- **TRIBeagle** — Design, implementation
- AI-assisted development (Claude)

## License
All rights reserved. This framework is not yet released.
