using HarmonyLib;
using ShapeshifterFramework.Utilities;
using UnityEngine;
using Verse;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch(typeof(PawnRenderNodeWorker_Eye), nameof(PawnRenderNodeWorker_Eye.OffsetFor))]
    public static class Patch_PawnRenderNodeWorker_Eye_OffsetFor
    {
        public static void Postfix(PawnRenderNodeWorker __instance, PawnDrawParms parms, ref Vector3 __result)
        {
            // 이중 실행 방지 (HediffEye가 부를 때는 무시)
            if (__instance.GetType() != typeof(PawnRenderNodeWorker_Eye)) return;

            // 유틸리티 호출 한 줄로 끝! (머리 배율 적용)
            ShapeshiftRenderUtility.ApplyOffsetScale(parms, ref __result, useHeadScale: true);
        }
    }
}