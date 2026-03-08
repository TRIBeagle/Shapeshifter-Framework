// ShapeshifterFramework | Utilities | ShapeshiftTargetUtility.cs
// 목적 : 특정 타겟 폰에게 폼 변신을 시도할 때 호출되는 안전한 프런트 API.
// 용도 : 대상의 상태(사망 여부, Comp 유무, 조건 충족 여부)를 검증하고 성공 확률(successChance)을 굴려 변신을 확정하며, 실패 시 알맞은 인게임 메시지(토스트)를 출력함.

using RimWorld;
using Verse;

namespace ShapeshifterFramework.Utilities
{
    public static class ShapeshiftTargetUtility
    {
        /// <summary>대상 Pawn에 폼 적용 시도. 성공 시 true.</summary>
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
                Messages.Message("SSF_Message_CannotTransform".Translate(form.LabelCap), MessageTypeDefOf.RejectInput, false);
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

            // 이미 변신 중이면 폼 전환 (prevOverride로 현재 폼 defName 전달)
            string prev = (comp.isTransformed && comp.currentForm != null) ? comp.currentForm.defName : null;
            comp.ApplyForm(form, prev); // 지속시간/부가효과는 FormDef에 따름
            return true;
        }
    }
}
