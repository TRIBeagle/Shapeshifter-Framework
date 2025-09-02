// Patch_PawnRenderNodeWorker_Overlay_ScaleFor.cs (C# 7.3)
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
        static float SafeDiv(float a, float b)
        {
            return Mathf.Approximately(b, 0f) ? 1f : (a / b);
        }

        static void Postfix(PawnRenderNodeWorker __instance, PawnRenderNode node, PawnDrawParms parms, ref Vector3 __result)
        {
            // 오버레이 전용
            if (!(__instance is PawnRenderNodeWorker_Overlay)) return;

            var pawn = parms.pawn;
            if (pawn == null) return;

            // props 가져오기(캐시 유틸, out 오버로드)
            PawnRenderNodeProperties props;
            if (!ShapeshiftReflectionCache.TryGetPropsFromNode(node, out props))
                return;

            var layer = props != null ? props.overlayLayer : PawnOverlayDrawer.OverlayLayer.Body;

            var ls = pawn.ageTracker != null ? pawn.ageTracker.CurLifeStage : null;
            var eff = ShapeshiftSizeFactorResolver.Effective(pawn);

            float ratio = 1f;
            if (layer == PawnOverlayDrawer.OverlayLayer.Head)
            {
                float vanillaHead = (ls != null && ls.headSizeFactor.HasValue) ? ls.headSizeFactor.Value : 1f;
                ratio = SafeDiv(eff.headSizeFactor, vanillaHead);
            }
            else
            {
                float vanillaBodyW = (ls != null && ls.bodyWidth.HasValue) ? ls.bodyWidth.Value : 1.5f;
                ratio = SafeDiv(eff.bodyWidth, vanillaBodyW);
            }

            if (!Mathf.Approximately(ratio, 1f))
                __result = new Vector3(__result.x * ratio, __result.y, __result.z * ratio);
        }
    }
}
