// ShapeshifterFramework | Ideology | Patch_ShapeshiftIdeologyTaboo.cs
// 목적 : 변신 금기 규율 위반 시 "금기를 어김" 기억 감정을 1회 부여.
// 용도 : ShapeshiftCoreUtility.FireFormApplied 호출 직후(Postfix)에 개입.
//        폰의 이데올로기에 SSF_Shapeshifting_Taboo 규율이 있으면 기억 감정 추가.
// 주의 : Ideology DLC 미설치 시 ModsConfig.IdeologyActive 체크로 즉시 반환.

using HarmonyLib;
using RimWorld;
using ShapeshifterFramework.Utilities;
using Verse;

namespace ShapeshifterFramework.Ideology
{
    /// <summary>변신 시 금기 규율 위반 기억 감정 자동 부여.</summary>
    [HarmonyPatch(typeof(ShapeshiftCoreUtility), "FireFormApplied")]
    public static class Patch_ShapeshiftIdeologyTaboo
    {
        static void Postfix(Pawn pawn, ShapeshiftFormDef form)
        {
            if (!ModsConfig.IdeologyActive) return;
            if (pawn == null || pawn.Ideo == null) return;

            // 금기 규율 Def 조회 (캐시 없이 GetNamedSilentFail — 로드 시 1회 호출 아님, 변신 시점에만 호출)
            var tabooDef = DefDatabase<PreceptDef>.GetNamedSilentFail("SSF_Shapeshifting_Taboo");
            if (tabooDef == null) return;

            // 폰의 이데올로기에 금기 규율이 있는지 확인
            var precepts = pawn.Ideo.PreceptsListForReading;
            for (int i = 0; i < precepts.Count; i++)
            {
                if (precepts[i].def == tabooDef)
                {
                    var thoughtDef = DefDatabase<ThoughtDef>.GetNamedSilentFail("SSF_Thought_ShapeshiftTabooViolated");
                    if (thoughtDef == null) return;

                    // Thought_MemoryPrecept는 precept 참조가 필요 — TryGainMemory(ThoughtDef, Pawn, Precept) 사용
                    pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(thoughtDef, null, precepts[i]);
                    return;
                }
            }
        }
    }
}
