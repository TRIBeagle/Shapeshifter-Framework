# Shapeshifter Framework — ShapeshiftFormDef Manual

> **Auto-generated from source code** — reflects the actual C# fields in `ShapeshiftFormDef.cs` and related components.

This document details all XML tags used to create custom shapeshifting forms in the `ShapeshifterFramework`.
All options **fall back to vanilla behavior if left blank**, so you only need to define the features you actually want to change.

---

## Architecture Overview

The Shapeshifter Framework separates concerns across multiple components:

| Component | Responsibility |
|-----------|---------------|
| **ShapeshiftFormDef** | Visuals, equipment, tools/verbs, sounds, VFX, duration, UI |
| **linkedHediff (HediffDef)** | Stat offsets, stat factors, capacity modifiers (vanilla pattern) |
| **CompProperties_AbilityShiftTarget** | Cast conditions (races, mutants), success chance |
| **Ability acquisition sources** | Genes, hediffs, items (CompGiveAbility_SSF), drugs, projectiles |

Stats and capacities are **not** defined on the FormDef. They are defined on the `linkedHediff`'s HediffDef stages, using the vanilla HediffDef pattern (`statOffsets`, `statFactors`, `capMods`).

---

## 1. Basic Information
* `<defName>` (Required): Unique ID of the form. Cannot be duplicated.
* `<label>`: The display name of the form in-game.
* `<description>`: Tooltip and description text.

## 2. Main Hediff (Stats & Capacities)
* `<linkedHediff>`: (Required) The `HediffDef` that marks the transformation state. Removing this hediff automatically ends the transformation.
* `<formAllowedRaces>`: (Optional) List of `ThingDef` race defs that can receive this form. If omitted or empty, any race is allowed (default behavior). Note: this filters the **target** (who receives the form), while `CompProperties_AbilityShiftTarget.allowedRaces` filters the **caster** (who can cast the ability).

```xml
<formAllowedRaces>
  <li>Human</li>
</formAllowedRaces>
```

**Stats and capacities are defined in the linkedHediff's HediffDef, not in the FormDef.** Use the standard vanilla HediffDef pattern:

```xml
<HediffDef>
  <defName>MyForm_Hediff</defName>
  <hediffClass>ShapeshifterFramework.Hediffs.Hediff_ShapeshiftForm</hediffClass>
  <label>my form</label>
  <isBad>false</isBad>
  <stages>
    <li>
      <statOffsets>
        <MoveSpeed>1.5</MoveSpeed>
      </statOffsets>
      <statFactors>
        <MeleeHitChance>1.20</MeleeHitChance>
      </statFactors>
      <capMods>
        <li><capacity>Moving</capacity><postFactor>1.30</postFactor></li>
        <li><capacity>Manipulation</capacity><setMax>0.2</setMax></li>
      </capMods>
    </li>
  </stages>
</HediffDef>
```

## 3. Scale & Offset
Adjusts the rendered size and position of the character.
* `<bodyDrawScale>`: Overall body rendering scale multiplier (Default: 1.0).
* `<headDrawScale>`: Additional head rendering multiplier (multiplied with body scale. Default: 1.0).
* `<portraitDrawScale>`: Scale multiplier applied **only** in the bottom-left UI portrait window. Useful for fitting giant forms into the frame.
* `<bodyOffset>` / `<headOffset>`: 2D Vector (X, Z) to adjust position (e.g., `(0, 0.5)`).

## 4. Part Override Options
Hide or replace textures, colors, and shaders for specific body parts.
Supported tags: `<body>`, `<head>`, `<hair>`, `<beard>`, `<tattooBody>`, `<tattooHead>`

