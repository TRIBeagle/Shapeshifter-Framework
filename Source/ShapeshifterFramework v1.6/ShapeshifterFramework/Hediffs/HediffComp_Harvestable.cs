// ShapeshifterFramework | Hediffs | HediffComp_Harvestable.cs
// 목적 : hediff 보유 중 자원 수확을 가능하게 하는 범용 HediffComp.
// 용도 : 바닐라 CompShearable/CompMilkable/CompEggLayer 3종을 HediffComp 하나로 통합.
//        autoSpawn=false (기본): WorkGiver가 수확 (울/우유 패턴)
//        autoSpawn=true: fullness 1.0 시 바닥에 자동 스폰 (알 패턴)
//        requiredGender=Female: 암컷만 활성 (우유/알 패턴)

using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ShapeshifterFramework.Hediffs
{
    public class HediffCompProperties_Harvestable : HediffCompProperties
    {
        /// <summary>수확할 자원 ThingDef.</summary>
        public ThingDef resourceDef;

        /// <summary>수확 1회당 자원 수량.</summary>
        public int resourceAmount = 10;

        /// <summary>fullness 0→1 충전에 걸리는 게임 틱. 기본 600000 = 10일 (60000틱/일).
        /// 10일은 양털 등 느린 수확물 기준 — 알/우유처럼 잦은 수확물은 1~2일(60000~120000)로 짧게 설정 권장.</summary>
        public int intervalTicks = 600000;

        /// <summary>true면 fullness 1.0 도달 시 바닥에 자동 스폰 (알 패턴). false면 WorkGiver 수확 (울/우유 패턴).</summary>
        public bool autoSpawn = false;

        /// <summary>수확 활성에 필요한 성별. null이면 제한 없음.</summary>
        public Gender? requiredGender = null;

        /// <summary>인스펙터 표시 번역 키. {0} = fullness%.</summary>
        public string inspectStringKey = "SSF_Harvestable_Fullness";

        /// <summary>Scribe 저장 키.</summary>
        public string saveKey = "ssfHarvestFullness";

        public HediffCompProperties_Harvestable()
        {
            compClass = typeof(HediffComp_Harvestable);
        }

        public override IEnumerable<string> ConfigErrors(HediffDef parentDef)
        {
            foreach (var e in base.ConfigErrors(parentDef)) yield return e;
            if (resourceDef == null)
                yield return $"{parentDef.defName}: HediffCompProperties_Harvestable.resourceDef is null";
            if (intervalTicks <= 0)
                yield return $"{parentDef.defName}: HediffCompProperties_Harvestable.intervalTicks must be > 0 (got {intervalTicks})";
            if (resourceAmount <= 0)
                yield return $"{parentDef.defName}: HediffCompProperties_Harvestable.resourceAmount must be > 0 (got {resourceAmount})";
        }
    }

    /// <summary>hediff 존재 시 자원 fullness가 틱마다 성장. 수확 또는 자동 스폰.</summary>
    public class HediffComp_Harvestable : HediffComp
    {
        // ── 폰별 캐시: WorkGiver에서 O(1) 조회용 ──
        private static readonly Dictionary<Pawn, HediffComp_Harvestable> _cache = new Dictionary<Pawn, HediffComp_Harvestable>(32);

        /// <summary>캐시에서 O(1) 조회. 없으면 null.</summary>
        public static HediffComp_Harvestable TryGetCached(Pawn pawn)
        {
            if (pawn != null && _cache.TryGetValue(pawn, out var comp))
                return comp;
            return null;
        }

        private void RegisterCache()
        {
            var pawn = Pawn;
            if (pawn != null) _cache[pawn] = this;
        }

        private void UnregisterCache()
        {
            var pawn = Pawn;
            if (pawn != null) _cache.Remove(pawn);
        }

        /// <summary>게임 로드/맵 전환 시 캐시 정리.</summary>
        public static void ClearCache() => _cache.Clear();

        private float fullness;

        public HediffCompProperties_Harvestable Props => (HediffCompProperties_Harvestable)props;

        /// <summary>자원 충전도 (0~1).</summary>
        public float Fullness => fullness;

        /// <summary>WorkGiver 수확 가능 여부 (autoSpawn=false일 때만 사용).</summary>
        public bool ActiveAndFull => !Props.autoSpawn && IsActive && fullness >= 1f;

        /// <summary>활성 여부.</summary>
        private bool IsActive
        {
            get
            {
                var pawn = Pawn;
                if (pawn == null || pawn.Dead) return false;
                if (pawn.Faction == null) return false;
                if (pawn.Suspended) return false;
                if (Props.requiredGender.HasValue && pawn.gender != Props.requiredGender.Value) return false;
                return true;
            }
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (!IsActive) return;
            if (Props.intervalTicks <= 0) return;

            var pawn = Pawn;
            if (pawn == null) return;
            // 느린 누적기 — 60틱 간격으로 60배 누적(성장률 동일), 매틱 BodyResourceGrowthSpeed 호출을 ~1/60로 절감
            if (!pawn.IsHashIntervalTick(60)) return;

            fullness += 60f / Props.intervalTicks * PawnUtility.BodyResourceGrowthSpeed(pawn);

            if (fullness >= 1f)
            {
                if (Props.autoSpawn)
                {
                    // 알 패턴: 자동 스폰 후 리셋
                    AutoSpawnResource(pawn);
                    fullness = 0f;
                }
                else
                {
                    fullness = 1f; // WorkGiver 수확 대기
                }
            }
        }

        /// <summary>autoSpawn 시 자원을 바닥에 자동 스폰.</summary>
        private void AutoSpawnResource(Pawn pawn)
        {
            if (Props.resourceDef == null || !pawn.Spawned) return;

            int total = Props.resourceAmount;
            while (total > 0)
            {
                int stack = Mathf.Clamp(total, 1, Props.resourceDef.stackLimit);
                total -= stack;
                Thing thing = ThingMaker.MakeThing(Props.resourceDef);
                thing.stackCount = stack;
                GenPlace.TryPlaceThing(thing, pawn.Position, pawn.Map, ThingPlaceMode.Near);
            }
        }

        /// <summary>수확 실행 (WorkGiver 경유). 바닐라 Gathered 패턴.</summary>
        public void Gathered(Pawn doer)
        {
            if (!IsActive || Props.resourceDef == null) return;
            // public 진입점 — 비스폰/맵 없음 시 Pawn.Map/doer.Map NRE 방지 (WorkGiver 외 호출자 대비)
            if (Pawn == null || !Pawn.Spawned || Pawn.Map == null || doer?.Map == null) return;

            if (!Rand.Chance(doer.GetStatValue(StatDefOf.AnimalGatherYield)))
            {
                MoteMaker.ThrowText(
                    (doer.DrawPos + Pawn.DrawPos) / 2f,
                    Pawn.Map,
                    "TextMote_ProductWasted".Translate(), 3.65f);
            }
            else
            {
                int total = GenMath.RoundRandom(Props.resourceAmount * fullness);
                while (total > 0)
                {
                    int stack = Mathf.Clamp(total, 1, Props.resourceDef.stackLimit);
                    total -= stack;
                    Thing thing = ThingMaker.MakeThing(Props.resourceDef);
                    thing.stackCount = stack;
                    GenPlace.TryPlaceThing(thing, doer.Position, doer.Map, ThingPlaceMode.Near);
                }
            }

            fullness = 0f;
        }

        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            RegisterCache();
        }

        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            UnregisterCache();
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref fullness, Props?.saveKey ?? "ssfHarvestFullness", 0f);

            // 로드 후 캐시 재등록
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                RegisterCache();
        }

        /// <summary>hediff 이름 옆 괄호 안에 표시 (건강 탭). 예: "bear form (Resource 45%)"</summary>
        public override string CompLabelInBracketsExtra
        {
            get
            {
                if (!IsActive || fullness <= 0f) return null;
                return Props.inspectStringKey.Translate(fullness.ToStringPercent());
            }
        }

        public override string CompTipStringExtra
        {
            get
            {
                if (!IsActive) return null;
                return Props.inspectStringKey.Translate(fullness.ToStringPercent());
            }
        }
    }
}
