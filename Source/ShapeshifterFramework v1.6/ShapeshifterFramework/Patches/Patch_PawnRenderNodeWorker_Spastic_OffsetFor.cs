using HarmonyLib;
using ShapeshifterFramework.Utilities;
using UnityEngine;
using Verse;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch(typeof(PawnRenderNodeWorker_Spastic), "OffsetFor")]
    [HarmonyPriority(Priority.Last)]
    public static class Patch_PawnRenderNodeWorker_Spastic_OffsetFor
    {
        public static void Postfix(PawnDrawParms parms, ref Vector3 __result)
        {
            // 위치(Offset) 보정이므로 ApplyOffsetScale 사용 (X, Z축만 팽창)
            ShapeshiftRenderUtility.ApplyOffsetScale(parms, ref __result, useHeadScale: false);
        }
    }
}