# Shapeshifter Framework — FormDef Creation Guide

> Complete reference for creating custom transformation forms. Reflects the HediffComp-based architecture where **HediffDef is the entry point** and **FormDef is the visual/behavioral data sheet**.

All fields are **optional** unless noted otherwise. Omitted fields fall back to vanilla defaults.

---

## Quick Start

A minimal setup requires two pieces: a **FormDef** (visuals, gear, tools) and a **HediffDef** (entry point, stats).

```xml
<!-- 1. FormDef — visual/behavioral data sheet -->
<ShapeshifterFramework.ShapeshiftFormDef>
  <defName>MyForm</defName>
  <label>My Form</label>
  <body>
    <mode>Replace</mode>
    <replacementTexPath>Things/Pawn/MyCreature/MyCreature</replacementTexPath>
  </body>
  <head><mode>Hidden</mode></head>
</ShapeshifterFramework.ShapeshiftFormDef>

<!-- 2. HediffDef — entry point with stats -->
<HediffDef ParentName="SSF_ShapeshiftFormBase">
  <defName>MyForm_Hediff</defName>
  <label>my form</label>
  <stages>
    <li>
      <statOffsets><MoveSpeed>1.5</MoveSpeed></statOffsets>
    </li>
  </stages>
  <comps>
    <li Class="ShapeshifterFramework.Hediffs.HediffCompProperties_ShapeshiftCore">
      <formDef>MyForm</formDef>
      <durationTicks>30000</durationTicks>
    </li>
  </comps>
</HediffDef>
```

Stats and capacities go on the **HediffDef stages** (vanilla `statOffsets`, `statFactors`, `capMods`). The FormDef only holds visuals, gear, tools, sounds, and sustain conditions.

---

## Abstract Base Forms

Three pre-built FormDef parents in `SSF_BaseForms.xml`:

| Parent | Equipment | Visual Hiding | Best For |
|--------|-----------|---------------|----------|
| `SSF_BaseForm_Animal` | Inventory | All parts hidden, all graphics hidden | Full creature replacement |
| `SSF_BaseForm_Humanoid` | Keep | Overhead apparel hidden | Humanlike with extras |
| `SSF_BaseForm_Armored` | Keep | None | Equipment-focused forms |

Usage: `<ShapeshifterFramework.ShapeshiftFormDef ParentName="SSF_BaseForm_Animal">`

---

## Field Reference

### 1. Identity

| Field | Type | Description |
|-------|------|-------------|
| `defName` | string | **Required.** Unique ID. |
| `label` | string | Display name. |
| `description` | string | Tooltip text. |

### 2. HediffDef Entry Point

The **HediffDef** is the entry point for all transformations. The FormDef is a pure data sheet referenced by the HediffDef's `HediffCompProperties_ShapeshiftCore.formDef`.

**Architecture:**
- HediffDef owns the comp `HediffComp_ShapeshiftCore` which references a FormDef.
- Stats/capacities are defined on HediffDef stages (vanilla `statOffsets`, `statFactors`, `capMods`).
- N:1 mapping: multiple HediffDefs can reference the **same** FormDef (same visuals, different stat profiles).
- Removing the hediff externally auto-reverts the form.

**Abstract base:** Use `SSF_ShapeshiftFormBase` as `ParentName` to inherit all required defaults (`hediffClass`, `isBad`, `initialSeverity`, etc.).

```xml
<HediffDef ParentName="SSF_ShapeshiftFormBase">
  <defName>WolfForm_Hediff</defName>
  <label>wolf form</label>
  <stages>
    <li>
      <statOffsets><MoveSpeed>2.5</MoveSpeed></statOffsets>
      <capMods>
        <li><capacity>Moving</capacity><postFactor>1.30</postFactor></li>
      </capMods>
    </li>
  </stages>
  <comps>
    <li Class="ShapeshifterFramework.Hediffs.HediffCompProperties_ShapeshiftCore">
      <formDef>WolfForm</formDef>
    </li>
  </comps>
</HediffDef>
```

