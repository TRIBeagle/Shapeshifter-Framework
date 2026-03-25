// ShapeshifterFramework | Patches | Patch_FoodUtility_WillIngestFromInventoryNow.cs
// 목적 : 이데올로기 변신 금기 또는 이미 변신 중인 폰의 인벤토리 기즈모에서 변신 약물을 숨김.
//        연장 약물(IngestionOutcomeDoer_ExtendShapeshift) 역시 비변신/폼 불일치 시 숨김.
// 용도 : 징집 상태에서 Pawn_InventoryTracker.GetGizmos → FoodUtility.WillIngestFromInventoryNow 경로로
//        노출되는 변신/연장 약물 섭취 버튼을 사전 차단. FloatMenu 패치로 잡히지 않는 기즈모 경로 방어.

using HarmonyLib;
using RimWorld;
using ShapeshifterFramework.Utilities;
using Verse;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch(typeof(FoodUtility), nameof(FoodUtility.WillIngestFromInventoryNow))]
    internal static class Patch_FoodUtility_WillIngestFromInventoryNow
    {
        /// <summary>변신 금기이거나 이미 변신 중이면 변신 약물의 기즈모 노출을 차단.
        /// 연장 약물은 비변신/폼 불일치 시 기즈모 숨김.</summary>
        static void Postfix(Pawn pawn, Thing inv, ref bool __result)
        {
            if (!__result) return;
            if (pawn == null || inv == null) return;

            // 1) 변신 약물 (IngestionOutcomeDoer_Shapeshift)
            if (ShapeshiftEligibility.HasShapeshiftOutcomeDoer(inv.def))
            {
                // 이데올로기 변신 금기
                if (ShapeshiftEligibility.IsIdeologyForbidden(pawn))
                {
                    __result = false;
                    return;
                }

                // 이미 변신 중 (allowedFromForms 예외)
                if (ShapeshiftEligibility.IsAlreadyTransformed(pawn))
                {
                    var drugAllowed = ShapeshiftEligibility.GetDrugAllowedFromForms(inv.def);
                    if (!ShapeshiftEligibility.IsFormTransitionAllowed(pawn, drugAllowed))
                        __result = false;
                }
                return;
            }

            // 2) 연장 약물 (IngestionOutcomeDoer_ExtendShapeshift) — 비변신/폼 불일치 시 숨김
            if (ShapeshiftEligibility.GetExtendDrugBlockReason(pawn, inv.def) != null)
                __result = false;
        }
    }
}
