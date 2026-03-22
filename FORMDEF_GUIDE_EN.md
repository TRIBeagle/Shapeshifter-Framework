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
| `linkedSacredAnimalDef` | `ThingDef` | `null` | Animal race this form represents. Grants +5 mood if it matches the pawn's ideology venerated animal |

**Shapeshifting Precept (5 stages):**

| Stage | Label | Mood | Opinion | Memory | Special |
|-------|-------|------|---------|--------|---------|
| 0 | Blasphemy against nature | -10 | -20 | -10 (5d) | **Forbidden** |
| 1 | Unnatural power | -5 | -10 | -5 (3d) | - |
| - | Don't care | - | - | - | No effects |
| 2 | Special talent | +5 | +10 | - | - |
| 3 | Divine blessing | +10 | +20 | - | - |

> At the "Blasphemy against nature" stage, the ability gizmo is disabled and all shapeshifting paths (ability/drug/projectile) are blocked.

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

### 5.2 Drug (Ingestible)
```xml
<ThingDef ParentName="MakeableDrugBase">
  <defName>MyMod_WolfElixir</defName>
  <label>wolf elixir</label>
  <ingestible>
    <outcomeDoers>
      <li Class="ShapeshifterFramework.Comps.IngestionOutcomeDoer_Shapeshift">
        <hediffDef>MyMod_Hediff_WolfForm</hediffDef>
      </li>
    </outcomeDoers>
  </ingestible>
</ThingDef>
```

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
    </li>
  </comps>
</ThingDef>

<!-- Target-select scroll -->
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
