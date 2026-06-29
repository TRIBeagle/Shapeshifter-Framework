// ShapeshifterFramework | Hediffs | HediffComp_AutoShift.cs
// 목적 : 조건부 자동 변신 로직. 특정 hediff를 보유한 Pawn이 조건 충족 시 자동으로 변신.
// 용도 : - CompPostTick에서 주기적으로 체력/정신상태/밝기/전투 조건을 검사.
//        - 조건 충족 시 ShapeshiftCoreUtility.GiveShiftHediff() 호출.
//        - triggerOnce=true면 발동 후 hediff 자체 제거 (1회성 저주 등).
// 주의 : HediffComp_ShapeshiftCore를 직접 수정하지 않음. 독립적인 HediffComp로 동작.

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

        // EnemiesAreNearby 캐싱: 전체 맵 적 스캔이라 비용 큼 — 250틱(약 4.2초)간 결과 재사용
        // 의도적으로 ExposeData에 저장하지 않음 — 캐시는 휘발성이 정답.
        // 세이브에서 복원하면 오래된 stale 데이터로 잘못된 변신 트리거 위험.
        // 로드 후 첫 호출에서 -99999 초기값으로 즉시 재평가됨 (보수적 안전 동작).
        private const int EnemyCheckCacheTicks = 250;
        private int _enemyCheckTick = -99999;
        private bool _enemyCheckCached;

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

            var pawn = parent.pawn;
            if (pawn == null || pawn.Dead) return;

            // 주기 체크
            int interval = Props.checkIntervalTicks > 0 ? Props.checkIntervalTicks : 120;
            if (!pawn.IsHashIntervalTick(interval)) return;

            // 이미 변신 중이면 건너뜀
            if (ShapeshiftRegistry.IsActive(pawn)) return;

            // 조건 판정 (OR: 하나라도 충족 시 트리거)
            if (!AnyConditionMet(pawn)) return;

            // HediffDef 기반 변신 진입
            if (Props.hediffDef == null)
            {
                Log.Error("[SSF] HediffComp_AutoShift: hediffDef is not specified.");
                return;
            }
            ShapeshiftCoreUtility.GiveShiftHediff(pawn, Props.hediffDef);

            // triggerOnce: 발동 후 hediff 제거
            // CompPostTick 내부에서 RemoveHediff 호출 시 리스트 변조 위험 → severity=0으로 자연 제거 유도
            if (Props.triggerOnce)
            {
                hasTriggered = true;
                parent.Severity = 0f;
            }
        }

        /// <summary>조건 판정. requireAllConditions=false(기본): OR — 하나라도 충족 시 true. requireAllConditions=true: AND — 정의된 조건 모두 충족 시 true.</summary>
        private bool AnyConditionMet(Pawn pawn)
        {
            bool andMode = Props.requireAllConditions;
            int defined = 0; // 정의된 조건 수 (AND에서 0이면 false)
            int passed = 0;  // 충족된 조건 수

            // 체력 조건
            if (Props.healthThreshold.HasValue)
            {
                defined++;
                if (pawn.health?.summaryHealth != null
                    && pawn.health.summaryHealth.SummaryHealthPercent < Props.healthThreshold.Value)
                {
                    if (!andMode) return true;
                    passed++;
                }
            }

            // 정신 상태 조건
            if (Props.triggerMentalStates != null && Props.triggerMentalStates.Count > 0)
            {
                defined++;
                if (pawn.InMentalState && pawn.MentalStateDef != null)
                {
                    for (int i = 0; i < Props.triggerMentalStates.Count; i++)
                    {
                        if (Props.triggerMentalStates[i] == pawn.MentalStateDef)
                        {
                            if (!andMode) return true;
                            passed++;
                            break;
                        }
                    }
                }
            }

            // severity 조건 (이 hediff 자체의 severity)
            if (Props.severityThreshold.HasValue)
            {
                defined++;
                if (parent.Severity >= Props.severityThreshold.Value)
                {
                    if (!andMode) return true;
                    passed++;
                }
            }

            // 밝기 조건 (SunGlow 기반, 바이옴/계절 자동 반영)
            if (Props.triggerSunGlowBelow.HasValue)
            {
                defined++;
                if (pawn.Spawned && pawn.Map != null
                    && GenCelestial.CurCelestialSunGlow(pawn.Map) < Props.triggerSunGlowBelow.Value)
                {
                    if (!andMode) return true;
                    passed++;
                }
            }

            // 전투 조건: 징집 상태이거나 최근 피격(5초 이내) + 근처 적대 폰
            if (Props.triggerInCombat)
            {
                defined++;
                if (pawn.Spawned)
                {
                    bool inCombat = pawn.Drafted
                        || (pawn.mindState != null
                            && Find.TickManager.TicksGame - pawn.mindState.lastAttackTargetTick < 300);
                    if (inCombat)
                    {
                        int now = Find.TickManager.TicksGame;
                        if (now - _enemyCheckTick >= EnemyCheckCacheTicks)
                        {
                            _enemyCheckCached = PawnUtility.EnemiesAreNearby(pawn);
                            _enemyCheckTick = now;
                        }
                        if (_enemyCheckCached)
                        {
                            if (!andMode) return true;
                            passed++;
                        }
                    }
                }
            }

            // AND 모드: 정의된 조건 모두 충족 시 true
            if (andMode) return defined > 0 && passed == defined;

            // OR 모드: 여기까지 왔으면 아무것도 충족 안 됨
            return false;
        }
    }
}
