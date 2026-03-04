// ShapeshifterFramework | Patches | Patch_HealthCardUtility_GetPawnCapacityTip.cs
// 목적 : 인게임 건강(Health) 탭에서 의식, 조작 등의 능력치에 마우스를 올렸을 때 뜨는 툴팁에 변신 폼이 주는 스탯 보정치를 추가.
// 용도 : GetPawnCapacityTip에 Postfix로 개입하여, ShapeshiftCapacityExplainUtility가 생성한 한 줄(예: "늑대인간 가산: +20%")을 바닐라 툴팁 문자열 맨 밑에 덧붙임.

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
