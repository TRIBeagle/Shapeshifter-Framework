// ShapeshifterFramework | Comps | CompAbilityEffect_GiveHediff_Shapeshift.cs
// 목적 : 바닐라 CompAbilityEffect_GiveHediff를 확장하여 SSF 전용 캐스트 조건 + sourceItem 추적을 추가.
// 용도 : - ShouldHideGizmo: 캐스터의 종족/뮤턴트 조건으로 기즈모 숨김
//        - CanApplyOn: 대상 유효성 판별 (다른 폼 변신 중 차단, allowedFromForms 예외)
//        - Apply: 바닐라 hediff 부여 위임 + sourceItem 후처리
//        - GizmoDisabled: 이데올로기 금지 + 변신 중 차단 (allowedFromForms 허용)

using RimWorld;
using ShapeshifterFramework.Hediffs;
using ShapeshifterFramework.Utilities;
using System.Collections.Generic;
using Verse;

namespace ShapeshifterFramework.Comps
{
    /// <summary>바닐라 GiveHediff를 확장한 SSF 어빌리티 효과. hediff 부여는 바닐라에 위임.</summary>
    public class CompAbilityEffect_GiveHediff_Shapeshift : CompAbilityEffect_GiveHediff
    {
        public new CompProperties_AbilityGiveHediff_Shapeshift Props => (CompProperties_AbilityGiveHediff_Shapeshift)props;

        // 캐시: hediffDef → HediffCompProperties_ShapeshiftCore.formDef
        private ShapeshiftFormDef _cachedFormDef;
        private bool _formDefResolved;

        /// <summary>hediffDef에 바인딩된 ShapeshiftFormDef 조회 (캐시).</summary>
        private ShapeshiftFormDef ResolvedFormDef
        {
            get
            {
                if (!_formDefResolved)
                {
                    _cachedFormDef = null;
                    if (Props.hediffDef != null)
                    {
                        var coreProps = Props.hediffDef.CompProps<HediffCompProperties_ShapeshiftCore>();
                        _cachedFormDef = coreProps?.formDef;
                    }
                    _formDefResolved = true;
                }
                return _cachedFormDef;
            }
        }

        /// <summary>캐스터 조건 미충족 시 기즈모 숨김.</summary>
        public override bool ShouldHideGizmo
        {
            get
            {
                var caster = parent?.pawn;
                if (caster == null) return false;

                // 종족 필터
                if (!PassAllowDisallow(caster.def, Props.allowedRaces, Props.disallowedRaces))
                    return true;

                // 뮤턴트 필터 (Anomaly)
                if (Active(Props.allowedMutants) || Active(Props.disallowedMutants))
                {
                    if (!ModLister.AnomalyInstalled)
                    {
                        if (Active(Props.allowedMutants)) return true;
                    }
                    else
                    {
                        if (!PassMutantFilter(caster, Props.allowedMutants, Props.disallowedMutants))
                            return true;
                    }
                }

                return false;
            }
        }

        /// <summary>대상 유효성 판정.</summary>
        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (!base.CanApplyOn(target, dest)) return false;

            var pawn = target.Pawn;

            // AoE(바닥 클릭) — Pawn 없는 location 타겟은 통과시켜
            // 바닐라 Ability_EffectRadius가 반경 내 폰에 개별 Apply를 호출하도록 함
            if (pawn == null) return !target.HasThing;

            if (pawn.Dead) return false;

            // 대상 종족이 폼의 formAllowedRaces에 없으면 차단
            var formDef = ResolvedFormDef;
            if (formDef != null && !ShapeshiftEligibility.IsRaceAllowed(pawn, formDef))
                return false;