**[Inner Options]**
* `<mode>`: Choose between `Default` (keep vanilla), `Hidden`, or `Replace`.
* `<replacementTexPath>`: Path to the new texture (Requires `Replace` mode).
* `<swimmingReplacementTexPath>`: Specific texture used when the pawn is in water.
* `<color>` / `<swimmingColor>`: Color tint applied to the texture (e.g., `(112,82,65)` or `(0.7, 0.8, 1.0, 0.5)` with alpha).
* `<shaderTypeDefName>`: Change the shader (e.g., `Cutout`, `Transparent`).
* `<swimmingShaderTypeDefName>`: Specific shader to use while the pawn is swimming. Falls back to `shaderTypeDefName`, then to node default.
* `<shadowVolume>` / `<shadowOffset>`: Override the drop shadow size and position (**Only valid in `<body>`**). e.g., `(0.6, 1.0, 0.6)`
* `<male>` / `<female>`: Nest an identical structure inside these tags for gender-specific overrides.

## 5. Render Hiding / Showing
Forcefully hide or show worn apparel, weapons, genes, or hediffs during the transformation.
Written as a list (`<li>`). You can use the special keyword **"All"** to apply to the entire category.

**Apparel:**
* `<renderHideApparelLayers>` / `<renderHideApparelDefNames>`: Hide specific layers (e.g., `OnSkin`, `Overhead`) or specific apparel defNames.
* `<renderShowApparelLayers>` / `<renderShowApparelDefNames>`: Whitelist exceptions — items to **keep visible** despite hide rules.

**Weapons:**
* `<renderHideWeaponTags>` / `<renderHideWeaponDefNames>`: Hide specific weapon tags or defNames.
* `<renderShowWeaponTags>` / `<renderShowWeaponDefNames>`: Whitelist exceptions.

**Genes:**
* `<renderHideGeneExclusionTags>` / `<renderHideGeneDefNames>`: Hide gene graphics by exclusion tags or defNames.
* `<renderShowGeneExclusionTags>` / `<renderShowGeneDefNames>`: Whitelist exceptions.

**Hediffs:**
* `<renderHideHediffDefNames>`: Hide hediff graphics (e.g., wounds, implants).
* `<renderShowHediffDefNames>`: Whitelist exceptions.

> **Tip:** Use `<renderHideApparelLayers><li>All</li></renderHideApparelLayers>` to hide all apparel graphics, then use `<renderShowApparelDefNames>` to selectively show specific items (e.g., capes).

## 6. Equipment Handling
Defines what happens to the pawn's current apparel and weapons when they transform.

**Basic Handling:**
* `<apparelOnTransform>` / `<weaponsOnTransform>`: Options are `Keep` (keep wearing), `Inventory` (move to inventory), or `Drop` (drop on floor). (Default: `Keep`).
* `<apparelEquipLock>` / `<weaponEquipLock>`: Prevents changing gear while transformed. `Auto` (matches the above setting), `Locked`, or `Unlocked`. (Default: `Auto`).

**Spawned Equipment (summoned on transform, destroyed on revert):**
* `<spawnApparelOnTransform>`: List of `ThingDef` apparel to spawn and force-equip.
* `<spawnWeaponOnTransform>`: List of `ThingDef` weapons to spawn and force-equip.
* `<spawnApparelStuff>` / `<spawnWeaponStuff>`: Material (`ThingDef`) for spawned equipment (e.g., `Plasteel`).
* `<conflictingGearHandling>`: How to handle existing gear that conflicts with spawned equipment. Options: `Keep`, `Inventory`, `Drop`. (Default: `Inventory`).

## 7. Render Nodes
Custom render nodes added only while this form is active (e.g., ears, tails).
* `<renderNodeProperties>`: List of `PawnRenderNodeProperties`. Uses the standard RimWorld render node system.

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

## 8. Type & Color Overrides
* `<bodyType>`: Force a specific `BodyTypeDef` (e.g., `Thin`, `Fat`, `Hulk`).
* `<headType>`: Force a specific `HeadTypeDef` (e.g., `Male_AverageNormal`).
* `<hairColor>`: Override hair color (e.g., `(0.85, 0.85, 0.95)`). Ignored if texture Replace mode is used.
* `<skinColor>`: Override skin color (e.g., `(0.7, 0.8, 1.0)`). Ignored if texture Replace mode is used.

