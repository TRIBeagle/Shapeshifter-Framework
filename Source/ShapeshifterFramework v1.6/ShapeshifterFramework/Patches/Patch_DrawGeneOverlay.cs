// Patch_DrawGeneOverlay.cs
// 목적: 유전자 UI에 폼으로 억제되는 유전자 표시(딤/슬래시/툴팁).
// 용도: GeneUIUtility.DrawGene 후처리로 억제 상태 시각 피드백 추가.
// 주의: Priority.Last로 다른 모드 오버레이 위에 덮어씀. 판정 로직은 전담 유틸로 위임.

using HarmonyLib;
using RimWorld;
using ShapeshifterFramework.Comps;
using ShapeshifterFramework.Utilities;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch(typeof(GeneUIUtility))]
    [HarmonyPriority(Priority.Last)] // 마지막에 오버레이
    public static class Patch_DrawGeneOverlay
    {
        // Rect/Gene 파라미터 인덱스 캐시 (TargetMethod에서 탐색 후 저장)
        private static int _rectArgIndex = -1;
        private static int _geneArgIndex = -1;

        // 대상: DrawGene(Rect, Gene, ...) 오버로드 동적 탐색
        static MethodBase TargetMethod()
        {
            MethodBase target = null;
            var methods = AccessTools.GetDeclaredMethods(typeof(GeneUIUtility));

            for (int i = 0; i < methods.Count; i++)
            {
                var m = methods[i];
                if (m.Name != "DrawGene") continue;

                var ps = m.GetParameters();
                bool hasRect = false;
                bool hasGene = false;

                for (int j = 0; j < ps.Length; j++)
                {
                    if (ps[j].ParameterType == typeof(Rect)) hasRect = true;
                    if (ps[j].ParameterType == typeof(Gene)) hasGene = true;
                }

                if (hasRect && hasGene)
                {
                    target = m;
                    break;
                }
            }

            if (target == null)
            {
                throw new MissingMethodException("[SSF] GeneUIUtility.DrawGene not found.");
            }

            // Rect/Gene 파라미터 인덱스 미리 탐색해 캐시
            var parameters = target.GetParameters();
            for (int i = 0; i < parameters.Length; i++)
            {
                if (_rectArgIndex < 0 && parameters[i].ParameterType == typeof(Rect)) _rectArgIndex = i;
                if (_geneArgIndex < 0 && parameters[i].ParameterType == typeof(Gene)) _geneArgIndex = i;
            }
            return target;
        }

        // 후처리: 억제 대상일 때 디밍 + 레드 슬래시 + 얇은 외곽선 + 툴팁
        static void Postfix(Rect geneRect, Gene gene)
        {
            if (gene == null) return;
            var pawn = gene.pawn;
            if (pawn == null) return;

            if (!ShapeshiftVisualFilter.ShouldHideGeneForUI(pawn, gene)) return;

            // ── 디밍(아주 옅음)
            var prevCol = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.28f);
            Widgets.DrawTextureFitted(geneRect, BaseContent.BlackTex, 1f);

            // ── 얇은 외곽선(매우 옅은 그레이)
            GUI.color = new Color(1f, 1f, 1f, 0.12f);
            Widgets.DrawBox(geneRect, 1);

            // ── 툴팁
            var comp = pawn.TryGetComp<CompShapeshifter>();
            string formLabel = (comp != null && comp.isTransformed && comp.currentForm != null && !string.IsNullOrEmpty(comp.currentForm.label))
                ? comp.currentForm.label
                : "Shapeshift".Translate().ToString();
            TooltipHandler.TipRegion(geneRect, "ShapeshiftGeneAppearanceHidden".Translate(formLabel));

            GUI.color = prevCol;
        }
    }
}