// Patch_PawnCapacityUtility_CalculateCapacityLevel.cs
// 목적: 캐퍼시티 계산에 폼 capMods(가산/배수)를 반영.
// 용도: Postfix에서 offset 누적 후 factor 곱. 이후 바닐라 클램프 흐름 유지.
// 주의: 변신 중 아닐 때/대상 capMods 없을 때는 스킵.

using HarmonyLib;
using ShapeshifterFramework.Comps;
using System.Collections.Generic;
using Verse;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch(typeof(PawnCapacityUtility), nameof(PawnCapacityUtility.CalculateCapacityLevel),
        new System.Type[] {
            typeof(HediffSet),
            typeof(PawnCapacityDef),
            typeof(List<PawnCapacityUtility.CapacityImpactor>),
            typeof(bool)
        })]
    public static class Patch_PawnCapacityUtility_CalculateCapacityLevel
    {
        static void Postfix(
            [HarmonyArgument(0)] HediffSet diffSet,
            [HarmonyArgument(1)] PawnCapacityDef capacity,
            ref float __result)
        {
            if (diffSet == null) return;
            Pawn pawn = diffSet.pawn;
            if (pawn == null) return;

            var comp = pawn.GetComp<CompShapeshifter>();
            if (comp == null || !comp.isTransformed) return;

            var form = comp.currentForm;
            if (form == null || form.capMods == null || form.capMods.Count == 0) return;

            float factor = 1f;
            float offset = 0f;

            // 해당 capacity 대상 capMods만 모아 합산
            for (int i = 0; i < form.capMods.Count; i++)
            {
                var m = form.capMods[i];
                if (m == null || m.capacity != capacity) continue;

                offset += m.offset;        // 가산은 누적
                factor *= m.postFactor;    // 0f 포함, 항상 곱함
            }

            // 최종 반영: + → ×
            __result = (__result + offset) * factor;
        }
    }
}
