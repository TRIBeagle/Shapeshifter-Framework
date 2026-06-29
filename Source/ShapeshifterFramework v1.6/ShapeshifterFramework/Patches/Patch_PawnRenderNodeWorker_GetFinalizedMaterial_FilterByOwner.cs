// ShapeshifterFramework | Patches | Patch_PawnRenderNodeWorker_GetFinalizedMaterial_FilterByOwner.cs
// 목적 : 폼의 시각적 필터 규칙(renderShow/Hide)에 따라 옷, 유전자, 헤디프의 렌더링 텍스처(Material)를 강제로 투명하게(null) 만듦.
// 용도 : 노드의 소유자(gene/apparel/hediff)를 PawnRenderNode의 public 필드에서 직접 읽은 뒤, ShapeshiftVisualFilter의 차단 로직에 걸리면 결과값(__result)을 null로 반환해 렌더링을 완전히 스킵시킴.

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
    internal static class Patch_PawnRenderNodeWorker_GetFinalizedMaterial_FilterByOwner
    {
        // Gene exclusionTags 수집용 재사용 셋 — 렌더 핫패스 GC 할당 방지 및 O(1) 중복 검사
        // [ThreadStatic]: 병렬 렌더 패스(ParallelPreRenderPawnAt)에서 스레드 간 경합 방지
        [ThreadStatic] static HashSet<string> _tmpTagSet;
        [ThreadStatic] static List<string> _tmpTags;

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
        private static NodeOwnerEntry GetOrResolveOwner(PawnRenderNode node)
        {
            if (_nodeOwnerCache.TryGetValue(node, out var entry))
                return entry;

            entry = new NodeOwnerEntry();

            // RimWorld 1.6: PawnRenderNode는 gene/apparel/hediff를 public 필드로 직접 노출
            if (node.gene != null)
            {
                entry.ownerKind = 1;
                entry.owner = node.gene;
            }
            else if (node.apparel != null)
            {
                entry.ownerKind = 2;
                entry.owner = node.apparel;
            }
            else if (node.hediff != null)
            {
                entry.ownerKind = 3;
                entry.owner = node.hediff;
            }
            else
            {
                // 소유자 없음
                entry.ownerKind = 4;
                entry.owner = null;
            }

            _nodeOwnerCache.Add(node, entry);
            return entry;
        }

        #endregion

        [HarmonyPostfix, HarmonyPriority(Priority.Last)]
        static void Postfix(PawnRenderNode node, PawnDrawParms parms, ref Material __result)
        {
            if (__result == null) return;
            Pawn pawn = parms.pawn; if (pawn == null) return;

            // 비변신 폰은 즉시 스킵 — 렌더 핫패스에서 불필요한 리플렉션/할당 방지
            if (!ShapeshiftRegistry.IsActive(pawn)) return;

            // 캐시에서 노드 소유자 조회 (최초 1회만 탐색)
            var cached = GetOrResolveOwner(node);

            switch (cached.ownerKind)
            {
                case 1: // 유전자
                {
                    var gene = (Gene)cached.owner;

                    // exclusionTags 수집 (재사용 셋+리스트, O(1) 중복 검사)
                    // [ThreadStatic] 필드는 비주 스레드에서 초기화자가 실행되지 않으므로 지연 초기화
                    if (_tmpTagSet == null) _tmpTagSet = new HashSet<string>(StringComparer.Ordinal);
                    if (_tmpTags == null) _tmpTags = new List<string>(8);
                    _tmpTagSet.Clear();
                    _tmpTags.Clear();

                    var tagsA = ShapeshiftReflectionCache.TryGetExclusionTags(gene.def);
                    if (tagsA != null)
                        for (int i = 0; i < tagsA.Count; i++)
                            if (_tmpTagSet.Add(tagsA[i])) _tmpTags.Add(tagsA[i]);

                    var tagsB = ShapeshiftReflectionCache.TryGetExclusionTags(node.Props);
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
