// ShapeshifterFramework | Patches | Patch_Toils_Ingest_ChewIngestible.cs
// 목적 : 폰이 음식을 씹어 먹을 때 나는 소리(Ingest Sound)를 변신 폼의 식사음으로 대체.
// 용도 : AddIngestionEffects에 Postfix로 개입하여, 바닐라가 등록한 사운드 재생 델리게이트(Func)를 가로채고 폼 전용 식사음(soundEating)을 최우선으로 반환하는 커스텀 델리게이트로 덮어씌움.

using HarmonyLib;
using RimWorld;
using ShapeshifterFramework.Utilities;
using Verse;
using Verse.AI;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch(typeof(Toils_Ingest), nameof(Toils_Ingest.AddIngestionEffects))]
    internal static class Patch_Toils_Ingest_AddIngestionEffects
    {
        static void Postfix(Toil toil, Pawn chewer, TargetIndex ingestibleInd)
        {
            // toil.soundDef / PlaySustainerOrSound는 내부적으로
            // toil.activeSkillSustainerOrSound (Func<SoundDef>) 필드에 저장됨
            // 이걸 자체 delegate로 덮어씀
            toil.PlaySustainerOrSound(() =>
            {
                // 1) 폼 사운드 우선 — HediffComp_ShapeshiftCore 기반 조회
                if (ShapeshiftCoreUtility.TryGetCore(chewer, out var core))
                {
                    var form = core.currentForm;
                    if (form?.soundEating != null)
                        return form.soundEating;
                }

                // 2) 바닐라 폴백
                if (!chewer.RaceProps.Humanlike)
                    return chewer.RaceProps.soundEating;

                LocalTargetInfo target = toil.actor.CurJob.GetTarget(ingestibleInd);
                return target.HasThing ? target.Thing.def?.ingestible?.ingestSound : null;
            });
        }
    }
}
