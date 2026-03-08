// ShapeshifterFramework | Patches | Patch_PawnRenderNodeWorker_GetFinalizedMaterial_FilterByOwner.cs
// 목적 : 폼의 시각적 필터 규칙(renderShow/Hide)에 따라 옷, 유전자, 헤디프의 렌더링 텍스처(Material)를 강제로 투명하게(null) 만듦.
// 용도 : 노드의 소유자(Owner)가 누군지 리플렉션 캐시를 이용해 안전하게 추적한 뒤, ShapeshiftVisualFilter의 차단 로직에 걸리면 결과값(__result)을 null로 반환해 렌더링을 완전히 스킵시킴.

using HarmonyLib;
using RimWorld;
using ShapeshifterFramework.Utilities;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch(typeof(PawnRenderNodeWorker), "GetFinalizedMaterial")]
    public static class Patch_PawnRenderNodeWorker_GetFinalizedMaterial_FilterByOwner
    {
        // Gene 타입 캐시
        static readonly Type T_Gene = AccessTools.TypeByName("RimWorld.Gene");

        [HarmonyPostfix, HarmonyPriority(Priority.Last)]
        static void Postfix(PawnRenderNodeWorker __instance, PawnRenderNode node, PawnDrawParms parms, ref Material __result)
        {
            if (__result == null) return;
            Pawn pawn = parms.pawn; if (pawn == null) return;

            // owner 탐색
            object owner = ShapeshiftReflectionCache.TryGetOwnerFromNode(node);

            // Gene 소유 노드
            bool isGeneOwner = owner != null && T_Gene != null && T_Gene.IsInstanceOfType(owner);
            Gene gene = isGeneOwner ? (Gene)owner : null;

            if (!isGeneOwner)
            {
                // 필드에서 Gene 타입 탐색
                gene = ShapeshiftReflectionCache.TryScanFieldsForType<Gene>(node, __instance);
                isGeneOwner = (gene != null);
            }

            if (isGeneOwner)
            {
                // exclusionTags 수집
                List<string> tags = null;

                object geneDef = ShapeshiftReflectionCache.GetInstanceProperty<object>(gene, "def");
                var tagsA = ShapeshiftReflectionCache.TryGetExclusionTags(geneDef);
                if (tagsA != null && tagsA.Count > 0) tags = new List<string>(tagsA);

                object props = ShapeshiftReflectionCache.TryGetPropsFromNode(node);
                var tagsB = ShapeshiftReflectionCache.TryGetExclusionTags(props);
                if (tagsB != null && tagsB.Count > 0)
                {
                    if (tags == null) tags = new List<string>(tagsB);
                    else
                    {
                        for (int i = 0; i < tagsB.Count; i++)
                            if (!tags.Contains(tagsB[i])) tags.Add(tagsB[i]);
                    }
                }

                if (ShapeshiftVisualFilter.ShouldHideGeneByDefOrTags(pawn, gene, tags))
                    __result = null;
                return;
            }

            // Apparel 소유 노드
            Apparel apparel = owner as Apparel;
            if (apparel == null)
            {
                // 필드에서 Apparel 탐색
                apparel = ShapeshiftReflectionCache.TryScanFieldsForType<Apparel>(node, __instance);
            }

            if (apparel != null)
            {
                if (ShapeshiftVisualFilter.ShouldHideApparelGraphic(pawn, apparel))
                    __result = null;
                return;
            }

            // Hediff 소유 노드
            Hediff hediff = owner as Hediff;
            if (hediff == null)
            {
                hediff = ShapeshiftReflectionCache.TryScanFieldsForType<Hediff>(node, __instance);
            }

            if (hediff != null)
            {
                if (ShapeshiftVisualFilter.ShouldHideHediffGraphic(pawn, hediff))
                    __result = null;
                return;
            }
        }
    }
}
