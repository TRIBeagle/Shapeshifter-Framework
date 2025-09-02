using HarmonyLib;
using RimWorld;
using ShapeshifterFramework.Comps;
using Verse;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch(typeof(ThoughtWorker_Precept_GroinChestHairOrFaceUncovered), nameof(ThoughtWorker_Precept_GroinChestHairOrFaceUncovered.HasUncoveredGroinChestHairOrFace))]
    public static class Patch_ThoughtWorker_Precept_GroinChestHairOrFaceUncovered
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
