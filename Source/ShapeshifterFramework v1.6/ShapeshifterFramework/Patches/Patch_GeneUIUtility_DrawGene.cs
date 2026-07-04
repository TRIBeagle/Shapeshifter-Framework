// ShapeshifterFramework | Patches | Patch_GeneUIUtility_DrawGene.cs
// 목적 : 인게임 유전자(Gene) UI 탭에서, 현재 변신 폼에 의해 외형 렌더링이 강제로 억제된(숨겨진) 유전자를 시각적으로 구별.
// 용도 : DrawGene 메서드에 Postfix로 개입하여 억제된 유전자 아이콘 위에 반투명한 검은색 디밍 박스와 얇은 외곽선을 덧그리고 마우스 오버 툴팁을 추가함.

using HarmonyLib;
using RimWorld;
using ShapeshifterFramework.Utilities;
using System;
using System.Reflection;
using UnityEngine;
using Verse;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch(typeof(GeneUIUtility))]
    [HarmonyPriority(Priority.Last)] // 마지막에 오버레이
    internal static class Patch_GeneUIUtility_DrawGene
    {
        private static MethodBase _target;

        /// <summary>대상 미발견 시 패치 스킵 — TargetMethod에서 throw하면 PatchAll 전체가 중단되므로 Prepare 가드 사용.</summary>
        static bool Prepare()
        {
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
                    _target = m;
                    break;
                }
            }

            if (_target == null)
                Log.Warning("[SSF] GeneUIUtility.DrawGene not found - patch skipped (RimWorld version change?)");
            return _target != null;
        }

        // 대상: DrawGene(Rect, Gene, ...) 오버로드 (Prepare에서 탐색)
        [HarmonyTargetMethod]
        static MethodBase TargetMethod() => _target;

        // 억제 대상: 디밍 + 외곽선 + 툴팁
        static void Postfix(Rect geneRect, Gene gene)
        {
            if (gene == null) return;
            var pawn = gene.pawn;
            if (pawn == null) return;

            if (!ShapeshiftVisualFilter.ShouldHideGeneForUI(pawn, gene)) return;

            // 디밍
            var prevCol = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.28f);
            Widgets.DrawTextureFitted(geneRect, BaseContent.BlackTex, 1f);

            // 외곽선
            GUI.color = new Color(1f, 1f, 1f, 0.12f);
            Widgets.DrawBox(geneRect, 1);

            // 툴팁
            string formLabel;
            if (ShapeshiftRegistry.TryGet(pawn, out var comp, out var form) && !string.IsNullOrEmpty(form.label))
                formLabel = form.label;
            else
                formLabel = "SSF_Fallback_Transform".Translate().ToString();
            TooltipHandler.TipRegion(geneRect, "SSF_Inspect_GeneHidden".Translate(formLabel));

            GUI.color = prevCol;
        }
    }
}