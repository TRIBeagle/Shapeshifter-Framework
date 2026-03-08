// ShapeshifterFramework | Patches | Patch_PawnRenderNodeWorker_Spastic_OffsetFor.cs
// 목적 : 촉수(Tentacle) 등 자체적으로 꿈틀거리는(Spastic) 애니메이션을 가진 파츠의 움직임 반경을 변신 크기에 맞춰 보정.
// 용도 : 거대한 폼으로 변신했을 때 촉수의 꾸물거리는 폭이 너무 좁아보이는 시각적 이질감을 없애기 위해, X 및 Z축 이동(Offset) 폭을 바디 스케일에 비례하여 팽창시킴.

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
            // X, Z축 오프셋 보정
            ShapeshiftRenderUtility.ApplyOffsetScale(parms, ref __result, useHeadScale: false);
        }
    }
}