# ShapeshiftFormDef Guide (Latest / Detailed)

This is the latest documentation for writing **ShapeshiftFormDef** XMLs.  
Design goal:
- **Override only what you specify**
- Keep everything else vanilla
- Swimming shader/shadow behavior includes safe fallbacks to match vanilla expectations

---

## 0) Folder

Place your shapeshift form XML files (`*.xml`) here:

```
/Defs/ShapeshiftForms/
```

---

## 1) Basic Structure

```xml
<Defs>
  <ShapeshiftFormDef>
    <defName>MyForm</defName>
    <label>My Form</label>
    <description>...</description>
    ...
  </ShapeshiftFormDef>
</Defs>
```

---

## 2) Required / Main Fields

### defName (Required)
- Unique ID of the form
- **Must be unique**
- Also referenced by `allowedFromForms`

### label (Optional)
- UI display name
- Defaults to `defName` if omitted

### description (Optional)
- Tooltip/description text

---

## 3) Scale / Offsets (Render tuning)

Older docs used `customDrawSize`/`customHeadDrawSize`.  
Current fields are:

### bodyDrawScale (Optional, default 1)
- Overall body scale multiplier

### headDrawScale (Optional, default 1)
- Additional head multiplier  
- Final head scale = `(bodyDrawScale × headDrawScale)`

### bodyOffset / headOffset (Optional, default (0,0))
- 2D offsets (Vector2)

### portraitDrawScale (Optional, default 1)
- Additional multiplier applied only in portrait UI

---

## 4) Part Graphics Override (PartOverrideOption)

Parts:
- body
- head
- hair
- beard
- tattooBody
- tattooHead

Each part uses the same option layout (with some body-only fields).

---

### 4.1 mode (Default: Default)
| Value | Meaning |
|---|---|
| Default | keep vanilla |
| Hidden | do not render this part |
| Replace | use replacement texture |

If omitted, treated as `Default`.

---

### 4.2 replacementTexPath (Used by Replace)
- Replacement texture path when `mode=Replace`
- If missing/empty, replacement may not occur.

---

### 4.3 swimmingReplacementTexPath (Body-only)
- Swimming texture path (used only on water tiles)
- If omitted, normal body texture is used.

---

### 4.4 Colors: color / swimmingColor (Optional)
| Field | Meaning |
|---|---|
| color | tint for normal/land |
| swimmingColor | tint when the swimming texture is actually used |

Priority:
1) If swimming texture is used and `swimmingColor` is set → use it
2) Otherwise if `color` is set → use it
3) Otherwise → `Color.white`

---

### 4.5 Shaders: shaderTypeDefName / swimmingShaderTypeDefName (Optional)
| Field | Meaning |
|---|---|
| shaderTypeDefName | ShaderTypeDef defName for land |
| swimmingShaderTypeDefName | shader when swimming texture is used |

Defaults when omitted:
- Land: part default shader (Cutout-family per render node)
- Swimming (body + swimming texture used):  
  `swimmingShaderTypeDefName` → `shaderTypeDefName` → **Transparent fallback**

---

### 4.6 Ground Shadow Override: shadowVolume / shadowOffset (Body-only, Optional)
Overrides the ellipse ground shadow.

Rules:
- If any shadow override is specified → **only the form shadow is drawn** (vanilla shadow is suppressed)
- If none specified → **only vanilla shadow is drawn**
- While swimming texture is used → **no shadow** (vanilla-like)

---

### 4.7 Gender blocks: male / female (Optional)
You can override per gender. Missing fields fall back to the common/base values.

---

## 5) Hide/Show Apparel / Weapons / Gene visuals

Special value `"All"` is supported.

### 5.1 Apparel
- renderHideApparelLayers
- renderHideApparelDefNames
- renderShowApparelLayers
- renderShowApparelDefNames

### 5.2 Weapons
- renderHideWeaponTags
- renderHideWeaponDefNames
- renderShowWeaponTags
- renderShowWeaponDefNames