**N:1 mapping example** — same visuals, different stats:

```xml
<!-- Two HediffDefs sharing one FormDef -->
<HediffDef ParentName="SSF_ShapeshiftFormBase">
  <defName>WolfForm_Normal</defName>
  <label>wolf form</label>
  <stages><li><statOffsets><MoveSpeed>1.5</MoveSpeed></statOffsets></li></stages>
  <comps>
    <li Class="ShapeshifterFramework.Hediffs.HediffCompProperties_ShapeshiftCore">
      <formDef>WolfForm</formDef>
    </li>
  </comps>
</HediffDef>

<HediffDef ParentName="SSF_ShapeshiftFormBase">
  <defName>WolfForm_Alpha</defName>
  <label>alpha wolf form</label>
  <stages><li><statOffsets><MoveSpeed>3.0</MoveSpeed><ArmorRating_Sharp>0.5</ArmorRating_Sharp></statOffsets></li></stages>
  <comps>
    <li Class="ShapeshifterFramework.Hediffs.HediffCompProperties_ShapeshiftCore">
      <formDef>WolfForm</formDef>
      <durationTicks>15000</durationTicks>
    </li>
  </comps>
</HediffDef>
```

**CompProperties override fields** (null = use FormDef default):

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `formDef` | ShapeshiftFormDef | null | **Core.** The FormDef this hediff applies. null = set at runtime via `SetFormDef()`. |
| `durationTicks` | int? | null | Override form duration. null = use FormDef value. |
| `canRevertVoluntarily` | bool? | null | Override voluntary revert. null = use FormDef value. |
| `revertOnDowned` | bool? | null | Override revert-on-downed. null = use FormDef value. |
| `sustainApparels` | List\<ThingDef\> | null | Override sustain apparel list. |
| `sustainWeapons` | List\<ThingDef\> | null | Override sustain weapon list. |
| `sustainHediffs` | List\<HediffDef\> | null | Override sustain hediff list. |
| `sustainGenes` | List\<GeneDef\> | null | Override sustain gene list (Biotech). |
| `sustainMode` | SustainMode? | null | Override sustain mode (`All` / `Any`). |
| `revertDrops` | List\<ThingDefCountClass\> | null | Override revert drops. |
| `revertAddHediffs` | List\<HediffAddEntry\> | null | Override revert hediffs. |

### 3. Race / Mutant Filters

These filter the **target** (who receives the form). They apply to all trigger paths (abilities, drugs, scrolls, projectiles).

| Field | Type | Description |
|-------|------|-------------|
| `formAllowedRaces` | List\<ThingDef\> | Only these races can receive the form. Empty = no restriction. |
| `formDisallowedRaces` | List\<ThingDef\> | These races cannot receive the form. Overrides allowed list. |
| `formAllowedMutants` | List\<MutantDef\> | Only these mutants can receive the form. (`MayRequire: Ludeon.RimWorld.Anomaly`) |
| `formDisallowedMutants` | List\<MutantDef\> | These mutants cannot receive the form. (`MayRequire: Ludeon.RimWorld.Anomaly`) |

> **Caster-side** filters (`allowedRaces`, `disallowedRaces`) are on `CompProperties_AbilityShapeshift`, not on the FormDef.

### 4. Scale & Offset

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `bodyDrawScale` | float? | 1.0 | Body rendering scale multiplier. |
| `headDrawScale` | float? | 1.0 | Head scale multiplier (applied on top of body scale). |
| `portraitDrawScale` | float? | 1.0 | Scale in the bottom-left portrait only. |
| `bodyOffset` | Vector2? | (0,0) | Body position offset (X, Z). |
| `headOffset` | Vector2? | (0,0) | Head position offset (X, Z). |

### 5. Part Overrides

Control textures, colors, and shaders for body parts: `<body>`, `<head>`, `<hair>`, `<beard>`, `<tattooBody>`, `<tattooHead>`.

Each accepts a `PartOverrideOption` block:

