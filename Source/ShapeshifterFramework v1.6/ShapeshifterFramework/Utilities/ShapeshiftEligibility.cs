// ShapeshifterFramework | Utilities | ShapeshiftEligibility.cs
// 목적 : 기본적인 변신 가능 여부 판정 (종족/뮤턴트 필터, 사망 체크) + 이데올로기/폼 전환 차단 헬퍼.
// 용도 : 모든 변신 경로(어빌리티/약물/스크롤/투사체)에서 공통으로 사용하는 FormDef 수준 필터.
//        어빌리티 시전자 조건(Comp.allowedRaces/allowedMutants)은 CompAbilityEffect_GiveHediff_Shapeshift에서 별도 처리.
//        이데올로기 차단은 각 UI 진입점에서 IsIdeologyForbidden()을 직접 호출.

using RimWorld;
using ShapeshifterFramework.Comps;
using ShapeshifterFramework.Hediffs;
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

        /// <summary>카테고리 bool 검사. 폰의 종류(인간형/동물/메카노이드/뮤턴트)가 폼에서 허용되는지.</summary>
        private static bool IsCategoryAllowed(Pawn pawn, ShapeshiftFormDef form)
        {
            if (pawn.RaceProps == null) return false;

            // 메카노이드 체크
            if (pawn.RaceProps.IsMechanoid && !form.allowMechanoids) return false;

            // 뮤턴트 체크 (뮤턴트이면서 allowMutants=false → 차단)
            if (pawn.mutant?.Def != null && !form.allowMutants) return false;

            // 인간형/동물 체크 (메카노이드/뮤턴트가 아닌 경우)
            if (pawn.RaceProps.Humanlike && !form.allowHumanlike) return false;
            if (pawn.RaceProps.Animal && !form.allowAnimals) return false;

            return true;
        }

        /// <summary>뮤턴트 세부 필터. allowMutants=true일 때만 호출. 화이트/블랙 리스트.</summary>
        private static bool IsMutantAllowed(Pawn pawn, ShapeshiftFormDef form)
        {
            var mutantDef = pawn.mutant?.Def;
            if (mutantDef == null) return true; // 비뮤턴트 → 통과

            // 화이트리스트: 설정되어 있으면 해당 뮤턴트만 허용
            var allow = form.formAllowedMutants;
            if (allow != null && allow.Count > 0 && !allow.Contains(mutantDef))
                return false;

            // 블랙리스트: 지정된 뮤턴트는 차단 (화이트/블랙 독립 적용 — 어느 쪽이든 걸리면 불가)
            var disallow = form.formDisallowedMutants;
            if (disallow != null && disallow.Count > 0 && disallow.Contains(mutantDef))
                return false;

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

        /// <summary>Pawn이 이미 변신 중인지 판정. 동일 폼 포함 모든 변신 상태에서 true 반환.</summary>
        public static bool IsAlreadyTransformed(Pawn pawn)
        {
            return IsAlreadyTransformed(pawn, out _);
        }

        /// <summary>오버로드: 판정 결과와 함께 현재 ShapeshiftCore를 반환. 이중 조회 방지용.</summary>
        public static bool IsAlreadyTransformed(Pawn pawn, out HediffComp_ShapeshiftCore core)
        {
            core = null;
            if (pawn == null) return false;
            if (!ShapeshiftRegistry.TryGet(pawn, out core, out _)) return false;
            return core.isTransformed;
        }

        /// <summary>ThingDef의 ingestible.outcomeDoers에 IngestionOutcomeDoer_Shapeshift가 포함되어 있는지 확인.</summary>
        public static bool HasShapeshiftOutcomeDoer(ThingDef def)
        {
            if (def?.ingestible?.outcomeDoers == null) return false;
            for (int i = 0; i < def.ingestible.outcomeDoers.Count; i++)
            {
                if (def.ingestible.outcomeDoers[i] is IngestionOutcomeDoer_Shapeshift)
                    return true;
            }
            return false;
        }

        /// <summary>Pawn의 현재 변신 폼이 allowedFromForms 목록에 포함되어 있는지 판정.
        /// 이미 변신 중인 폰에 대해 특정 폼에서의 전환을 허용할 때 사용.</summary>
        public static bool IsFormTransitionAllowed(Pawn pawn, List<string> allowedFromForms)
        {
            if (allowedFromForms == null || allowedFromForms.Count == 0) return false;
            if (!IsAlreadyTransformed(pawn, out var core)) return false;
            if (core.currentForm == null) return false;

            for (int i = 0; i < allowedFromForms.Count; i++)
            {
                if (string.Equals(allowedFromForms[i], core.currentForm.defName, System.StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        /// <summary>변신 약물(ThingDef)의 IngestionOutcomeDoer_Shapeshift에서 allowedFromForms 목록을 가져옴.
        /// 약물 패치에서 폼 전환 허용 여부를 판정할 때 사용.</summary>
        public static List<string> GetDrugAllowedFromForms(ThingDef def)
        {
            if (def?.ingestible?.outcomeDoers == null) return null;
            for (int i = 0; i < def.ingestible.outcomeDoers.Count; i++)
            {
                if (def.ingestible.outcomeDoers[i] is IngestionOutcomeDoer_Shapeshift doer)
                    return doer.allowedFromForms;
            }
            return null;
        }

        /// <summary>ThingDef의 ingestible.outcomeDoers에 IngestionOutcomeDoer_ExtendShapeshift가 포함되어 있는지 확인.</summary>
        public static bool HasExtendShapeshiftOutcomeDoer(ThingDef def)
        {
            if (def?.ingestible?.outcomeDoers == null) return false;
            for (int i = 0; i < def.ingestible.outcomeDoers.Count; i++)
            {
                if (def.ingestible.outcomeDoers[i] is IngestionOutcomeDoer_ExtendShapeshift)
                    return true;
            }
            return false;
        }

        /// <summary>연장 약물(ThingDef)의 IngestionOutcomeDoer_ExtendShapeshift에서 targetFormDef를 가져옴.
        /// 약물 패치에서 폼 일치 여부를 판정할 때 사용.</summary>
        public static string GetExtendTargetFormDef(ThingDef def)
        {
            if (def?.ingestible?.outcomeDoers == null) return null;
            for (int i = 0; i < def.ingestible.outcomeDoers.Count; i++)
            {
                if (def.ingestible.outcomeDoers[i] is IngestionOutcomeDoer_ExtendShapeshift doer)
                    return doer.targetFormDef;
            }
            return null;
        }

        /// <summary>연장 약물의 사용 가능 여부를 판정하고 차단 사유를 반환.
        /// null이면 사용 가능, 문자열이면 차단 사유(번역 키 결과).</summary>
        public static string GetExtendDrugBlockReason(Pawn pawn, ThingDef drugDef)
        {
            if (!HasExtendShapeshiftOutcomeDoer(drugDef)) return null;

            // 변신 중이 아니면 차단
            if (!IsAlreadyTransformed(pawn, out var core))
                return "SSF_Message_NotTransformed".Translate(pawn.LabelShort);

            // targetFormDef 지정 시 현재 폼과 불일치하면 차단
            string targetForm = GetExtendTargetFormDef(drugDef);
            if (!targetForm.NullOrEmpty() && core.currentForm?.defName != targetForm)
                return "SSF_Message_WrongFormForExtend".Translate(pawn.LabelShort);

            return null;
        }

        /// <summary>기본 변신 가능 여부 판정. 종족/뮤턴트 필터, 사망 체크.
        /// 이데올로기 차단과 폼 전환 차단은 각 진입점에서 별도 처리.</summary>
        public static bool CanTransformBasic(Pawn pawn, ShapeshiftFormDef form)
        {
            if (pawn == null || form == null) return false;
            if (pawn.Dead) return false;

            // 1. 카테고리 bool (인간형/동물/메카노이드/뮤턴트)
            if (!IsCategoryAllowed(pawn, form)) return false;

            // 2. 종족 세부 리스트 (화이트/블랙)
            if (!IsRaceAllowed(pawn, form)) return false;

            // 3. 뮤턴트 세부 리스트 (allowMutants=true일 때만 의미)
            if (!IsMutantAllowed(pawn, form)) return false;

            // 4. hediff 기반 금지 (질병/저주 등 보유 시 변신 차단)
            if (form.forbiddenHediffs != null && form.forbiddenHediffs.Count > 0 && pawn.health?.hediffSet != null)
            {
                for (int i = 0; i < form.forbiddenHediffs.Count; i++)
                {
                    if (pawn.health.hediffSet.GetFirstHediffOfDef(form.forbiddenHediffs[i]) != null)
                        return false;
                }
            }

            // 5. 정신 상태 기반 금지 (버서크/광기 등 중 변신 차단)
            if (form.forbiddenMentalStates != null && form.forbiddenMentalStates.Count > 0
                && pawn.InMentalState && pawn.MentalStateDef != null)
            {
                for (int i = 0; i < form.forbiddenMentalStates.Count; i++)
                {
                    if (form.forbiddenMentalStates[i] == pawn.MentalStateDef)
                        return false;
                }
            }

            return true;
        }
    }
}
