// Patch_PawnRenderer_GetBodyPos.cs (C# 7.3)
using HarmonyLib;
using RimWorld;
using ShapeshifterFramework.Utilities;
using Verse;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch(typeof(PawnRenderer))]
    [HarmonyPatch("GetBodyPos")]
    internal class Patch_PawnRenderer_GetBodyPos
    {
        private static void Postfix(ref bool showBody, Pawn ___pawn)
        {
            // 인간형 + 플레이어 소속 + 비수감자
            if (___pawn == null || !___pawn.RaceProps.Humanlike
                || ___pawn.Faction == null || ___pawn.Faction != Faction.OfPlayer
                || ___pawn.IsPrisoner)
                return;

            var comp = ShapeshiftUtility.GetShapeShiftComp(___pawn);
            if (comp == null || comp.currentForm == null || !ShapeshiftUtility.IsShapeShifting(___pawn))
                return;

            // 이미 보이거나 침대가 아니면 스킵
            if (showBody || ___pawn.CurrentBed() == null)
                return;

            // 침대에서도 바디 보이도록 강제
            showBody = true;
        }
    }
}