| Field | Description |
|-------|-------------|
| `mode` | `Default` / `Hidden` / `Replace` |
| `replacementTexPath` | Texture path (requires `Replace` mode). |
| `swimmingReplacementTexPath` | Texture used while swimming. |
| `color` | Color tint: `(R,G,B)` or `(R,G,B,A)`. |
| `swimmingColor` | Color tint while swimming. Falls back to `color`. |
| `shaderTypeDefName` | Shader override (e.g., `Cutout`, `Transparent`). |
| `swimmingShaderTypeDefName` | Shader while swimming. Falls back to `shaderTypeDefName`. |
| `shadowVolume` | Shadow ellipse size (Vector3). **Body only.** |
| `shadowOffset` | Shadow position offset (Vector3). **Body only.** |
| `male` / `female` | Gender-specific `PartOverrideOption` (same structure). |

```xml
<body>
  <mode>Replace</mode>
  <replacementTexPath>Things/Pawn/Animal/Wolf/Wolf</replacementTexPath>
  <color>(112, 82, 65)</color>
  <male>
    <replacementTexPath>Things/Pawn/Animal/Wolf/WolfMale</replacementTexPath>
  </male>
</body>
```

### 6. Graphic Hiding / Showing

Hide or force-show graphics during transformation. Use `<li>All</li>` for the entire category.

**Apparel:**
- `renderHideApparelLayers` / `renderShowApparelLayers` — by layer (e.g., `OnSkin`, `Overhead`)
- `renderHideApparelDefNames` / `renderShowApparelDefNames` — by defName

**Weapons:**
- `renderHideWeaponTags` / `renderShowWeaponTags` — by weapon tag
- `renderHideWeaponDefNames` / `renderShowWeaponDefNames` — by defName

**Genes:**
- `renderHideGeneExclusionTags` / `renderShowGeneExclusionTags`
- `renderHideGeneDefNames` / `renderShowGeneDefNames`

**Hediffs:**
- `renderHideHediffDefNames` / `renderShowHediffDefNames`

> **Tip:** Hide all, then whitelist: `<renderHideApparelLayers><li>All</li></renderHideApparelLayers>` + `<renderShowApparelDefNames><li>Apparel_Cape</li></renderShowApparelDefNames>`.

### 7. Equipment Handling

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `apparelOnTransform` | GearHandling | Keep | `Keep` / `Inventory` / `Drop` |
| `weaponsOnTransform` | GearHandling | Keep | `Keep` / `Inventory` / `Drop` |
| `apparelEquipLock` | EquipLockMode | Auto | `Auto` / `Locked` / `Unlocked` — prevents gear changes while transformed. |
| `weaponEquipLock` | EquipLockMode | Auto | Same as above for weapons. |

**Spawned Equipment** (created on transform, destroyed on revert):

| Field | Type | Description |
|-------|------|-------------|
| `spawnApparelOnTransform` | List\<ThingDef\> | Apparel to spawn and force-equip. |
| `spawnWeaponOnTransform` | List\<ThingDef\> | Weapons to spawn and force-equip. |
| `spawnApparelStuff` | ThingDef | Material for spawned apparel (e.g., `Plasteel`). |
| `spawnWeaponStuff` | ThingDef | Material for spawned weapons. |
| `conflictingGearHandling` | GearHandling | `Inventory` | How to handle existing gear that conflicts with spawned equipment. |

### 8. Render Nodes

Custom render nodes active only during this form (ears, tails, wings, etc.). Uses RimWorld's standard `PawnRenderNodeProperties`.

```xml
<renderNodeProperties>
  <li>
    <nodeClass>PawnRenderNode_AttachmentHead</nodeClass>
    <workerClass>PawnRenderNodeWorker_FlipWhenCrawling</workerClass>
    <texPath>Things/Pawn/Humanlike/HeadAttachments/FloppyEars/FloppyEars</texPath>
    <colorType>Skin</colorType>
    <parentTagDef>Head</parentTagDef>
    <drawData>
      <defaultData><layer>70</layer></defaultData>
    </drawData>
  </li>
</renderNodeProperties>
```

