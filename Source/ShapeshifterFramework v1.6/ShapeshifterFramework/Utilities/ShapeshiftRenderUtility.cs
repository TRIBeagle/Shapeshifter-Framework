using ShapeshifterFramework.Comps;
using UnityEngine;
using Verse;

namespace ShapeshifterFramework.Utilities
{
    public static class ShapeshiftRenderUtility
    {
        public static float GetShapeScale(Pawn pawn, bool useHeadScale = false)
        {
            if (pawn == null) return 1f;

            var comp = pawn.TryGetComp<CompShapeshifter>();
            if (comp == null || !comp.isTransformed || comp.currentForm == null) return 1f;

            var ls = pawn.ageTracker?.CurLifeStage;
            var eff = ShapeshiftSizeFactorResolver.Effective(pawn);

            if (useHeadScale)
            {
                float vanillaHead = (ls != null && ls.headSizeFactor.HasValue) ? ls.headSizeFactor.Value : 1f;
                return Mathf.Approximately(vanillaHead, 0f) ? 1f : (eff.headSizeFactor / vanillaHead);
            }
            else
            {
                float vanillaBodyW = (ls != null && ls.bodyWidth.HasValue) ? ls.bodyWidth.Value : 1.5f;
                return Mathf.Approximately(vanillaBodyW, 0f) ? 1f : (eff.bodyWidth / vanillaBodyW);
            }
        }

        public static void ApplyOffsetScale(PawnDrawParms parms, ref Vector3 offset, bool useHeadScale = false)
        {
            float s = GetShapeScale(parms.pawn, useHeadScale);
            if (Mathf.Approximately(s, 1f)) return;

            offset.x *= s;
            offset.z *= s;
        }

        public static void ApplyDrawScale(PawnDrawParms parms, ref Vector3 scale, bool useHeadScale = false)
        {
            float s = GetShapeScale(parms.pawn, useHeadScale);
            if (Mathf.Approximately(s, 1f)) return;

            scale *= s;
        }
    }
}