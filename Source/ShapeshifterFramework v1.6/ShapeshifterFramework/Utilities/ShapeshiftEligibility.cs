// ShapeshifterFramework | Utilities | ShapeshiftEligibility.cs
// 목적 : 기본적인 변신 가능 여부 판정 (종족/뮤턴트 필터, 같은 폼 재변신 방지, 사망 체크).
// 용도 : 모든 변신 경로(어빌리티/약물/스크롤/투사체)에서 공통으로 사용하는 FormDef 수준 필터.
//        어빌리티 시전자 조건(Comp.allowedRaces/allowedMutants)은 CompAbilityEffect_Shapeshift에서 별도 처리.

using RimWorld;
using System.Collections.Generic;
using Verse;

namespace ShapeshifterFramework.Utilities
{
    internal static class ShapeshiftEligibility
    {
        /// <summary>폼의 formAllowedRaces/formDisallowedRaces에 대상 종족이 부합하는지 판정. 둘 다 null/빈 목록이면 제한 없음.</summary>
        public static bool IsRaceAllowed(Pawn pawn, ShapeshiftFormDef form)
        {
            if (pawn == null || form == null) return false;

            // disallow 우선: 명시적 차단 목록에 있으면 즉시 거부
            if (form.formDisallowedRaces != null && form.formDisallowedRaces.Count > 0
                && form.formDisallowedRaces.Contains(pawn.def))
                return false;

            // allow: 목록이 있으면 포함되어야 통과
            if (form.formAllowedRaces != null && form.formAllowedRaces.Count > 0)
                return form.formAllowedRaces.Contains(pawn.def);

            return true;
        }

        // 뮤턴트 수집용 재사용 리스트 — GC 할당 방지
        private static readonly List<MutantDef> _tmpMutants = new List<MutantDef>(4);

        /// <summary>폼의 formAllowedMutants/formDisallowedMutants에 대상 뮤턴트가 부합하는지 판정. 둘 다 null/빈 목록이면 제한 없음.</summary>
        public static bool IsMutantAllowed(Pawn pawn, ShapeshiftFormDef form)
        {
            if (pawn == null || form == null) return false;

            var allow = form.formAllowedMutants;
            var disallow = form.formDisallowedMutants;

            bool hasAllow = allow != null && allow.Count > 0;
            bool hasDisallow = disallow != null && disallow.Count > 0;
            if (!hasAllow && !hasDisallow) return true;

            // 대상 폰의 뮤턴트 수집
            var hediffs = pawn.health?.hediffSet?.hediffs;
            if (hediffs == null)
                return !hasAllow; // allow 목록이 있는데 hediff 없으면 불통과

            _tmpMutants.Clear();
            var allMutants = DefDatabase<MutantDef>.AllDefsListForReading;
            for (int i = 0; i < hediffs.Count; i++)
            {
                var hDef = hediffs[i]?.def;
                if (hDef == null) continue;
                for (int j = 0; j < allMutants.Count; j++)
                {
                    if (allMutants[j]?.hediff == hDef)
                    {
                        _tmpMutants.Add(allMutants[j]);
                        break;
                    }
                }
            }

            // allow: 폰 뮤턴트 중 하나라도 allow 목록에 있어야 통과
            if (hasAllow)
            {
                bool found = false;
                for (int i = 0; i < _tmpMutants.Count && !found; i++)
                    for (int j = 0; j < allow.Count && !found; j++)
                        if (_tmpMutants[i] == allow[j]) found = true;
                if (!found) return false;
            }

            // disallow: 폰 뮤턴트 중 하나라도 disallow 목록에 있으면 차단
            if (hasDisallow)
            {
                for (int i = 0; i < _tmpMutants.Count; i++)
                    for (int j = 0; j < disallow.Count; j++)
                        if (_tmpMutants[i] == disallow[j]) return false;
            }

            return true;
        }

        /// <summary>기본 변신 가능 여부 판정. 종족/뮤턴트 필터, 같은 폼 재변신 방지, 사망 체크.</summary>
        public static bool CanTransformBasic(Pawn pawn, ShapeshiftFormDef form, string currentFormDefName)
        {
            if (pawn == null || form == null) return false;
            if (pawn.Dead) return false;

            // 종족 필터
            if (!IsRaceAllowed(pawn, form)) return false;

            // 뮤턴트 필터
            if (!IsMutantAllowed(pawn, form)) return false;

            // 같은 폼 재변신 방지
            if (currentFormDefName != null && string.Equals(currentFormDefName, form.defName, System.StringComparison.Ordinal))
                return false;

            return true;
        }
    }
}
