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