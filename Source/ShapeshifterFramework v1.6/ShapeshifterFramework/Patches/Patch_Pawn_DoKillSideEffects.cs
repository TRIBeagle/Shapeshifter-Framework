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
            if (ShapeshiftVoiceUtility.TryGetDeath(pawn, out var customSound))
            {
                ShapeshiftVoiceUtility.PlayOneShotAt(pawn, customSound, 1f);
                return;
            }

            // 2. 변신 폰이 아니라면 원래 바닐라 함수를 정상적으로 실행
            LifeStageUtility.PlayNearestLifestageSound(pawn, lifestageGetter, geneGetter, mutantGetter, volumeFactor);
        }
    }
}