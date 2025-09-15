// .NET Framework 4.8 / C# 7.3
using HarmonyLib;
using ShapeshifterFramework.Utilities;
using Verse;

namespace ShapeshifterFramework.Patches
{
    /// <summary>
    /// Pawn_HealthTracker.DropBloodSmear → FilthMaker 호출 전후로 Pawn 스코프 세팅
    /// - DropBloodFilth 패치와 동일한 구조
    /// - 변신 ON 상태면 FilthMaker에서 bloodSmearDef 캐시 적용됨
    /// </summary>
    [HarmonyPatch(typeof(Pawn_HealthTracker), "DropBloodSmear")]
    internal static class Patch_Pawn_HealthTracker_DropBloodSmear
    {
        // Pawn_HealthTracker 안의 private readonly Pawn pawn; 필드 접근자
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
