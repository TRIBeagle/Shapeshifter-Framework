// ShapeshifterFramework | Patches | Patch_DamageWorker_AddInjury_PlayWoundedVoiceSound.cs
// 목적 : 폰이 데미지를 입었을 때 바닐라의 종족 부상음 대신 폼 전용 피격음(Wounded)을 재생.
// 용도 : 데미지 적용 자체는 건드리지 않고, private 사운드 재생 메서드인 PlayWoundedVoiceSound에만 Prefix로 개입하여 폼 사운드가 있으면 원본 사운드 재생을 스킵(return false)함.

using HarmonyLib;
using ShapeshifterFramework.Utilities;
using System;
using System.Reflection;
using Verse;

namespace ShapeshifterFramework.Patches
{
    /// <summary>부상 시 변신 폼의 wounded 사운드로 교체.</summary>
    [HarmonyPatch]
    internal static class Patch_DamageWorker_AddInjury_PlayWoundedVoiceSound
    {
        // 대상 메서드: private void PlayWoundedVoiceSound(DamageInfo dinfo, Pawn pawn)
        static MethodBase TargetMethod()
        {
            var t = typeof(DamageWorker_AddInjury);
            return AccessTools.Method(t, "PlayWoundedVoiceSound", new Type[] { typeof(DamageInfo), typeof(Pawn) });
        }

        static bool Prefix(DamageInfo dinfo, Pawn pawn)
        {
            if (pawn == null) return true;

            SoundDef formWounded;
            if (ShapeshiftVoiceUtility.TryGetWounded(pawn, out formWounded))
            {
                // 바닐라 LifeStageUtility와 동일하게 CurLifeStage pitch/vol 적용 처리
                ShapeshiftVoiceUtility.PlayOneShotAt(pawn, formWounded, volumeFactor: 1f);
                return false; // 원본 사운드 스킵
            }

            return true; // 변신 사운드가 없으면 바닐라 동작 유지
        }
    }
}
