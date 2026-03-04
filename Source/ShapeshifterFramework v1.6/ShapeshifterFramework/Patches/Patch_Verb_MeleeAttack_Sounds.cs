// ShapeshifterFramework | Patches | Patch_Verb_MeleeAttack_Sounds.cs
// 목적 : 폰이 무기 없이 맨손(또는 폼의 무기)으로 근접 공격을 할 때 발생하는 타격음/빗나감 소리를 폼 전용 사운드로 교체.
// 용도 : HitPawn, HitBuilding, Miss 3가지 바닐라 메서드에 개입하며, 폰이 실제 장비(EquipmentSource)를 들고 타격할 때는 바닐라 무기 소리를 존중하여 개입하지 않음.

using HarmonyLib;
using RimWorld;
using Verse;

namespace ShapeshifterFramework.Patches
{
    /// <summary>
    /// 변신 폼에서 지정한 근접 사운드를 우선 적용한다.
    /// - Verb_MeleeAttack 의 사운드 선택 메서드 3종(Postfix)에서 __result 대체
    /// - 원본 장비/툴/재질 우선 규칙은 그대로 두고, "폼에 지정되어 있으면"만 덮어씀
    /// - C# 7.3 호환 (지역 함수/nullable 참조형 미사용)
    /// </summary>
    [HarmonyPatch]
    public static class Patch_Verb_MeleeAttack_Sounds
    {
        // ──────────────────────────────────────────────────────────────────────
        // HitPawn
        [HarmonyPatch(typeof(Verb_MeleeAttack), "SoundHitPawn")]
        [HarmonyPostfix]
        static void Postfix_HitPawn(Verb_MeleeAttack __instance, ref SoundDef __result)
        {
            // 무기 들고 있으면 바닐라/무기 사운드 유지
            if (__instance?.EquipmentSource != null) return;

            var pawn = __instance.CasterPawn;
            var form = pawn?.TryGetComp<ShapeshifterFramework.Comps.CompShapeshifter>()?.currentForm;
            if (form != null && form.soundMeleeHitPawn != null)
                __result = form.soundMeleeHitPawn;
        }

        // HitBuilding
        [HarmonyPatch(typeof(Verb_MeleeAttack), "SoundHitBuilding")]
        [HarmonyPostfix]
        static void Postfix_HitBuilding(Verb_MeleeAttack __instance, ref SoundDef __result)
        {
            if (__instance?.EquipmentSource != null) return;

            var pawn = __instance.CasterPawn;
            var form = pawn?.TryGetComp<ShapeshifterFramework.Comps.CompShapeshifter>()?.currentForm;
            if (form != null && form.soundMeleeHitBuilding != null)
                __result = form.soundMeleeHitBuilding;
        }

        // Miss
        [HarmonyPatch(typeof(Verb_MeleeAttack), "SoundMiss")]
        [HarmonyPostfix]
        static void Postfix_Miss(Verb_MeleeAttack __instance, ref SoundDef __result)
        {
            if (__instance?.EquipmentSource != null) return;

            var pawn = __instance.CasterPawn;
            var form = pawn?.TryGetComp<ShapeshifterFramework.Comps.CompShapeshifter>()?.currentForm;
            if (form != null && form.soundMeleeMiss != null)
                __result = form.soundMeleeMiss;
        }
    }
}
