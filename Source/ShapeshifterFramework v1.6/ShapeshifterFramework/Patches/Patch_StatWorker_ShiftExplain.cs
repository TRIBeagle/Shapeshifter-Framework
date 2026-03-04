// ShapeshifterFramework | Patches | Patch_StatWorker_ShiftExplain.cs
// 목적 : 인게임 정보 창(Info Card)에서 특정 스탯(Stat)의 세부 계산식 툴팁 하단에 변신 폼으로 인한 보정 수치를 표시.
// 용도 : GetExplanationUnfinalized에 Postfix로 개입하여, 폼 설정(statFactors, statOffsets)이 적용된 경우 번역된 요약 문자열을 StringBuilder로 깔끔하게 덧붙임.

using HarmonyLib;
using RimWorld;
using ShapeshifterFramework.Comps;
using System.Reflection;
using System.Text;
using Verse;

namespace ShapeshifterFramework.Patches
{
    // 대상: StatWorker.GetExplanationUnfinalized(StatRequest, ToStringNumberSense)
    [HarmonyPatch(typeof(StatWorker), nameof(StatWorker.GetExplanationUnfinalized),
        new[] { typeof(StatRequest), typeof(ToStringNumberSense) })]
    public static class Patch_StatWorker_ShiftExplain
    {
        // StatWorker의 private 필드 'stat' 접근(현재 계산 중인 StatDef)
        static readonly FieldInfo StatField = AccessTools.Field(typeof(StatWorker), "stat");

        static void Postfix(StatWorker __instance, StatRequest req, ToStringNumberSense numberSense, ref string __result)
        {
            // 1) Pawn 확보(Thing 또는 req.Pawn)
            Pawn pawn = req.HasThing ? req.Thing as Pawn : req.Pawn;
            if (pawn == null) return;

            // 2) 변신 폼 확인
            var comp = pawn.TryGetComp<CompShapeshifter>();
            var form = comp?.currentForm;
            if (comp == null || !comp.isTransformed || form == null) return;

            // 3) 현재 계산 중인 스탯
            var stat = (StatDef)StatField.GetValue(__instance);
            if (stat == null) return;

            // 4) 폼의 배수/가산 합산
            float factor = 1f, offset = 0f;

            if (form.statFactors != null)
                for (int i = 0; i < form.statFactors.Count; i++)
                {
                    var f = form.statFactors[i];
                    if (f?.stat == stat) factor *= f.value;
                }

            if (form.statOffsets != null)
                for (int i = 0; i < form.statOffsets.Count; i++)
                {
                    var o = form.statOffsets[i];
                    if (o?.stat == stat) offset += o.value;
                }

            // 변화 없으면 종료
            if (factor == 1f && offset == 0f) return;

            // 5) 설명 문자열 뒤에 추가(가산 → 배수 순)
            var sb = new StringBuilder(__result ?? string.Empty);
            string formLabel = form.label ?? "Shapeshift";

            // "ShapeshiftStatOffset": 예) "{0} 가산: +1.2 / -0.8"
            if (offset != 0f)
                sb.AppendLine("ShapeshiftStatOffset".Translate(formLabel, offset.ToString("+0.##;-0.##")));

            // "ShapeshiftStatFactor": 예) "{0} 배수: x{1}"
            if (factor != 1f)
                sb.AppendLine("ShapeshiftStatFactor".Translate(formLabel, factor.ToString("0.##")));

            __result = sb.ToString();
        }
    }
}