// ShapeshifterFramework | Ideology | Patch_ShapeshiftIdeologyTaboo.cs
// 목적 : 변신 시 HistoryEvent(SSF_Shapeshifted)를 발행하여 규율 연동 기억 감정을 자동 처리.
// 용도 : ShapeshiftCoreUtility.FireFormApplied 호출 직후(Postfix)에 개입.
//        PreceptComp_SelfTookMemoryThought가 이벤트를 수신하여 금기 위반 기억 감정을 부여.
//        PreceptComp_KnowsMemoryThought가 이벤트를 수신하여 목격자 기억 감정을 부여.
// 주의 : Ideology DLC 미설치 시 ModsConfig.IdeologyActive 체크로 즉시 반환.

using HarmonyLib;
using RimWorld;
using ShapeshifterFramework.Utilities;
using Verse;

namespace ShapeshifterFramework.Ideology
{
    /// <summary>변신 시 HistoryEvent 발행 — 규율 컴프가 기억 감정을 자동 부여.</summary>
    [HarmonyPatch(typeof(ShapeshiftCoreUtility), "FireFormApplied")]
    public static class Patch_ShapeshiftIdeologyTaboo
    {
        static void Postfix(Pawn pawn, ShapeshiftFormDef form)
        {
            if (!ModsConfig.IdeologyActive) return;
            if (pawn == null || pawn.Ideo == null) return;

            var eventDef = ShapeshiftDefOf.SSF_Shapeshifted;
            if (eventDef == null) return;

            // HistoryEvent 발행 — PreceptComp_SelfTookMemoryThought가 자동 처리
            Find.HistoryEventsManager.RecordEvent(new HistoryEvent(eventDef, pawn.Named(HistoryEventArgsNames.Doer)));
        }
    }
}
