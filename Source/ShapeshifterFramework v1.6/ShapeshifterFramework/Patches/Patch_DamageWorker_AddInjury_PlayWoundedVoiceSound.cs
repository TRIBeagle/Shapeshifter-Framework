// .NET Framework 4.8 / C# 7.3
using System;
using System.Reflection;
using HarmonyLib;
using Verse;
using RimWorld;
using ShapeshifterFramework.Utilities;

namespace ShapeshifterFramework.Patches
{
    /// <summary>
    /// 부상음(wounded) 재생을 변신 폼 soundWounded로 대체.
    /// - DamageWorker_AddInjury.ApplyToPawn(...) 경로에서 호출되는 private 메서드 훅.
    /// - 원본 효과/이펙트/모테 등은 그대로 진행(사운드만 대체).
    /// </summary>
    [HarmonyPatch]
    internal static class Patch_DamageWorker_AddInjury_PlayWoundedVoiceSound
    {
        // 대상: private void PlayWoundedVoiceSound(DamageInfo dinfo, Pawn pawn)
        // 호출 앵커: ApplyToPawn 내 damageResult.wounded 분기 직후 호출. :contentReference[oaicite:6]{index=6}
        static MethodBase TargetMethod()
        {
            var t = typeof(DamageWorker_AddInjury);
            return AccessTools.Method(t, "PlayWoundedVoiceSound", new Type[] { typeof(DamageInfo), typeof(Pawn) });
        }

        static bool Prefix(DamageInfo dinfo, Pawn pawn)
        {
            if (pawn == null) return true;

            SoundDef formWounded;
            if (ShapeshiftVoiceHelper.TryGetWounded(pawn, out formWounded))
            {
                // 바닐라 LifeStageUtility는 오버라이드 시 CurLifeStage pitch/vol을 적용한다. 동일하게 처리. :contentReference[oaicite:7]{index=7}
                ShapeshiftVoiceHelper.PlayOneShotAt(pawn, formWounded, volumeFactor: 1f);
                return false; // 원본 사운드 스킵
            }

            return true;
        }
    }
}
