# Shapeshifter Framework - FormDef Manual

This document details all the functionalities of the `ShapeshiftFormDef` XML tag used to create custom shapeshifting forms in the `ShapeshifterFramework`.

All options are designed to **fall back to vanilla behavior if left blank**, so you only need to define the features you actually want to change.

## 1. Basic Information
* `<defName>` (Required): Unique ID of the form. Cannot be duplicated.
* `<label>`: The display name of the form in-game.
* `<description>`: Tooltip and description text.

## 2. Scale & Offset
Adjusts the rendered size and position of the character.
* `<bodyDrawScale>`: Overall body rendering scale multiplier (Default: 1.0).
* `<headDrawScale>`: Additional head rendering multiplier (Multiplies with body scale. Default: 1.0).
* `<portraitDrawScale>`: Scale multiplier applied **only** in the bottom UI portrait window. Useful for fitting giant forms into the frame.
* `<bodyOffset>` / `<headOffset>`: 2D Vector to adjust the X and Z position (e.g., `(0, 0.5)`).

## 3. Part Override Options
Hide or replace textures, colors, and shaders for specific body parts.
Supported tags: `<body>`, `<head>`, `<hair>`, `<beard>`, `<tattooBody>`, `<tattooHead>`

**[Inner Options]**
* `<mode>`: Choose between `Default` (keep vanilla), `Hidden`, or `Replace`.
* `<replacementTexPath>`: Path to the new texture (Requires `Replace` mode).
* `<swimmingReplacementTexPath>`: Specific texture used when the pawn is in water.
* `<color>` / `<swimmingColor>`: Color tint applied to the texture.
* `<shaderTypeDefName>`: Change the shader (e.g., `Cutout`, `Transparent`).
* `<swimmingShaderTypeDefName>`: Specific shader to use while the pawn is swimming.
* `<shadowVolume>` / `<shadowOffset>`: Override the drop shadow size and position (Only valid in `<body>`).
* `<male>` / `<female>`: You can nest an identical structure inside these tags for gender-specific overrides.

## 4. Render Hiding/Showing
Forcefully hide or show worn apparel, genes, or hediffs during the transformation.
Written as a list (`<li>`). You can use the special keyword **"All"** to apply to the entire category.
* `<renderHideApparelLayers>` / `<renderHideApparelDefNames>`: Hide specific layers (e.g., `OnSkin`) or specific apparels.
* `<renderHideWeaponTags>` / `<renderHideWeaponDefNames>`: Hide specific weapons.
* `<renderHideGeneExclusionTags>` / `<renderHideGeneDefNames>`: Hide gene graphics.
* `<renderHideHediffDefNames>`: Hide hediff graphics (e.g., wounds, implants).
* *Note: Changing `Hide` to `Show` creates an whitelist exception.*

## 5. Equipment Handling
Defines what happens to the pawn's current apparel and weapons when they transform.
* `<apparelOnTransform>` / `<weaponsOnTransform>`: Options are `Keep` (keep wearing), `Inventory` (move to bag), or `Drop` (drop on floor). (Default: `Keep`).
* `<apparelEquipLock>` / `<weaponEquipLock>`: Prevents changing gear while transformed. `Auto` (matches the above setting), `Locked`, or `Unlocked`. (Default: `Auto`).

## 6. Stats & Capacities
* `<statOffsets>`: Flat additions (+ or -) to stats.
* `<statFactors>`: Percentage multipliers (x) to stats.
* `<capMods>`: Modifies PawnCapacities (Sight, Hearing, BloodPumping, etc.).
* `<bodyType>` / `<headType>`: Force a specific body or head type.

## 7. Additions (Hediffs & Abilities)
Grant temporary powers or status effects while transformed.
* `<addAbilities>`: List of `AbilityDef` to grant.
* `<addHediffs>`: List of `HediffAddEntry`.
    * `<hediff>`: The HediffDef to apply.
    * `<targetPart>` / `<targetGroups>`: Specific body part(s) to target.
    * `<severity>`: Initial severity.
    * `<addedPartPolicy>`: How to handle missing parts or bionics. `ForceAdd` (overwrite everything), `StrictFleshOnly` (fail if bionic), `RegrowFleshOnly` (restore missing parts but ignore bionics).

## 8. Requirements & Filters
Restrict who can transform into this form.
* **Strict Filters:**
    * `<allowedRaces>` / `<disallowedRaces>`
    * `<allowedMutants>` / `<allowedXenotypes>`
    * `<allowedFromForms>`: Restricts transformation to only happen from specific other forms.
* **Condition Requirements:**
    * `<requiredGenes>`, `<requiredItems>`, `<requiredApparels>`, `<requiredWeapons>`, `<requiredAbilities>`, `<requiredHediffs>`.
    * `<requirementsMode>`: `All` (must meet all criteria) or `Any` (must meet at least one category).

## 9. Combat & Work
* `<verbs>` / `<tools>`: Add custom ranged/melee attacks to the form.
* `<replaceNativeVerbs>` / `<replaceNativeTools>`: Set to `true` to disable the pawn's original attacks and solely use the form's attacks.
* `<verbGizmoOptions>`: Configure auto-attack toggle UI for custom verbs (`label`, `iconPath`, `autoAttackDefault`).
* `<disabledWorkTypesOnTransform>` / `<disabledWorkTagsOnTransform>`: Prevent the pawn from doing certain jobs (e.g., Firefighting, Caring) while transformed.
* `<suppressIdeologyUncoveredThoughts>`: Prevents negative mood thoughts about being "naked" when the form forces gear dropping. (Default: `true`).

## 10. VFX, SFX & UI
* `<durationTicks>`: How long the form lasts. Leave blank for infinite. (60,000 ticks = 1 in-game day).
* `<gizmoIconPathEnter>` / `<gizmoIconPathRevert>`: Custom icons for the transform/revert buttons.
* `<transformEnterSound>` / `<transformExitSound>`: Audio played upon transforming.
* `<transformEnterEffecter>` / `<transformEnterFleck>`: Particles and visual effects.
* `<soundCall>`, `<soundWounded>`, `<soundDeath>`, `<soundMeleeHitPawn>`, etc.: Custom voice lines and hit sounds.
* `<bloodDef>` / `<bloodSmearDef>`: Change the color/texture of the blood dropped when injured.

## 11. Compatibility
* `<showHarAddons>`: If `true`, Humanoid Alien Races (HAR) BodyAddons will remain visible after transforming. (Default: `false`).
* `<faHeadTypeDef>` and others: Replace facial textures with specific Facial Animation assets while transformed.

---

### 📝 XML Example

```xml
<Defs>
  <ShapeshifterFramework.ShapeshiftFormDef>
    <defName>SSF_WolfForm</defName>
    <label>Wolf Form</label>
    <description>A powerful wolf form. Drops weapons and runs fast.</description>
    
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
    
    <statOffsets>
      <li><stat>MoveSpeed</stat><value>2.5</value></li>
      <li><stat>ArmorRating_Sharp</stat><value>0.4</value></li>
    </statOffsets>
    
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
    <durationTicks>30000</durationTicks> </ShapeshifterFramework.ShapeshiftFormDef>
</Defs>