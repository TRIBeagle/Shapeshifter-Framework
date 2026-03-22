// ShapeshifterFramework | Ideology | ThoughtWorker_SacredAnimalForm.cs
// 목적 : 성스러운 동물 폼 감정 — 변신 폼이 이데올로기의 숭배 동물과 일치하면 규율 단계별 기분 부여.
// 용도 : formDef.linkedSacredAnimalDef가 Ideo.VeneratedAnimals에 포함되면 활성.
//        변신 규율(SSF_Shapeshifting) 단계에 따라 stage 분기: Abhorrent(-8) / Disapproved(-3) / DontCare(+2) / Respected(+5) / Sublime(+8).
//        규율 미적용 시 DontCare(stage 2) 기본.
// 주의 : Ideology DLC 전용. MayRequire로 ThoughtDef 로딩 자체가 DLC 의존.

using System.Collections.Generic;
using RimWorld;
using ShapeshifterFramework.Utilities;
using Verse;

namespace ShapeshifterFramework.Ideology
{
    /// <summary>성스러운 동물 폼 변신 시 규율 단계별 기분 상황 감정 워커.</summary>
    public class ThoughtWorker_SacredAnimalForm : ThoughtWorker
    {
        // 규율 defName → stage index 매핑
        private static readonly Dictionary<string, int> preceptStageMap = new Dictionary<string, int>
        {
            { "SSF_Shapeshifting_Abhorrent",   0 },
            { "SSF_Shapeshifting_Disapproved", 1 },
            { "SSF_Shapeshifting_DontCare",    2 },
            { "SSF_Shapeshifting_Respected",   3 },
            { "SSF_Shapeshifting_Sublime",     4 },
        };

        /// <summary>변신 폼의 linkedSacredAnimalDef가 숭배 동물과 일치하면 규율 단계에 따른 stage 반환.</summary>
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (!ModsConfig.IdeologyActive) return ThoughtState.Inactive;
            if (p.Ideo == null) return ThoughtState.Inactive;

            if (!ShapeshiftRegistry.TryGet(p, out var comp, out var form))
                return ThoughtState.Inactive;

            if (form.linkedSacredAnimalDef == null) return ThoughtState.Inactive;

            var venerated = p.Ideo.VeneratedAnimals;
            if (venerated == null) return ThoughtState.Inactive;

            bool isSacred = false;
            for (int i = 0; i < venerated.Count; i++)
            {
                if (venerated[i] == form.linkedSacredAnimalDef)
                {
                    isSacred = true;
                    break;
                }
            }

            if (!isSacred) return ThoughtState.Inactive;

            // 규율 단계 확인
            int stage = 2; // 기본: DontCare (규율 미적용 시)
            var precepts = p.Ideo.PreceptsListForReading;
            for (int i = 0; i < precepts.Count; i++)
            {
                if (preceptStageMap.TryGetValue(precepts[i].def.defName, out int mapped))
                {
                    stage = mapped;
                    break;
                }
            }

            return ThoughtState.ActiveAtStage(stage);
        }
    }
}
