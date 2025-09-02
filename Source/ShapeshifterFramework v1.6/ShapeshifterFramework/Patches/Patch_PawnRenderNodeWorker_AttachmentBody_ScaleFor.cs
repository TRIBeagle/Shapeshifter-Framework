// Patch_PawnRenderNodeWorker_AttachmentBody_ScaleFor.cs  (C# 7.3)
using HarmonyLib;
using ShapeshifterFramework.Comps;
using ShapeshifterFramework.Utilities;
using UnityEngine;
using Verse;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch(typeof(PawnRenderNodeWorker_AttachmentBody), "ScaleFor")]
    [HarmonyPriority(Priority.Last)]
    public static class Patch_PawnRenderNodeWorker_AttachmentBody_ScaleFor
    {
        static float SafeDiv(float a, float b) { return Mathf.Approximately(b, 0f) ? 1f : (a / b); }

        static void Postfix(PawnDrawParms parms, ref Vector3 __result)
        {
            var pawn = parms.pawn;
            if (pawn == null) return;

            var comp = pawn.TryGetComp<CompShapeshifter>();
            if (comp == null || !comp.isTransformed || comp.currentForm == null) return;

            var ls = pawn.ageTracker != null ? pawn.ageTracker.CurLifeStage : null;
            float vanillaBodyW = (ls != null && ls.bodyWidth.HasValue) ? ls.bodyWidth.Value : 1.5f;

            var eff = ShapeshiftSizeFactorResolver.Effective(pawn);
            float s = SafeDiv(eff.bodyWidth, vanillaBodyW);
            if (Mathf.Approximately(s, 1f)) return;

            // 메시만 등방 스케일. 오프셋은 바닐라 유지.
            __result = new Vector3(__result.x * s, __result.y, __result.z * s);
        }
    }
}
