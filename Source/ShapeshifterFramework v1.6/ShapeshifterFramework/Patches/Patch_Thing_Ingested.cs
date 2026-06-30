// ShapeshifterFramework | Patches | Patch_Thing_Ingested.cs
// 목적 : 이미 변신 중이거나 이데올로기 금기인 폰이 변신 약물을 섭취하려 할 때, 약물 소모 없이 차단하고 메시지를 표시.
//        연장 약물(IngestionOutcomeDoer_ExtendShapeshift) 역시 비변신/폼 불일치 시 차단.
// 용도 : 인벤토리 기즈모, 드래프트 소비 등 FloatMenu 패치로 잡히지 않는 약물 섭취 경로를 방어.
//        Thing.Ingested Prefix로 개입하여, 변신/연장 약물이면 섭취 자체를 차단.

using HarmonyLib;
using RimWorld;
using ShapeshifterFramework.Utilities;
using Verse;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch(typeof(Thing), nameof(Thing.Ingested))]
    internal static class Patch_Thing_Ingested
    {
        /// <summary>변신 중이거나 이데올로기 금기인 폰이 변신 약물을 섭취하면 차단.
        /// 연장 약물은 비변신/폼 불일치 시 차단.</summary>
        static bool Prefix(Thing __instance, Pawn ingester, ref float __result)
        {
            if (ingester == null) return true;

            string blockMessage = null;

            // 1) 변신 약물 (IngestionOutcomeDoer_Shapeshift)
            if (ShapeshiftEligibility.HasShapeshiftOutcomeDoer(__instance.def))
            {
                bool ideologyBlocked = ShapeshiftEligibility.IsIdeologyForbidden(ingester);
                bool transformBlocked = false;
                if (ShapeshiftEligibility.IsAlreadyTransformed(ingester))
                {
                    var drugAllowed = ShapeshiftEligibility.GetDrugAllowedFromForms(__instance.def);
                    transformBlocked = !ShapeshiftEligibility.IsFormTransitionAllowed(ingester, drugAllowed);
                }
                // 차단 사유 구분 표시 — 이데올로기 금기 vs 이미 변신 중 (IngestBlock 패치와 일관)
                if (ideologyBlocked)
                    blockMessage = "SSF_GizmoDisabled_IdeologyForbidden".Translate();
                else if (transformBlocked)
                    blockMessage = ShapeshiftEligibility.AlreadyTransformedReason(ingester);
            }
            // 2) 연장 약물 (IngestionOutcomeDoer_ExtendShapeshift) — 비변신/폼 불일치 시 차단
            else
            {
                blockMessage = ShapeshiftEligibility.GetExtendDrugBlockReason(ingester, __instance.def);
            }

            if (blockMessage == null) return true;

            // 섭취 차단: 약물 소모 없이 메시지만 표시
            Messages.Message(blockMessage, ingester, MessageTypeDefOf.RejectInput, false);
            ingester.jobs?.EndCurrentJob(Verse.AI.JobCondition.Incompletable, false);
            __result = 0f;
            return false;
        }
    }
}
