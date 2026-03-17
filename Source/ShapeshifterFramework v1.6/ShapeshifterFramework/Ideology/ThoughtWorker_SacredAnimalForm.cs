// ShapeshifterFramework | Ideology | ThoughtWorker_SacredAnimalForm.cs
// 목적 : 성스러운 동물 폼 감정 — 변신 폼이 이데올로기의 숭배 동물과 일치하면 기분 +5.
// 용도 : 규율과 독립적으로 동작. formDef.linkedSacredAnimalDef가 Ideo.VeneratedAnimals에 포함되면 활성.
// 주의 : Ideology DLC 전용. MayRequire로 ThoughtDef 로딩 자체가 DLC 의존.

using RimWorld;
using ShapeshifterFramework.Utilities;
using Verse;

namespace ShapeshifterFramework.Ideology
{
    /// <summary>성스러운 동물 폼 변신 시 기분 보너스 상황 감정 워커.</summary>
    public class ThoughtWorker_SacredAnimalForm : ThoughtWorker
    {
        /// <summary>변신 폼의 linkedSacredAnimalDef가 이데올로기 숭배 동물 목록에 있으면 활성.</summary>
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (!ModsConfig.IdeologyActive) return ThoughtState.Inactive;
            if (p.Ideo == null) return ThoughtState.Inactive;

            if (!ShapeshiftRegistry.TryGet(p, out var comp, out var form))
                return ThoughtState.Inactive;

            if (form.linkedSacredAnimalDef == null) return ThoughtState.Inactive;

            var venerated = p.Ideo.VeneratedAnimals;
            if (venerated == null) return ThoughtState.Inactive;

            for (int i = 0; i < venerated.Count; i++)
            {
                if (venerated[i] == form.linkedSacredAnimalDef)
                    return ThoughtState.ActiveAtStage(0);
            }

            return ThoughtState.Inactive;
        }
    }
}
