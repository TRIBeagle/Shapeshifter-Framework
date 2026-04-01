// ShapeshifterFramework | Patches | Patch_PawnRenderer_CachedFrameScaling.cs
// 목적 : bodyDrawScale > 1인 변신 폼의 아틀라스 캐시를 비활성화하여 풀 렌더링 강제.
// 용도 : 바닐라는 줌아웃 시 인간형 폰을 고정 크기 아틀라스 프레임에 캐시하는데,
//        bodyDrawScale > 1인 대형 폼은 메쉬가 프레임을 넘어 클리핑 발생.
//        ParallelGetPreRenderResults의 disableCache 인자를 true로 강제하여
//        바닐라 비인간형(동물, 메카노이드)과 동일한 풀 렌더링 경로를 사용.
// 주의 : bodyDrawScale ≤ 1인 폼(인간 크기 이하)은 캐시를 정상 사용하므로 성능 비용 없음.

using System;
using System.Reflection;
using HarmonyLib;
using ShapeshifterFramework.Utilities;
using UnityEngine;
using Verse;

namespace ShapeshifterFramework.Patches
{
    /// <summary>bodyDrawScale > 1인 변신 폰은 disableCache=true로 풀 렌더링 강제.</summary>
    [HarmonyPatch(typeof(PawnRenderer), "ParallelPreRenderPawnAt")]
    internal static class Patch_PawnRenderer_ParallelPreRenderPawnAt_DisableCache
    {
        // vanilla-private: PawnRenderer.ParallelGetPreRenderResults, PawnRenderer.results (RimWorld 1.6)
        private static readonly FieldInfo _resultsField =
            AccessTools.Field(typeof(PawnRenderer), "results");

        // ParallelGetPreRenderResults → MethodInfo + 재사용 인자 배열
        // 반환형이 private struct(PreRenderResults)라 Delegate.CreateDelegate 불가 → Invoke 사용
        private static readonly MethodInfo _getPreRenderResultsMI =
            AccessTools.Method(typeof(PawnRenderer), "ParallelGetPreRenderResults");

        // Invoke용 인자 배열 재사용 — 병렬 렌더링 대응으로 ThreadStatic
        [ThreadStatic] private static object[] _invokeArgs;

        /// <summary>줌아웃 캐시 비활성화 임계값 (바닐라 ZoomRootSize).</summary>
        private const float ZoomOutCacheThreshold = 18f;

        static Patch_PawnRenderer_ParallelPreRenderPawnAt_DisableCache()
        {
            if (_getPreRenderResultsMI == null)
                Log.Warning("[SSF] Reflection failed: PawnRenderer.ParallelGetPreRenderResults not found. CachedFrameScaling patch disabled.");
            if (_resultsField == null)
                Log.Warning("[SSF] Reflection failed: PawnRenderer.results not found. CachedFrameScaling patch disabled.");
        }

        [HarmonyPrefix]
        static bool Prefix(PawnRenderer __instance, Pawn ___pawn, Vector3 drawLoc, Rot4? rotOverride, bool neverAimWeapon)
        {
            // 리플렉션 대상이 없으면 바닐라 실행 (바닐라 버전 불일치 방어)
            if (_getPreRenderResultsMI == null || _resultsField == null) return true;

            // 줌아웃 상태가 아니면 캐시가 사용되지 않으므로 개입 불필요
            if (Find.CameraDriver.ZoomRootSize <= ZoomOutCacheThreshold) return true;

            Pawn pawn = ___pawn;
            if (pawn == null) return true;
            if (!ShapeshiftRegistry.TryGet(pawn, out _, out var form)) return true;

            float scale = form.bodyDrawScale ?? 1f;
            if (scale <= 1f) return true;

            // disableCache = true로 호출 → useCached = false + renderTree.ParallelPreDraw 실행
            // PreRenderResults가 private struct라 MethodInfo.Invoke 사용 (박싱 불가피)
            if (_invokeArgs == null) _invokeArgs = new object[4];
            _invokeArgs[0] = drawLoc;
            _invokeArgs[1] = rotOverride;
            _invokeArgs[2] = neverAimWeapon;
            _invokeArgs[3] = true; // disableCache
            object result = _getPreRenderResultsMI.Invoke(__instance, _invokeArgs);
            if (result == null) return true;
            _resultsField.SetValue(__instance, result);
            return false;
        }
    }
}