## 9. Sustain Conditions
Conditions that must remain true to keep the transformation active. If conditions break, the form is automatically reverted.
* `<sustainApparels>`: List of `ThingDef` apparel that must stay equipped.
* `<sustainWeapons>`: List of `ThingDef` weapons that must stay equipped.
* `<sustainHediffs>`: List of `HediffDef` that must remain on the pawn.
* `<sustainGenes>`: List of `GeneDef` that must remain (Biotech DLC, `MayRequire`).
* `<sustainMode>`: `All` (every condition must be met) or `Any` (at least one condition must be met).

## 10. Additions (Hediffs & Abilities)
Grant temporary powers or status effects while transformed.
* `<addAbilities>`: List of `AbilityDef` to grant. Supports `MayRequire` for DLC-conditional abilities.
* `<addHediffs>`: List of `HediffAddEntry`:
    * `<hediff>`: The `HediffDef` to apply.
    * `<targetPart>`: Specific `BodyPartDef` to target (applies to all matching parts, e.g., both arms).
    * `<targetGroups>`: List of `BodyPartGroupDef` to target.
    * `<severity>`: Initial severity value.
    * `<addedPartPolicy>`: How to handle missing parts or bionics:
        * `ForceAdd` — overwrite everything (destroy bionics, restore missing parts, then apply).
        * `StrictFleshOnly` — fail if the part has bionics or is missing.
        * `RegrowFleshOnly` — restore missing parts but leave bionics alone.

## 11. Combat & Work
**Verbs & Tools:**
* `<verbs>`: List of `VerbProperties` to add (ranged/melee attacks).
* `<tools>`: List of `Tool` to add (melee tools).
* `<replaceNativeVerbs>`: Set to `true` to disable the pawn's original verbs and solely use the form's verbs.
* `<replaceNativeTools>`: Set to `true` to replace the pawn's ThingDef tools (restored on revert).
* `<verbGizmoOptions>`: List of `VerbGizmoOption` matched by index to `<verbs>`:
    * `<label>`: Verb command label.
    * `<desc>`: Verb command description.
    * `<toggleLabel>` / `<toggleDesc>`: Auto-attack toggle button labels.
    * `<iconPath>`: Custom icon path for the verb gizmo.
    * `<autoAttackDefault>`: Auto-attack toggle initial value. `null` = first ranged verb ON, rest OFF.
* `<damageSourceDef>`: `ThingDef` used as the damage source in wound labels (e.g., `Warg` → "Warg teeth"). `null` uses default pawn label.

**Work Restrictions:**
* `<disabledWorkTypesOnTransform>`: List of `WorkTypeDef` to disable (e.g., `Firefighter`).
* `<disabledWorkTagsOnTransform>`: `WorkTags` flags to disable (e.g., `Violent`, `Crafting`). Multiple values can be listed with `<li>` tags.
* `<suppressIdeologyUncoveredThoughts>`: Prevents negative mood thoughts about being "naked" when the form forces gear dropping. (Default: `true`).

## 12. VFX, SFX & UI

**Duration & Revert:**
* `<durationTicks>`: How long the form lasts. Leave blank for infinite. (60,000 ticks = 1 in-game day).
* `<canRevertVoluntarily>`: If `false`, the pawn cannot manually revert via gizmo (for debuff/curse forms). (Default: `true`).
* `<revertOnDowned>`: If `true`, the form is automatically reverted when the pawn is downed (incapacitated). (Default: `false`).

**Gizmo Icons:**
* `<gizmoIconPathEnter>` / `<gizmoIconPathRevert>`: Custom icons for the transform/revert buttons.

**Transform Sounds:**
* `<transformEnterSound>` / `<transformExitSound>`: Audio played upon transforming/reverting.

**Transform Effecters:**
* `<transformEnterEffecter>` / `<transformExitEffecter>`: Effecter VFX played on transform/revert.

**Transform Flecks (lightweight particles):**
* `<transformEnterFleck>` / `<transformExitFleck>`: `FleckDef` to spawn.
* `<transformEnterFleckCount>` / `<transformExitFleckCount>`: Number of fleck particles (0 = disabled).
* `<transformEnterFleckScale>` / `<transformExitFleckScale>`: Fleck particle scale (Default: 1.0).

