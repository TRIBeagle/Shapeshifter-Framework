// ShapeshifterFramework | Comps | CompAbilityEffect_ShiftTarget.cs
// 목적 : Ability를 사용하여 대상(Pawn)을 변신시키는 효과(Effect) 컴포넌트.
// 용도 : - ShouldHideGizmo: 캐스터의 종족/뮤턴트/제노타입 조건 + 같은 폼 재시전 차단
//        - CanApplyOn: 대상 유효성 판별
//        - Apply: ShapeshiftTargetUtility.TryShiftPawn을 호출하여 변신 시도

using RimWorld;
using ShapeshifterFramework.Utilities;
using System.Collections.Generic;
using Verse;

namespace ShapeshifterFramework.Comps
{
    /// <summary>대상 Pawn을 폼으로 변신시키는 Ability 효과.</summary>
    public class CompAbilityEffect_ShiftTarget : CompAbilityEffect
    {
        public new CompProperties_AbilityShiftTarget Props => (CompProperties_AbilityShiftTarget)props;

        // 캐시: formDefName → ShapeshiftFormDef
        private ShapeshiftFormDef _cachedFormDef;
        private bool _formDefResolved;

        private ShapeshiftFormDef ResolvedFormDef
        {
            get
            {
                if (!_formDefResolved)
                {
                    _cachedFormDef = DefDatabase<ShapeshiftFormDef>.GetNamedSilentFail(Props.formDefName);
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

                // 제노타입 필터 (Biotech)
                if (Active(Props.allowedXenotypes) || Active(Props.disallowedXenotypes))
                {
                    if (!ModsConfig.BiotechActive)
                    {
                        if (Active(Props.allowedXenotypes)) return true;
                    }
                    else
                    {
                        var xeno = TryGetXenotype(caster);
                        if (!PassAllowDisallow(xeno, Props.allowedXenotypes, Props.disallowedXenotypes))
                            return true;
                    }
                }

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

                // 같은 폼 재시전 숨김
                var comp = caster.TryGetComp<CompShapeshifter>();
                if (comp != null && comp.isTransformed && comp.currentForm != null)
                {
                    if (string.Equals(comp.currentForm.defName, Props.formDefName, System.StringComparison.Ordinal))
                        return true;
                }

                return false;
            }
        }

        /// <summary>대상 유효성 판정.</summary>
        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (!base.CanApplyOn(target, dest)) return false;

            var pawn = target.Pawn;
            if (pawn == null || pawn.Dead) return false;

            // 이미 같은 폼으로 변신 중이면 차단
            var comp = pawn.TryGetComp<CompShapeshifter>();
            if (comp != null && comp.isTransformed && comp.currentForm != null
                && string.Equals(comp.currentForm.defName, Props.formDefName, System.StringComparison.Ordinal))
            {
                return false;
            }

            return true;
        }

        /// <summary>대상 Pawn에 폼 변신 시도.</summary>
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            var pawn = target.Pawn;
            if (pawn == null) return;

            ShapeshiftTargetUtility.TryShiftPawn(pawn, Props.formDefName, Props.successChance);
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

        private static XenotypeDef TryGetXenotype(Pawn pawn)
        {
            try { return pawn?.genes?.Xenotype; } catch { return null; }
        }

        private static bool PassMutantFilter(Pawn pawn, List<MutantDef> allow, List<MutantDef> disallow)
        {
            var hediffs = pawn?.health?.hediffSet?.hediffs;
            if (hediffs == null) return !Active(allow); // allow 있으면 실패

            // Pawn의 뮤턴트 수집
            var pawnMutants = new List<MutantDef>(2);
            var allMutants = DefDatabase<MutantDef>.AllDefsListForReading;
            for (int i = 0; i < hediffs.Count; i++)
            {
                var h = hediffs[i];
                if (h?.def == null) continue;
                for (int j = 0; j < allMutants.Count; j++)
                {
                    if (allMutants[j]?.hediff == h.def)
                    {
                        pawnMutants.Add(allMutants[j]);
                        break;
                    }
                }
            }

            // allow 체크: pawnMutants ∩ allow ≠ ∅
            if (Active(allow))
            {
                bool found = false;
                for (int i = 0; i < pawnMutants.Count && !found; i++)
                    for (int j = 0; j < allow.Count && !found; j++)
                        if (pawnMutants[i] == allow[j]) found = true;
                if (!found) return false;
            }

            // disallow 체크: pawnMutants ∩ disallow = ∅
            if (Active(disallow))
            {
                for (int i = 0; i < pawnMutants.Count; i++)
                    for (int j = 0; j < disallow.Count; j++)
                        if (pawnMutants[i] == disallow[j]) return false;
            }

            return true;
        }

        #endregion
    }
}
