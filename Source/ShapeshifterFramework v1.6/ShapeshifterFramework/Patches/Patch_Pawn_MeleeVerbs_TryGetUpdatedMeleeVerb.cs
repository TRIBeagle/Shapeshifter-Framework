// ShapeshifterFramework | Patches | Patch_Pawn_MeleeVerbs_TryGetUpdatedMeleeVerb.cs
// 목적 : 바닐라 Pawn_MeleeVerbs가 폼의 근접 도구를 무시하는 문제 해결.
// 용도 : pawn.verbTracker에서 네이티브 근접이 제거된 상태이므로, 바닐라 ChooseMeleeVerb가
//        빈 리스트를 받아 에러를 출력하기 전에 Prefix에서 폼 verb를 직접 반환.
//        replaceNativeTools=false일 때는 바닐라 실행 후 Postfix에서 power 비교.

using HarmonyLib;
using RimWorld;
using ShapeshifterFramework.Comps;
using ShapeshifterFramework.Utilities;
using System.Collections.Generic;
using Verse;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch(typeof(Pawn_MeleeVerbs), nameof(Pawn_MeleeVerbs.TryGetMeleeVerb))]
    internal static class Patch_Pawn_MeleeVerbs_TryGetMeleeVerb
    {
        static bool Prefix(Pawn_MeleeVerbs __instance, ref Verb __result, Thing target)
        {
            try
            {
                var pawn = __instance.Pawn;
                if (pawn == null) return true;

                if (!ShapeshiftRegistry.TryGet(pawn, out var comp, out var form)) return true;
                if (form.tools == null || form.tools.Count == 0) return true;

                bool replaceNative = form.replaceNativeTools.HasValue && form.replaceNativeTools.Value;

                // replaceNativeTools=true → 네이티브가 제거되어 바닐라가 빈 리스트 에러 냄.
                // Prefix에서 직접 폼 verb 반환하여 바닐라 스킵.
                if (replaceNative)
                {
                    var vt = comp.ShapeshiftVerbTracker;
                    if (vt == null) return true;

                    var bestMelee = FindBestFormMelee(vt.AllVerbs);
                    if (bestMelee != null)
                    {
                        __result = bestMelee;
                        return false; // 바닐라 스킵
                    }
                }

                // replaceNativeTools=false → 바닐라 실행 후 Postfix에서 비교
            }
            catch (System.Exception e)
            {
                Log.Warning($"[SSF] TryGetMeleeVerb Prefix failed: {e}");
            }
            return true;
        }

        static void Postfix(Pawn_MeleeVerbs __instance, ref Verb __result, Thing target)
        {
            try
            {
                // replaceNativeTools=true는 Prefix에서 처리 완료
                // 여기는 replaceNativeTools=false (추가 모드)만 처리
                var pawn = __instance.Pawn;
                if (pawn == null) return;

                if (!ShapeshiftRegistry.TryGet(pawn, out var comp, out var form)) return;
                if (form.tools == null || form.tools.Count == 0) return;

                // replaceNativeTools=true는 Prefix에서 이미 처리됨
                if (form.replaceNativeTools.HasValue && form.replaceNativeTools.Value) return;

                var vt = comp.ShapeshiftVerbTracker;
                if (vt == null) return;

                var bestFormMelee = FindBestFormMelee(vt.AllVerbs);
                if (bestFormMelee == null) return;

                if (__result == null)
                {
                    __result = bestFormMelee;
                    return;
                }

                // power 비교: 폼 도구가 더 강하면 교체
                var vanillaMelee = __result as Verb_MeleeAttack;
                var formMelee = bestFormMelee as Verb_MeleeAttack;
                float vanillaPower = (vanillaMelee?.tool != null) ? vanillaMelee.tool.power : 0f;
                float formPower = (formMelee?.tool != null) ? formMelee.tool.power : 0f;

                if (formPower > vanillaPower)
                    __result = bestFormMelee;
            }
            catch (System.Exception e)
            {
                Log.Warning($"[SSF] TryGetMeleeVerb Postfix failed: {e}");
            }
        }

        private static Verb FindBestFormMelee(List<Verb> verbs)
        {
            Verb best = null;
            float bestPower = -1f;
            for (int i = 0; i < verbs.Count; i++)
            {
                var v = verbs[i];
                if (v == null || v.verbProps == null) continue;
                if (!v.verbProps.IsMeleeAttack) continue;

                var vma = v as Verb_MeleeAttack;
                float power = (vma?.tool != null) ? vma.tool.power : 0f;
                if (best == null || power > bestPower)
                {
                    best = v;
                    bestPower = power;
                }
            }
            return best;
        }
    }
}