### 9. Type & Color Overrides

| Field | Type | Description |
|-------|------|-------------|
| `bodyType` | BodyTypeDef | Force body type (e.g., `Thin`, `Hulk`). |
| `headType` | HeadTypeDef | Force head type. |
| `hairColor` | Color? | Hair color override. Ignored if hair mode is `Replace`. |
| `skinColor` | Color? | Skin color override. Ignored if body mode is `Replace`. |

### 10. Sustain Conditions

Conditions that must stay true to keep the form active. Breaking them auto-reverts.

| Field | Type | Description |
|-------|------|-------------|
| `sustainApparels` | List\<ThingDef\> | Must remain equipped. |
| `sustainWeapons` | List\<ThingDef\> | Must remain equipped. |
| `sustainHediffs` | List\<HediffDef\> | Must remain on pawn. |
| `sustainGenes` | List\<GeneDef\> | Must remain (Biotech). |
| `sustainMode` | SustainMode? | `All` (every condition) or `Any` (at least one). |

### 11. Additions (Hediffs & Abilities)

Granted while transformed. Automatically removed on revert.

| Field | Type | Description |
|-------|------|-------------|
| `addAbilities` | List\<AbilityDef\> | Abilities granted during transformation. Supports `MayRequire`. |
| `addHediffs` | List\<HediffAddEntry\> | Hediffs applied during transformation (tracked — removed on revert). |

**HediffAddEntry fields:**

| Field | Type | Description |
|-------|------|-------------|
| `hediff` | HediffDef | The hediff to apply. |
| `targetPart` | BodyPartDef | Apply to all matching body parts (e.g., both arms). |
| `targetGroups` | List\<BodyPartGroupDef\> | Apply to parts in these groups. |
| `severity` | float? | Initial severity. |
| `addedPartPolicy` | AddedPartPolicy | `ForceAdd` (overwrite bionics/restore missing), `StrictFleshOnly` (fail if bionic/missing), `RegrowFleshOnly` (restore missing, skip bionics). |

### 12. Combat

**Verbs & Tools:**

| Field | Type | Description |
|-------|------|-------------|
| `verbs` | List\<VerbProperties\> | Additional ranged/melee attacks. |
| `tools` | List\<Tool\> | Additional melee tools. |
| `replaceNativeVerbs` | bool? | `true` = disable pawn's original verbs. |
| `replaceNativeTools` | bool? | `true` = replace pawn's ThingDef tools (restored on revert). |
| `damageSourceDef` | ThingDef | Wound label source (e.g., `Warg` → "Warg teeth"). |

