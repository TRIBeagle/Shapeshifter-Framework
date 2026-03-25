// ShapeshifterFramework | Patches | Patch_Thing_DrawSize.cs
// 목적 : 변신 폼의 bodyDrawScale에 비례하여 Thing.DrawSize를 확대, 카메라 줌아웃 시 렌더링 컬링 바운드를 올바르게 반영.
// 용도 : 바닐라 휴먼라이크 폰은 DrawSize가 (1,1)로 고정이라, 스케일이 커져도 게임 엔진이 원래 크기로 컬링함.
//        이 패치로 DrawSize를 bodyDrawScale 배수만큼 키워, 줌아웃 시 잘림 현상을 방지.
// 주의 : bodyDrawScale이 1이면 성능 비용 없이 바로 반환.

using HarmonyLib;
using ShapeshifterFramework.Utilities;
using UnityEngine;
using Verse;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch(typeof(Thing), "get_DrawSize")]
    [HarmonyPriority(Priority.Last)]
    public static class Patch_Thing_DrawSize
    {
        [HarmonyPostfix]
        static void Postfix(Thing __instance, ref Vector2 __result)
        {
            Pawn pawn = __instance as Pawn;
            if (pawn == null) return;

            if (!ShapeshiftRegistry.TryGet(pawn, out var comp, out var form)) return;

            float sBody = form.bodyDrawScale.HasValue ? Mathf.Max(0.01f, form.bodyDrawScale.Value) : 1f;
            if (Mathf.Approximately(sBody, 1f)) return;

            // 바닐라 기본값(1,1)에 bodyDrawScale 배수 적용
            __result *= sBody;
        }
    }
}
