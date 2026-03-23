// ShapeshifterFramework | Patches | Patch_PawnRenderer_CachedFrameScaling.cs
// 목적 : bodyDrawScale > 1인 변신 폼의 아틀라스 캐시를 비활성화하여 풀 렌더링 강제.
// 용도 : 바닐라는 줌아웃 시 인간형 폰을 고정 크기 아틀라스 프레임에 캐시하는데,
//        bodyDrawScale > 1인 대형 폼은 메쉬가 프레임을 넘어 클리핑 발생.
//        ParallelGetPreRenderResults의 disableCache 인자를 true로 강제하여
//        바닐라 비인간형(동물, 메카노이드)과 동일한 풀 렌더링 경로를 사용.
// 주의 : bodyDrawScale ≤ 1인 폼(인간 크기 이하)은 캐시를 정상 사용하므로 성능 비용 없음.

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
        private static readonly MethodInfo _getPreRenderResults =
            AccessTools.Method(typeof(PawnRenderer), "ParallelGetPreRenderResults");
        private static readonly FieldInfo _resultsField =
            AccessTools.Field(typeof(PawnRenderer), "results");

        // ParallelPreRenderPawnAt은 병렬 호출 가능 → ThreadStatic으로 스레드별 배열 사용
        [System.ThreadStatic]
        private static object[] _invokeArgs;

        private static object[] InvokeArgs => _invokeArgs ?? (_invokeArgs = new object[4]);

        [HarmonyPrefix]
        static bool Prefix(PawnRenderer __instance, Pawn ___pawn, Vector3 drawLoc, Rot4? rotOverride, bool neverAimWeapon)
        {
            // Reflection 대상이 없으면 바닐라 실행 (바닐라 버전 불일치 방어)
            if (_getPreRenderResults == null || _resultsField == null) return true;

            // 줌아웃 상태가 아니면 캐시가 사용되지 않으므로 개입 불필요
            if (Find.CameraDriver.ZoomRootSize <= 18f) return true;

            Pawn pawn = ___pawn;
            if (pawn == null) return true;
            if (!ShapeshiftRegistry.TryGet(pawn, out _, out var form)) return true;

            float scale = form.bodyDrawScale.HasValue ? form.bodyDrawScale.Value : 1f;
            if (scale <= 1f) return true;

            // disableCache = true로 호출 → useCached = false + renderTree.ParallelPreDraw 실행
            var args = InvokeArgs;
            args[0] = drawLoc;
            args[1] = rotOverride;
            args[2] = neverAimWeapon;
            args[3] = true;
            object result = _getPreRenderResults.Invoke(__instance, args);
            if (result == null) return true; // Invoke 실패 시 바닐라 폴백
            _resultsField.SetValue(__instance, result);
            return false;
        }
    }
}
