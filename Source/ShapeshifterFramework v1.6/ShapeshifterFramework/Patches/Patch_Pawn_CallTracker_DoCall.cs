// .NET Framework 4.8 / C# 7.3
using System;
using HarmonyLib;
using Verse;
using RimWorld;
using ShapeshifterFramework.Utilities;

namespace ShapeshifterFramework.Patches
{
    /// <summary>
    /// 평상시 콜 보이스를 변신 폼 soundCall로 대체.
    /// - 공격적 콜(soundAngry)은 요구사항에 없으므로 바닐라 유지.
    /// - 변신 전/후 자동 복귀(캐시 해제).
    /// </summary>
    [HarmonyPatch(typeof(Pawn_CallTracker), nameof(Pawn_CallTracker.DoCall))]
    internal static class Patch_Pawn_CallTracker_DoCall
    {
        // 서명: public void DoCall(bool forceAggressive = false)
        // 앵커: Pawn_CallTracker.DoCall 내부에서 LifeStageUtility.PlayNearestLifestageSound 호출. :contentReference[oaicite:4]{index=4}
        static bool Prefix(Pawn_CallTracker __instance, bool forceAggressive = false)
        {
            var pawn = __instance?.pawn;
            if (pawn == null) return true; // 원본

            // 공격적 콜은 원본 사용(요구범위: soundCall만 치환)
            if (forceAggressive) return true;

            SoundDef formCall;
            if (ShapeshiftVoiceHelper.TryGetCall(pawn, out formCall))
            {
                // 바닐라 IdleCallVolumeFactor를 그대로 적용하려면,
                // 내부 계산을 그대로 재현하기 어렵기 때문에 안전하게 1f를 기본값으로.
                // 바닐라 DoCall은 Idle일 때 volumeFactor를 전달함 (코멘트용 앵커). :contentReference[oaicite:5]{index=5}
                ShapeshiftVoiceHelper.PlayOneShotAt(pawn, formCall, volumeFactor: 1f);
                return false; // 원본 skip
            }

            return true; // 캐시가 없으면 바닐라
        }
    }
}
