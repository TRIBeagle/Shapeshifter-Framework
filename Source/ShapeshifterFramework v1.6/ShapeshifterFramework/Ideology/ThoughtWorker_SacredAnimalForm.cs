// ShapeshifterFramework | Ideology | ThoughtWorker_SacredAnimalForm.cs
// 목적 : 성스러운 동물 폼 감정 — 변신 폼이 이데올로기의 숭배 동물과 일치하면 규율 단계별 기분 부여.
// 용도 : formDef.linkedSacredAnimalDef가 Ideo.VeneratedAnimals에 포함되면 활성.
//        변신 규율(SSF_Shapeshifting) 단계에 따라 stage 분기: Abhorrent(-8) / Disapproved(-3) / DontCare(+2) / Respected(+5) / Sublime(+8).
//        규율 미적용 시 DontCare(stage 2) 기본.
// 주의 : Ideology DLC 전용. MayRequire로 ThoughtDef 로딩 자체가 DLC 의존.

using RimWorld;
using ShapeshifterFramework.Utilities;
using Verse;

namespace ShapeshifterFramework.Ideology
{
    /// <summary>성스러운 동물 폼 변신 시 규율 단계별 기분 상황 감정 워커.</summary>
    public class ThoughtWorker_SacredAnimalForm : ThoughtWorker
    {
        /// <summary>변신 폼의 linkedSacredAnimalDef가 숭배 동물과 일치하면 규율 단계에 따른 stage 반환.</summary>
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (!ModsConfig.IdeologyActive) return ThoughtState.Inactive;
            if (p == null || p.Ideo == null) return ThoughtState.Inactive;

            if (!SacredAnimalFormUtility.IsSacredAnimalForm(p))
                return ThoughtState.Inactive;

            return ThoughtState.ActiveAtStage(SacredAnimalFormUtility.GetPreceptStage(p));
        }
    }
}
