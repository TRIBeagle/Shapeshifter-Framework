// ShapeshifterFrameworkSettings.cs

using UnityEngine;
using Verse;

namespace ShapeshifterFramework
{
    public class ShapeshifterFrameworkSettings : ModSettings
    {
        public int maxInlineGizmoCount = 8;
        public bool scaleHeldWeapons = true;
        public bool enablePortraitScale = true;

        public bool autoReequipFromInventory = true;
        public bool autoReequipFromGround = true;

        public bool forbidDroppedItemsOnTransform = true;

        // ── NEW: verb 자동공격 토글 버튼 노출
        public bool showVerbAutoToggle = true;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref maxInlineGizmoCount, "maxInlineGizmoCount", 8);
            Scribe_Values.Look(ref scaleHeldWeapons, "scaleHeldWeapons", true);
            Scribe_Values.Look(ref enablePortraitScale, "enablePortraitScale", true);

            Scribe_Values.Look(ref autoReequipFromInventory, "autoReequipFromInventory", true);
            Scribe_Values.Look(ref autoReequipFromGround, "autoReequipFromGround", true);

            Scribe_Values.Look(ref forbidDroppedItemsOnTransform, "forbidDroppedItemsOnTransform", true);

            // NEW
            Scribe_Values.Look(ref showVerbAutoToggle, "showVerbAutoToggle", true);

            maxInlineGizmoCount = Mathf.Clamp(maxInlineGizmoCount, 1, 24);
        }

        public void DoSettingsWindowContents(Rect inRect)
        {
            var list = new Listing_Standard();
            list.Begin(inRect);

            list.Label("MaxInlineGizmoCountSetting_title".Translate() + $" : {maxInlineGizmoCount}");
            list.Label("MaxInlineGizmoCountSetting_Desc".Translate());
            float giz = maxInlineGizmoCount;
            giz = list.Slider(giz, 1f, 24f);
            maxInlineGizmoCount = Mathf.Clamp(Mathf.RoundToInt(giz), 1, 24);
            list.Gap(8f);

            bool enPortrait = enablePortraitScale;
            list.CheckboxLabeled("EnablePortraitScaleSetting_title".Translate(),
                ref enPortrait, "EnablePortraitScaleSetting_desc".Translate());
            enablePortraitScale = enPortrait;
            list.Gap(4f);

            bool tmpScaleHeld = scaleHeldWeapons;
            list.CheckboxLabeled("ScaleHeldWeaponsSetting_title".Translate(),
                ref tmpScaleHeld, "ScaleHeldWeaponsSetting_desc".Translate());
            scaleHeldWeapons = tmpScaleHeld;
            list.Gap(10f);

            bool tmpInv = autoReequipFromInventory;
            list.CheckboxLabeled("AutoReequipFromInventory_title".Translate(),
                ref tmpInv, "AutoReequipFromInventory_desc".Translate());
            autoReequipFromInventory = tmpInv;

            bool tmpGround = autoReequipFromGround;
            list.CheckboxLabeled("AutoReequipFromGround_title".Translate(),
                ref tmpGround, "AutoReequipFromGround_desc".Translate());
            autoReequipFromGround = tmpGround;

            list.Gap(10f);

            bool tmpForbid = forbidDroppedItemsOnTransform;
            list.CheckboxLabeled("forbidDroppedItemsOnTransform_title".Translate(),
                ref tmpForbid, "forbidDroppedItemsOnTransform_desc".Translate());
            forbidDroppedItemsOnTransform = tmpForbid;

            list.Gap(10f);

            bool tmpShowToggle = showVerbAutoToggle;
            list.CheckboxLabeled("ShowVerbAutoToggle_title".Translate(),
                ref tmpShowToggle, "ShowVerbAutoToggle_desc".Translate());
            showVerbAutoToggle = tmpShowToggle;

            list.Gap(12f);

            if (list.ButtonText("resetAllSetting_title".Translate()))
            {
                maxInlineGizmoCount = 8;
                scaleHeldWeapons = true;
                enablePortraitScale = true;
                autoReequipFromInventory = true;
                autoReequipFromGround = true;
                forbidDroppedItemsOnTransform = true;
                showVerbAutoToggle = true; // NEW
            }

            list.End();
        }
    }
}
