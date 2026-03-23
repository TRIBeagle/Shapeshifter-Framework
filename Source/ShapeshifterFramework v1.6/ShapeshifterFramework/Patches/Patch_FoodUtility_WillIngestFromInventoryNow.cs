// ShapeshifterFramework | Patches | Patch_FoodUtility_WillIngestFromInventoryNow.cs
// 목적 : 이데올로기 변신 금기 또는 이미 변신 중인 폰의 인벤토리 기즈모에서 변신 약물을 숨김.
// 용도 : 징집 상태에서 Pawn_InventoryTracker.GetGizmos → FoodUtility.WillIngestFromInventoryNow 경로로
//        노출되는 변신 약물 섭취 버튼을 사전 차단. FloatMenu 패치로 잡히지 않는 기즈모 경로 방어.

using HarmonyLib;
using RimWorld;
using ShapeshifterFramework.Utilities;
using Verse;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch(typeof(FoodUtility), nameof(FoodUtility.WillIngestFromInventoryNow))]
    internal static class Patch_FoodUtility_WillIngestFromInventoryNow
    {
        /// <summary>변신 금기이거나 이미 변신 중이면 변신 약물의 기즈모 노출을 차단.</summary>
        static void Postfix(Pawn pawn, Thing inv, ref bool __result)
        {
            if (!__result) return;
            if (pawn == null || inv == null) return;

            if (!ShapeshiftEligibility.HasShapeshiftOutcomeDoer(inv.def)) return;

            // 이데올로기 변신 금기
            if (ShapeshiftEligibility.IsIdeologyForbidden(pawn))
            {
                __result = false;
                return;
            }

            // 이미 변신 중
            if (ShapeshiftEligibility.IsAlreadyTransformed(pawn))
            {
                __result = false;
            }
        }
    }
}
