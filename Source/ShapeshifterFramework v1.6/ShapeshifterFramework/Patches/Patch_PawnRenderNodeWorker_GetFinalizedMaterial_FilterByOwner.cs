// ShapeshifterFramework | Patches | Patch_PawnRenderNodeWorker_GetFinalizedMaterial_FilterByOwner.cs
// 목적 : 폼의 시각적 필터 규칙(renderShow/Hide)에 따라 옷, 유전자, 헤디프의 렌더링 텍스처(Material)를 강제로 투명하게(null) 만듦.
// 용도 : 노드의 소유자(Owner)가 누군지 리플렉션 캐시를 이용해 안전하게 추적한 뒤, ShapeshiftVisualFilter의 차단 로직에 걸리면 결과값(__result)을 null로 반환해 렌더링을 완전히 스킵시킴.

using HarmonyLib;
using RimWorld;
using ShapeshifterFramework.Utilities;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch(typeof(PawnRenderNodeWorker), "GetFinalizedMaterial")]
    public static class Patch_PawnRenderNodeWorker_GetFinalizedMaterial_FilterByOwner
    {
        // Gene 타입 캐시
        static readonly Type T_Gene = AccessTools.TypeByName("RimWorld.Gene");

        // Gene exclusionTags 수집용 재사용 셋 — 렌더 핫패스 GC 할당 방지 및 O(1) 중복 검사
        static readonly HashSet<string> _tmpTagSet = new HashSet<string>(StringComparer.Ordinal);
        static readonly List<string> _tmpTags = new List<string>(8);

        #region 노드 Owner 캐시 — 노드별 소유자(Gene/Apparel/Hediff)는 런타임에 변하지 않으므로 최초 1회만 탐색

        /// <summary>노드별 소유자 캐시 엔트리.</summary>
        private sealed class NodeOwnerEntry
        {
            /// <summary>0=미탐색, 1=Gene, 2=Apparel, 3=Hediff, 4=없음</summary>
            public byte ownerKind;
            public object owner;
        }

        // ConditionalWeakTable: 노드가 GC되면 엔트리 자동 제거
        private static readonly ConditionalWeakTable<PawnRenderNode, NodeOwnerEntry> _nodeOwnerCache
            = new ConditionalWeakTable<PawnRenderNode, NodeOwnerEntry>();

        /// <summary>노드의 소유자를 캐시에서 조회하거나, 최초 1회 탐색 후 캐시.</summary>
        private static NodeOwnerEntry GetOrResolveOwner(PawnRenderNode node, PawnRenderNodeWorker worker)
        {
            if (_nodeOwnerCache.TryGetValue(node, out var entry))
                return entry;

            entry = new NodeOwnerEntry();

            // owner 탐색
            object owner = ShapeshiftReflectionCache.TryGetOwnerFromNode(node);

            // Gene 판별
            bool isGene = owner != null && T_Gene != null && T_Gene.IsInstanceOfType(owner);
            if (isGene)
            {
                entry.ownerKind = 1;
                entry.owner = owner;
                _nodeOwnerCache.Add(node, entry);
                return entry;
            }

            if (!isGene)
            {
                var gene = ShapeshiftReflectionCache.TryScanFieldsForType<Gene>(node, worker);
                if (gene != null)
                {
                    entry.ownerKind = 1;
                    entry.owner = gene;
                    _nodeOwnerCache.Add(node, entry);
                    return entry;
                }
            }

            // Apparel 판별
            Apparel apparel = owner as Apparel;
            if (apparel == null)
                apparel = ShapeshiftReflectionCache.TryScanFieldsForType<Apparel>(node, worker);
            if (apparel != null)
            {
                entry.ownerKind = 2;
                entry.owner = apparel;
                _nodeOwnerCache.Add(node, entry);
                return entry;
            }

            // Hediff 판별
            Hediff hediff = owner as Hediff;
            if (hediff == null)
                hediff = ShapeshiftReflectionCache.TryScanFieldsForType<Hediff>(node, worker);
            if (hediff != null)
            {
                entry.ownerKind = 3;
                entry.owner = hediff;
                _nodeOwnerCache.Add(node, entry);
                return entry;
            }

            // 소유자 없음
            entry.ownerKind = 4;
            entry.owner = null;
            _nodeOwnerCache.Add(node, entry);
            return entry;
        }

        #endregion

        [HarmonyPostfix, HarmonyPriority(Priority.Last)]
        static void Postfix(PawnRenderNodeWorker __instance, PawnRenderNode node, PawnDrawParms parms, ref Material __result)
        {
            if (__result == null) return;
            Pawn pawn = parms.pawn; if (pawn == null) return;

            // 비변신 폰은 즉시 스킵 — 렌더 핫패스에서 불필요한 리플렉션/할당 방지
            if (!ShapeshiftRegistry.IsActive(pawn)) return;

            // 캐시에서 노드 소유자 조회 (최초 1회만 리플렉션 탐색)
            var cached = GetOrResolveOwner(node, __instance);

            switch (cached.ownerKind)
            {
                case 1: // 유전자
                {
                    var gene = (Gene)cached.owner;

                    // exclusionTags 수집 (재사용 셋+리스트, O(1) 중복 검사)
                    _tmpTagSet.Clear();
                    _tmpTags.Clear();

                    object geneDef = ShapeshiftReflectionCache.GetInstanceProperty<object>(gene, "def");
                    var tagsA = ShapeshiftReflectionCache.TryGetExclusionTags(geneDef);
                    if (tagsA != null)
                        for (int i = 0; i < tagsA.Count; i++)
                            if (_tmpTagSet.Add(tagsA[i])) _tmpTags.Add(tagsA[i]);

                    object props = ShapeshiftReflectionCache.TryGetPropsFromNode(node);
                    var tagsB = ShapeshiftReflectionCache.TryGetExclusionTags(props);
                    if (tagsB != null)
                        for (int i = 0; i < tagsB.Count; i++)
                            if (_tmpTagSet.Add(tagsB[i])) _tmpTags.Add(tagsB[i]);

                    if (ShapeshiftVisualFilter.ShouldHideGeneByDefOrTags(pawn, gene, _tmpTags.Count > 0 ? (IList<string>)_tmpTags : null))
                        __result = null;
                    return;
                }

                case 2: // 의류
                {
                    if (ShapeshiftVisualFilter.ShouldHideApparelGraphic(pawn, (Apparel)cached.owner))
                        __result = null;
                    return;
                }

                case 3: // 헤디프
                {
                    if (ShapeshiftVisualFilter.ShouldHideHediffGraphic(pawn, (Hediff)cached.owner))
                        __result = null;
                    return;
                }

                // case 4: 소유자 없음 — 아무 작업 없이 리턴
            }
        }
    }
}
