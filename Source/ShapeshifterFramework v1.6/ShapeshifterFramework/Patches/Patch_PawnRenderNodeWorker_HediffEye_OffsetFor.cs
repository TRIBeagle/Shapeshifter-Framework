using HarmonyLib;
using ShapeshifterFramework.Utilities;
using UnityEngine;
using Verse;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch(typeof(PawnRenderNodeWorker_HediffEye), nameof(PawnRenderNodeWorker_HediffEye.OffsetFor))]
    public static class Patch_PawnRenderNodeWorker_HediffEye_OffsetFor
    {
        public static void Postfix(PawnDrawParms parms, ref Vector3 __result)
        {
            // HediffEye는 이중 실행 걱정이 없으므로 바로 유틸리티 호출
            ShapeshiftRenderUtility.ApplyOffsetScale(parms, ref __result, useHeadScale: true);
        }
    }
}