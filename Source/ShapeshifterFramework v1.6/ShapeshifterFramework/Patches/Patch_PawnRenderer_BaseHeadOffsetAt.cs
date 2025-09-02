// Patch_PawnRenderer_BaseHeadOffsetAt.cs  (C# 7.3)
using HarmonyLib;
using ShapeshifterFramework.Comps;
using ShapeshifterFramework.Utilities;
using UnityEngine;
using Verse;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch(typeof(PawnRenderer), "BaseHeadOffsetAt")]
    public static class Patch_PawnRenderer_BaseHeadOffsetAt
    {
        [HarmonyPostfix, HarmonyPriority(Priority.Last)]
        static void Postfix(PawnRenderer __instance, Rot4 rotation, ref Vector3 __result)
        {
            Pawn pawn = ShapeshiftReflectionCache.GetPawn(__instance);
            if (pawn == null) return;

            // 1) 바닐라 sqrt(bodySizeFactor) 보정 유지
            var ls = pawn.ageTracker != null ? pawn.ageTracker.CurLifeStage : null;
            float vanilla = Mathf.Max(0.01f, ls != null ? ls.bodySizeFactor : 1f);
            float target = Mathf.Max(0.01f, ShapeshiftSizeFactorResolver.Effective(pawn).bodySizeFactor);
            if (!Mathf.Approximately(target, vanilla))
            {
                float mul = Mathf.Sqrt(target / vanilla);
                __result = new Vector3(__result.x * mul, __result.y, __result.z * mul);
            }

            // 2) 헤드 오프셋 추가(회전 반영)
            var comp = pawn.TryGetComp<CompShapeshifter>();
            var form = comp != null ? comp.currentForm : null;
            if (comp == null || !comp.isTransformed || form == null) return;

            Vector2 add2 = form.headOffset.HasValue ? form.headOffset.Value : Vector2.zero;
            if (add2 == Vector2.zero) return;

            Vector3 add = new Vector3(add2.x, 0f, add2.y);
            if (rotation == Rot4.West) add.x = -add.x;

            __result = new Vector3(__result.x + add.x, __result.y, __result.z + add.z);
        }
    }
}