**Verb Gizmo Options** (`verbGizmoOptions`, matched by `verbLabel` to verb's `label`):

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `verbLabel` | string | null | **Recommended.** Matches the verb's `<label>` field (case-insensitive). Order-independent. Falls back to index matching if omitted. |
| `label` | string | null | Verb command gizmo label. Falls back to `verbProps.label` if omitted. |
| `desc` | string | null | Verb command gizmo description. Falls back to default if omitted. |
| `toggleLabel` | string | null | Auto-attack toggle button label. Falls back to `label` if omitted. |
| `toggleDesc` | string | null | Auto-attack toggle button description. Falls back to default if omitted. |
| `iconPath` | string | null | Custom icon texture path. Overrides the verb's `UIIcon` if specified. |
Auto-attack default: first ranged verb is ON, rest OFF. Toggling one verb ON turns all others OFF (exclusive).

> **Multi-select behavior:** When multiple pawns are selected, verb attack gizmos (`Command_VerbTarget`) merge for pawns with the same form+verb. Auto-attack toggles are hidden during multi-select — configure per-pawn by selecting individually.
>
> **Mod settings:** If `showVerbAutoToggle` is disabled, toggle gizmos are hidden and all auto-attack is OFF. Pawns will only fire form verbs via manual target commands.

**Work Restrictions:**

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `disabledWorkTypesOnTransform` | List\<WorkTypeDef\> | — | Specific work types to disable. |
| `disabledWorkTagsOnTransform` | WorkTags | None | Work tag flags to disable (e.g., `Violent`, `Crafting`). |
| `suppressIdeologyUncoveredThoughts` | bool | true | Suppress "naked" mood thoughts from gear removal. |

### 13. VFX & Sound

**Duration & Revert:**

Duration and revert behavior fields can be set on the **FormDef** (as defaults) or overridden on the **HediffCompProperties_ShapeshiftCore** (see Section 2).

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `durationTicks` | int? | null (infinite) | Form duration in ticks. 60000 = 1 in-game day. |
| `canRevertVoluntarily` | bool | true | `false` = no revert gizmo (forced/curse forms). |
| `revertOnDowned` | bool | false | Auto-revert on incapacitation. |
| `revertDrops` | List\<ThingDefCountClass\> | — | Items dropped on revert (shed skin, crystals, etc.). |
| `revertAddHediffs` | List\<HediffAddEntry\> | — | Hediffs applied on revert (fatigue, etc.). **Not tracked** — follows vanilla lifecycle. Uses the same `HediffAddEntry` structure as `addHediffs` (see Section 11). |

```xml
<!-- revertAddHediffs example — apply fatigue to whole body on revert -->
<revertAddHediffs>
  <li>
    <hediff>SSF_TransformFatigue</hediff>
    <severity>0.5</severity>
  </li>
</revertAddHediffs>
```

**Gizmo Icons:**
- `gizmoIconPathEnter` / `gizmoIconPathRevert` — custom button icons.

**Transform FX (one-shot on enter/exit):**

| Field | Description |
|-------|-------------|
| `transformEnterSound` / `transformExitSound` | SoundDef on transform/revert. |
| `transformEnterEffecter` / `transformExitEffecter` | EffecterDef on transform/revert. |
| `transformEnterFleck` / `transformExitFleck` | FleckDef particles. |
| `transformEnterFleckCount` / `transformExitFleckCount` | Particle count (0 = disabled). |
| `transformEnterFleckScale` / `transformExitFleckScale` | Particle scale (default 1.0). |
| `transformEnterFxDelayTicks` / `transformExitFxDelayTicks` | Delay before FX plays. |
| `transformFxCooldownTicks` | Cooldown between same FX (default 30). |

**Ambient VFX (continuous during transformation):**

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `ambientEffecter` | EffecterDef | — | Persistent effecter, ticked every frame (aura, smoke). Auto-cleaned on revert. |
| `ambientFleck` | FleckDef | — | Periodically spawned fleck (sparks, fire). |
| `ambientFleckIntervalTicks` | int | 60 | Spawn interval in ticks. |
| `ambientFleckScale` | float | 1.0 | Fleck scale. |

### 14. Voice & Blood

**Voice (replace pawn vocalizations):**
- `soundCall`, `soundWounded`, `soundDeath`, `soundAngry`, `soundEating`

**Melee Sounds:**
- `soundMeleeHitPawn`, `soundMeleeHitBuilding`, `soundMeleeMiss`

**Blood & Flesh:**

| Field | Type | Description |
|-------|------|-------------|
| `bloodDef` | ThingDef | Blood filth on injury. |
| `bloodSmearDef` | ThingDef | Blood smear when crawling. |
| `fleshType` | FleshTypeDef | Flesh type override (e.g., `Insectoid`). |

### 15. Mod Compatibility

Mod-specific fields are now placed in **DefModExtension** blocks on the FormDef, keeping the core FormDef clean.

**HAR (Humanoid Alien Races):**

```xml
<ShapeshifterFramework.ShapeshiftFormDef>
  <defName>MyForm</defName>
  <!-- ... other fields ... -->
  <modExtensions>
    <li Class="ShapeshifterFramework.Compat.HARFormExtension" MayRequire="erdelf.HumanoidAlienRaces">
      <showHarAddons>true</showHarAddons>
    </li>
  </modExtensions>
</ShapeshifterFramework.ShapeshiftFormDef>
```

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `showHarAddons` | bool | false | Keep HAR BodyAddons visible during transformation. |

**Facial Animation:**

```xml
<modExtensions>
  <li Class="ShapeshifterFramework.Compat.FAFormExtension" MayRequire="Nals.FacialAnimation">
    <faHeadTypeDef>Cat_Head</faHeadTypeDef>
    <faEyeballTypeDef>Cat_Eyeball</faEyeballTypeDef>
    <faLidTypeDef>Cat_Lid</faLidTypeDef>
    <faBrowTypeDef>Cat_Brow</faBrowTypeDef>
    <faMouthTypeDef>Cat_Mouth</faMouthTypeDef>
    <faSkinTypeDef>Cat_Skin</faSkinTypeDef>
    <faEyeColor>(255, 200, 0, 255)</faEyeColor>
    <faEyeColor2>(200, 150, 0, 255)</faEyeColor2>
  </li>
</modExtensions>
```

| Field | Type | Description |
|-------|------|-------------|
| `faHeadTypeDef` | string | FA head type override. |
| `faEyeballTypeDef` | string | FA eyeball type override. |
| `faLidTypeDef` | string | FA lid type override. |
| `faBrowTypeDef` | string | FA brow type override. |
| `faMouthTypeDef` | string | FA mouth type override. |
| `faSkinTypeDef` | string | FA skin type override. |
| `faEyeColor` | ColorInt | FA eye color (R, G, B, A). |
| `faEyeColor2` | ColorInt | FA secondary eye color (R, G, B, A). |

**Simple Sidearms:** Automatic — no XML needed. Weapon memory is backed up on transform and restored on revert.

---

## Trigger System

The FormDef defines **what** the form looks like. The HediffDef defines **stats and entry point**. **How** and **when** the transformation activates is handled by trigger components:

### Base AbilityDefs (Abstract Parents)

Use these as `ParentName` to inherit common settings:

| Base | Purpose | Key Defaults |
|------|---------|-------------|
| `SSF_BaseSelfShiftAbility` | Self-cast shift (no target) | `hostile=false`, `targetRequired=false`, `range=0`, `warmupTime=0` |
| `SSF_BaseTargetedShiftAbility` | Target another pawn | `hostile=false`, `range=15`, `warmupTime=1.0`, `canTargetPawns=true` |
| `SSF_BaseAoEShiftAbility` | AoE ground/pawn target | `hostile=true`, `range=25`, `warmupTime=2.5`, `canTargetLocations=true` |

All three share: `category=SSF_Shapeshift`, `iconPath=UI/Commands/SSF_Shift_Enter`, `casterMustBeCapableOfViolence=false`.

### CompProperties_AbilityShapeshift

Attach to an `AbilityDef`'s `<comps>`:

| Field | Type | Description |
|-------|------|-------------|
| `hediffDef` | HediffDef | **Preferred.** The HediffDef to apply (must contain `HediffCompProperties_ShapeshiftCore`). |
| `formDefName` | string | Legacy fallback. FormDef defName to apply (uses generic hediff). Ignored if `hediffDef` is set. |
| `successChance` | float | Success probability (default 1.0). |
| `allowedRaces` / `disallowedRaces` | List\<ThingDef\> | Caster race filter. |
| `allowedMutants` / `disallowedMutants` | List\<MutantDef\> | Caster mutant filter (Anomaly). |
| `allowedFromForms` | List\<string\> | Forms from which this ability can be cast while transformed. Empty = disabled while transformed. |
| `affectHostileOnly` | bool | If true, AoE abilities only apply to pawns hostile to the caster. Default false. |

### Acquisition Sources

| Source | Component | Trigger |
|--------|-----------|---------|
| Gene | `GeneDef.abilities` | Gene grants ability (Biotech). |
| Hediff | `HediffCompProperties_GiveAbility` | Hediff grants ability while present. |
| Item (equipped) | `CompProperties_GiveAbility_Shapeshift` (`requireEquipped=true`) | Equipped item grants ability. When the ability triggers a shift, the item is tracked as a `sourceItem` and `sourceItemRequireEquipped=true` — unequipping reverts the form. |
| Item (inventory) | `CompProperties_GiveAbility_Shapeshift` (`requireEquipped=false`) | Inventory item grants ability. The item is tracked as a `sourceItem` with `sourceItemRequireEquipped=false` — dropping or destroying reverts, but moving to inventory is OK. |
| Drug | `IngestionOutcomeDoer_Shapeshift` | Drug triggers shift directly. Fields: `hediffDef` (preferred), `formDefName` (fallback). |
| Scroll/UseItem | `CompProperties_UseEffect_Shapeshift` | Item use triggers shift directly. Fields: `hediffDef` (preferred), `formDefName` (fallback). |
| Projectile | `PolymorphProjectileExtension` | Projectile hit triggers shift. Fields: `hediffDef` (preferred), `formDefName` (fallback), `aoeRadius`, `affectAllies`. |

> **Vanilla GiveHediff compatibility:** Because the entry point is a standard HediffDef, vanilla `GiveHediff` operations (e.g., from other mods, dev tools, or vanilla hediff givers) will work. When the hediff is added, `HediffComp_ShapeshiftCore.CompPostPostAdd` automatically removes any existing shapeshift hediff (preventing stacking) and calls `ApplyForm()`.
>
> **Recommended:** For drug/projectile triggers, prefer `IngestionOutcomeDoer_Shapeshift` and `Projectile_Polymorph` over vanilla `IngestionOutcomeDoer_GiveHediff`. The SSF variants provide `successChance` (resistance roll) and proper `ApplyShift()` flow. While vanilla GiveHediff works (CompPostPostAdd handles stacking), the SSF variants offer a richer feature set.

### HediffComp_AutoShift (Conditional Auto-Shift)

Attach `HediffCompProperties_AutoShift` to any HediffDef. Triggers transformation automatically when conditions are met.

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `hediffDef` | HediffDef | — | **Preferred.** HediffDef to apply for the shift. |
| `formDefName` | string | — | Legacy fallback. FormDef defName. Ignored if `hediffDef` is set. |
| `healthThreshold` | float | 0 (disabled) | Trigger below this health %. E.g., `0.3` = 30%. |
| `triggerMentalStates` | List\<MentalStateDef\> | — | Trigger on these mental states. |
| `triggerSunGlowBelow` | float | 0 (disabled) | Trigger when sun glow is below this value. `0.5` = night. |
| `triggerInCombat` | bool | false | Trigger when drafted/attacked and enemies nearby. |
| `checkIntervalTicks` | int | 120 | Check interval (120 = 2 seconds). |
| `successChance` | float | 1.0 | Shift probability per check. |
| `triggerOnce` | bool | false | Remove hediff after first trigger. |

**Logic:** Conditions are OR — any single match triggers the shift. Already-transformed pawns are skipped.

```xml
<HediffDef>
  <defName>Curse_Werewolf</defName>
  <hediffClass>HediffWithComps</hediffClass>
  <label>werewolf curse</label>
  <isBad>false</isBad>
  <comps>
    <li Class="ShapeshifterFramework.Hediffs.HediffCompProperties_AutoShift">
      <hediffDef>WerewolfForm_Hediff</hediffDef>
      <healthThreshold>0.3</healthThreshold>
      <triggerSunGlowBelow>0.5</triggerSunGlowBelow>
      <successChance>0.8</successChance>
    </li>
  </comps>
</HediffDef>
```

### Multi-Stage Chains

Use `addAbilities` to grant a stage-2 ability only while in stage-1 form. The stage-2 ability must list stage-1 in `allowedFromForms`.

```
Stage 1 (BeastkinForm) → addAbilities grants [FullBeast ability]
  → FullBeast ability has allowedFromForms: [BeastkinForm]
  → Pawn uses FullBeast → enters FullBeastForm
  → Reverting BeastkinForm removes FullBeast ability
```

---

## Events / External Mod Integration

The framework exposes C# events for external mods to react to transformations:

```csharp
// Subscribe (e.g., in your mod's HarmonyInit or GameComponent)
ShapeshifterFramework.Utilities.ShapeshiftCoreUtility.OnFormApplied += MyOnFormApplied;
ShapeshifterFramework.Utilities.ShapeshiftCoreUtility.OnFormRemoved += MyOnFormRemoved;

// Handlers
private static void MyOnFormApplied(Pawn pawn, ShapeshiftFormDef form)
{
    // Called after a form is fully applied (visuals, gear, hediffs all set).
    Log.Message($"{pawn.LabelShortCap} shifted into {form.defName}");
}

private static void MyOnFormRemoved(Pawn pawn, ShapeshiftFormDef form)
{
    // Called after a form is fully reverted (originals restored).
    Log.Message($"{pawn.LabelShortCap} reverted from {form.defName}");
}
```

| Event | Signature | Fires When |
|-------|-----------|------------|
| `OnFormApplied` | `Action<Pawn, ShapeshiftFormDef>` | After `HediffComp_ShapeshiftCore.ApplyForm()` completes successfully. |
| `OnFormRemoved` | `Action<Pawn, ShapeshiftFormDef>` | After `HediffComp_ShapeshiftCore.RemoveForm()` completes successfully. |

> **Note:** Event handlers are cleared on game load (`GameComponent.FinalizeInit`). Re-subscribe in your `GameComponent.FinalizeInit` or use a Harmony postfix on `ShapeshiftCoreUtility.ClearEvents` to re-register.

---

## Complete Example

```xml
<Defs>
  <!-- 1. FormDef — visuals, gear, tools (no stats, no linkedHediff) -->
  <ShapeshifterFramework.ShapeshiftFormDef ParentName="SSF_BaseForm_Animal">
    <defName>SSF_WolfForm</defName>
    <label>Wolf Form</label>
    <description>A powerful wolf form.</description>

    <bodyDrawScale>1.5</bodyDrawScale>
    <body>
      <mode>Replace</mode>
      <replacementTexPath>Things/Pawn/Animal/Wolf/Wolf</replacementTexPath>
    </body>

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

    <soundWounded>Pawn_Dog_Injured</soundWounded>
    <bloodDef>Filth_Blood</bloodDef>
    <gizmoIconPathEnter>UI/Commands/TransformWolf</gizmoIconPathEnter>
  </ShapeshifterFramework.ShapeshiftFormDef>

  <!-- 2. HediffDef — entry point with stats, references the FormDef -->
  <HediffDef ParentName="SSF_ShapeshiftFormBase">
    <defName>SSF_WolfFormHediff</defName>
    <label>wolf form</label>
    <stages>
      <li>
        <statOffsets>
          <MoveSpeed>2.5</MoveSpeed>
          <ArmorRating_Sharp>0.4</ArmorRating_Sharp>
        </statOffsets>
      </li>
    </stages>
    <comps>
      <li Class="ShapeshifterFramework.Hediffs.HediffCompProperties_ShapeshiftCore">
        <formDef>SSF_WolfForm</formDef>
        <durationTicks>30000</durationTicks>
      </li>
    </comps>
  </HediffDef>

  <!-- 3. Ability to trigger the form (references HediffDef, not FormDef) -->
  <AbilityDef ParentName="SSF_BaseSelfShiftAbility">
    <defName>SSF_Ability_Wolf</defName>
    <label>wolf shift</label>
    <description>Transform into a wolf.</description>
    <cooldownTicksRange>3000</cooldownTicksRange>
    <comps>
      <li Class="ShapeshifterFramework.Comps.CompProperties_AbilityShapeshift">
        <hediffDef>SSF_WolfFormHediff</hediffDef>
      </li>
    </comps>
  </AbilityDef>
</Defs>
```
