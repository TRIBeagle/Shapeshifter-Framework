// ShapeshifterFramework | Patches | Patch_PawnRenderNodeWorker_Spastic_ScaleFor.cs
// 목적 : Anomaly DLC의 촉수(Tentacle) 등 꾸물거리는(Spastic) 렌더 노드의 전체 메쉬 크기(Scale)를 보정.
// 용도 : ScaleFor에 Postfix로 개입하여 바디 전체 크기 배율(bodyDrawScale)을 적용, 거대해진 폼의 스케일에 맞춰 Spastic 노드의 그래픽 사이즈를 팽창시킴.

using HarmonyLib;
using ShapeshifterFramework.Utilities;
using UnityEngine;
using Verse;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch(typeof(PawnRenderNodeWorker_Spastic), "ScaleFor")]
    [HarmonyPriority(Priority.Last)]
    internal static class Patch_PawnRenderNodeWorker_Spastic_ScaleFor
    {
        public static void Postfix(PawnDrawParms parms, ref Vector3 __result)
        {
            // 몸통 기준 스케일 적용
            ShapeshiftRenderUtility.ApplyDrawScale(parms, ref __result, useHeadScale: false);
        }
    }
}