### 5.3 Genes (visuals)
- renderHideGeneExclusionTags
- renderHideGeneDefNames
- renderShowGeneExclusionTags
- renderShowGeneDefNames

Older `exclusionTags` concept is now represented by the gene hide/show lists above.

---

## 6) Gear handling on transform / Equip lock policy

### 6.1 GearHandling
- apparelOnTransform / weaponsOnTransform
Values:
- None
- Inventory
- Drop

### 6.2 EquipLockMode
- apparelEquipLock / weaponEquipLock
Values:
- Auto
- Always
- Never

---

## 7) Form-only render nodes (renderNodeProperties)

### renderNodeProperties (Optional)
Type: `List<PawnRenderNodeProperties>`  
Added to the pawn render tree only while the form is active.

Same style as vanilla `RaceDef.renderNodeProperties`:

```xml
<renderNodeProperties>
  <li>
	<nodeClass>PawnRenderNode_AttachmentHead</nodeClass>
  </li>
</renderNodeProperties>
```

`PawnRenderNodeProperties` can vary depending on the node class; reference vanilla node property definitions.

---

## 8) Type / Stats / Capacities

- bodyType (BodyTypeDef)
- headType (HeadTypeDef)
- statOffsets (StatModifier list)
- statFactors (StatModifier list)
- capMods (PawnCapacityModifier list)

---

## 9) Requirements / Allowed filters

Requirements (categories):
- requiredGenes
- requiredItems
- requiredApparels
- requiredWeapons
- requiredAbilities
- requiredHediffs

Aggregation mode:
- requirementsMode (All/Any)

Allowed / blocked filters (always evaluated first):
- allowedRaces / disallowedRaces
- allowedMutants / disallowedMutants (DLC)
- allowedXenotypes / disallowedXenotypes (Biotech)

Previous form restriction:
- allowedFromForms (string list of defNames)
- include "None" for untransformed state

---

## 10) Temporary grants while transformed

- addHediffs (HediffAddEntry list)
- addAbilities (AbilityDef list)

---

## 11) Verbs / Tools (add or replace)

- verbs (VerbProperties list)
- tools (Tool list)
- replaceNativeVerbs / replaceNativeTools
- verbGizmoOptions (aligned with verbs order)

---

## 12) Work restrictions / Ideology uncovered thoughts

- disabledWorkTypesOnTransform (WorkTypeDef list)
- disabledWorkTagsOnTransform (WorkTags flags)
- suppressIdeologyUncoveredThoughts (default true)

---

## 13) FX / Sounds / Blood / Flesh

Transform enter/exit FX:
- transformEnterSound / transformExitSound
- transformEnterEffecter / transformExitEffecter
- transformEnterFleck / transformExitFleck
- counts/scales/delays/cooldown

Voice / behavior sound overrides:
- soundCall, soundWounded, soundDeath, soundAngry, soundEating
- soundMeleeHitPawn, soundMeleeHitBuilding, soundMeleeMiss

Blood/flesh:
- bloodDef
- bloodSmearDef
- fleshType

---

## 14) Gizmo / Duration

- hideGizmo (default false)
- gizmoIconPathEnter / gizmoIconPathRevert
- durationTicks (null = infinite)

---

## 15) HAR option

- showHarAddons (default false)

---

## 16) Full Feature Example XML

