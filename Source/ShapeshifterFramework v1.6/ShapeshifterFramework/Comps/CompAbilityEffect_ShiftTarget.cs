// ShapeshifterFramework | Comps | CompAbilityEffect_ShiftTarget.cs
// 목적   : 지정 대상 Pawn을 변신시키는 능력(Ability) 효과 컴포넌트.
// 용도   : RimWorld.CompAbilityEffect를 상속하여 대상의 Pawn을 추출하고,
//          ShapeshiftTargetUtility.TryShiftPawn(pawn, formDefName, successChance)을 호출한다.
// 변경   : 2025-09-22 v1.0 — 프로젝트 주석 규칙 적용(주석만 정리, 로직 변경 없음).

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
