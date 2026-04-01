# Shapeshifter Framework — ShapeshiftFormDef Quick Reference

> For the full manual, see `/FORMDEF_GUIDE_EN.md` in the project root.

## Key Architecture

- **HediffDef** → Entry point for transformation. Defines stat offsets (`statOffsets`, `statFactors`, `capMods`) in `stages`
- **HediffComp_ShapeshiftCore** → Included in HediffDef `comps`. References a FormDef via `formDef` to execute shift logic
- **ShapeshiftFormDef** → Pure data sheet. Visuals, equipment, tools, sounds, VFX
- **CompProperties_AbilityGiveHediff_Shapeshift** → Cast conditions (races, mutants), success chance
- **Ability sources** → Genes, hediffs, items, drugs, scrolls, projectiles

## FormDef Fields Summary

| Category | Key Fields |
|----------|------------|
| Basic | `defName`, `label`, `description`, `formAllowedRaces`, `formDisallowedRaces`, `formAllowedMutants`, `formDisallowedMutants` |
| Scale | `bodyDrawScale`, `headDrawScale`, `portraitDrawScale`, `bodyOffset`, `headOffset` |
| Parts | `body`, `head`, `hair`, `beard`, `tattooBody`, `tattooHead` (each with `mode`, `replacementTexPath`, `color`, `shaderTypeDefName`, `male`/`female`, etc.) |
| Render Hide/Show | `renderHide*`/`renderShow*` for Apparel (Layers/DefNames), Weapons (Tags/DefNames), Genes (ExclusionTags/DefNames), Hediffs (DefNames) |
| Equipment | `apparelOnTransform`, `weaponsOnTransform` (Keep/Inventory/Drop), `apparelEquipLock`, `weaponEquipLock` (Auto/Locked/Unlocked) |
| Spawn Equip | `spawnApparelOnTransform`, `spawnWeaponOnTransform`, `spawnApparelStuff`, `spawnWeaponStuff`, `conflictingGearHandling` |
| Render Nodes | `renderNodeProperties` (PawnRenderNodeProperties list) |
| Type/Color | `bodyType`, `headType`, `hairColor`, `skinColor` |
| Sustain | `sustainApparels`, `sustainWeapons`, `sustainHediffs`, `sustainGenes`, `sustainMode` (All/Any) |
| Additions | `addHediffs` (HediffAddEntry list), `addAbilities` (AbilityDef list) |
| Combat | `verbs`, `tools`, `replaceNativeVerbs`, `replaceNativeTools`, `verbGizmoOptions` |
| Work | `disabledWorkTypesOnTransform`, `disabledWorkTagsOnTransform`, `suppressIdeologyUncoveredThoughts` |
| Ideology | `linkedSacredAnimalDef` — venerated animal match → mood by precept stage (-8/-3/+2/+5/+8). Precept `SSF_Shapeshifting` (Abhorrent/Disapproved/DontCare/Respected/Sublime) |
| VFX/SFX | `transformEnterSound`/`ExitSound`, `transformEnterEffecter`/`ExitEffecter`, `transformEnterFleck`/`ExitFleck` (+Count, +Scale), FX delay/cooldown ticks |
| Ambient VFX | `ambientEffecter`, `ambientFleck`, `ambientFleckIntervalTicks`, `ambientFleckScale` |
| Revert | `revertDrops` (ThingDefCountClass list), `revertAddHediffs` (HediffDef list) |
| UI | `gizmoIconPathEnter`/`Revert`, `durationTicks`, `canRevertVoluntarily` |
| Voice | `soundCall`, `soundWounded`, `soundDeath`, `soundAngry`, `soundEating` |
| Melee SFX | `soundMeleeHitPawn`, `soundMeleeHitBuilding`, `soundMeleeMiss` |
| Blood | `bloodDef`, `bloodSmearDef`, `fleshType` |
| HAR | `showHarAddons` |
| Facial Anim | `faHeadTypeDef`, `faEyeballTypeDef`, `faLidTypeDef`, `faBrowTypeDef`, `faMouthTypeDef`, `faSkinTypeDef`, `faEyeColor`, `faEyeColor2` |

## Abstract Base Forms

| Base | Equipment | Apparel Hiding |
|------|-----------|----------------|
| `SSF_BaseForm_Animal` | Drop all | Hide all |
| `SSF_BaseForm_Humanoid` | Keep all | Overhead only |
| `SSF_BaseForm_Armored` | Keep all | None |

## Minimal Example

```xml
<!-- 1. FormDef — visuals, gear, tools -->
<ShapeshifterFramework.ShapeshiftFormDef ParentName="SSF_BaseForm_Animal">
  <defName>MyForm</defName>
  <label>My Form</label>
  <body><mode>Replace</mode><replacementTexPath>MyMod/Textures/MyForm</replacementTexPath></body>
  <durationTicks>30000</durationTicks>
</ShapeshifterFramework.ShapeshiftFormDef>

<!-- 2. HediffDef — entry point + stat offsets -->
<HediffDef ParentName="SSF_ShapeshiftFormBase">
  <defName>MyFormHediff</defName>
  <label>my form</label>
  <stages><li><statOffsets><MoveSpeed>1.0</MoveSpeed></statOffsets></li></stages>
  <comps>
    <li Class="ShapeshifterFramework.Hediffs.HediffCompProperties_ShapeshiftCore">
      <formDef>MyForm</formDef>
    </li>
  </comps>
</HediffDef>

<!-- 3. AbilityDef — trigger -->
<AbilityDef ParentName="SSF_BaseSelfShiftAbility">
  <defName>MyAbility</defName>
  <label>my shift</label>
  <comps>
    <li Class="ShapeshifterFramework.Comps.CompProperties_AbilityGiveHediff_Shapeshift">
      <hediffDef>MyFormHediff</hediffDef>
    </li>
  </comps>
</AbilityDef>
```
