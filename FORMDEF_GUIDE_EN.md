# Shapeshifter Framework — FormDef Creation Guide

> Complete reference for creating custom transformation forms.
> **HediffDef** is the entry point (stats/severity), **ShapeshiftFormDef** is the visual/behavioral data sheet.
> All fields are **optional** unless noted. Omitted fields use vanilla defaults.

---

## Table of Contents
1. [Quick Start](#1-quick-start)
2. [Abstract Base Forms](#2-abstract-base-forms)
3. [Field Reference](#3-field-reference)
4. [HediffDef Configuration](#4-hediffdef-configuration)
5. [Trigger System](#5-trigger-system)
6. [Events & External Integration](#6-events--external-integration)
7. [Complete Example](#7-complete-example)
8. [Combat Extended Compatibility](#8-combat-extended-compatibility)

---

## 1. Quick Start

Every shapeshift needs two defs: a **ShapeshiftFormDef** (visuals/behavior) and a **HediffDef** (stats/entry point).

### Minimal FormDef
```xml
<ShapeshifterFramework.ShapeshiftFormDef ParentName="SSF_BaseForm_Animal">
  <defName>MyMod_WolfForm</defName>
  <label>wolf form</label>
  <description>Transform into a wolf.</description>
  <body>
    <mode>Replace</mode>
    <replacementTexPath>MyMod/Pawn/Wolf</replacementTexPath>
  </body>
  <bodyDrawScale>0.8</bodyDrawScale>
  <durationTicks>30000</durationTicks>  <!-- 12 hours -->
</ShapeshifterFramework.ShapeshiftFormDef>
```

### Minimal HediffDef
```xml
<HediffDef ParentName="SSF_ShapeshiftFormBase">
  <defName>MyMod_Hediff_WolfForm</defName>
  <label>wolf form</label>
  <description>Transformed into a wolf.</description>
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

### Minimal AbilityDef (Trigger)
```xml
<AbilityDef ParentName="SSF_BaseSelfShiftAbility">
  <defName>MyMod_Ability_Wolf</defName>
  <label>wolf form</label>
  <description>Transform into a wolf.</description>
  <comps>
    <li Class="ShapeshifterFramework.Comps.CompProperties_AbilityGiveHediff_Shapeshift">
      <hediffDef>MyMod_Hediff_WolfForm</hediffDef>
    </li>
  </comps>
</AbilityDef>
```

---

## 2. Abstract Base Forms

Three abstract parents are provided. Choose one as `ParentName` based on your form type.

### SSF_BaseForm_Animal
Full creature transformation. Hides all human parts (head, hair, beard, tattoos), all apparel/weapons/genes/hediff graphics. Apparel and weapons go to inventory.

### SSF_BaseForm_Humanoid
Semi-human transformation. Keeps body, head, hair, beard, tattoos visible. Only hides Overhead apparel layer (helmets). Gear stays equipped.

### SSF_BaseForm_Armored
Power suit / armor transformation. Keeps human appearance and existing gear. Conflicting gear goes to inventory. Apparel equip locked, weapons unlocked.

### Abstract Ability Parents

| Parent | Type | Range | Warmup | Hostile |
|--------|------|-------|--------|---------|
| `SSF_BaseSelfShiftAbility` | Self-cast | 0 | 0s | No |
| `SSF_BaseTargetedShiftAbility` | Target pawn | 15 | 1.0s | No |
| `SSF_BaseAoEShiftAbility` | AoE location | 25 | 2.5s | Yes |

### Abstract HediffDef Parent

`SSF_ShapeshiftFormBase` — pre-configured with `Hediff_ShapeshiftForm` class, `HediffCompProperties_ShapeshiftCore` comp, good defaults (isBad=false, severity=1, etc.). Always use this as parent for form hediffs.

---

## 3. Field Reference

### 3.1 Race & Mutant Filters
| Field | Type | Description |
|-------|------|-------------|
| `formAllowedRaces` | `List<ThingDef>` | Only these races can use this form. Empty = no restriction. Null entries produce ConfigError. |
| `formDisallowedRaces` | `List<ThingDef>` | These races are blocked. Takes priority over allow. Null entries produce ConfigError. |
| `formAllowedMutants` | `List<MutantDef>` | [Anomaly] Only these mutant types allowed. Null entries produce ConfigError. |
| `formDisallowedMutants` | `List<MutantDef>` | [Anomaly] These mutant types blocked. Null entries produce ConfigError. |

```xml
<formAllowedRaces><li>Human</li></formAllowedRaces>
<formDisallowedRaces><li>Waster</li></formDisallowedRaces>
```

### 3.2 Scale & Offset
| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `bodyDrawScale` | `float?` | 1.0 | Body render scale |
| `headDrawScale` | `float?` | 1.0 | Head render scale |
| `bodyOffset` | `Vector2?` | (0,0) | Body position offset (x, z) |
| `headOffset` | `Vector2?` | (0,0) | Head position offset (x, z) |
| `portraitDrawScale` | `float?` | 1.0 | Portrait UI scale |

```xml
<bodyDrawScale>1.5</bodyDrawScale>
<headDrawScale>0.8</headDrawScale>
<bodyOffset>(0, -0.1)</bodyOffset>
<portraitDrawScale>1.2</portraitDrawScale>
```

### 3.3 Part Overrides
Each part (`body`, `head`, `hair`, `beard`, `tattooBody`, `tattooHead`) accepts a `PartOverrideOption`:

| Field | Type | Description |
|-------|------|-------------|
| `mode` | `PartControlMode` | `Default` / `Hidden` / `Replace` |
| `replacementTexPath` | `string` | Texture path (when mode=Replace) |
| `swimmingReplacementTexPath` | `string` | Swimming variant texture |
| `color` | `Color?` | Tint color |
| `swimmingColor` | `Color?` | Swimming variant color |
| `shaderTypeDefName` | `string` | Shader override (e.g., `Transparent`) |
| `swimmingShaderTypeDefName` | `string` | Swimming shader override |
| `shadowVolume` | `Vector3?` | Shadow box size |
| `shadowOffset` | `Vector3?` | Shadow position offset |
| `male` | `PartOverrideOption` | Male-specific override |
| `female` | `PartOverrideOption` | Female-specific override |

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

### 3.4 Graphic Visibility Filters
Control which apparel, weapons, genes, and hediffs are rendered during transformation.

**Filter logic:**
- `renderHide*` = hide matching items (blacklist)
- `renderShow*` = override hide and force show (whitelist, takes priority)
- Special value `All` = match everything
- Wildcard `*` prefix/suffix supported (e.g., `Flak*`, `*Jacket`)

| Field | Matches Against |
|-------|----------------|
| `renderHideApparelLayers` / `renderShowApparelLayers` | Apparel layer names (`Overhead`, `Shell`, `Middle`, etc.) |
| `renderHideApparelDefNames` / `renderShowApparelDefNames` | Apparel defName |
| `renderHideWeaponTags` / `renderShowWeaponTags` | Weapon tags |
| `renderHideWeaponDefNames` / `renderShowWeaponDefNames` | Weapon defName |
| `renderHideGeneExclusionTags` / `renderShowGeneExclusionTags` | Gene exclusion tags |
| `renderHideGeneDefNames` / `renderShowGeneDefNames` | Gene defName |
| `renderHideHediffDefNames` / `renderShowHediffDefNames` | Hediff defName |

```xml
<!-- Hide all apparel except power armor -->
<renderHideApparelLayers><li>All</li></renderHideApparelLayers>
<renderShowApparelDefNames><li>Apparel_PowerArmor</li></renderShowApparelDefNames>

<!-- Hide all genes except specific ones -->
<renderHideGeneExclusionTags><li>All</li></renderHideGeneExclusionTags>
<renderShowGeneDefNames><li>Gene_ToughSkin</li></renderShowGeneDefNames>
```

### 3.5 Equipment Handling
| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `apparelOnTransform` | `GearHandling` | `Keep` | `Keep` / `Inventory` / `Drop` |
| `weaponsOnTransform` | `GearHandling` | `Keep` | Same options |
| `apparelEquipLock` | `EquipLockMode` | `Auto` | `Auto` / `Locked` / `Unlocked` |
| `weaponEquipLock` | `EquipLockMode` | `Auto` | Same options |
| `conflictingGearHandling` | `GearHandling` | `Inventory` | Where spawned gear conflicts go |

**EquipLockMode.Auto** logic:
- If gear goes to Inventory/Drop → lock that slot (prevent equipping during transform)
- If gear is Kept → unlock that slot

```xml
<apparelOnTransform>Inventory</apparelOnTransform>
<weaponsOnTransform>Drop</weaponsOnTransform>
<apparelEquipLock>Locked</apparelEquipLock>
<weaponEquipLock>Unlocked</weaponEquipLock>
```

### 3.6 Spawn Gear
Spawn equipment when transforming. Removed automatically on revert.

| Field | Type | Description |
|-------|------|-------------|
| `spawnApparelOnTransform` | `List<ThingDef>` | Apparel to spawn and wear. Must be IsApparel; non-apparel entries produce ConfigError. |
| `spawnWeaponOnTransform` | `List<ThingDef>` | Weapons to spawn and equip. Must be IsWeapon; non-weapon entries produce ConfigError. |
| `spawnApparelStuff` | `ThingDef` | Material for spawned apparel |
| `spawnWeaponStuff` | `ThingDef` | Material for spawned weapons |

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

### 3.7 Render Nodes
Add extra render layers (ears, tails, wings, etc.) using vanilla `PawnRenderNodeProperties`.

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

### 3.8 Type & Color Overrides
| Field | Type | Description |
|-------|------|-------------|
| `bodyType` | `BodyTypeDef` | Override body type (Thin, Fat, Hulk, etc.) |
| `headType` | `HeadTypeDef` | Override head type |
| `hairColor` | `Color?` | Override hair color |
| `skinColor` | `Color?` | Override skin color |

```xml
<bodyType>Hulk</bodyType>
<headType>Stump</headType>
<hairColor>(0.2, 0.2, 0.2)</hairColor>
<skinColor>(0.6, 0.5, 0.4)</skinColor>
```

### 3.9 Sustain Conditions
Form auto-reverts when conditions are no longer met. `sustainMode` controls whether **All** or **Any** condition must be satisfied.

| Field | Type | Description |
|-------|------|-------------|
| `sustainApparels` | `List<ThingDef>` | Must wear these apparel |
| `sustainWeapons` | `List<ThingDef>` | Must equip these weapons |
| `sustainHediffs` | `List<HediffDef>` | Must have these hediffs |
| `sustainGenes` | `List<GeneDef>` | [Biotech] Must have these genes. Null entries produce ConfigError. |
| `sustainMode` | `SustainMode?` | `All` (default) / `Any` |

```xml
<sustainHediffs>
  <li>MyMod_GuardianMark</li>
</sustainHediffs>
<sustainMode>Any</sustainMode>
```

### 3.10 Added Effects
| Field | Type | Description |
|-------|------|-------------|
| `addHediffs` | `List<HediffAddEntry>` | Hediffs applied on transform, removed on revert |
| `addAbilities` | `List<AbilityDef>` | Abilities granted on transform, removed on revert |

**HediffAddEntry fields:**
| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `hediff` | `HediffDef` | **required** | Hediff to add |
| `targetPart` | `BodyPartDef` | null | Specific body part |
| `targetGroups` | `List<BodyPartGroupDef>` | null | Body part groups to match |
| `severity` | `float?` | null | Initial severity |
| `addedPartPolicy` | `AddedPartPolicy` | `ForceAdd` | `ForceAdd` / `StrictFleshOnly` / `RegrowFleshOnly` |

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

### 3.11 Verbs & Tools (Combat)
| Field | Type | Description |
|-------|------|-------------|
| `verbs` | `List<VerbProperties>` | Custom ranged/melee verbs |
| `tools` | `List<Tool>` | Custom melee tools (bite, claw, etc.) |
| `replaceNativeVerbs` | `bool?` | Replace all vanilla verbs with form verbs |
| `replaceNativeTools` | `bool?` | Replace all vanilla tools with form tools |
| `verbGizmoOptions` | `List<VerbGizmoOption>` | Per-verb gizmo labels/icons/toggles |
| `damageSourceDef` | `ThingDef` | Override damage source for verbs |

**VerbGizmoOption fields:**
| Field | Type | Description |
|-------|------|-------------|
| `verbLabel` | `string` | Match verb by its label |
| `label` | `string` | Command button label |
| `desc` | `string` | Command button description |
| `toggleLabel` | `string` | Auto-attack toggle label |
| `toggleDesc` | `string` | Auto-attack toggle description |
| `iconPath` | `string` | Icon override |
| `durationCostTicks` | `int` | Shift duration deducted per use (0 = free). Burst weapons deduct once per burst. |

```xml
<verbs>
  <li>
    <verbClass>Verb_MeleeAttackDamage</verbClass>
    <label>bite</label>
    <meleeDamageBaseAmount>20</meleeDamageBaseAmount>
    <meleeDamageDef>Bite</meleeDamageDef>
  </li>
</verbs>
<tools>
  <li>
    <label>claws</label>
    <capacities><li>Scratch</li></capacities>
    <power>15</power>
    <cooldownTime>1.5</cooldownTime>
    <linkedBodyPartsGroup>FrontLeftPaw</linkedBodyPartsGroup>
  </li>
</tools>
<replaceNativeTools>true</replaceNativeTools>
<verbGizmoOptions>
  <li>
    <verbLabel>bite</verbLabel>
    <label>Bite Attack</label>
    <desc>Powerful jaw attack</desc>
    <toggleLabel>Auto-bite</toggleLabel>
    <toggleDesc>Toggle automatic bite attacks</toggleDesc>
    <durationCostTicks>2500</durationCostTicks> <!-- costs ~1 hour of shift time per use -->
  </li>
</verbGizmoOptions>
```

### 3.12 Melee Sounds
| Field | Type | Description |
|-------|------|-------------|
| `soundMeleeHitPawn` | `SoundDef` | Hit pawn sound |
| `soundMeleeHitBuilding` | `SoundDef` | Hit building sound |
| `soundMeleeMiss` | `SoundDef` | Miss sound |

### 3.13 Work Restrictions
| Field | Type | Description |
|-------|------|-------------|
| `disabledWorkTypesOnTransform` | `List<WorkTypeDef>` | Specific work types disabled |
| `disabledWorkTagsOnTransform` | `WorkTags` | Work tags disabled (flags) |

```xml
<disabledWorkTypesOnTransform>
  <li>Cooking</li>
  <li>Crafting</li>
</disabledWorkTypesOnTransform>
<disabledWorkTagsOnTransform>Intellectual</disabledWorkTagsOnTransform>
```

### 3.14 Ideology
| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `suppressIdeologyUncoveredThoughts` | `bool` | `true` | Suppress "uncovered" thoughts from Ideology |
| `linkedSacredAnimalDef` | `ThingDef` | `null` | Animal race this form represents. Mood varies by precept stage when matching venerated animal (-8 / -3 / +2 / +5 / +8) |

**Shapeshifting Precept (5 stages):**

| Stage | Label | Mood | Opinion | Memory | Special |
|-------|-------|------|---------|--------|---------|
| 0 | Blasphemy against nature | -10 | -20 | -10 (5d) | **Self-initiated forbidden** |
| 1 | Unnatural power | -5 | -10 | -5 (3d) | - |
| - | Don't care | - | - | - | No effects |
| 2 | Special talent | +5 | +10 | - | - |
| 3 | Divine blessing | +10 | +20 | - | - |

> At the "Blasphemy against nature" stage, **self-initiated** transforms are blocked: ability gizmos are disabled, drug ingestion menus are disabled, and usable items (scrolls) cannot be used. **Forced transforms by others** (abilities cast by another pawn, projectiles, surgery-administered drugs) are still allowed — the pawn suffers thought penalties but the transform proceeds normally.

### 3.15 VFX & Sound (Enter/Exit)
| Field | Type | Description |
|-------|------|-------------|
| `transformEnterSound` | `SoundDef` | Sound on transform enter |
| `transformExitSound` | `SoundDef` | Sound on transform exit |
| `transformEnterEffecter` | `EffecterDef` | Effecter on transform enter |
| `transformExitEffecter` | `EffecterDef` | Effecter on transform exit |
| `transformEnterFleck` | `FleckDef` | Fleck on transform enter |
| `transformEnterFleckCount` | `int` | Number of enter flecks (default 0) |
| `transformEnterFleckScale` | `float` | Enter fleck scale (default 1.0) |
| `transformExitFleck` | `FleckDef` | Fleck on transform exit |
| `transformExitFleckCount` | `int` | Number of exit flecks (default 0) |
| `transformExitFleckScale` | `float` | Exit fleck scale (default 1.0) |
| `transformEnterFxDelayTicks` | `int` | Delay before enter FX (default 0) |
| `transformExitFxDelayTicks` | `int` | Delay before exit FX (default 0) |
| `transformFxCooldownTicks` | `int` | Min ticks between FX plays (default 30) |

```xml
<transformEnterSound>SSFTest_Sound_DarkKnightEnter</transformEnterSound>
<transformEnterEffecter>SSFTest_Effecter_DarkKnightEnter</transformEnterEffecter>
<transformEnterFleck>FleckStatic_PsychicPulse</transformEnterFleck>
<transformEnterFleckCount>5</transformEnterFleckCount>
<transformEnterFleckScale>2.0</transformEnterFleckScale>
<transformEnterFxDelayTicks>10</transformEnterFxDelayTicks>
```

### 3.16 Ambient VFX
Persistent effects during the transformation.

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `ambientEffecter` | `EffecterDef` | null | Sustained effecter |
| `ambientFleck` | `FleckDef` | null | Periodically spawned fleck |
| `ambientFleckIntervalTicks` | `int` | 60 | Spawn interval (ticks) |
| `ambientFleckScale` | `float` | 1.0 | Fleck scale |

```xml
<ambientFleck>FleckStatic_PsychicEffect</ambientFleck>
<ambientFleckIntervalTicks>120</ambientFleckIntervalTicks>
<ambientFleckScale>0.5</ambientFleckScale>
```

### 3.17 Revert Byproducts
| Field | Type | Description |
|-------|------|-------------|
| `revertDrops` | `List<ThingDefCountClass>` | Items dropped on revert |
| `revertAddHediffs` | `List<HediffAddEntry>` | Hediffs applied on revert |

```xml
<revertDrops>
  <li><thingDef>WoolMuffalo</thingDef><count>10</count></li>
</revertDrops>
<revertAddHediffs>
  <li><hediff>MyMod_Exhaustion</hediff><severity>0.5</severity></li>
</revertAddHediffs>
```

### 3.18 Voice Overrides
| Field | Type | Description |
|-------|------|-------------|
| `soundCall` | `SoundDef` | Idle/call sound |
| `soundWounded` | `SoundDef` | Wounded sound |
| `soundDeath` | `SoundDef` | Death sound |
| `soundAngry` | `SoundDef` | Angry/aggro sound |
| `soundEating` | `SoundDef` | Eating sound |

### 3.19 Blood & Flesh
| Field | Type | Description |
|-------|------|-------------|
| `bloodDef` | `ThingDef` | Blood filth override |
| `bloodSmearDef` | `ThingDef` | Blood smear override |
| `fleshType` | `FleshTypeDef` | Flesh type override |

### 3.20 UI & Duration
| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `durationTicks` | `int?` | null (permanent) | Form duration. null = no timer. |
| `canRevertVoluntarily` | `bool` | `true` | Show revert gizmo |
| `revertOnDowned` | `bool` | `false` | Auto-revert when downed |
| `gizmoIconPathEnter` | `string` | null | Custom enter gizmo icon |
| `gizmoIconPathRevert` | `string` | null | Custom revert gizmo icon |

---

## 4. HediffDef Configuration

### HediffCompProperties_ShapeshiftCore
This comp links HediffDef to FormDef and allows per-hediff behavior overrides.

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `formDef` | `ShapeshiftFormDef` | null | Target form. null = runtime `SetFormDef()`. |
| `durationTicks` | `int?` | null | Override FormDef.durationTicks |
| `canRevertVoluntarily` | `bool?` | null | Override FormDef.canRevertVoluntarily |
| `revertOnDowned` | `bool?` | null | Override FormDef.revertOnDowned |
| `sustainApparels` | `List<ThingDef>` | null | Override FormDef.sustainApparels |
| `sustainWeapons` | `List<ThingDef>` | null | Override FormDef.sustainWeapons |
| `sustainHediffs` | `List<HediffDef>` | null | Override FormDef.sustainHediffs |
| `sustainGenes` | `List<GeneDef>` | null | [Biotech] Override FormDef.sustainGenes |
| `sustainMode` | `SustainMode?` | null | Override FormDef.sustainMode |
| `revertDrops` | `List<ThingDefCountClass>` | null | Override FormDef.revertDrops |
| `revertAddHediffs` | `List<HediffAddEntry>` | null | Override FormDef.revertAddHediffs |

**null = use FormDef value. Explicit value = override.**

### N:1 Mapping Example
Multiple HediffDefs can share one FormDef with different stats:
```xml
<!-- Same wolf visuals, different stats -->
<HediffDef ParentName="SSF_ShapeshiftFormBase">
  <defName>MyMod_Hediff_WolfAlpha</defName>
  <label>alpha wolf</label>
  <stages><li><statOffsets><MoveSpeed>2.0</MoveSpeed></statOffsets></li></stages>
  <comps><li Class="ShapeshifterFramework.Hediffs.HediffCompProperties_ShapeshiftCore">
    <formDef>MyMod_WolfForm</formDef>
    <durationTicks>60000</durationTicks>
  </li></comps>
</HediffDef>

<HediffDef ParentName="SSF_ShapeshiftFormBase">
  <defName>MyMod_Hediff_WolfPup</defName>
  <label>wolf pup</label>
  <stages><li><statOffsets><MoveSpeed>0.5</MoveSpeed></statOffsets></li></stages>
  <comps><li Class="ShapeshifterFramework.Hediffs.HediffCompProperties_ShapeshiftCore">
    <formDef>MyMod_WolfForm</formDef>
    <durationTicks>15000</durationTicks>
  </li></comps>
</HediffDef>
```

---

## 5. Trigger System

### 5.1 Ability (Self / Target / AoE)
```xml
<!-- Self-cast -->
<AbilityDef ParentName="SSF_BaseSelfShiftAbility">
  <defName>MyMod_Ability_Wolf</defName>
  <label>wolf form</label>
  <comps>
    <li Class="ShapeshifterFramework.Comps.CompProperties_AbilityGiveHediff_Shapeshift">
      <hediffDef>MyMod_Hediff_WolfForm</hediffDef>
    </li>
  </comps>
</AbilityDef>

<!-- Targeted (buff ally) -->
<AbilityDef ParentName="SSF_BaseTargetedShiftAbility">
  <defName>MyMod_Ability_BuffAlly</defName>
  <label>bestow wolf form</label>
  <comps>
    <li Class="ShapeshifterFramework.Comps.CompProperties_AbilityGiveHediff_Shapeshift">
      <hediffDef>MyMod_Hediff_WolfForm</hediffDef>
    </li>
  </comps>
</AbilityDef>

<!-- AoE (hostile only) -->
<AbilityDef ParentName="SSF_BaseAoEShiftAbility">
  <defName>MyMod_Ability_MassPolymorph</defName>
  <label>mass polymorph</label>
  <comps>
    <li Class="ShapeshifterFramework.Comps.CompProperties_AbilityGiveHediff_Shapeshift">
      <hediffDef>MyMod_Hediff_SheepForm</hediffDef>
      <affectHostileOnly>true</affectHostileOnly>
    </li>
    <li Class="CompProperties_AbilityEffectRadius"><radius>5</radius></li>
  </comps>
</AbilityDef>
```

**CompProperties_AbilityGiveHediff_Shapeshift extra fields:**
| Field | Type | Description |
|-------|------|-------------|
| `hediffDef` | `HediffDef` | **required** — inherited from vanilla |
| `allowedRaces` | `List<ThingDef>` | Caster race filter |
| `disallowedRaces` | `List<ThingDef>` | Caster race block |
| `allowedMutants` | `List<MutantDef>` | [Anomaly] Caster mutant filter |
| `disallowedMutants` | `List<MutantDef>` | [Anomaly] Caster mutant block |
| `affectHostileOnly` | `bool` | Only affect hostile targets (AoE) |
| `allowedFromForms` | `List<string>` | FormDef defNames that can cast while transformed |

> **Transformation blocking:** While transformed, **all** additional transformations (including the same form) are blocked unless the current form's `defName` is listed in `allowedFromForms`. This applies consistently across all trigger types (ability, drug, item, projectile).
>
> **Self-cast gizmo auto-hide:** Self-only abilities (`targetRequired = false`) are automatically hidden from the gizmo bar while transformed (unless the current form is in `allowedFromForms`). Target-required abilities remain visible.
>
> **Ability tooltip:** The ability hover tooltip automatically displays the target form name and duration (or "Permanent") — no extra configuration required.

#### Duration Cost for Abilities

Add `CompProperties_AbilityEffect_ShapeshiftDurationCost` as an additional comp to deduct shift time on ability use. Works with any ability — shift or non-shift.

```xml
<comps>
  <!-- main ability effect (e.g. stun) -->
  <li Class="CompProperties_AbilityGiveHediff">
    <compClass>CompAbilityEffect_GiveHediff</compClass>
    <hediffDef>Stunned</hediffDef>
  </li>
  <!-- shift duration cost -->
  <li Class="ShapeshifterFramework.Comps.CompProperties_AbilityEffect_ShapeshiftDurationCost">
    <durationCostTicks>7500</durationCostTicks>        <!-- ~3 hours of shift time -->
    <requireTransformed>true</requireTransformed>       <!-- block if not transformed (default true) -->
  </li>
</comps>
```

| Field | Type | Description |
|-------|------|-------------|
| `durationCostTicks` | `int` | Ticks deducted from shift timer on use |
| `requireTransformed` | `bool` | If true (default), gizmo is disabled when not transformed |

### 5.2 Drug (Ingestible)
```xml
<ThingDef ParentName="MakeableDrugBase">
  <defName>MyMod_WolfElixir</defName>
  <label>wolf elixir</label>
  <ingestible>
    <outcomeDoers>
      <li Class="ShapeshifterFramework.Comps.IngestionOutcomeDoer_Shapeshift">
        <hediffDef>MyMod_Hediff_WolfForm</hediffDef>
        <!-- Optional: allow ingestion while in specific forms -->
        <!-- <allowedFromForms><li>MyMod_BeastkinForm</li></allowedFromForms> -->
      </li>
    </outcomeDoers>
  </ingestible>
</ThingDef>
```

| Field | Type | Description |
|-------|------|-------------|
| `hediffDef` | `HediffDef` | **required** — HediffDef with `HediffComp_ShapeshiftCore` |
| `allowedFromForms` | `List<string>` | FormDef defNames that allow ingestion while transformed |

### 5.3 Usable Item (Scroll / Artifact)
```xml
<!-- Self-use scroll -->
<ThingDef ParentName="ResourceBase">
  <defName>MyMod_WolfScroll</defName>
  <label>scroll of wolf form</label>
  <comps>
    <li Class="CompProperties_Usable"><useJob>UseItem</useJob><useLabel>Use scroll</useLabel></li>
    <li Class="CompProperties_UseEffectDestroySelf"/>
    <li Class="ShapeshifterFramework.Comps.CompProperties_UseEffect_Shapeshift">
      <hediffDef>MyMod_Hediff_WolfForm</hediffDef>
      <!-- Optional: allow use while in specific forms -->
      <!-- <allowedFromForms><li>MyMod_BeastkinForm</li></allowedFromForms> -->
    </li>
  </comps>
</ThingDef>

<!-- Target-select scroll (usable while the user is transformed) -->
<ThingDef ParentName="ResourceBase">
  <defName>MyMod_WolfScroll_Target</defName>
  <label>scroll of bestow wolf form</label>
  <comps>
    <li Class="CompProperties_Usable"><useJob>UseItem</useJob><useLabel>Use on target</useLabel></li>
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

| Field | Type | Description |
|-------|------|-------------|
| `hediffDef` | `HediffDef` | **required** — HediffDef with `HediffComp_ShapeshiftCore` |
| `allowedFromForms` | `List<string>` | FormDef defNames that allow self-use while transformed |

> **Target-select items:** Items with `CompTargetable` (e.g., `CompProperties_TargetablePawn`) can always be used by a transformed pawn, since the effect targets another pawn. The target's transformation state is checked instead.

### 5.3.1 Duration Extension — Drug

Extends the remaining duration of an active shapeshift. Separate from `IngestionOutcomeDoer_Shapeshift` (which applies a new transformation).

```xml
<ThingDef ParentName="MakeableDrugBase">
  <defName>MyMod_BearRefreshElixir</defName>
  <label>bear refresh elixir</label>
  <ingestible>
    <outcomeDoers>
      <li Class="ShapeshifterFramework.Comps.IngestionOutcomeDoer_ExtendShapeshift">
        <extendTicks>30000</extendTicks>
        <!-- Optional: only extend if in a specific form. Omit to extend any form. -->
        <targetFormDef>MyMod_BearForm</targetFormDef>
        <!-- Optional: allow extending beyond original max duration (default false) -->
        <allowExtendBeyondMax>false</allowExtendBeyondMax>
      </li>
    </outcomeDoers>
  </ingestible>
</ThingDef>
```

| Field | Type | Description |
|-------|------|-------------|
| `extendTicks` | `int` | **required** — Ticks to extend (+) or reduce (−) |
| `targetFormDef` | `string` | FormDef defName. If set, only extends when pawn is in that form. If omitted, extends any active form |
| `allowExtendBeyondMax` | `bool` | If `true`, duration can exceed the form's original max. Default `false` |

### 5.3.2 Duration Extension — Item

```xml
<ThingDef ParentName="ResourceBase">
  <defName>MyMod_RefreshScroll</defName>
  <label>refresh scroll</label>
  <comps>
    <li Class="CompProperties_Usable"><useJob>UseItem</useJob><useLabel>Use scroll</useLabel></li>
    <li Class="CompProperties_UseEffectDestroySelf"/>
    <li Class="ShapeshifterFramework.Comps.CompProperties_UseEffect_ExtendShapeshift">
      <extendTicks>30000</extendTicks>
      <targetFormDef>MyMod_BearForm</targetFormDef>
      <allowExtendBeyondMax>false</allowExtendBeyondMax>
    </li>
  </comps>
</ThingDef>
```

| Field | Type | Description |
|-------|------|-------------|
| `extendTicks` | `int` | **required** — Ticks to extend (+) or reduce (−) |
| `targetFormDef` | `string` | FormDef defName. If set, only extends when pawn is in that form. If omitted, extends any active form |
| `allowExtendBeyondMax` | `bool` | If `true`, duration can exceed the form's original max. Default `false` |

> **Not transformed:** If the pawn is not currently transformed (or is in the wrong form when `targetFormDef` is set), the item/drug is consumed but has no effect — a reject message is shown.

### 5.4 Projectile
```xml
<ThingDef ParentName="BaseProjectileNeolithic">
  <defName>MyMod_Proj_CursedArrow</defName>
  <thingClass>ShapeshifterFramework.Projectiles.Projectile_GiveHediff_Shapeshift</thingClass>
  <label>cursed arrow</label>
  <modExtensions>
    <li Class="ShapeshifterFramework.Projectiles.GiveHediffProjectileExtension_Shapeshift">
      <hediffDef>MyMod_Hediff_SheepForm</hediffDef>
      <aoeRadius>0</aoeRadius>
      <affectAllies>false</affectAllies>
    </li>
  </modExtensions>
</ThingDef>
```

### 5.5 Equipment → Ability Grant
```xml
<!-- Weapon that grants ability while equipped -->
<ThingDef ParentName="BaseMeleeWeapon_Cool_MakeableMetallic">
  <defName>MyMod_DarkBlade</defName>
  <label>dark blade</label>
  <comps>
    <li Class="ShapeshifterFramework.Comps.CompProperties_GiveAbility_Shapeshift">
      <ability>MyMod_Ability_DarkKnight</ability>
    </li>
  </comps>
</ThingDef>

<!-- Apparel that grants ability while worn -->
<ThingDef ParentName="ApparelBase">
  <defName>MyMod_MagicCloak</defName>
  <label>magic cloak</label>
  <comps>
    <li Class="ShapeshifterFramework.Comps.CompProperties_GiveAbility_Shapeshift">
      <ability>MyMod_Ability_Phantom</ability>
    </li>
  </comps>
</ThingDef>
```

### 5.6 Gene → Ability
```xml
<GeneDef MayRequire="Ludeon.RimWorld.Biotech">
  <defName>MyMod_Gene_WolfBlood</defName>
  <label>wolf blood</label>
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

### 5.7 AutoShift (Conditional Trigger)
```xml
<HediffDef>
  <defName>MyMod_Hediff_WerewolfCurse</defName>
  <label>werewolf curse</label>
  <hediffClass>HediffWithComps</hediffClass>
  <comps>
    <li Class="ShapeshifterFramework.Hediffs.HediffCompProperties_AutoShift">
      <hediffDef>MyMod_Hediff_WolfForm</hediffDef>
      <triggerSunGlowBelow>0.3</triggerSunGlowBelow>  <!-- deep night -->
      <healthThreshold>0.3</healthThreshold>           <!-- below 30% HP -->
      <triggerInCombat>true</triggerInCombat>
      <checkIntervalTicks>120</checkIntervalTicks>     <!-- every 2 seconds -->
      <triggerOnce>false</triggerOnce>                 <!-- repeatable -->
    </li>
  </comps>
</HediffDef>
```

**HediffCompProperties_AutoShift fields:**
| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `hediffDef` | `HediffDef` | null | **required** — form hediff to apply |
| `healthThreshold` | `float?` | null | Trigger when HP% below this |
| `severityThreshold` | `float?` | null | Trigger when this hediff's severity >= value |
| `triggerMentalStates` | `List<MentalStateDef>` | null | Trigger on these mental states |
| `triggerSunGlowBelow` | `float?` | null | Trigger when sun glow below this |
| `triggerInCombat` | `bool` | false | Trigger when drafted + enemies nearby |
| `checkIntervalTicks` | `int` | 120 | Check interval (ticks) |
| `triggerOnce` | `bool` | false | Remove this hediff after first trigger |

Conditions are evaluated with **OR** logic — any single condition triggers the shift.

### 5.8 Multi-Stage Form Chains
Use `allowedFromForms` to enable casting while already transformed:

```xml
<!-- Stage 1: Beastkin (humanoid) -->
<AbilityDef ParentName="SSF_BaseSelfShiftAbility">
  <defName>MyMod_Ability_Beastkin</defName>
  <comps>
    <li Class="ShapeshifterFramework.Comps.CompProperties_AbilityGiveHediff_Shapeshift">
      <hediffDef>MyMod_Hediff_Beastkin</hediffDef>
    </li>
  </comps>
</AbilityDef>

<!-- Stage 2: Full Beast (requires Beastkin form) -->
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

## 6. Events & External Integration

### C# Events
```csharp
using ShapeshifterFramework.Utilities;

// Subscribe to transformation events
ShapeshiftCoreUtility.OnFormApplied += (pawn, form) => { /* ... */ };
ShapeshiftCoreUtility.OnFormRemoved += (pawn, form) => { /* ... */ };
```

> **Important:** Event handlers are cleared on every game load (`GameComponent.FinalizeInit`).
> Do **not** register handlers in `[StaticConstructorOnStartup]` — they will be lost after the first load.
> Instead, register them in your own `GameComponent.FinalizeInit()` override to ensure they survive save/load cycles:
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
// Apply a shapeshift
ShapeshiftCoreUtility.GiveShiftHediff(pawn, hediffDef);

// Remove current shapeshift
ShapeshiftCoreUtility.RemoveForm(pawn);

// Query current form
if (ShapeshiftCoreUtility.TryGetCore(pawn, out var core))
{
    bool isShifted = core.isTransformed;
    ShapeshiftFormDef form = core.currentForm;
}

// Extend or reduce remaining duration (timed forms only; ignored for permanent)
core.ExtendDuration(2500);                // +1 hour
core.ExtendDuration(-1250);               // -0.5 hours (reaches 0 → auto-revert next tick)
core.ExtendDuration(30000, false);        // +5 hours, capped at original max duration
core.ExtendDuration(30000, true);         // +5 hours, can exceed original max
```

---

## 7. Complete Example

A wolf form with full features:

```xml
<!-- FormDef -->
<ShapeshifterFramework.ShapeshiftFormDef ParentName="SSF_BaseForm_Animal">
  <defName>MyMod_WolfForm</defName>
  <label>wolf form</label>
  <description>A fierce wolf transformation.</description>

  <!-- Visuals -->
  <body>
    <mode>Replace</mode>
    <replacementTexPath>MyMod/Pawn/Wolf</replacementTexPath>
    <swimmingReplacementTexPath>MyMod/Pawn/Wolf_Swimming</swimmingReplacementTexPath>
    <shadowVolume>(0.4, 0.0, 0.5)</shadowVolume>
  </body>
  <bodyDrawScale>0.85</bodyDrawScale>

  <!-- Duration & Revert -->
  <durationTicks>30000</durationTicks>
  <canRevertVoluntarily>true</canRevertVoluntarily>
  <revertOnDowned>true</revertOnDowned>

  <!-- Combat -->
  <tools>
    <li>
      <label>fangs</label>
      <capacities><li>Bite</li></capacities>
      <power>18</power>
      <cooldownTime>2.0</cooldownTime>
    </li>
    <li>
      <label>claws</label>
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

  <!-- Voice -->
  <soundCall>Pawn_Wolf_Call</soundCall>
  <soundWounded>Pawn_Wolf_Wounded</soundWounded>
  <soundDeath>Pawn_Wolf_Death</soundDeath>

  <!-- Blood -->
  <bloodDef>Filth_Blood</bloodDef>
  <fleshType>Normal</fleshType>

  <!-- Revert Drops -->
  <revertDrops>
    <li><thingDef>WoolMuffalo</thingDef><count>5</count></li>
  </revertDrops>

  <!-- Work Restrictions -->
  <disabledWorkTagsOnTransform>Intellectual</disabledWorkTagsOnTransform>
</ShapeshifterFramework.ShapeshiftFormDef>
```

---

## 8. Combat Extended Compatibility

SSF form verbs are **NativeVerbs** (`EquipmentSource=null`), so they are naturally exempt from CE's ammo system (`CompAmmoUser`). Forms work out of the box with CE — no code changes needed.

However, CE adds extra fields to melee tools (`armorPenetrationSharp`, `armorPenetrationBlunt`) and ranged verbs (`recoilAmount`, etc.). These default to `0` if not specified, meaning form attacks will have no armor penetration in CE unless patched.

### How to Add CE Support

Use **XPath patches** with `MayRequire="CETeam.CombatExtended"` to replace vanilla `Tool` with `CombatExtended.ToolCE` and vanilla `VerbProperties` with `CombatExtended.VerbPropertiesCE`.

**Key points:**
- `ToolCE` extends `Tool` → SSF's `List<Tool>` accepts it without code changes
- `VerbPropertiesCE` extends `VerbProperties` → same principle
- No hard dependency on CE assembly — patches only load when CE is active
- SSF does not apply any code patches for CE — modders handle it via MayRequire XML patches

### Tool Patch Example

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

### Verb Patch Example

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

> **Note:** In a real CE environment, projectiles should also be CE-specific ammo defs. The example above uses vanilla projectiles for demonstration.

### Patch File Location

Place CE patches in a folder that only loads when CE is active:
```
MyMod/
  CombatExtended/
    Patches/
      MyMod_Forms_CE.xml
  LoadFolders.xml   ← add CombatExtended folder with MayRequire
```

Or use `MayRequire` on each `<Operation>` element if placing patches alongside other patch files.

See `TestMod_SSF/CombatExtended/Patches/SSF_TestForms_CE.xml` for a working example.
