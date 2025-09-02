// 변신 폼일 때 하의 노출 사상 비활성
using HarmonyLib;
using RimWorld;
using ShapeshifterFramework.Comps;
using Verse;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch(typeof(ThoughtWorker_Precept_GroinUncovered), nameof(ThoughtWorker_Precept_GroinUncovered.HasUncoveredGroin))]
    public static class Patch_ThoughtWorker_Precept_GroinUncovered
    {
        static void Postfix(Pawn p, ref bool __result)
        {
            var comp = p?.TryGetComp<CompShapeshifter>();
            var form = (comp != null && comp.isTransformed) ? comp.currentForm as ShapeshiftFormDef : null;
            if (form != null && form.suppressIdeologyUncoveredThoughts)
                __result = false;
        }
    }
}
