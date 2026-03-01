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
        [HarmonyTargetMethod]
        static MethodBase TargetMethod()
        {
            var method = AccessTools.GetDeclaredMethods(typeof(GeneUIUtility)).FirstOrDefault(m =>
            {
                if (m.Name != "DrawGene") return false;
                var ps = m.GetParameters();
                return ps.Any(p => p.ParameterType == typeof(Rect))
                    && ps.Any(p => p.ParameterType == typeof(Gene));
            }) ?? throw new MissingMethodException("[SSF] GeneUIUtility.DrawGene not found.");

            // Rect/Gene 파라미터 인덱스 미리 탐색해 캐시
            var parameters = method.GetParameters();
            for (int i = 0; i < parameters.Length; i++)
            {
                if (_rectArgIndex < 0 && parameters[i].ParameterType == typeof(Rect)) _rectArgIndex = i;
                if (_geneArgIndex < 0 && parameters[i].ParameterType == typeof(Gene)) _geneArgIndex = i;
            }
            return method;
        }

        // 후처리: 억제 대상일 때 디밍 + 레드 슬래시 + 얇은 외곽선 + 툴팁
        static void Postfix(object[] __args)
        {
            if (__args == null) return;
            if (_rectArgIndex < 0 || _geneArgIndex < 0) return;
            if (_rectArgIndex >= __args.Length || _geneArgIndex >= __args.Length) return;

            var rect = __args[_rectArgIndex] is Rect r ? r : default(Rect);
            var gene = __args[_geneArgIndex] as Gene;
            if (gene == null) return;
            var pawn = gene.pawn;
            if (pawn == null) return;

            if (!ShapeshiftVisualFilter.ShouldHideGeneForUI(pawn, gene)) return;

            // ── 디밍(아주 옅음)
            var prevCol = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.28f);
            Widgets.DrawTextureFitted(rect, BaseContent.BlackTex, 1f);

            // ── 얇은 외곽선(매우 옅은 그레이)
            GUI.color = new Color(1f, 1f, 1f, 0.12f);
            Widgets.DrawBox(rect, 1);

            // ── 툴팁
            var comp = pawn.TryGetComp<CompShapeshifter>();
            string formLabel = (comp != null && comp.isTransformed && comp.currentForm != null && !string.IsNullOrEmpty(comp.currentForm.label))
                ? comp.currentForm.label
                : "Shapeshift".Translate().ToString();
            TooltipHandler.TipRegion(rect, "ShapeshiftGeneAppearanceHidden".Translate(formLabel));

            GUI.color = prevCol;
        }
    }
}