**Timing & Spam Prevention:**
* `<transformEnterFxDelayTicks>` / `<transformExitFxDelayTicks>`: Delay before playing FX (in ticks).
* `<transformFxCooldownTicks>`: Cooldown before the same FX can play again (Default: 30 ticks).

## 13. Voice & Blood
**Voice overrides (replace pawn vocalizations while transformed):**
* `<soundCall>`: Idle call sound.
* `<soundWounded>`: Pain/injured sound.
* `<soundDeath>`: Death sound.
* `<soundAngry>`: Anger sound.
* `<soundEating>`: Eating sound.

**Melee combat sounds:**
* `<soundMeleeHitPawn>`: Melee hit on pawn sound.
* `<soundMeleeHitBuilding>`: Melee hit on building sound.
* `<soundMeleeMiss>`: Melee miss sound.

**Blood & Flesh:**
* `<bloodDef>`: `ThingDef` for blood filth when injured.
* `<bloodSmearDef>`: `ThingDef` for blood smear when crawling.
* `<fleshType>`: `FleshTypeDef` override (e.g., `Insectoid`). Changes wound textures and related behavior.

## 14. Compatibility

**Humanoid Alien Races (HAR):**
* `<showHarAddons>`: If `true`, HAR BodyAddons remain visible after transforming. (Default: `false`). `MayRequire: erdelf.HumanoidAlienRaces`

**Facial Animation:**
All fields below use `MayRequire: Nals.FacialAnimation`:
* `<faHeadTypeDef>`: Replace facial head type.
* `<faEyeballTypeDef>`: Replace eyeball type.
* `<faLidTypeDef>`: Replace eyelid type.
* `<faBrowTypeDef>`: Replace eyebrow type.
* `<faMouthTypeDef>`: Replace mouth type.
* `<faSkinTypeDef>`: Replace skin type.
* `<faEyeColor>` / `<faEyeColor2>`: Override eye colors (`ColorInt`).

**Simple Sidearms:**
* No XML fields required. Compatibility is automatic.
* On transformation: the pawn's sidearm memory is backed up and cleared to prevent Simple Sidearms from interfering with weapon swap logic.
* On revert: the original sidearm memory is restored, so the pawn remembers the same weapons as before transformation.

---

## Ability & Trigger System

The FormDef itself does **not** contain cast conditions or trigger logic. These are handled by separate components:

### CompProperties_AbilityShiftTarget
Attached to an `AbilityDef`'s `<comps>` to define the shift effect:
* `<formDefName>`: The `ShapeshiftFormDef` defName to apply.
* `<successChance>`: Probability of transformation (0.0–1.0, Default: 1.0).
* `<allowedRaces>` / `<disallowedRaces>`: List of `ThingDef` — restrict which races can cast.
* `<allowedMutants>` / `<disallowedMutants>`: List of `MutantDef` (Anomaly DLC) — restrict by mutant type.
* `<allowedFromForms>`: List of `string` (FormDef defNames) — while transformed, only allow casting from these forms. If null/empty: ability is **disabled (grayed out)** while transformed. Same-form recast is always hidden regardless.

### Ability Acquisition Sources

| Source | Component | Description |
|--------|-----------|-------------|
| **Gene** | `GeneDef.abilities` | Biotech DLC. Gene grants ability automatically. |
| **Hediff** | `HediffCompProperties_GiveAbility` | Vanilla pattern. Hediff grants ability while present. |
| **Item (inventory/equipped)** | `CompProperties_GiveAbility_SSF` | Custom. `requireEquipped=true` for equipped only, `false` for inventory possession. |
| **Drug** | `IngestionOutcomeDoer_Shapeshift` | Drug ingestion triggers shift directly (no ability). Fields: `formDefName`, `successChance`. |
| **Scroll/UseItem** | `CompProperties_UseEffect_ShiftTarget` | Item use triggers shift directly. Fields: `formDefName`, `successChance`. |
| **Projectile** | `PolymorphProjectileExtension` | Projectile hit triggers shift. Fields: `formDefName`, `successChance`, `aoeRadius`, `affectAllies`. |

