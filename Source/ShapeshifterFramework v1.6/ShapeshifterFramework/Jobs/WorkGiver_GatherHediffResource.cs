// ShapeshifterFramework | Jobs | WorkGiver_GatherHediffResource.cs
// 목적 : HediffComp_Harvestable 보유 폰에서 자원 수확 Job을 할당하는 WorkGiver.
// 용도 : 바닐라 WorkGiver_GatherAnimalBodyResources의 IsAnimal 체크를 제거하고,
//        hediff 기반으로 수확 가능한 폰을 탐색. 인간/동물 모두 대상.

using System.Collections.Generic;
using RimWorld;
using ShapeshifterFramework.Hediffs;
using Verse;
using Verse.AI;

namespace ShapeshifterFramework.Jobs
{
    public class WorkGiver_GatherHediffResource : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            // 같은 팩션 소속 + 맵에 스폰된 모든 폰 탐색
            return pawn.Map.mapPawns.SpawnedPawnsInFaction(pawn.Faction);
        }

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            // 맵에 수확 가능한 폰이 하나라도 있는지 빠른 검사
            var list = pawn.Map.mapPawns.SpawnedPawnsInFaction(pawn.Faction);
            for (int i = 0; i < list.Count; i++)
            {
                var comp = GetHarvestComp(list[i]);
                if (comp != null && comp.ActiveAndFull)
                    return false;
            }
            return true;
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!(t is Pawn target)) return false;
            if (target == pawn) return false; // 자기 자신 수확 불가
            if (target.Downed) return false;
            if (target.roping != null && target.roping.IsRopedByPawn) return false;
            if (!target.CanCasuallyInteractNow()) return false;

            var comp = GetHarvestComp(target);
            if (comp == null || !comp.ActiveAndFull) return false;

            return pawn.CanReserve(target, 1, -1, null, forced);
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return JobMaker.MakeJob(SSFJobDefOf.SSF_GatherHediffResource, t);
        }

        /// <summary>폰에서 ActiveAndFull인 HediffComp_Harvestable 검색.</summary>
        private static HediffComp_Harvestable GetHarvestComp(Pawn target)
        {
            if (target?.health?.hediffSet?.hediffs == null) return null;
            var hediffs = target.health.hediffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                var hwc = hediffs[i] as HediffWithComps;
                if (hwc?.comps == null) continue;
                for (int c = 0; c < hwc.comps.Count; c++)
                {
                    if (hwc.comps[c] is HediffComp_Harvestable h && h.ActiveAndFull)
                        return h;
                }
            }
            return null;
        }
    }

    /// <summary>SSF 전용 JobDef 참조.</summary>
    [DefOf]
    public static class SSFJobDefOf
    {
        public static JobDef SSF_GatherHediffResource;

        static SSFJobDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(SSFJobDefOf));
        }
    }
}
