// ShapeshifterFramework | Patches | Patch_Pawn_HealthTracker_DropBloodFilth.cs
// 목적 : 피를 흘릴 때(Filth) 및 이동 시 바닥에 묻히는 피(Smear)를 변신 폼에 맞는 혈흔으로 교체하기 위한 우회 연결(Hook).
// 용도 : 바닐라 FilthMaker 파라미터에는 Pawn 정보가 누락되어 있으므로, Prefix로 스레드 스코프(ShapeshiftFilthScope)에 현재 폰을 담아두고 Postfix에서 비워줌.

using HarmonyLib;
using ShapeshifterFramework.Utilities;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace ShapeshifterFramework.Patches
{
    /// <summary>DropBloodFilth / DropBloodSmear 전후로 FilthScope에 Pawn 세팅.</summary>
    [HarmonyPatch]
    internal static class Patch_Pawn_HealthTracker_DropBloodFilth
    {
        // pawn 필드 접근자
        private static readonly AccessTools.FieldRef<Pawn_HealthTracker, Pawn> pawnFieldRef =
            AccessTools.FieldRefAccess<Pawn_HealthTracker, Pawn>("pawn");

        /// <summary>DropBloodFilth, DropBloodSmear 두 메서드를 동시에 패치.</summary>
        static IEnumerable<MethodBase> TargetMethods()
        {
            var filth = AccessTools.Method(typeof(Pawn_HealthTracker), "DropBloodFilth");
            if (filth != null) yield return filth;

            // DropBloodSmear는 바닐라 버전에 따라 존재하지 않을 수 있음
            var smear = AccessTools.Method(typeof(Pawn_HealthTracker), "DropBloodSmear");
            if (smear != null) yield return smear;
        }

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
