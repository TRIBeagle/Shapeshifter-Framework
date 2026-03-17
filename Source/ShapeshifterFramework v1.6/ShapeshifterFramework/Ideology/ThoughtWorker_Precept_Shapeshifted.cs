// ShapeshifterFramework | Ideology | ThoughtWorker_Precept_Shapeshifted.cs
// 목적 : 변신 규율 연동 — 변신 중인 폰에게 규율 단계별 기분 상황 감정 부여.
// 용도 : PreceptComp_SituationalThought에 의해 금기(-10) / 명예로움(+10) ThoughtDef와 연결됨.
//        변신 중이면 ActiveAtStage(0), 아니면 Inactive.

using RimWorld;
using ShapeshifterFramework.Utilities;
using Verse;

namespace ShapeshifterFramework.Ideology
{
    /// <summary>변신 중 자신의 기분에 영향을 주는 규율 상황 감정 워커.</summary>
    public class ThoughtWorker_Precept_Shapeshifted : ThoughtWorker_Precept
    {
        /// <summary>폰이 현재 변신 중이면 활성.</summary>
        protected override ThoughtState ShouldHaveThought(Pawn p)
        {
            if (ShapeshiftRegistry.IsActive(p))
                return ThoughtState.ActiveAtStage(0);
            return ThoughtState.Inactive;
        }
    }
}
