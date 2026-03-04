// ShapeshifterFramework | Patches | Patch_PawnCapacityUtility_CalculateCapacityLevel.cs
// 목적 : 폰의 실제 신체 능력(Capacity: 조작, 이동, 시각 등) 수치를 계산할 때 변신 폼의 보정치(capMods)를 반영.
// 용도 : 바닐라 계산이 완료된 직후(Postfix), 폼에 설정된 가산(Offset)을 먼저 더하고 배수(Factor)를 곱하여 최종 수치(__result)를 변조함.

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

            var comp = pawn.TryGetComp<CompShapeshifter>();
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
