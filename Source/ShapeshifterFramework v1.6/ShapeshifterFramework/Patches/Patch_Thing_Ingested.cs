// ShapeshifterFramework | Patches | Patch_Thing_Ingested.cs
// 목적 : 이미 변신 중이거나 이데올로기 금기인 폰이 변신 약물을 섭취하려 할 때, 약물 소모 없이 차단하고 메시지를 표시.
// 용도 : 인벤토리 기즈모, 드래프트 소비 등 FloatMenu 패치로 잡히지 않는 약물 섭취 경로를 방어.
//        Thing.Ingested Prefix로 개입하여, IngestionOutcomeDoer_Shapeshift 포함 약물이면 섭취 자체를 차단.

using HarmonyLib;
using RimWorld;
using ShapeshifterFramework.Utilities;
using Verse;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch(typeof(Thing), nameof(Thing.Ingested))]
    internal static class Patch_Thing_Ingested
    {
        /// <summary>변신 중이거나 이데올로기 금기인 폰이 변신 약물을 섭취하면 차단.</summary>
        static bool Prefix(Thing __instance, Pawn ingester, ref int __result)
        {
            if (ingester == null) return true;
            if (!ShapeshiftEligibility.HasShapeshiftOutcomeDoer(__instance.def)) return true;

            bool blocked = ShapeshiftEligibility.IsAlreadyTransformed(ingester)
                        || ShapeshiftEligibility.IsIdeologyForbidden(ingester);
            if (!blocked) return true;

            // 섭취 차단: 약물 소모 없이 메시지만 표시
            Messages.Message("SSF_Menu_Blocked".Translate(), ingester, MessageTypeDefOf.RejectInput, false);
            ingester.jobs?.EndCurrentJob(Verse.AI.JobCondition.Incompletable, false);
            __result = 0;
            return false;
        }
    }
}
