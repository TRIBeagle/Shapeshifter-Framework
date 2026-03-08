// ShapeshifterFramework | Comps | CompAbilityEffect_ShiftTarget.cs
// 목적 : 초능력이나 마법(Ability)을 사용하여 지정한 대상(Pawn)을 강제로 변신시키는 효과(Effect) 컴포넌트.
// 용도 : - CanApplyOn: 대상이 유효한 폰(Pawn)이고 살아있는지(Dead == false) 1차로 판별. 같은 폼 재시전 차단.
//        - Apply: ShapeshiftTargetUtility.TryShiftPawn을 호출하여 폼(formDefName)과 확률(successChance)을 기반으로 실제 변신을 시도.
// 주의 : 능력 자체의 타겟팅 조건 외에, 대상이 변신 가능한지(Eligibility)에 대한 정밀한 검증은 유틸리티 내부의 엔진 게이트에 완전히 위임함.

using RimWorld;
using ShapeshifterFramework.Utilities;
using Verse;

namespace ShapeshifterFramework.Comps
{
    /// <summary>대상 Pawn을 폼으로 변신시키는 Ability 효과.</summary>
    public class CompAbilityEffect_ShiftTarget : CompAbilityEffect
    {
        public new CompProperties_AbilityShiftTarget Props => (CompProperties_AbilityShiftTarget)props;

        /// <summary>대상 유효성 판정. 같은 폼 재시전 차단 포함.</summary>
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
    }
}
