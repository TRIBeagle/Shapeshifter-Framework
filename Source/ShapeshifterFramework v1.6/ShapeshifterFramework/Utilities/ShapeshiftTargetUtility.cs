// ShapeshifterFramework | Utilities | ShapeshiftTargetUtility.cs
// 목적 : 특정 타겟 폰에게 폼 변신을 시도할 때 호출되는 안전한 프런트 API.
// 용도 : 대상의 상태(사망 여부, HediffComp_ShapeshiftCore 유무, 조건 충족 여부)를 검증하고 성공 확률(successChance)을 굴려 변신을 확정하며, 실패 시 알맞은 인게임 메시지(토스트)를 출력함.

using RimWorld;
using ShapeshifterFramework.Hediffs;
using Verse;

namespace ShapeshifterFramework.Utilities
{
    public static class ShapeshiftTargetUtility
    {
        /// <summary>대상 Pawn에 폼 적용 시도 (formDefName 문자열 기반, 후방 호환). 성공 시 true.</summary>
        public static bool TryShiftPawn(Pawn target, string formDefName, float successChance = 1f)
        {
            if (target == null || target.Dead) return false;

            // HediffComp_ShapeshiftCore 조회
            if (!ShapeshiftCoreUtility.TryGetCore(target, out var core))
            {
                Messages.Message("SSF_ShiftTarget_NoComp".Translate(target.LabelShortCap), MessageTypeDefOf.RejectInput, false);
                return false;
            }

            var form = string.IsNullOrEmpty(formDefName) ? null : DefDatabase<ShapeshiftFormDef>.GetNamedSilentFail(formDefName);
            if (form == null)
            {
                Messages.Message("SSF_ShiftTarget_MissingForm".Translate(formDefName), MessageTypeDefOf.RejectInput, false);
                return false;
            }

            if (!core.CanTransform(form))
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
            string prev = (core.isTransformed && core.currentForm != null) ? core.currentForm.defName : null;
            core.ApplyForm(form, prev); // 지속시간/부가효과는 FormDef에 따름
            return true;
        }

        /// <summary>대상 Pawn에 HediffDef 기반 변신 적용 시도. ShapeshiftCoreUtility.ApplyShift 위임. 성공 시 true.</summary>
        public static bool TryShiftPawn(Pawn target, HediffDef hediffDef, float successChance = 1f)
        {
            if (target == null || target.Dead || hediffDef == null) return false;
            return ShapeshiftCoreUtility.ApplyShift(target, hediffDef, successChance);
        }
    }
}
