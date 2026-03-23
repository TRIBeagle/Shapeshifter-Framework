// ShapeshifterFramework | Comps | CompAbilityEffect_ShapeshiftDurationCost.cs
// 목적 : 어빌리티 발동 시 변신 잔여 시간을 차감하는 CompAbilityEffect.
// 용도 : 변신 전용 어빌리티(addAbilities)에 이 comp를 추가하면,
//        사용할 때마다 durationCostTicks만큼 변신 타이머를 깎는다.
//        requireTransformed=true면 변신 중이 아닐 때 사용 차단.

using RimWorld;
using ShapeshifterFramework.Utilities;
using Verse;

namespace ShapeshifterFramework.Comps
{
    /// <summary>어빌리티 사용 시 변신 시간 차감 효과.</summary>
    public class CompAbilityEffect_ShapeshiftDurationCost : CompAbilityEffect
    {
        public new CompProperties_AbilityEffect_ShapeshiftDurationCost Props
            => (CompProperties_AbilityEffect_ShapeshiftDurationCost)props;

        /// <summary>변신 중이 아니면 기즈모 비활성화 (requireTransformed=true 시).</summary>
        public override bool GizmoDisabled(out string reason)
        {
            if (Props.requireTransformed)
            {
                var caster = parent?.pawn;
                if (caster != null && !ShapeshiftRegistry.IsActive(caster))
                {
                    reason = "SSF_Message_NotTransformed".Translate(caster.LabelShort);
                    return true;
                }
            }
            reason = null;
            return false;
        }

        /// <summary>어빌리티 적용 시 변신 시간 차감.</summary>
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            var caster = parent?.pawn;
            if (caster == null) return;

            if (Props.durationCostTicks <= 0) return;

            if (!ShapeshiftRegistry.TryGet(caster, out var core, out _)) return;

            core.ExtendDuration(-Props.durationCostTicks, true);

            if (ShapeshifterFrameworkMod.Settings?.enableDebugLog == true)
            {
                string abilityName = parent?.def?.label ?? "Unknown";
                Log.Message($"[SSF] 어빌리티 '{abilityName}' 사용 — 변신 시간 {Props.durationCostTicks}틱 차감 (남은: {core.RemainingShapeshiftTicks}틱)");
            }
        }
    }
}
