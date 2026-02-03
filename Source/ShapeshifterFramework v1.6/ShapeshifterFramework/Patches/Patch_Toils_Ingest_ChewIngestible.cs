// .NET Framework 4.8 / C# 7.3
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ShapeshifterFramework.Patches
{
    /// <summary>
    /// 먹는 소리 선택 로직을 폼 우선으로 치환.
    /// - 폼이 soundEating을 제공하면 인간형 여부와 무관하게 그 사운드를 재생
    /// - 폼에 값이 없으면 바닐라 분기(비인간형: race.soundEating, 인간형: ingestible.ingestSound)
    /// 유지
    /// 주의: 바닐라 ChewIngestible의 토일 구성(진행바/회전/예약/해제 등) 그대로 반영
    /// </summary>
    [HarmonyPatch(typeof(Toils_Ingest), nameof(Toils_Ingest.ChewIngestible))]
    internal static class Patch_Toils_Ingest_ChewIngestible
    {
        // RimWorld 1.6 시그니처: (Pawn chewer, float durationMultiplier, TargetIndex ingestibleInd, TargetIndex eatSurfaceInd = TargetIndex.None)
        static bool Prefix(ref Toil __result, Pawn chewer, float durationMultiplier, TargetIndex ingestibleInd, TargetIndex eatSurfaceInd = TargetIndex.None)
        {
            var toil = ToilMaker.MakeToil("ChewIngestible");

            // --- initAction (바닐라와 동일) ---
            toil.initAction = delegate
            {
                Pawn actor = toil.actor;
                Thing thing = actor.CurJob.GetTarget(ingestibleInd).Thing;
                if (!thing.IngestibleNow)
                {
                    chewer.jobs.EndCurrentJob(JobCondition.Incompletable);
                }
                else
                {
                    toil.actor.pather.StopDead();
                    actor.jobs.curDriver.ticksLeftThisToil = Mathf.RoundToInt(thing.def.ingestible.baseIngestTicks * durationMultiplier);
                    if (thing.Spawned)
                        thing.Map.physicalInteractionReservationManager.Reserve(chewer, actor.CurJob, thing);
                }
            };

            // --- tickIntervalAction (바닐라와 동일) ---
            toil.tickIntervalAction = delegate (int delta)
            {
                if (chewer != toil.actor)
                {
                    toil.actor.rotationTracker.FaceCell(chewer.Position);
                }
                else
                {
                    Thing thing = toil.actor.CurJob.GetTarget(ingestibleInd).Thing;
                    if (thing != null && thing.Spawned)
                        toil.actor.rotationTracker.FaceCell(thing.Position);
                    else if (eatSurfaceInd != TargetIndex.None && toil.actor.CurJob.GetTarget(eatSurfaceInd).IsValid)
                        toil.actor.rotationTracker.FaceCell(toil.actor.CurJob.GetTarget(eatSurfaceInd).Cell);
                }

                toil.actor.GainComfortFromCellIfPossible(delta);
            };

            // --- ProgressBar (바닐라와 동일) ---
            toil.WithProgressBar(ingestibleInd, () =>
            {
                Thing thing = toil.actor.CurJob.GetTarget(ingestibleInd).Thing;
                if (thing == null) return 1f;
                float total = Mathf.Round(thing.def.ingestible.baseIngestTicks * durationMultiplier);
                return 1f - (float)toil.actor.jobs.curDriver.ticksLeftThisToil / total;
            });

            toil.defaultCompleteMode = ToilCompleteMode.Delay;
            toil.FailOnDestroyedOrNull(ingestibleInd);

            // --- FinishAction (바닐라와 동일) ---
            toil.AddFinishAction(delegate
            {
                Thing thing = chewer?.CurJob?.GetTarget(ingestibleInd).Thing;
                if (thing != null && chewer.Map.physicalInteractionReservationManager.IsReservedBy(chewer, thing))
                    chewer.Map.physicalInteractionReservationManager.Release(chewer, toil.actor.CurJob, thing);
            });

            toil.handlingFacing = true;

            // --- ★ 여기서 사운드 선택 로직 주입 ★ ---
            // 폼->동물 사운드->음식 사운드 순으로 선택
            toil.PlaySustainerOrSound(() =>
            {
                // 1) 폼 사운드 우선
                var comp = chewer?.TryGetComp<ShapeshifterFramework.Comps.CompShapeshifter>();
                var form = comp != null ? comp.currentForm : null; // 네가 쓰는 필드명(currentForm) 기준
                SoundDef formEat = form != null ? form.soundEating : null;

                if (formEat != null)
                    return formEat;

                // 2) 바닐라 동물 분기
                if (chewer != null && !chewer.RaceProps.Humanlike)
                    return chewer.RaceProps.soundEating; // 바닐라 선택지 (동물)  :contentReference[oaicite:3]{index=3}

                // 3) 바닐라 인간형: 음식의 ingestSound
                LocalTargetInfo target = toil.actor.CurJob.GetTarget(ingestibleInd);
                return target.HasThing ? target.Thing.def.ingestible.ingestSound : null; //  :contentReference[oaicite:4]{index=4}
            });

            __result = toil;
            return false; // 원본 ChewIngestible 실행 막고 교체본 사용
        }
    }
}
