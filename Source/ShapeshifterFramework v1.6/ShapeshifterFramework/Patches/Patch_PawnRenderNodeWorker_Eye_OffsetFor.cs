// ShapeshifterFramework | Patches | Patch_PawnRenderNodeWorker_Eye_OffsetFor.cs
// 목적 : 머리가 커지거나 작아질 때, 바닐라 '눈(Eye)' 그래픽의 위치 오프셋을 머리 스케일에 맞춰 동기화.
// 용도 : 이중 스케일링(HediffEye와의 충돌)을 막기 위해 현재 워커의 실제 타입이 PawnRenderNodeWorker_Eye일 때만 보정을 수행함.

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
            // HediffEye 경유 시 무시
            if (__instance.GetType() != typeof(PawnRenderNodeWorker_Eye)) return;

            // 머리 배율 적용
            ShapeshiftRenderUtility.ApplyOffsetScale(parms, ref __result, useHeadScale: true);
        }
    }
}