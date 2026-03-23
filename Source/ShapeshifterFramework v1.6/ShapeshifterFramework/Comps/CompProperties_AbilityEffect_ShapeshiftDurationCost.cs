// ShapeshifterFramework | Comps | CompProperties_AbilityEffect_ShapeshiftDurationCost.cs
// 목적 : 어빌리티 사용 시 변신 잔여 시간을 차감하는 CompAbilityEffect의 속성 클래스.
// 용도 : XML에서 durationCostTicks를 정의하여, 해당 어빌리티 발동 시 변신 타이머를 깎는다.

using RimWorld;
using System.Collections.Generic;
using Verse;

namespace ShapeshifterFramework.Comps
{
    /// <summary>어빌리티 사용 시 변신 시간 차감 효과 속성 정의.</summary>
    public class CompProperties_AbilityEffect_ShapeshiftDurationCost : CompProperties_AbilityEffect
    {
        /// <summary>어빌리티 사용 시 차감할 변신 잔여 틱. 양수 필수.</summary>
        public int durationCostTicks;

        /// <summary>변신 중이 아니면 어빌리티 사용 자체를 차단할지 여부. 기본 true.</summary>
        public bool requireTransformed = true;

        public CompProperties_AbilityEffect_ShapeshiftDurationCost()
        {
            compClass = typeof(CompAbilityEffect_ShapeshiftDurationCost);
        }

        /// <summary>어빌리티 툴팁 StatSummary에 차감 시간 표시.</summary>
        public override IEnumerable<string> ExtraStatSummary()
        {
            if (durationCostTicks > 0)
            {
                string costStr = GenDate.ToStringTicksToPeriod(durationCostTicks, allowSeconds: false, shortForm: false);
                yield return "SSF_DurationCost".Translate(costStr);
            }
        }
    }
}
