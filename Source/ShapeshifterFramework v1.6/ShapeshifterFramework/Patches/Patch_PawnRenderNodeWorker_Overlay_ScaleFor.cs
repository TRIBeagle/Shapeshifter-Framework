// ShapeshifterFramework | Patches | Patch_PawnRenderNodeWorker_Overlay_ScaleFor.cs
// 목적 : 폰 위에 덧그려지는 상태 이상 오버레이 그래픽의 크기를 변신 스케일에 맞춤.
// 용도 : 대상이 머리 레이어(OverlayLayer.Head)인지 판별하여 몸통과 머리의 각기 다른 배율을 정확히 구분하여 적용함.

using HarmonyLib;
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
            // 오버레이 워커만 처리
            if (!(__instance is PawnRenderNodeWorker_Overlay)) return;

            // 비변신 폰 즉시 스킵 — 오버레이 워커에서도 리플렉션/스케일 연산 방지
            if (!ShapeshiftRegistry.IsActive(parms.pawn)) return;

            PawnRenderNodeProperties props = node?.Props;
            if (props == null) return;

            // Head 레이어 여부 확인
            bool isHead = (props.overlayLayer == PawnOverlayDrawer.OverlayLayer.Head);

            // 머리/몸통 구분하여 배율 적용
            ShapeshiftRenderUtility.ApplyDrawScale(parms, ref __result, useHeadScale: isHead);
        }
    }
}