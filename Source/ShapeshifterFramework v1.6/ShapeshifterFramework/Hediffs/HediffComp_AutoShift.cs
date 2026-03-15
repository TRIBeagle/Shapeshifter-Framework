// ShapeshifterFramework | Hediffs | HediffComp_AutoShift.cs
// 목적 : 조건부 자동 변신 로직. 특정 hediff를 보유한 Pawn이 조건 충족 시 자동으로 변신.
// 용도 : - CompPostTick에서 주기적으로 체력/정신상태/밝기/전투 조건을 검사.
//        - 조건 충족 시 ShapeshiftTargetUtility.TryShiftPawn() 호출.
//        - triggerOnce=true면 발동 후 hediff 자체 제거 (1회성 저주 등).
// 주의 : CompShapeshifter를 직접 수정하지 않음. 독립적인 HediffComp로 동작.

using RimWorld;
using ShapeshifterFramework.Utilities;
using Verse;

namespace ShapeshifterFramework.Hediffs
{
    /// <summary>조건부 자동 변신 HediffComp.</summary>
    public class HediffComp_AutoShift : HediffComp
    {
        private HediffCompProperties_AutoShift Props => (HediffCompProperties_AutoShift)props;

        // triggerOnce 발동 여부 (세이브/로드)
        private bool hasTriggered;

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref hasTriggered, "hasTriggered", false);
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            // 이미 발동한 1회성이면 건너뜀
            if (hasTriggered) return;

            // 주기 체크
            int interval = Props.checkIntervalTicks > 0 ? Props.checkIntervalTicks : 120;
            if (!parent.pawn.IsHashIntervalTick(interval)) return;

            var pawn = parent.pawn;
            if (pawn == null || pawn.Dead) return;

            // 이미 변신 중이면 건너뜀
            if (ShapeshiftUtility.IsShapeShifting(pawn)) return;

            // 조건 판정 (OR: 하나라도 충족 시 트리거)
            if (!AnyConditionMet(pawn)) return;

            // 변신 시도
            bool shifted = ShapeshiftTargetUtility.TryShiftPawn(pawn, Props.formDefName, Props.successChance);

            // triggerOnce: 성공 시 hediff 제거
            if (shifted && Props.triggerOnce)
            {
                hasTriggered = true;
                if (pawn.health != null)
                    pawn.health.RemoveHediff(parent);
            }
        }

        /// <summary>4가지 조건 중 하나라도 충족하면 true.</summary>
        private bool AnyConditionMet(Pawn pawn)
        {
            // 체력 조건
            if (Props.healthThreshold.HasValue
                && pawn.health?.summaryHealth != null
                && pawn.health.summaryHealth.SummaryHealthPercent < Props.healthThreshold.Value)
                return true;

            // 정신 상태 조건
            if (Props.triggerMentalStates != null && Props.triggerMentalStates.Count > 0
                && pawn.InMentalState && pawn.MentalStateDef != null)
            {
                for (int i = 0; i < Props.triggerMentalStates.Count; i++)
                {
                    if (Props.triggerMentalStates[i] == pawn.MentalStateDef)
                        return true;
                }
            }

            // 밝기 조건 (SunGlow 기반, 바이옴/계절 자동 반영)
            if (Props.triggerSunGlowBelow.HasValue && pawn.Spawned && pawn.Map != null)
            {
                if (GenCelestial.CurCelestialSunGlow(pawn.Map) < Props.triggerSunGlowBelow.Value)
                    return true;
            }

            // 전투 조건: 징집 상태이거나 최근 피격(5초 이내) + 근처 적대 폰
            if (Props.triggerInCombat && pawn.Spawned)
            {
                bool inCombat = pawn.Drafted
                    || (pawn.mindState != null
                        && Find.TickManager.TicksGame - pawn.mindState.lastAttackTargetTick < 300);
                if (inCombat && PawnUtility.EnemiesAreNearby(pawn))
                    return true;
            }

            return false;
        }
    }
}
