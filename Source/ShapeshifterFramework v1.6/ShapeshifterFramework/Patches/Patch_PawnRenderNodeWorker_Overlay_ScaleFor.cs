// ShapeshifterFramework | Patches | Patch_PawnRenderNodeWorker_Overlay_ScaleFor.cs
// 목적 : 폰 위에 덧그려지는 상태 이상 오버레이 그래픽의 크기를 변신 스케일에 맞춤.
// 용도 : 대상이 머리 레이어(OverlayLayer.Head)인지 판별하여 몸통과 머리의 각기 다른 배율을 정확히 구분하여 적용함.

using HarmonyLib;
using RimWorld;
using ShapeshifterFramework.Utilities;
using UnityEngine;
using Verse;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch(typeof(PawnRenderNodeWorker), "ScaleFor")]
    public static class Patch_PawnRenderNodeWorker_Overlay_ScaleFor
    {
        public static void Postfix(PawnRenderNodeWorker __instance, PawnRenderNode node, PawnDrawParms parms, ref Vector3 __result)
        {
            // 오버레이 워커가 아니면 무시
            if (!(__instance is PawnRenderNodeWorker_Overlay)) return;

            PawnRenderNodeProperties props;
            if (!ShapeshiftReflectionCache.TryGetPropsFromNode(node, out props)) return;

            // 해당 오버레이가 머리(Head) 레이어인지 확인
            bool isHead = (props != null && props.overlayLayer == PawnOverlayDrawer.OverlayLayer.Head);

            // 머리면 머리 배율(true), 몸통이면 몸통 배율(false)로 크기(Scale) 팽창
            ShapeshiftRenderUtility.ApplyDrawScale(parms, ref __result, useHeadScale: isHead);
        }
    }
}