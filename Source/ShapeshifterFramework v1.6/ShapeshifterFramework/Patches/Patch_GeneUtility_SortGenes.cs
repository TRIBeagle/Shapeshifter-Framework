// ShapeshifterFramework | Patches | Patch_GeneUtility_SortGenes.cs
// 목적 : 다른 모드의 오류나 세이브 손상으로 인해 유전자 데이터가 파괴되었을 때, UI를 정렬하다가 발생하는 치명적 에러(NullReferenceException)를 방지.
// 용도 : SortGenes가 실행되기 직전(Prefix), 리스트 내부의 null 값이나 def, 카테고리가 없는 유령(Ghost) 유전자들을 강제로 삭제(RemoveAll)하여 바닐라 정렬 로직을 보호함.

using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch(typeof(GeneUtility), nameof(GeneUtility.SortGenes))]
    internal static class Patch_GeneUtility_SortGenes
    {
        static void Prefix(List<Gene> genes)
        {
            if (genes == null) return;
            // null / def == null / displayCategory == null 제거
            genes.RemoveAll(g => g == null || g.def == null || g.def.displayCategory == null);
        }
    }
}
