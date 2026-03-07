// ShapeshifterFramework | Patches | Patch_PawnRenderNodeWorker_HediffEye_OffsetFor.cs
// 목적 : 생체공학 눈 같은 헤디프 전용 안구 그래픽의 위치 오프셋을 머리 스케일에 맞춰 동기화.
// 용도 : Eye_OffsetFor와 별개로 작동하며, 머리 배율(useHeadScale: true)을 적용하여 눈 그래픽이 공중에 뜨거나 파묻히는 것을 방지함.

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
            // 머리 배율 적용
            ShapeshiftRenderUtility.ApplyOffsetScale(parms, ref __result, useHeadScale: true);
        }
    }
}