using RimWorld;
using ShapeshifterFramework.Utilities;
using Verse;

namespace ShapeshifterFramework.Comps
{
    public class CompAbilityEffect_ShiftTarget : CompAbilityEffect
    {
        public new CompProperties_AbilityShiftTarget Props => (CompProperties_AbilityShiftTarget)props;

        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            // Ability의 기본 타겟 판정 + Pawn 존재만 확인 (폼 필터는 유틸 내부에서 엔진 게이트가 검증)
            return base.CanApplyOn(target, dest) && target.Pawn != null && !target.Pawn.Dead;
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            var pawn = target.Pawn;
            if (pawn == null) return;

            ShapeshiftTargetUtility.TryShiftPawn(pawn, Props.formDefName, Props.successChance);
        }
    }
}
