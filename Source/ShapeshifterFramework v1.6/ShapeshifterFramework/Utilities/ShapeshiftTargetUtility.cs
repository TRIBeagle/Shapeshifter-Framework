using RimWorld;
using Verse;

namespace ShapeshifterFramework.Utilities
{
    public static class ShapeshiftTargetUtility
    {
        /// <summary>
        /// 대상 Pawn에 폼 적용을 시도한다. (성공확률/폼 필터/토스트 처리 포함)
        /// 성공 시 true, 실패(저항/조건불충족/컴프없음/폼누락) 시 false.
        /// </summary>
        public static bool TryShiftPawn(Pawn target, string formDefName, float successChance = 1f)
        {
            if (target == null || target.Dead) return false;

            if (!ShapeshiftUtility.TryGetComp(target, out var comp))
            {
                Messages.Message("[SSF] Target has no shapeshift comp.", MessageTypeDefOf.RejectInput, false);
                return false;
            }

            var form = ShapeshiftUtility.GetDefSafe<ShapeshiftFormDef>(formDefName);
            if (form == null)
            {
                Messages.Message($"[SSF] Missing ShapeshiftFormDef: {formDefName}", MessageTypeDefOf.RejectInput, false);
                return false;
            }

            if (!comp.CanTransform(target, form))
            {
                Messages.Message("Shapeshift_CannotTransform".Translate(form.LabelCap), MessageTypeDefOf.RejectInput, false);
                return false;
            }

            // 확률(0~1)
            var p = successChance;
            if (p < 0f) p = 0f; else if (p > 1f) p = 1f;
            if (p < 1f && Rand.Value > p)
            {
                Messages.Message("SSF_ShiftTarget_Resisted".Translate(target.LabelShortCap), MessageTypeDefOf.RejectInput, false);
                return false;
            }

            comp.ApplyForm(form); // 지속시간/부가효과는 FormDef에 따름
            return true;
        }
    }
}
