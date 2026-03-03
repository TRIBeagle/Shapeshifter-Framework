using HarmonyLib;
using RimWorld;
using ShapeshifterFramework.Utilities;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch(typeof(Pawn), "DoKillSideEffects")]
    public static class Patch_Pawn_DoKillSideEffects_PlayDeathVoiceSound
    {
        private static bool _reported = false;

        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo vanillaPlaySoundMethod = AccessTools.Method(typeof(LifeStageUtility), "PlayNearestLifestageSound");
            MethodInfo ourWrapperMethod = AccessTools.Method(typeof(Patch_Pawn_DoKillSideEffects_PlayDeathVoiceSound), nameof(PlayNearestLifestageSoundWrapper));

            // 타겟 메서드 시그니처가 변했거나 로드 순서 문제 발생 시 원본 유지 (Fail-safe)
            if (vanillaPlaySoundMethod == null || ourWrapperMethod == null)
            {
                if (!_reported)
                {
                    Log.Warning("[SSF] DoKillSideEffects patch failed: cannot resolve sound method. Keeping vanilla.");
                    _reported = true;
                }
                return instructions;
            }

            // Harmony 제공 유틸: 동일 시그니처 메서드 호출을 안전하게 치환
            return instructions.MethodReplacer(vanillaPlaySoundMethod, ourWrapperMethod);
        }

        // 바닐라 사운드 대신 호출될 Wrapper 함수
        public static void PlayNearestLifestageSoundWrapper(Pawn pawn, Func<LifeStageAge, SoundDef> lifestageGetter, Func<GeneDef, SoundDef> geneGetter, Func<MutantDef, SoundDef> mutantGetter, float volumeFactor)
        {
            // 1. 변신 폰이라면 커스텀 사운드를 재생하고 즉시 종료 (바닐라 소리는 나지 않음)
            if (ShapeshiftVoiceHelper.TryGetDeath(pawn, out var customSound))
            {
                ShapeshiftVoiceHelper.PlayOneShotAt(pawn, customSound, 1f);
                return;
            }

            // 2. 변신 폰이 아니라면 원래 바닐라 함수를 정상적으로 실행
            LifeStageUtility.PlayNearestLifestageSound(pawn, lifestageGetter, geneGetter, mutantGetter, volumeFactor);
        }
    }
}