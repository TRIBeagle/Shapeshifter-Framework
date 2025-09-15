// .NET Framework 4.8 / C# 7.3
using HarmonyLib;
using ShapeshifterFramework.Utilities;
using System.Reflection;
using Verse;

namespace ShapeshifterFramework.Patches
{
    /// <summary>
    /// Pawn.DoKillSideEffects 내부의 사망 보이스(soundDeath)를 변신 폼 값으로 대체.
    /// - 호출 앵커: Pawn.DoKillSideEffects (Pawn.Kill 실행 중 호출됨)
    /// - 변신 OFF 상태면 캐시 없음 → 바닐라 그대로 실행.
    /// - 변신 ON 상태에서 캐시에 soundDeath 있으면 폼 사망음 재생 후 바닐라 호출 스킵.
    /// - 순서: 사망음 먼저, 그 후 Kill 로직이 이어지므로 꼬이지 않음.
    /// </summary>
    [HarmonyPatch]
    internal static class Patch_Pawn_DoKillSideEffects_PlayDeathVoiceSound
    {
        static MethodBase TargetMethod()
        {
            // Pawn.cs에 정의된 private void DoKillSideEffects(DamageInfo? dinfo, Hediff exactCulprit, bool spawned)
            return AccessTools.Method(typeof(Pawn), "DoKillSideEffects");
        }

        static bool Prefix(Pawn __instance, DamageInfo? dinfo, Hediff exactCulprit, bool spawned)
        {
            if (__instance == null) return true;

            SoundDef formDeath;
            if (ShapeshiftVoiceHelper.TryGetDeath(__instance, out formDeath))
            {
                ShapeshiftVoiceHelper.PlayOneShotAt(__instance, formDeath, volumeFactor: 1f);
                return false; // 바닐라 사망음 스킵
            }

            return true; // 캐시 없음 → 원래 로직
        }
    }
}
