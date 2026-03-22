// ShapeshifterFramework | Utilities | ShapeshiftEligibility.cs
// 목적 : 기본적인 변신 가능 여부 판정 (종족/뮤턴트 필터, 사망 체크) + 이데올로기/폼 전환 차단 헬퍼.
// 용도 : 모든 변신 경로(어빌리티/약물/스크롤/투사체)에서 공통으로 사용하는 FormDef 수준 필터.
//        어빌리티 시전자 조건(Comp.allowedRaces/allowedMutants)은 CompAbilityEffect_GiveHediff_Shapeshift에서 별도 처리.
//        이데올로기 차단은 각 UI 진입점에서 IsIdeologyForbidden()을 직접 호출.

using RimWorld;
using ShapeshifterFramework.Hediffs;
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

        /// <summary>폼의 formAllowedMutants/formDisallowedMutants에 대상 뮤턴트가 부합하는지 판정. 둘 다 null/빈 목록이면 제한 없음.</summary>
        private static bool IsMutantAllowed(Pawn pawn, ShapeshiftFormDef form)
        {
            if (pawn == null || form == null) return false;

            var allow = form.formAllowedMutants;
            var disallow = form.formDisallowedMutants;

            bool hasAllow = allow != null && allow.Count > 0;
            bool hasDisallow = disallow != null && disallow.Count > 0;
            if (!hasAllow && !hasDisallow) return true;

            // 바닐라에서 Pawn당 뮤턴트는 1개 — pawn.mutant?.Def로 직접 조회
            var mutantDef = pawn.mutant?.Def;

            // allow: 뮤턴트가 allow 목록에 있어야 통과
            if (hasAllow)
            {
                if (mutantDef == null || !allow.Contains(mutantDef))
                    return false;
            }

            // disallow: 뮤턴트가 disallow 목록에 있으면 차단
            if (hasDisallow)
            {
                if (mutantDef != null && disallow.Contains(mutantDef))
                    return false;
            }

            return true;
        }

        // 캐시: SSF_Shapeshifting_Abhorrent PreceptDef (게임 로드 후 불변)
        private static PreceptDef _abhorrentPrecept;
        private static bool _abhorrentPreceptResolved;

        /// <summary>이데올로기 규율에 의해 변신이 금지되는지 판정. SSF_Shapeshifting_Abhorrent 규율 시 금지.
        /// UI 진입점(기즈모/FloatMenu/CanBeUsedBy)에서 직접 호출해 사전 차단용.</summary>
        public static bool IsIdeologyForbidden(Pawn pawn)
        {
            if (!ModsConfig.IdeologyActive) return false;
            if (pawn == null || pawn.Ideo == null) return false;

            if (!_abhorrentPreceptResolved)
            {
                _abhorrentPrecept = DefDatabase<PreceptDef>.GetNamedSilentFail("SSF_Shapeshifting_Abhorrent");
                _abhorrentPreceptResolved = true;
            }
            var preceptDef = _abhorrentPrecept;
            if (preceptDef == null) return false;

            // Pawn의 이데올로기가 해당 규율을 보유하고 있으면 금지
            for (int i = 0; i < pawn.Ideo.PreceptsListForReading.Count; i++)
            {
                if (pawn.Ideo.PreceptsListForReading[i].def == preceptDef)
                    return true;
            }

            return false;
        }

        /// <summary>Pawn이 이미 다른 폼으로 변신 중인지 판정. hediffDef에 바인딩된 formDef와 현재 폼을 비교.
        /// 같은 폼이면 false (갱신 허용), 다른 폼이면 true (차단 필요).</summary>
        public static bool IsTransformedIntoDifferentForm(Pawn pawn, HediffDef hediffDef)
        {
            if (pawn == null || hediffDef == null) return false;
            if (!ShapeshiftRegistry.TryGet(pawn, out var core, out _)) return false;
            if (!core.isTransformed || core.currentForm == null) return false;

            var coreProps = hediffDef.CompProps<HediffCompProperties_ShapeshiftCore>();
            var formDef = coreProps?.formDef;
            if (formDef == null) return false;

            return core.currentForm != formDef;
        }

        /// <summary>기본 변신 가능 여부 판정. 종족/뮤턴트 필터, 사망 체크.
        /// 이데올로기 차단과 폼 전환 차단은 각 진입점에서 별도 처리.</summary>
        public static bool CanTransformBasic(Pawn pawn, ShapeshiftFormDef form)
        {
            if (pawn == null || form == null) return false;
            if (pawn.Dead) return false;

            // 종족 필터
            if (!IsRaceAllowed(pawn, form)) return false;

            // 뮤턴트 필터
            if (!IsMutantAllowed(pawn, form)) return false;

            return true;
        }
    }
}
