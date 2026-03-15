// ShapeshifterFramework | Utilities | ShapeshiftEligibility.cs
// 목적 : 기본적인 변신 가능 여부 판정 (같은 폼 재변신 방지, 사망/쓰러짐 체크).
// 용도 : 종족/뮤턴트/제노타입/아이템 등 세부 조건은 AbilityDef의 CompAbilityEffect_ShiftTarget 및 소스 기반 부여(유전자/헤디프/아이템)로 이전됨.

using System.Collections.Generic;
using Verse;

namespace ShapeshifterFramework.Utilities
{
    internal static class ShapeshiftEligibility
    {
        /// <summary>폼의 applicableRaces에 대상 종족이 포함되는지 판정. null/빈 목록이면 제한 없음.</summary>
        public static bool IsRaceAllowed(Pawn pawn, ShapeshiftFormDef form)
        {
            if (pawn == null || form == null) return false;
            if (form.applicableRaces == null || form.applicableRaces.Count == 0) return true;
            return form.applicableRaces.Contains(pawn.def);
        }

        /// <summary>기본 변신 가능 여부 판정. 종족 필터, 같은 폼 재변신 방지, 사망 체크.</summary>
        public static bool CanTransformBasic(Pawn pawn, ShapeshiftFormDef form, string currentFormDefName)
        {
            if (pawn == null || form == null) return false;
            if (pawn.Dead) return false;

            // 종족 필터
            if (!IsRaceAllowed(pawn, form)) return false;

            // 같은 폼 재변신 방지
            if (currentFormDefName != null && string.Equals(currentFormDefName, form.defName, System.StringComparison.Ordinal))
                return false;

            return true;
        }
    }
}
