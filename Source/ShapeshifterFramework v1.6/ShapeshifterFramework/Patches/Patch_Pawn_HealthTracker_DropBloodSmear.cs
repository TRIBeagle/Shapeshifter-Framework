// ShapeshifterFramework | Patches | Patch_Pawn_HealthTracker_DropBloodSmear.cs
// 목적 : 폰이 이동하며 바닥에 피를 묻힐 때(Smear), 바닐라 붉은 피 대신 변신 폼의 전용 혈흔을 생성하기 위한 컨텍스트 주입.
// 용도 : DropBloodFilth 패치와 동일하게 Prefix/Postfix로 개입하여, FilthMaker가 호출되는 찰나의 순간에만 ShapeshiftFilthScope에 현재 폰을 캐싱해 둠.

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
