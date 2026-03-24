// ShapeshifterFramework | Root | ShapeshifterFrameworkSettings.cs
// 목적 : 인게임 환경 설정(Mod Settings)의 실제 데이터 보관 및 세이브/로드(IExposable) 처리.
// 용도 : 초상화/무기 스케일 동기화, 장비 자동 재착용 옵션, 디버그 로그 활성화 등의 유저 설정값을 변수로 관리함.

using UnityEngine;
using Verse;

namespace ShapeshifterFramework
{
    public class ShapeshifterFrameworkSettings : ModSettings
    {
        public bool scaleHeldWeapons = true;
        public bool enablePortraitScale = true;

        public bool autoReequipFromInventory = true;
        public bool autoReequipFromGround = true;

        public bool forbidDroppedItemsOnTransform = true;
        public bool showVerbAutoToggle = true;
        public bool showShapeshiftBar = true;

        public bool enableDebugLog = false;

        /// <summary>모든 설정을 기본값으로 초기화.</summary>
        public void ResetToDefaults()
        {
            scaleHeldWeapons = true;
            enablePortraitScale = true;
            autoReequipFromInventory = true;
            autoReequipFromGround = true;
            forbidDroppedItemsOnTransform = true;
            showVerbAutoToggle = true;
            showShapeshiftBar = true;
            enableDebugLog = false;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref scaleHeldWeapons, "scaleHeldWeapons", true);
            Scribe_Values.Look(ref enablePortraitScale, "enablePortraitScale", true);

            Scribe_Values.Look(ref autoReequipFromInventory, "autoReequipFromInventory", true);
            Scribe_Values.Look(ref autoReequipFromGround, "autoReequipFromGround", true);

            Scribe_Values.Look(ref forbidDroppedItemsOnTransform, "forbidDroppedItemsOnTransform", true);
            Scribe_Values.Look(ref showVerbAutoToggle, "showVerbAutoToggle", true);
            Scribe_Values.Look(ref showShapeshiftBar, "showShapeshiftBar", true);

            Scribe_Values.Look(ref enableDebugLog, "enableDebugLog", false);

        }

        public void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard list = new Listing_Standard();
            list.Begin(inRect);

            bool tmpPortrait = enablePortraitScale;
            list.CheckboxLabeled("SSF_Setting_PortraitScale_Title".Translate(), ref tmpPortrait, "SSF_Setting_PortraitScale_Desc".Translate());
            enablePortraitScale = tmpPortrait;

            bool tmpWeapons = scaleHeldWeapons;
            list.CheckboxLabeled("SSF_Setting_WeaponScale_Title".Translate(), ref tmpWeapons, "SSF_Setting_WeaponScale_Desc".Translate());
            scaleHeldWeapons = tmpWeapons;

            list.Gap(10f);

            bool tmpInv = autoReequipFromInventory;
            list.CheckboxLabeled("SSF_Setting_ReequipInventory_Title".Translate(), ref tmpInv, "SSF_Setting_ReequipInventory_Desc".Translate());
            autoReequipFromInventory = tmpInv;

            bool tmpGround = autoReequipFromGround;
            list.CheckboxLabeled("SSF_Setting_ReequipGround_Title".Translate(), ref tmpGround, "SSF_Setting_ReequipGround_Desc".Translate());
            autoReequipFromGround = tmpGround;

            list.Gap(10f);

            bool tmpForbid = forbidDroppedItemsOnTransform;
            list.CheckboxLabeled("SSF_Setting_ForbidItems_Title".Translate(), ref tmpForbid, "SSF_Setting_ForbidItems_Desc".Translate());
            forbidDroppedItemsOnTransform = tmpForbid;

            list.Gap(10f);

            bool tmpShowToggle = showVerbAutoToggle;
            list.CheckboxLabeled("SSF_Setting_VerbToggle_Title".Translate(), ref tmpShowToggle, "SSF_Setting_VerbToggle_Desc".Translate());
            showVerbAutoToggle = tmpShowToggle;

            bool tmpShowBar = showShapeshiftBar;
            list.CheckboxLabeled("SSF_Setting_ShapeshiftBar_Title".Translate(), ref tmpShowBar, "SSF_Setting_ShapeshiftBar_Desc".Translate());
            showShapeshiftBar = tmpShowBar;

            list.Gap(10f);

            bool tmpDebug = enableDebugLog;
            list.CheckboxLabeled("SSF_Setting_DebugLog_Title".Translate(), ref tmpDebug, "SSF_Setting_DebugLog_Desc".Translate());
            enableDebugLog = tmpDebug;

            list.Gap(12f);

            if (list.ButtonText("SSF_Setting_ResetAll_Title".Translate()))
            {
                ResetToDefaults();
            }

            list.End();
        }
    }
}