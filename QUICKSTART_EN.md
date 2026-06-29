# Shapeshifter Framework — Quick Start Guide

Create your first transformation form in 3 XML files. No C# required.

> Full field reference: [FORMDEF_GUIDE_EN.md](FORMDEF_GUIDE_EN.md)

---

## Prerequisites

1. RimWorld 1.6
2. [Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077)
3. Shapeshifter Framework (this mod)

## File Structure

Your mod should look like this:
```
YourMod/
  About/
    About.xml
  1.6/
    Defs/
      MyFormDef.xml      <-- Step 1: form visuals & behavior
      MyHediffDef.xml    <-- Step 2: stats & entry point
      MyAbilityDef.xml   <-- Step 3: trigger method
```

---

## Step 1: FormDef (Visuals & Behavior)

The FormDef defines **what the pawn looks and acts like** while transformed.

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>

  <ShapeshifterFramework.ShapeshiftFormDef ParentName="SSF_BaseForm_Animal">
    <defName>MyMod_WolfForm</defName>
    <label>wolf form</label>
    <description>A fierce wolf transformation.</description>

    <!-- Body texture replacement -->
    <body>
      <mode>Replace</mode>
      <replacementTexPath>Things/Pawn/Animal/Wolf_Timber/Wolf_Timber</replacementTexPath>
    </body>

    <!-- Visual scale (2x size) -->
    <bodyDrawScale>2.0</bodyDrawScale>

    <!-- Duration: 30000 ticks = 12.5 in-game hours. Remove for permanent. -->
    <durationTicks>30000</durationTicks>

    <!-- Player can manually revert -->
    <canRevertVoluntarily>true</canRevertVoluntarily>

    <!-- Melee tools (claws & bite) -->
    <tools>
      <li>
        <label>claws</label>
        <capacities><li>Scratch</li></capacities>
        <power>12</power>
        <cooldownTime>1.5</cooldownTime>
      </li>
      <li>
        <label>bite</label>
        <capacities><li>Bite</li></capacities>
        <power>15</power>
        <cooldownTime>2.0</cooldownTime>
      </li>
    </tools>
    <replaceNativeTools>true</replaceNativeTools>
  </ShapeshifterFramework.ShapeshiftFormDef>

</Defs>
```

**Base forms** (use as `ParentName`):
| Base | Gear | Apparel Hiding |
|------|------|----------------|
| `SSF_BaseForm_Animal` | Drop all | Hide all |
| `SSF_BaseForm_Humanoid` | Keep all | Hide overhead only |
| `SSF_BaseForm_Armored` | Keep all | Hide nothing |

---

## Step 2: HediffDef (Stats & Entry Point)

The HediffDef is the **bridge** between the trigger and the form. Stats go here, not in the FormDef.

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>

  <HediffDef ParentName="SSF_ShapeshiftFormBase">
    <defName>MyMod_WolfFormHediff</defName>
    <label>wolf form</label>
    <description>Transformed into a wolf. Faster movement, stronger melee.</description>
    <isBad>false</isBad>
    <defaultLabelColor>(0.6, 0.8, 0.4)</defaultLabelColor>

    <!-- Stats: offsets ADD, factors MULTIPLY -->
    <stages>
      <li>
        <statOffsets>
          <MoveSpeed>1.5</MoveSpeed>
        </statOffsets>
        <statFactors>
          <MeleeHitChance>1.3</MeleeHitChance>
          <MeleeDodgeChance>1.2</MeleeDodgeChance>
        </statFactors>
      </li>
    </stages>

    <!-- Link to FormDef -->
    <comps>
      <li Class="ShapeshifterFramework.Hediffs.HediffCompProperties_ShapeshiftCore">
        <formDef>MyMod_WolfForm</formDef>
        <!-- Optional overrides (uncomment to use):
        <durationTicks>60000</durationTicks>
        <canRevertVoluntarily>false</canRevertVoluntarily>
        -->
      </li>
    </comps>
  </HediffDef>

</Defs>
```

---

## Step 3: Trigger (AbilityDef)

Choose how the transformation is triggered. Ability is the most common.

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>

  <!-- Self-cast ability -->
  <AbilityDef ParentName="SSF_BaseSelfShiftAbility">
    <defName>MyMod_Ability_WolfShift</defName>
    <label>wolf shift</label>
    <description>Transform into a wolf.</description>
    <iconPath>UI/Abilities/MyWolfIcon</iconPath>  <!-- your icon, or remove for default -->
    <comps>
      <li Class="ShapeshifterFramework.Comps.CompProperties_AbilityGiveHediff_Shapeshift">
        <hediffDef>MyMod_WolfFormHediff</hediffDef>
      </li>
    </comps>
  </AbilityDef>

</Defs>
```

**Grant the ability** to a pawn via:
- A gene: `<abilities><li>MyMod_Ability_WolfShift</li></abilities>`
- A hediff: `<comps><li Class="HediffCompProperties_GiveAbility"><ability>MyMod_Ability_WolfShift</ability></li></comps>`
- Dev mode: Debug Actions > "Give ability..."

---

## Other Trigger Methods

| Method | Instead of AbilityDef, use... |
|--------|-------------------------------|
| **Drug** | `IngestionOutcomeDoer_Shapeshift` in ThingDef.ingestible.outcomeDoers |
| **Item (scroll)** | `CompUseEffect_Shapeshift` in ThingDef.comps |
| **Projectile** | `Projectile_GiveHediff_Shapeshift` as projectile class |
| **Equipment grant** | `CompEquipmentGiveAbility_Shapeshift` in weapon/apparel comps |
| **Auto-shift** | `HediffCompProperties_AutoShift` comp on a hediff (health/combat/light triggers) |

See [FORMDEF_GUIDE_EN.md](FORMDEF_GUIDE_EN.md) Section 5 for full details on each method.

---

## Test It

1. Enable your mod + Shapeshifter Framework
2. Start a new game or load a save
3. Dev mode > Debug Actions > "Give ability..." > select your ability
4. Select a colonist > click the ability gizmo

---

## Customize From Here

Once basic shifting works, explore these features:

| Want to... | Add this to FormDef |
|------------|---------------------|
| Hide head | `<head><mode>Hidden</mode></head>` |
| Custom sounds | `<soundCall>...</soundCall>`, `<soundMeleeHitPawn>...</soundMeleeHitPawn>` |
| VFX on transform | `<transformEnterFleck>PsycastSkipFlashEntry</transformEnterFleck>` |
| Sustain conditions | `<sustainApparels><li>Apparel_PowerArmor</li></sustainApparels>` |
| Block by hediff | `<forbiddenHediffs><li>Flu</li></forbiddenHediffs>` |
| Custom blood | `<bloodDef>Filth_BloodInsect</bloodDef>` |
| Spawn gear | `<spawnWeaponOnTransform><li>MeleeWeapon_LongSword</li></spawnWeaponOnTransform>` |

Full field reference: [FORMDEF_GUIDE_EN.md](FORMDEF_GUIDE_EN.md)
