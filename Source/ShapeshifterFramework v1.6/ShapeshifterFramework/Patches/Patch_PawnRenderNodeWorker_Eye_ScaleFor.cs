// Patch_PawnRenderNodeWorker_Eye_ScaleFor.cs (C# 7.3)
using HarmonyLib;
using ShapeshifterFramework.Comps;
using ShapeshifterFramework.Utilities;
using UnityEngine;
using Verse;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch(typeof(PawnRenderNodeWorker_Eye), "ScaleFor")]
    [HarmonyPriority(Priority.Last)]
    public static class Patch_PawnRenderNodeWorker_Eye_ScaleFor
    {
        static float SafeDiv(float a, float b) { return Mathf.Approximately(b, 0f) ? 1f : (a / b); }

        static void Postfix(PawnDrawParms parms, ref Vector3 __result)
        {
            var pawn = parms.pawn;
            if (pawn == null) return;

            var comp = pawn.TryGetComp<CompShapeshifter>();
            if (comp == null || !comp.isTransformed || comp.currentForm == null) return;

            var ls = pawn.ageTracker != null ? pawn.ageTracker.CurLifeStage : null;
            float vanillaHead = (ls != null && ls.headSizeFactor.HasValue) ? ls.headSizeFactor.Value : 1f;

            var eff = ShapeshiftSizeFactorResolver.Effective(pawn);
            float s = SafeDiv(eff.headSizeFactor, vanillaHead);
            if (Mathf.Approximately(s, 1f)) return;

            __result = new Vector3(__result.x * s, __result.y, __result.z * s);
        }
    }
}
