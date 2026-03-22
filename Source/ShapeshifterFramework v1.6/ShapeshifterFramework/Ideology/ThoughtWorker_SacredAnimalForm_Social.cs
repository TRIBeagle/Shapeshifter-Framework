// ShapeshifterFramework | Ideology | ThoughtWorker_SacredAnimalForm_Social.cs
// 목적 : 성스러운 동물 폼 사회 감정 — 상대가 숭배 동물 폼이면 규율 단계별 의견 부여.
// 용도 : otherPawn이 성스러운 동물 폼이면 관찰자(p)의 규율 단계에 따라 의견 분기.
//        Abhorrent(-10) / Disapproved(-5) / DontCare(+1) / Respected(+5) / Sublime(+10).
// 주의 : Ideology DLC 전용.

using RimWorld;
using ShapeshifterFramework.Utilities;
using Verse;

namespace ShapeshifterFramework.Ideology
{
    /// <summary>성스러운 동물 폼인 상대에 대한 규율 단계별 의견 사회 감정 워커.</summary>
    public class ThoughtWorker_SacredAnimalForm_Social : ThoughtWorker
    {
        /// <summary>자기 자신에 대한 판정 — 사회 감정이므로 항상 비활성.</summary>
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            return ThoughtState.Inactive;
        }

        /// <summary>상대(otherPawn)가 성스러운 동물 폼이면 관찰자(p)의 규율 단계에 따른 stage 반환.</summary>
        protected override ThoughtState CurrentSocialStateInternal(Pawn p, Pawn otherPawn)
        {
            if (!ModsConfig.IdeologyActive) return ThoughtState.Inactive;
            if (p.Ideo == null) return ThoughtState.Inactive;

            // 상대가 성스러운 동물 폼인지 확인
            if (!SacredAnimalFormUtility.IsSacredAnimalForm(otherPawn))
                return ThoughtState.Inactive;

            // 관찰자의 규율 단계에 따라 의견 분기
            return ThoughtState.ActiveAtStage(SacredAnimalFormUtility.GetPreceptStage(p));
        }
    }
}
