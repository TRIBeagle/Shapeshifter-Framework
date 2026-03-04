// ShapeshifterFramework | Patches | Patch_ThoughtWorker_Precept_...
// 목적 : 변신 폼(동물, 괴물 등)일 때, 이데올로기(Ideology) 사상에 의한 '신체 노출(알몸)' 무드 디버프를 무효화.
// 용도 : 사상 기반 노출 판정(HasUncovered...)에 Postfix로 개입하여, 폼에 suppressIdeologyUncoveredThoughts가 활성화되어 있다면 무조건 노출 상태가 아닌 것(false)으로 덮어씌움.

using HarmonyLib;
using RimWorld;
using ShapeshifterFramework.Comps;
using Verse;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch(typeof(ThoughtWorker_Precept_GroinUncovered), nameof(ThoughtWorker_Precept_GroinUncovered.HasUncoveredGroin))]
    public static class Patch_ThoughtWorker_Precept_GroinUncovered
    {
        static void Postfix(Pawn p, ref bool __result)
        {
            var comp = p?.TryGetComp<CompShapeshifter>();
            var form = (comp != null && comp.isTransformed) ? comp.currentForm as ShapeshiftFormDef : null;
            if (form != null && form.suppressIdeologyUncoveredThoughts)
                __result = false;
        }
    }
}
