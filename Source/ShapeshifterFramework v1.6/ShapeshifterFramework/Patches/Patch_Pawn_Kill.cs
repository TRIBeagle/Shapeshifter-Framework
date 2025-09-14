using HarmonyLib;
using ShapeshifterFramework.Comps;
using System;
using Verse;

namespace ShapeshifterFramework.Patches
{
    /// <summary>
    /// Pawn.Kill Postfix에서 CompShapeshifter.Notify_Killed 호출
    /// - 사망음은 DoKillSideEffects Prefix에서 이미 대체됨.
    /// - Kill 종료 시점에 변신 해제 및 캐시 정리를 보장.
    /// </summary>
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
    internal static class Patch_Pawn_Kill
    {
        static void Postfix(Pawn __instance, DamageInfo? dinfo, Hediff exactCulprit)
        {
            if (__instance == null) return;

            var comp = __instance.GetComp<CompShapeshifter>();
            comp?.Notify_Killed(dinfo, exactCulprit);
        }
    }
}
