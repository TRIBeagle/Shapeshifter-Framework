// ShapeshifterFramework | Utilities | ShapeshiftRenderUtility.cs
// 목적 : 렌더링 루프 내부에서 PawnDrawParms에 폼의 스케일 배수 및 오프셋을 적용.
// 용도 : ShapeshiftSizeFactorResolver에서 계산된 배율(bodyWidth, headSizeFactor 등)을 가져와 바닐라 렌더 파라미터(Scale, Offset) 연산에 직접 곱해줌.

using UnityEngine;
using Verse;

namespace ShapeshifterFramework.Utilities
{
    public static class ShapeshiftRenderUtility
    {
        public static float GetShapeScale(Pawn pawn, bool useHeadScale = false)
        {
            if (pawn == null) return 1f;

            // 비변신 폰 조기 탈출 (프레임 캐시 진입 회피)
            if (!ShapeshiftRegistry.TryGet(pawn, out _, out _)) return 1f;

            // 변신 배수 비율은 SizeFactorResolver 프레임 캐시에서 즉시 반환.
            // (base 재조회/재나눗셈 제거 — 비율은 TryGetOverrides에서 base 약분 후 미리 계산됨)
            return ShapeshiftSizeFactorResolver.GetScaleRatio(pawn, useHeadScale);
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