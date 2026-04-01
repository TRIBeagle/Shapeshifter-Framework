// ShapeshifterFramework | Jobs | JobDriver_GatherHediffResource.cs
// 목적 : HediffComp_Harvestable 기반 자원 수확 JobDriver.
// 용도 : 바닐라 JobDriver_GatherAnimalBodyResources 패턴을 HediffComp용으로 재구현.
//        인간/동물 폰 모두에서 hediff 기반 수확 가능.

using System.Collections.Generic;
using RimWorld;
using ShapeshifterFramework.Hediffs;
using Verse;
using Verse.AI;

namespace ShapeshifterFramework.Jobs
{
    public class JobDriver_GatherHediffResource : JobDriver
    {
        private float gatherProgress;

        private const TargetIndex TargetInd = TargetIndex.A;

        /// <summary>수확 완료까지 필요한 작업량.</summary>
        private const float DefaultWorkTotal = 400f;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref gatherProgress, "gatherProgress", 0f);
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.GetTarget(TargetInd), job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetInd);
            this.FailOnDowned(TargetInd);
            this.FailOnNotCasualInterruptible(TargetInd);
            yield return Toils_Goto.GotoThing(TargetInd, PathEndMode.Touch);

            Toil wait = ToilMaker.MakeToil("MakeNewToils");
            wait.initAction = delegate
            {
                Pawn actor = wait.actor;
                Pawn target = (Pawn)job.GetTarget(TargetInd).Thing;
                actor.pather.StopDead();
                PawnUtility.ForceWait(target, 15000, null, maintainPosture: true);
            };
            wait.tickIntervalAction = delegate(int delta)
            {
                Pawn actor = wait.actor;
                actor.skills.Learn(SkillDefOf.Animals, 0.13f * delta);
                gatherProgress += actor.GetStatValue(StatDefOf.AnimalGatherSpeed) * delta;
                if (gatherProgress >= DefaultWorkTotal)
                {
                    var comp = GetHarvestComp((Pawn)job.GetTarget(TargetInd).Thing);
                    comp?.Gathered(pawn);
                    actor.jobs.EndCurrentJob(JobCondition.Succeeded);
                }
            };
            wait.AddFinishAction(delegate
            {
                Pawn target = (Pawn)job.GetTarget(TargetInd).Thing;
                if (target != null && target.CurJobDef == JobDefOf.Wait_MaintainPosture)
                    target.jobs.EndCurrentJob(JobCondition.InterruptForced);
            });
            wait.FailOnDespawnedOrNull(TargetInd);
            wait.FailOnCannotTouch(TargetInd, PathEndMode.Touch);
            wait.AddEndCondition(() =>
            {
                var comp = GetHarvestComp((Pawn)job.GetTarget(TargetInd).Thing);
                return (comp != null && comp.ActiveAndFull) ? JobCondition.Ongoing : JobCondition.Incompletable;
            });
            wait.defaultCompleteMode = ToilCompleteMode.Never;
            wait.WithProgressBar(TargetInd, () => gatherProgress / DefaultWorkTotal);
            wait.activeSkill = () => SkillDefOf.Animals;
            yield return wait;
        }

        private static HediffComp_Harvestable GetHarvestComp(Pawn target)
            => WorkGiver_GatherHediffResource.GetHarvestComp(target);
    }
}
