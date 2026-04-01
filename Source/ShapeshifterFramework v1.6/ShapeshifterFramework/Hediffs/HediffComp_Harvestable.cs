// ShapeshifterFramework | Hediffs | HediffComp_Harvestable.cs
// 목적 : hediff 보유 중 자원 수확을 가능하게 하는 범용 HediffComp.
// 용도 : 바닐라 CompHasGatherableBodyResource(ThingComp)와 동일한 fullness 성장 + 수확 패턴을
//        HediffComp로 구현. 변신 폼에 addHediffs로 부여하면 해당 폼 유지 중 수확 가능.
//        hediff 제거(변신 해제) 시 자동으로 수확 불가.
// 주의 : 바닐라 WorkGiver_GatherAnimalBodyResources는 IsAnimal 체크가 있어 인간 폰에 적용 불가.
//        별도 WorkGiver/JobDriver 또는 바닐라 패치가 필요함 (서브모드에서 구현).

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

        /// <summary>fullness 0→1 충전에 걸리는 게임 일수.</summary>
        public int intervalDays = 10;

        /// <summary>인스펙터에 표시할 번역 키. {0} = fullness%.</summary>
        public string inspectStringKey = "SSF_Harvestable_Fullness";

        /// <summary>Scribe 저장 키.</summary>
        public string saveKey = "ssfHarvestFullness";

        public HediffCompProperties_Harvestable()
        {
            compClass = typeof(HediffComp_Harvestable);
        }
    }

    /// <summary>hediff 존재 시 자원 fullness가 틱마다 성장하고, 가득 차면 수확 가능.</summary>
    public class HediffComp_Harvestable : HediffComp
    {
        private float fullness;

        public HediffCompProperties_Harvestable Props => (HediffCompProperties_Harvestable)props;

        /// <summary>자원 충전도 (0~1).</summary>
        public float Fullness => fullness;

        /// <summary>수확 가능 여부.</summary>
        public bool ActiveAndFull => IsActive && fullness >= 1f;

        /// <summary>활성 여부: 폰이 살아있고 소속 팩션이 있어야 함.</summary>
        private bool IsActive
        {
            get
            {
                var pawn = Pawn;
                if (pawn == null || pawn.Dead) return false;
                if (pawn.Faction == null) return false;
                if (pawn.Suspended) return false;
                return true;
            }
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (!IsActive) return;

            // 바닐라 CompHasGatherableBodyResource.CompTick 패턴: 1일 = 60000틱
            float growthPerTick = 1f / (Props.intervalDays * 60000f);

            // 바닐라처럼 BodyResourceGrowthSpeed 적용 (영양/건강 상태 반영)
            var pawn = Pawn;
            if (pawn != null)
                growthPerTick *= PawnUtility.BodyResourceGrowthSpeed(pawn);

            fullness = Mathf.Clamp01(fullness + growthPerTick);
        }

        /// <summary>수확 실행. 바닐라 CompHasGatherableBodyResource.Gathered 패턴.</summary>
        public void Gathered(Pawn doer)
        {
            if (!IsActive || Props.resourceDef == null) return;

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

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref fullness, Props?.saveKey ?? "ssfHarvestFullness", 0f);
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
