// ShapeshifterFramework | Patches | Patch_DamageWorker_AddInjury_PlayWoundedVoiceSound.cs
// 목적  : Pawn 부상음 재생을 변신 폼의 soundWounded로 대체.
// 용도  : DamageWorker_AddInjury.ApplyToPawn(...) 경로에서 호출되는 private 메서드
//         PlayWoundedVoiceSound를 후킹하여, 변신 중이면 폼 고유 사운드를 재생.
// 변경  : 2025-09-23 v1.0 — 프로젝트 주석 규칙 적용.

using HarmonyLib;
using ShapeshifterFramework.Utilities;
using System;
using System.Reflection;
using Verse;

namespace ShapeshifterFramework.Patches
{
    /// <summary>
    /// Pawn이 부상했을 때 변신 폼에 정의된 wounded 사운드로 교체.
    /// - DamageWorker_AddInjury.ApplyToPawn → damageResult.wounded 분기 직후 호출.
    /// - 원본 효과(데미지 적용, 이펙트 등)는 그대로 유지하고, 사운드만 대체한다.
    /// </summary>
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