```xml
<Defs>

  <ShapeshiftFormDef>
    <defName>Example_AllFeatures_Form</defName>
    <label>Example: All Features</label>
    <description>Example form demonstrating many features.</description>

    <bodyDrawScale>1.25</bodyDrawScale>
    <headDrawScale>1.08</headDrawScale>
    <bodyOffset>(0, 0)</bodyOffset>
    <headOffset>(0, 0.04)</headOffset>
    <portraitDrawScale>1.10</portraitDrawScale>

    <body>
      <mode>Replace</mode>
      <replacementTexPath>Things/Pawn/Example/Body_Common</replacementTexPath>
      <swimmingReplacementTexPath>Things/Pawn/Example/Body_Swim</swimmingReplacementTexPath>

      <color>(200,200,200)</color>
      <swimmingColor>(160,160,160)</swimmingColor>

      <shaderTypeDefName>CutoutComplex</shaderTypeDefName>
      <swimmingShaderTypeDefName>Transparent</swimmingShaderTypeDefName>

      <shadowVolume>(0.70, 0.55, 0.55)</shadowVolume>
      <shadowOffset>(0,0,-0.22)</shadowOffset>
    </body>

    <head>
      <mode>Replace</mode>
      <replacementTexPath>Things/Pawn/Example/Head_Common</replacementTexPath>
    </head>

    <hair><mode>Default</mode></hair>
    <beard><mode>Hidden</mode></beard>

    <renderHideApparelLayers><li>All</li></renderHideApparelLayers>
    <renderShowWeaponTags><li>Gun</li></renderShowWeaponTags>
    <renderHideGeneExclusionTags>
      <li>Hair</li><li>Beard</li><li>Tail</li><li>Voice</li>
    </renderHideGeneExclusionTags>

    <apparelOnTransform>Inventory</apparelOnTransform>
    <weaponsOnTransform>Drop</weaponsOnTransform>
    <weaponEquipLock>Always</weaponEquipLock>
	
    <renderNodeProperties>
      <li>
        <nodeClass>PawnRenderNode_AttachmentHead</nodeClass>
        <workerClass>PawnRenderNodeWorker_FlipWhenCrawling</workerClass>
        <texPath>Things/Pawn/Humanlike/HeadAttachments/FloppyEars/FloppyEars</texPath>
        <colorType>Skin</colorType>
        <parentTagDef>Head</parentTagDef>
        <useSkinShader>true</useSkinShader>
        <useRottenColor>true</useRottenColor>
        <rotDrawMode>Fresh, Rotting</rotDrawMode>
        <drawData>
          <defaultData>
            <layer>70</layer>
          </defaultData>
        </drawData>
      </li>
      <li>
        <workerClass>PawnRenderNodeWorker_AttachmentBody</workerClass>
        <texPath>Things/Pawn/Humanlike/BodyAttachments/FurryTail/FurryTail</texPath>
        <colorType>Hair</colorType>
        <overrideMeshSize>(1, 1)</overrideMeshSize>
        <parentTagDef>Body</parentTagDef>
        <rotDrawMode>Fresh, Rotting</rotDrawMode>
        <drawData>
          <scaleOffsetByBodySize>true</scaleOffsetByBodySize>
          <defaultData>
            <layer>-2</layer>
          </defaultData>
          <dataNorth>
            <offset>(0.1, 0, -0.25)</offset>
            <layer>90</layer>
          </dataNorth>
          <dataSouth>
            <offset>(-0.1, 0, -0.25)</offset>
          </dataSouth>
          <dataEast>
            <offset>(-0.5, 0, -0.15)</offset>
          </dataEast>
          <dataWest>
            <offset>(0.5, 0, -0.15)</offset>
          </dataWest>
        </drawData>
      </li>
    </renderNodeProperties>
	
    <statOffsets>
      <li><stat>MoveSpeed</stat><value>0.30</value></li>
    </statOffsets>

    <allowedRaces><li>Human</li></allowedRaces>
    <allowedFromForms><li>None</li></allowedFromForms>
    <requirementsMode>All</requirementsMode>

    <durationTicks>3600</durationTicks>
    <gizmoIconPathEnter>UI/Commands/ExampleTransform</gizmoIconPathEnter>
    <gizmoIconPathRevert>UI/Commands/ExampleRevert</gizmoIconPathRevert>
  </ShapeshiftFormDef>

</Defs>
```

---

## 17) Field name mapping from older docs

| Older docs | Current field |
|---|---|
| customDrawSize | bodyDrawScale |
| customHeadDrawSize | headDrawScale |
| duration | durationTicks |
| gizmoIconPath | gizmoIconPathEnter / gizmoIconPathRevert |
| exclusionTags | renderHideGeneExclusionTags (and show/hide split) |
| allowedPreviousForms | allowedFromForms |
