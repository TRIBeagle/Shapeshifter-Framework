using HarmonyLib;
using RimWorld;
using Verse;
using ShapeshifterFramework.Utilities;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection;
using System;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch(typeof(Pawn), "DoKillSideEffects")]
    public static class Patch_Pawn_DoKillSideEffects_PlayDeathVoiceSound
    {
        // 1. Prefix: 변신 폰의 커스텀 사망 사운드를 먼저 재생 (바닐라 흐름은 방해하지 않음)
        static void Prefix(Pawn __instance, DamageInfo? dinfo, Hediff exactCulprit, bool spawned)
        {
            if (!spawned) return;

            if (ShapeshiftVoiceHelper.TryGetDeath(__instance, out SoundDef sound))
            {
                ShapeshiftVoiceHelper.PlayOneShotAt(__instance, sound, 1f);
            }
        }

        // 2. Transpiler: 바닐라 사운드 재생 함수를 우리 Wrapper 함수로 바꿔치기
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo vanillaPlaySoundMethod = AccessTools.Method(typeof(LifeStageUtility), "PlayNearestLifestageSound");
            MethodInfo ourWrapperMethod = AccessTools.Method(typeof(Patch_Pawn_DoKillSideEffects_PlayDeathVoiceSound), "PlayNearestLifestageSoundWrapper");

            foreach (var code in instructions)
            {
                // 바닐라가 사운드를 재생하려고 하면
                if (code.Calls(vanillaPlaySoundMethod))
                {
                    // 우리가 만든 Wrapper 함수를 대신 호출하도록 목적지 변경!
                    yield return new CodeInstruction(OpCodes.Call, ourWrapperMethod);
                }
                else
                {
                    yield return code;
                }
            }
        }

        // 3. Wrapper 함수: 바닐라 사운드를 진짜로 틀지 말지 결정하는 수문장
        public static void PlayNearestLifestageSoundWrapper(Pawn pawn, Func<LifeStageAge, SoundDef> lifestageGetter, Func<GeneDef, SoundDef> geneGetter, Func<MutantDef, SoundDef> mutantGetter, float volumeFactor)
        {
            // 만약 변신 폰이라서 이미 Prefix에서 소리를 틀었다면? -> 바닐라 소리는 무시(Mute)!
            if (ShapeshiftVoiceHelper.TryGetDeath(pawn, out _))
            {
                return;
            }

            // 변신 폰이 아니라면 원래 바닐라 함수를 정상적으로 실행
            LifeStageUtility.PlayNearestLifestageSound(pawn, lifestageGetter, geneGetter, mutantGetter, volumeFactor);
        }
    }
}