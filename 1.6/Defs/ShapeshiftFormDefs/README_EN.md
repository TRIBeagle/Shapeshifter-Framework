# Shapeshifter Framework — ShapeshiftFormDef Quick Reference

> For the full manual, see `/FORMDEF_GUIDE_EN.md` in the project root.

## Key Architecture

- **ShapeshiftFormDef** → Visuals, equipment, tools, sounds, VFX, duration
- **linkedHediff (HediffDef)** → Stats (`statOffsets`, `statFactors`, `capMods`) — vanilla pattern
- **CompProperties_AbilityShiftTarget** → Cast conditions (races, mutants), success chance
- **Ability sources** → Genes, hediffs, items, drugs, scrolls, projectiles

## FormDef Fields Summary

| Category | Key Fields |
|----------|------------|
| Basic | `defName`, `label`, `description`, `linkedHediff`, `applicableRaces` |
| Scale | `bodyDrawScale`, `headDrawScale`, `portraitDrawScale`, `bodyOffset`, `headOffset` |
| Parts | `body`, `head`, `hair`, `beard`, `tattooBody`, `tattooHead` (each with `mode`, `replacementTexPath`, `color`, `shaderTypeDefName`, `male`/`female`, etc.) |
| Render Hide/Show | `renderHide*`/`renderShow*` for Apparel (Layers/DefNames), Weapons (Tags/DefNames), Genes (ExclusionTags/DefNames), Hediffs (DefNames) |
| Equipment | `apparelOnTransform`, `weaponsOnTransform` (Keep/Inventory/Drop), `apparelEquipLock`, `weaponEquipLock` (Auto/Locked/Unlocked) |
| Spawn Equip | `spawnApparelOnTransform`, `spawnWeaponOnTransform`, `spawnApparelStuff`, `spawnWeaponStuff`, `conflictingGearHandling` |
| Render Nodes | `renderNodeProperties` (PawnRenderNodeProperties list) |
| Type/Color | `bodyType`, `headType`, `hairColor`, `skinColor` |
| Sustain | `sustainApparels`, `sustainWeapons`, `sustainHediffs`, `sustainGenes`, `sustainMode` (All/Any) |
| Additions | `addHediffs` (HediffAddEntry list), `addAbilities` (AbilityDef list) |
| Combat | `verbs`, `tools`, `replaceNativeVerbs`, `replaceNativeTools`, `verbGizmoOptions`, `damageSourceDef` |
| Work | `disabledWorkTypesOnTransform`, `disabledWorkTagsOnTransform`, `suppressIdeologyUncoveredThoughts` |
| VFX/SFX | `transformEnterSound`/`ExitSound`, `transformEnterEffecter`/`ExitEffecter`, `transformEnterFleck`/`ExitFleck` (+Count, +Scale), FX delay/cooldown ticks |
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
<HediffDef>
  <defName>MyFormHediff</defName>
  <hediffClass>ShapeshifterFramework.Hediffs.Hediff_ShapeshiftForm</hediffClass>
  <label>my form</label>
  <isBad>false</isBad>
  <stages><li><statOffsets><MoveSpeed>1.0</MoveSpeed></statOffsets></li></stages>
</HediffDef>

<ShapeshifterFramework.ShapeshiftFormDef ParentName="SSF_BaseForm_Animal">
  <defName>MyForm</defName>
  <label>My Form</label>
  <linkedHediff>MyFormHediff</linkedHediff>
  <body><mode>Replace</mode><replacementTexPath>MyMod/Textures/MyForm</replacementTexPath></body>
  <durationTicks>30000</durationTicks>
</ShapeshifterFramework.ShapeshiftFormDef>

<AbilityDef ParentName="SSF_BaseSelfShiftAbility">
  <defName>MyAbility</defName>
  <label>my shift</label>
  <comps>
    <li Class="ShapeshifterFramework.Comps.CompProperties_AbilityShiftTarget">
      <formDefName>MyForm</formDefName>
    </li>
  </comps>
</AbilityDef>
```
