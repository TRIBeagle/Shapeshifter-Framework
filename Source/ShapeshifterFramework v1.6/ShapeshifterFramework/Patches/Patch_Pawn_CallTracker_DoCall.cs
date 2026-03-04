// ShapeshifterFramework | Patches | Patch_Pawn_CallTracker_DoCall.cs
// 목적 : 폰이 무작위로 내거나 전투 중 내뱉는 소리(Call, Angry Call)를 바닐라 종족음에서 변신 폼의 짐승/기계음 등으로 교체.
// 용도 : DoCall 메서드에 Prefix로 개입하여, 캐싱된 폼 전용 사운드가 존재하면 이를 재생하고 원본 바닐라 로직을 스킵함.

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
