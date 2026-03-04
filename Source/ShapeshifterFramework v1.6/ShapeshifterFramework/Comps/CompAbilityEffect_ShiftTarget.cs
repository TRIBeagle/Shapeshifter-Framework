// ShapeshifterFramework | Comps | CompAbilityEffect_ShiftTarget.cs
// 목적 : 초능력이나 마법(Ability)을 사용하여 지정한 대상(Pawn)을 강제로 변신시키는 효과(Effect) 컴포넌트.
// 용도 : - CanApplyOn: 대상이 유효한 폰(Pawn)이고 살아있는지(Dead == false) 1차로 판별.
//        - Apply: ShapeshiftTargetUtility.TryShiftPawn을 호출하여 폼(formDefName)과 확률(successChance)을 기반으로 실제 변신을 시도.
// 주의 : 능력 자체의 타겟팅 조건 외에, 대상이 변신 가능한지(Eligibility)에 대한 정밀한 검증은 유틸리티 내부의 엔진 게이트에 완전히 위임함.

using RimWorld;
using ShapeshifterFramework.Utilities;
using Verse;

namespace ShapeshifterFramework.Comps
{
    /// <summary>
    /// 대상 Pawn을 특정 폼으로 변신시키는 Ability 효과.
    /// - 대상 판정은 기본 CanApplyOn에 더해 <c>target.Pawn != null && !Dead</c>만 확인한다.
    /// - 폼 선택/확률 등 세부 검증은 <see cref="ShapeshiftTargetUtility.TryShiftPawn(Pawn, string, float)"/> 내부 엔진 게이트에서 수행.
    /// </summary>
    public class CompAbilityEffect_ShiftTarget : CompAbilityEffect
    {
        /// <summary>
        /// 이 컴포넌트의 설정 값.
        /// </summary>
        public new CompProperties_AbilityShiftTarget Props => (CompProperties_AbilityShiftTarget)props;

        /// <summary>
        /// 능력을 적용할 수 있는지 판정한다.
        /// 기본 판정(<see cref="CompAbilityEffect.CanApplyOn(LocalTargetInfo, LocalTargetInfo)"/>)에 더해
        /// 유효한 Pawn 대상(존재/미사망)만 확인한다.
        /// </summary>
        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            // Ability의 기본 타겟 판정 + Pawn 존재만 확인 (폼 필터는 유틸 내부에서 엔진 게이트가 검증)
            return base.CanApplyOn(target, dest) && target.Pawn != null && !target.Pawn.Dead;
        }

        /// <summary>
        /// 능력을 대상에게 적용한다. 대상 Pawn을 Props에 지정된 폼으로 시도 변신시킨다.
        /// </summary>
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            var pawn = target.Pawn;
            if (pawn == null) return;

            ShapeshiftTargetUtility.TryShiftPawn(pawn, Props.formDefName, Props.successChance);
        }
    }
}