### Multi-Stage Transformation (addAbilities chain)
Use `<addAbilities>` to grant a stage-2 transformation ability only while in stage-1 form.
**Important**: The stage-2 ability must have `<allowedFromForms>` listing the stage-1 form, otherwise it will be grayed out while transformed.
```
Stage 1 (BeastkinForm) → addAbilities: [FullBeast ability]
  → Pawn gains FullBeast ability while in Beastkin form
  → FullBeast ability has allowedFromForms: [BeastkinForm]
  → Using FullBeast ability → enters FullBeastForm
  → Reverting BeastkinForm → removes FullBeast ability
```

---

## Abstract Base Forms

The framework provides three abstract base forms in `SSF_BaseForms.xml`:

| Base | Equipment | Apparel Hiding | Use Case |
|------|-----------|----------------|----------|
| `SSF_BaseForm_Animal` | Drop all | Hide all apparel/weapons/head/hair/beard | Full animal replacement |
| `SSF_BaseForm_Humanoid` | Keep all | Hide overhead apparel only | Humanlike with additions |
| `SSF_BaseForm_Armored` | Keep all | None (keep all visible) | Equipment-focused forms |

---

## Complete XML Example

```xml
<Defs>
  <!-- Step 1: Define the main hediff for stats -->
  <HediffDef>
    <defName>SSF_WolfFormHediff</defName>
    <hediffClass>ShapeshifterFramework.Hediffs.Hediff_ShapeshiftForm</hediffClass>
    <label>wolf form</label>
    <description>Transformed into a wolf.</description>
    <isBad>false</isBad>
    <stages>
      <li>
        <statOffsets>
          <MoveSpeed>2.5</MoveSpeed>
          <ArmorRating_Sharp>0.4</ArmorRating_Sharp>
        </statOffsets>
        <capMods>
          <li><capacity>Moving</capacity><postFactor>1.30</postFactor></li>
        </capMods>
      </li>
    </stages>
  </HediffDef>

  <!-- Step 2: Define the form -->
  <ShapeshifterFramework.ShapeshiftFormDef ParentName="SSF_BaseForm_Animal">
    <defName>SSF_WolfForm</defName>
    <label>Wolf Form</label>
    <description>A powerful wolf form. Drops weapons and runs fast.</description>
    <linkedHediff>SSF_WolfFormHediff</linkedHediff>

    <bodyDrawScale>1.5</bodyDrawScale>
    <portraitDrawScale>0.8</portraitDrawScale>

    <apparelOnTransform>Drop</apparelOnTransform>
    <weaponsOnTransform>Drop</weaponsOnTransform>

    <body>
      <mode>Replace</mode>
      <replacementTexPath>Things/Pawn/Animal/Wolf/Wolf</replacementTexPath>
    </body>
    <head><mode>Hidden</mode></head>
    <hair><mode>Hidden</mode></hair>

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

    <transformEnterFleck>ExplosionFlash</transformEnterFleck>
    <transformEnterFleckCount>3</transformEnterFleckCount>
    <soundWounded>Pawn_Dog_Injured</soundWounded>
    <bloodDef>Filth_Blood</bloodDef>

    <gizmoIconPathEnter>UI/Commands/TransformWolf</gizmoIconPathEnter>
    <durationTicks>30000</durationTicks>
  </ShapeshifterFramework.ShapeshiftFormDef>

  <!-- Step 3: Define the ability -->
  <AbilityDef ParentName="SSF_BaseSelfShiftAbility">
    <defName>SSF_Ability_Wolf</defName>
    <label>wolf shift</label>
    <description>Transform into a wolf.</description>
    <cooldownTicksRange>3000</cooldownTicksRange>
    <comps>
      <li Class="ShapeshifterFramework.Comps.CompProperties_AbilityShiftTarget">
        <formDefName>SSF_WolfForm</formDefName>
        <successChance>1.0</successChance>
      </li>
    </comps>
  </AbilityDef>
</Defs>
```
