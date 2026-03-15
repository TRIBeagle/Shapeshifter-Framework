// ShapeshifterFramework | Patches | Patch_GlobalTextureAtlasManager_SkipLargeForms.cs
// 목적 : bodyDrawScale ≠ 1인 변신 폰은 텍스처 아틀라스 캐시를 건너뛰어 실시간 렌더링을 강제.
// 용도 : 바닐라는 줌아웃 시 폰을 고정 크기 아틀라스 프레임에 캐시하는데, bodyDrawScale이 1이 아닌
//        대형/소형 폼은 메쉬가 프레임에 맞지 않아 클리핑 발생. 아틀라스 캐시를 건너뛰면
//        모든 줌 레벨에서 실시간 렌더링되어 정확한 크기로 표시됨.
// 주의 : 변신 폰 수가 많으면 줌아웃 시 성능 비용이 발생할 수 있으나, 일반적으로 소수이므로 무시 가능.

using HarmonyLib;
using ShapeshifterFramework.Utilities;
using UnityEngine;
using Verse;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch(typeof(GlobalTextureAtlasManager), nameof(GlobalTextureAtlasManager.TryGetPawnFrameSet))]
    [HarmonyPriority(Priority.First)]
    public static class Patch_GlobalTextureAtlasManager_SkipLargeForms
    {
        /// <summary>bodyDrawScale ≠ 1인 변신 폰은 아틀라스 캐시를 건너뛰어 실시간 렌더링 강제.</summary>
        [HarmonyPrefix]
        static bool Prefix(Pawn pawn, ref bool __result)
        {
            if (!ShapeshiftRegistry.TryGet(pawn, out _, out var form)) return true;

            float sBody = form.bodyDrawScale.HasValue ? form.bodyDrawScale.Value : 1f;
            if (Mathf.Approximately(sBody, 1f)) return true;

            // 아틀라스 캐시 건너뛰기 — 호출자가 실시간 렌더링으로 폴백
            __result = false;
            return false;
        }
    }
}
