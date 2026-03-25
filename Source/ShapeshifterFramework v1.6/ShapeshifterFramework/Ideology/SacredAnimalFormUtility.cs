// ShapeshifterFramework | Ideology | SacredAnimalFormUtility.cs
// 목적 : 성스러운 동물 폼 판정 및 규율 단계 조회 공용 유틸리티.
// 용도 : ThoughtWorker_SacredAnimalForm, ThoughtWorker_SacredAnimalForm_Social에서 공유.

using System.Collections.Generic;
using RimWorld;
using ShapeshifterFramework.Utilities;
using Verse;

namespace ShapeshifterFramework.Ideology
{
    /// <summary>성스러운 동물 폼 관련 공용 유틸리티.</summary>
    public static class SacredAnimalFormUtility
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

        private const int DefaultStage = 2; // DontCare

        /// <summary>Pawn이 현재 성스러운 동물 폼인지 확인.</summary>
        public static bool IsSacredAnimalForm(Pawn p)
        {
            if (p.Ideo == null) return false;

            if (!ShapeshiftRegistry.TryGet(p, out var comp, out var form))
                return false;

            if (form.linkedSacredAnimalDef == null) return false;

            var venerated = p.Ideo.VeneratedAnimals;
            if (venerated == null) return false;

            for (int i = 0; i < venerated.Count; i++)
            {
                if (venerated[i] == form.linkedSacredAnimalDef)
                    return true;
            }

            return false;
        }

        /// <summary>Pawn의 이데올로기 규율 단계에 해당하는 stage index 반환. 규율 미적용 시 DontCare(2).</summary>
        public static int GetPreceptStage(Pawn p)
        {
            if (p.Ideo == null) return DefaultStage;

            var precepts = p.Ideo.PreceptsListForReading;
            for (int i = 0; i < precepts.Count; i++)
            {
                if (preceptStageMap.TryGetValue(precepts[i].def.defName, out int stage))
                    return stage;
            }

            return DefaultStage;
        }
    }
}
