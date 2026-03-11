// ShapeshifterFramework | Utilities | ShapeshiftEligibility.cs
// 목적 : 기본적인 변신 가능 여부 판정 (같은 폼 재변신 방지, 사망/쓰러짐 체크).
// 용도 : 종족/뮤턴트/제노타입/아이템 등 세부 조건은 AbilityDef의 CompAbilityEffect_ShiftTarget 및 소스 기반 부여(유전자/헤디프/아이템)로 이전됨.

using Verse;

namespace ShapeshifterFramework.Utilities
{
    internal static class ShapeshiftEligibility
    {
        /// <summary>기본 변신 가능 여부 판정. 같은 폼 재변신 방지, 사망/쓰러짐 체크.</summary>
        public static bool CanTransformBasic(Pawn pawn, ShapeshiftFormDef form, string currentFormDefName)
        {
            if (pawn == null || form == null) return false;
            if (pawn.Dead) return false;

            // 같은 폼 재변신 방지
            if (currentFormDefName != null && string.Equals(currentFormDefName, form.defName, System.StringComparison.Ordinal))
                return false;

            return true;
        }
    }
}
