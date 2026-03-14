// ShapeshifterFramework | Utilities | ShapeshiftRenderUtility.cs
// 목적 : 렌더링 루프 내부에서 PawnDrawParms에 폼의 스케일 배수 및 오프셋을 적용.
// 용도 : ShapeshiftSizeFactorResolver에서 계산된 배율(bodyWidth, headSizeFactor 등)을 가져와 바닐라 렌더 파라미터(Scale, Offset) 연산에 직접 곱해줌.

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

            if (!ShapeshiftRegistry.TryGet(pawn, out var comp, out var form)) return 1f;

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