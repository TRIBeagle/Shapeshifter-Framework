// ShapeshifterFramework | Ideology | ThoughtWorker_Precept_Shapeshifted_Social.cs
// 목적 : 변신 규율 연동 — 변신 중인 상대에 대한 의견 상황 감정 부여.
// 용도 : PreceptComp_SituationalThought에 의해 금기(-20) / 명예로움(+20) 사회 ThoughtDef와 연결됨.
//        상대(otherPawn)가 변신 중이면 ActiveAtStage(0), 아니면 Inactive.

using RimWorld;
using ShapeshifterFramework.Utilities;
using Verse;

namespace ShapeshifterFramework.Ideology
{
    /// <summary>변신 중인 상대에 대한 의견을 주는 규율 사회 감정 워커.</summary>
    public class ThoughtWorker_Precept_Shapeshifted_Social : ThoughtWorker_Precept
    {
        /// <summary>자기 자신에 대한 판정 — 사회 감정이므로 항상 비활성.</summary>
        protected override ThoughtState ShouldHaveThought(Pawn p)
        {
            return ThoughtState.Inactive;
        }

        /// <summary>상대(otherPawn)가 변신 중이면 활성.</summary>
        protected override ThoughtState CurrentSocialStateInternal(Pawn p, Pawn otherPawn)
        {
            if (p.Ideo == null || otherPawn == null) return ThoughtState.Inactive;
            if (ShapeshiftRegistry.IsActive(otherPawn))
                return ThoughtState.ActiveAtStage(0);
            return ThoughtState.Inactive;
        }
    }
}