            // 이미 다른 폼으로 변신 중이면 차단 — 단, allowedFromForms로 허용된 전환은 예외
            if (Props.hediffDef != null && ShapeshiftEligibility.IsTransformedIntoDifferentForm(pawn, Props.hediffDef))
            {
                // allowedFromForms 체크: 대상의 현재 폼이 허용 목록에 있으면 전환 허용
                if (Props.allowedFromForms != null && Props.allowedFromForms.Count > 0
                    && ShapeshiftCoreUtility.TryGetCore(pawn, out var core)
                    && core.isTransformed && core.currentForm != null)
                {
                    bool allowed = false;
                    for (int i = 0; i < Props.allowedFromForms.Count; i++)
                    {
                        if (string.Equals(Props.allowedFromForms[i], core.currentForm.defName, System.StringComparison.Ordinal))
                        { allowed = true; break; }
                    }
                    if (!allowed) return false;
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>대상 Pawn에 폼 변신 시도. hediff 부여는 바닐라에 위임, sourceItem만 후처리.</summary>
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            var pawn = target.Pawn;
            if (pawn == null) return;

            // affectHostileOnly: 캐스터에 적대가 아닌 폰은 스킵
            if (Props.affectHostileOnly && parent?.pawn != null && !pawn.HostileTo(parent.pawn))
                return;

            // 바닐라가 hediff 부여 (ApplyInner 호출)
            base.Apply(target, dest);

            // 후처리: 자기 자신에게 사용 시에만 아이템 추적 (타인 변신 시 캐스터 아이템은 대상에게 무의미)
            var caster = parent?.pawn;
            if (caster != null && caster == pawn)
            {
                Thing grantingItem = FindGrantingItem(caster, parent.def);
                if (grantingItem != null && ShapeshiftCoreUtility.TryGetCore(pawn, out var core))
                    core.sourceItems = new List<Thing> { grantingItem };
            }
        }

        /// <summary>캐스터의 장착 장비에서 해당 AbilityDef를 부여하는 아이템 탐색.
        /// 인벤토리 아이템은 sourceItem으로 추적하지 않음 (장착 해제 판정 불가).</summary>
        private static Thing FindGrantingItem(Pawn pawn, AbilityDef abilityDef)
        {
            if (abilityDef == null) return null;

            // 장비(apparel) 검색
            if (pawn.apparel != null)
            {
                var wornApparel = pawn.apparel.WornApparel;
                for (int i = 0; i < wornApparel.Count; i++)
                {
                    var comp = wornApparel[i].TryGetComp<CompGiveAbility_Shapeshift>();
                    if (comp != null && comp.Props.ability == abilityDef) return wornApparel[i];
                }
            }

            // 장비(equipment) 검색
            if (pawn.equipment != null)
            {
                var allEquip = pawn.equipment.AllEquipmentListForReading;
                for (int i = 0; i < allEquip.Count; i++)
                {
                    var comp = allEquip[i].TryGetComp<CompGiveAbility_Shapeshift>();
                    if (comp != null && comp.Props.ability == abilityDef) return allEquip[i];
                }
            }

            return null;
        }

        /// <summary>변신 중 다른 폼 어빌리티 비활성화 (allowedFromForms 허용 시 제외).</summary>
        public override bool GizmoDisabled(out string reason)
        {
            var caster = parent?.pawn;
            if (caster == null) { reason = null; return false; }

            // 이데올로기 금지 체크 (바닐라 키 사용)
            if (ShapeshiftEligibility.IsIdeologyForbidden(caster))
            {
                reason = "IdeoligionForbids".Translate();
                return true;
            }

            // 캐스터 종족이 폼의 formAllowedRaces에 없으면 비활성
            var selfForm = ResolvedFormDef;
            if (selfForm != null && !ShapeshiftEligibility.IsRaceAllowed(caster, selfForm))
            {
                reason = "SSF_GizmoDisabled_RaceNotAllowed".Translate(caster.def.label);
                return true;
            }

            // HediffComp_ShapeshiftCore 기반 조회
            if (!ShapeshiftCoreUtility.TryGetCore(caster, out var core) || !core.isTransformed || core.currentForm == null)
            {
                reason = null;
                return false;
            }

            // 같은 폼은 ShouldHideGizmo에서 이미 숨겨져 여기 도달하지 않음.
            // 다른 폼일 때만 allowedFromForms 체크.

            // allowedFromForms 체크
            if (Active(Props.allowedFromForms))
            {
                for (int i = 0; i < Props.allowedFromForms.Count; i++)
                {
                    if (string.Equals(Props.allowedFromForms[i], core.currentForm.defName, System.StringComparison.Ordinal))
                    {
                        reason = null;
                        return false;
                    }
                }
            }

            // 변신 중이고 허용되지 않은 폼 → 비활성
            reason = "SSF_GizmoDisabled_AlreadyTransformed".Translate(core.currentForm.label ?? core.currentForm.defName);
            return true;
        }

        #region 유틸리티 (조건 판정)

        private static bool Active<T>(List<T> list) => list != null && list.Count > 0;

        private static bool PassAllowDisallow<T>(T value, List<T> allow, List<T> disallow) where T : class
        {
            bool hasAllow = Active(allow);
            bool hasDisallow = Active(disallow);
            if (!hasAllow && !hasDisallow) return true;

            if (hasAllow)
            {
                bool found = false;
                for (int i = 0; i < allow.Count; i++)
                    if (allow[i] == value) { found = true; break; }
                if (!found) return false;
            }
            if (hasDisallow)
            {
                for (int i = 0; i < disallow.Count; i++)
                    if (disallow[i] == value) return false;
            }
            return true;
        }

        /// <summary>Pawn의 뮤턴트(바닐라 1개)가 allow/disallow 조건에 부합하는지 판정.</summary>
        private static bool PassMutantFilter(Pawn pawn, List<MutantDef> allow, List<MutantDef> disallow)
        {
            var mutantDef = pawn?.mutant?.Def;

            // allow 체크: 뮤턴트가 allow 목록에 있어야 통과
            if (Active(allow))
            {
                if (mutantDef == null || !allow.Contains(mutantDef))
                    return false;
            }

            // disallow 체크: 뮤턴트가 disallow 목록에 있으면 차단
            if (Active(disallow))
            {
                if (mutantDef != null && disallow.Contains(mutantDef))
                    return false;
            }

            return true;
        }

        #endregion
    }
}
