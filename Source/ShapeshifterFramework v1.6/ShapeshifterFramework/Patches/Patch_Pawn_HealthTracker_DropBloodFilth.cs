using HarmonyLib;
using Verse;
using RimWorld;
using ShapeshifterFramework.Utilities;
using System.Reflection;

namespace ShapeshifterFramework.Patches
{
    /// <summary>
    /// Pawn_HealthTracker.DropBloodFilth → FilthMaker 호출 전후로 Pawn 스코프 세팅
    /// </summary>
    [HarmonyPatch(typeof(Pawn_HealthTracker), "DropBloodFilth")]
    internal static class Patch_Pawn_HealthTracker_DropBloodFilth
    {
        // private readonly Pawn pawn; 필드에 대한 참조자 준비
        private static readonly AccessTools.FieldRef<Pawn_HealthTracker, Pawn> pawnFieldRef =
            AccessTools.FieldRefAccess<Pawn_HealthTracker, Pawn>("pawn");

        static void Prefix(Pawn_HealthTracker __instance)
        {
            Pawn pawn = pawnFieldRef(__instance);
            if (pawn != null)
                ShapeshiftFilthScope.CurrentPawn = pawn;
        }

        static void Postfix()
        {
            ShapeshiftFilthScope.CurrentPawn = null;
        }
    }
}
