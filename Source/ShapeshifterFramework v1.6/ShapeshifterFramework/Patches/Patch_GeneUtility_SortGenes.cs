// Patch_GeneUtility_SortGenes.cs
// 목적: 정렬 전 유전자 리스트 정리(null/def없음/카테고리없음 제거)로 NRE 예방.
// 용도: Prefix에서 리스트를 in-place 정리하여 타 모드와 호환성 개선.
// 주의: 원본 리스트 수정(RemoveAll).

using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch(typeof(GeneUtility), nameof(GeneUtility.SortGenes))]
    public static class Patch_GeneUtility_SortGenes
    {
        static void Prefix(List<Gene> genes)
        {
            if (genes == null) return;
            // null / def == null / displayCategory == null 제거
            genes.RemoveAll(g => g == null || g.def == null || g.def.displayCategory == null);
        }
    }
}
