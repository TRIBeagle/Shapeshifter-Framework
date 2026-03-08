// ShapeshifterFramework | Patches | Patch_Ability_GizmoDisabled.cs
// 목적 : 비폭력(Non-violent) 폰이 변신 어빌리티를 사용할 수 있도록 바닐라의 폭력 결격 차단을 해제.
// 용도 : 바닐라 Ability.GizmoDisabled()가 verbProps.violent와 무관하게 비폭력 폰을 차단할 수 있으므로,
//        CompProperties_AbilityShiftTarget를 포함한 shift 어빌리티에 대해서만 차단을 해제함.

using HarmonyLib;
using RimWorld;
using ShapeshifterFramework.Comps;
using Verse;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch(typeof(Ability), nameof(Ability.GizmoDisabled))]
    internal static class Patch_Ability_GizmoDisabled
    {
        static void Postfix(Ability __instance, ref bool __result, ref string reason)
        {
            // 비활성 상태가 아니면 건드리지 않음
            if (!__result) return;

            // 비폭력 폰이 아니면 건드리지 않음
            var pawn = __instance.pawn;
            if (pawn == null || !pawn.WorkTagIsDisabled(WorkTags.Violent)) return;

            // shift 어빌리티인지 확인
            var comps = __instance.def?.comps;
            if (comps == null) return;

            bool isShiftAbility = false;
            for (int i = 0; i < comps.Count; i++)
            {
                if (comps[i] is CompProperties_AbilityShiftTarget)
                {
                    isShiftAbility = true;
                    break;
                }
            }

            if (!isShiftAbility) return;

            // shift 어빌리티의 violence 차단 해제
            __result = false;
            reason = null;
        }
    }
}
