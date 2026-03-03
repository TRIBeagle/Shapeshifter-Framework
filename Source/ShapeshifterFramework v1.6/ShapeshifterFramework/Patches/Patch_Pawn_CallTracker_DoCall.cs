// .NET Framework 4.8 / C# 7.3
using HarmonyLib;
using ShapeshifterFramework.Utilities;
using Verse;

namespace ShapeshifterFramework.Patches
{
    /// <summary>
    /// Pawn_CallTracker.DoCall 패치
    /// - 평상시 콜(soundCall) → 폼 전용 soundCall 대체
    /// - 공격적 콜(forceAggressive = true) → 폼 전용 soundAngry 대체
    /// </summary>
    [HarmonyPatch(typeof(Pawn_CallTracker), nameof(Pawn_CallTracker.DoCall))]
    internal static class Patch_Pawn_CallTracker_DoCall
    {
        static bool Prefix(Pawn_CallTracker __instance, bool forceAggressive = false)
        {
            var pawn = __instance?.pawn;
            if (pawn == null) return true; // 원본 실행

            // ── 공격적 콜(angry) 처리 ──
            if (forceAggressive)
            {
                SoundDef formAngry;
                if (ShapeshiftVoiceUtility.TryGetAngry(pawn, out formAngry))
                {
                    ShapeshiftVoiceUtility.PlayOneShotAt(pawn, formAngry, volumeFactor: 1f);
                    return false; // 원본 skip
                }
                return true; // 캐시 없음 → 바닐라 실행
            }

            // ── 일반 콜(call) 처리 ──
            SoundDef formCall;
            if (ShapeshiftVoiceUtility.TryGetCall(pawn, out formCall))
            {
                ShapeshiftVoiceUtility.PlayOneShotAt(pawn, formCall, volumeFactor: 1f);
                return false; // 원본 skip
            }

            return true; // 변환 없음 → 바닐라 실행
        }
    }
}
