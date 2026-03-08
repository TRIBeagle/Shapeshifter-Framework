// ShapeshifterFramework | Patches | Patch_Pawn_MeleeVerbs_TryGetUpdatedMeleeVerb.cs
// 목적 : 실제 근접 전투에서 바닐라 Pawn_MeleeVerbs가 폼의 근접 도구를 무시하는 문제 해결.
// 용도 : Pawn_MeleeVerbs.GetUpdatedAvailableVerbsList()는 pawn.verbTracker에서만 verb를 수집하기 때문에
//        shapeshiftVerbTracker의 폼 근접 도구가 선택되지 않음.
//        TryGetMeleeVerb Postfix에서 replaceNativeTools=true이면 폼 도구로 강제 교체,
//        false이면 power 비교 후 더 강한 쪽을 선택.

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
        static void Postfix(Pawn_MeleeVerbs __instance, ref Verb __result, Thing target)
        {
            try
            {
                var pawn = __instance.Pawn;
                if (pawn == null) return;

                var comp = pawn.TryGetComp<CompShapeshifter>();
                if (comp == null || !comp.isTransformed) return;

                var form = comp.currentForm;
                if (form == null || form.tools == null || form.tools.Count == 0) return;

                var vt = comp.ShapeshiftVerbTracker;
                if (vt == null) return;

                var verbs = vt.AllVerbs;
                var bestFormMelee = FindBestFormMelee(verbs);
                if (bestFormMelee == null) return;

                bool replaceNative = form.replaceNativeTools.HasValue && form.replaceNativeTools.Value;

                if (replaceNative)
                {
                    // 교체 모드: 항상 폼 근접 도구 사용
                    ShapeshiftDiagnostics.Info($"TryGetMeleeVerb(replace): using form melee tool={((bestFormMelee as Verb_MeleeAttack)?.tool?.label ?? "?")} instead of vanilla={(((__result as Verb_MeleeAttack)?.tool?.label) ?? "null")}");
                    __result = bestFormMelee;
                    return;
                }

                // 추가 모드: 바닐라 결과와 power 비교
                if (__result == null)
                {
                    __result = bestFormMelee;
                    return;
                }

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
