// ShapeshifterFramework | Patches | Patch_Pawn_MeleeVerbs_TryGetUpdatedMeleeVerb.cs
// 목적 : 폼 근접 도구가 pawn.verbTracker에 직접 주입되지 못한 경우를 대비한 안전 장치.
// 용도 : RefreshPawn에서 폼 도구가 pawn.verbTracker에 주입되므로 바닐라가 자연 선택하지만,
//        만약 주입이 실패했을 때 shapeshiftVerbTracker에서 폼 근접 도구를 가져와 보완함.

using HarmonyLib;
using RimWorld;
using ShapeshifterFramework.Comps;
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
                // 바닐라가 이미 유효한 근접 verb를 골랐으면 스킵
                if (__result != null) return;

                var pawn = __instance.Pawn;
                if (pawn == null) return;

                var comp = pawn.TryGetComp<CompShapeshifter>();
                if (comp == null || !comp.isTransformed) return;

                var form = comp.currentForm;
                if (form == null || form.tools == null || form.tools.Count == 0) return;

                // 바닐라가 null을 반환했을 때만 shapeshiftVerbTracker에서 폴백
                var vt = comp.ShapeshiftVerbTracker;
                if (vt == null) return;

                var verbs = vt.AllVerbs;
                for (int i = 0; i < verbs.Count; i++)
                {
                    var v = verbs[i];
                    if (v != null && v.verbProps != null && v.verbProps.IsMeleeAttack)
                    {
                        __result = v;
                        return;
                    }
                }
            }
            catch (System.Exception e)
            {
                Log.Warning($"[SSF] TryGetMeleeVerb Postfix failed: {e}");
            }
        }
    }
}
