// Patch_HealthCardUtility_GetPawnCapacityTip.cs
// 목적: 건강 카드 능력치 툴팁 끝에 폼 보정 한 줄을 추가.
// 용도: Postfix에서 폼 capMods를 요약하여 __result에 덧붙임.
// 주의: 설명이 없으면 무변경. 줄바꿈 처리 중복에 주의.

using HarmonyLib;
using RimWorld;
using ShapeshifterFramework.Utilities;
using Verse;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch(typeof(HealthCardUtility), "GetPawnCapacityTip")]
    public static class Patch_HealthCardUtility_GetPawnCapacityTip
    {
        static void Postfix(
            [HarmonyArgument(0)] Pawn pawn,
            [HarmonyArgument(1)] PawnCapacityDef capacity,
            ref string __result)
        {
            // 변신 영향 설명 한 줄 생성(없으면 빈 문자열)
            string extra = ShapeshiftCapacityExplainUtility.BuildExplainLine(pawn, capacity);
            if (!string.IsNullOrEmpty(extra))
            {
                if (!string.IsNullOrEmpty(__result)) __result += "\n";
                __result += extra; // 맨 아래에 한 줄 추가
            }
        }
    }
}
