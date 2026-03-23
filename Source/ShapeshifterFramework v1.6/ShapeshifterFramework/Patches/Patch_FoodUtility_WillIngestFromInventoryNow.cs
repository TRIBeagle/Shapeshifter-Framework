// ShapeshifterFramework | Patches | Patch_FoodUtility_WillIngestFromInventoryNow.cs
// 목적 : 이데올로기 변신 금기 또는 이미 변신 중인 폰의 인벤토리 기즈모에서 변신 약물을 숨김.
// 용도 : 징집 상태에서 Pawn_InventoryTracker.GetGizmos → FoodUtility.WillIngestFromInventoryNow 경로로
//        노출되는 변신 약물 섭취 버튼을 사전 차단. FloatMenu 패치로 잡히지 않는 기즈모 경로 방어.

using HarmonyLib;
using RimWorld;
using ShapeshifterFramework.Comps;
using ShapeshifterFramework.Utilities;
using Verse;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch(typeof(FoodUtility), nameof(FoodUtility.WillIngestFromInventoryNow))]
    internal static class Patch_FoodUtility_WillIngestFromInventoryNow
    {
        /// <summary>변신 금기이거나 이미 변신 중이면 변신 약물의 기즈모 노출을 차단.</summary>
        static void Postfix(Pawn pawn, Thing thing, ref bool __result)
        {
            if (!__result) return;
            if (pawn == null || thing == null) return;

            if (!HasShapeshiftOutcomeDoer(thing.def)) return;

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

        /// <summary>ThingDef의 ingestible.outcomeDoers에 IngestionOutcomeDoer_Shapeshift가 포함되어 있는지 확인.</summary>
        private static bool HasShapeshiftOutcomeDoer(ThingDef def)
        {
            if (def?.ingestible?.outcomeDoers == null) return false;
            for (int i = 0; i < def.ingestible.outcomeDoers.Count; i++)
            {
                if (def.ingestible.outcomeDoers[i] is IngestionOutcomeDoer_Shapeshift)
                    return true;
            }
            return false;
        }
    }
}
