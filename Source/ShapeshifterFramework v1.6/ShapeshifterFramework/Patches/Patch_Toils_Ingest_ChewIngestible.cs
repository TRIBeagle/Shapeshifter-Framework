// .NET Framework 4.8 / C# 7.3
using HarmonyLib;
using RimWorld;
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
                // 1) 폼 사운드 우선
                var comp = chewer?.TryGetComp<ShapeshifterFramework.Comps.CompShapeshifter>();
                var form = comp?.currentForm;
                if (form?.soundEating != null)
                    return form.soundEating;

                // 2) 바닐라 폴백
                if (!chewer.RaceProps.Humanlike)
                    return chewer.RaceProps.soundEating;

                LocalTargetInfo target = toil.actor.CurJob.GetTarget(ingestibleInd);
                return target.HasThing ? target.Thing.def.ingestible.ingestSound : null;
            });
        }
    }
}
