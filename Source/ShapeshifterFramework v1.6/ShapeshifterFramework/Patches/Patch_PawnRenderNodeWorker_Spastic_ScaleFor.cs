using HarmonyLib;
using ShapeshifterFramework.Utilities;
using UnityEngine;
using Verse;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch(typeof(PawnRenderNodeWorker_Spastic), "ScaleFor")]
    [HarmonyPriority(Priority.Last)]
    public static class Patch_PawnRenderNodeWorker_Spastic_ScaleFor
    {
        public static void Postfix(PawnDrawParms parms, ref Vector3 __result)
        {
            // 몸통(Body) 기준 전체 크기(Scale) 팽창
            ShapeshiftRenderUtility.ApplyDrawScale(parms, ref __result, useHeadScale: false);
        }
    }
}