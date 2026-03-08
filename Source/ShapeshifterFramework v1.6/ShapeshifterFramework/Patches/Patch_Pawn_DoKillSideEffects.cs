// ShapeshifterFramework | Patches | Patch_Pawn_DoKillSideEffects.cs
// 목적 : 폰이 사망할 때 출력되는 바닐라 종족 사망음 대신 폼에 지정된 전용 사망음(DeathVoice)을 재생.
// 용도 : Harmony MethodReplacer를 사용하여 바닐라 LifeStageUtility 사운드 호출부를 모드 전용 Wrapper 메서드로 통째로 치환하며, 타 모드와의 충돌을 막는 Fail-safe 구조를 갖춤.

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
    public static class Patch_Pawn_DoKillSideEffects
    {
        private static bool _reported = false;

        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo vanillaPlaySoundMethod = AccessTools.Method(typeof(LifeStageUtility), "PlayNearestLifestageSound");
            MethodInfo ourWrapperMethod = AccessTools.Method(typeof(Patch_Pawn_DoKillSideEffects), nameof(PlayNearestLifestageSoundWrapper));

            // 메서드 미발견 시 원본 유지
            if (vanillaPlaySoundMethod == null || ourWrapperMethod == null)
            {
                if (!_reported)
                {
                    Log.Warning("[SSF] DoKillSideEffects patch failed: cannot resolve sound method. Keeping vanilla.");
                    _reported = true;
                }
                return instructions;
            }

            // 메서드 호출 치환
            return instructions.MethodReplacer(vanillaPlaySoundMethod, ourWrapperMethod);
        }

        // 사운드 Wrapper
        public static void PlayNearestLifestageSoundWrapper(Pawn pawn, Func<LifeStageAge, SoundDef> lifestageGetter, Func<GeneDef, SoundDef> geneGetter, Func<MutantDef, SoundDef> mutantGetter, float volumeFactor)
        {
            // 1. 폼 사운드 재생
            if (ShapeshiftVoiceUtility.TryGetDeath(pawn, out var customSound))
            {
                ShapeshiftVoiceUtility.PlayOneShotAt(pawn, customSound, 1f);
                return;
            }

            // 2. 바닐라 폴백
            LifeStageUtility.PlayNearestLifestageSound(pawn, lifestageGetter, geneGetter, mutantGetter, volumeFactor);
        }
    }
}