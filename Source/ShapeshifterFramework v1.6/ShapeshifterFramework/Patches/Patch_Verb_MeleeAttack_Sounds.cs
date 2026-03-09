// ShapeshifterFramework | Patches | Patch_Verb_MeleeAttack_Sounds.cs
// 목적 : 폰이 무기 없이 맨손(또는 폼의 도구)으로 근접 공격을 할 때 발생하는 타격음/빗나감 소리를 폼 전용 사운드로 교체.
// 용도 : HitPawn, HitBuilding, Miss 3가지 바닐라 메서드에 개입하며, 폰이 실제 장비(CompEquippable)를 들고 타격할 때는 바닐라 무기 소리를 존중하여 개입하지 않음.
// 참고 : EquipmentSource는 바디 verb에서도 폰 자신을 반환(Pawn→ThingWithComps)하므로, EquipmentCompSource(CompEquippable)로 무기 여부를 판별해야 함.

using HarmonyLib;
using RimWorld;
using Verse;

namespace ShapeshifterFramework.Patches
{
    /// <summary>변신 폼의 근접 사운드를 우선 적용.</summary>
    [HarmonyPatch]
    public static class Patch_Verb_MeleeAttack_Sounds
    {
        // 무기 verb인지 판별 (CompEquippable 소유 verb만 무기)
        private static bool IsWeaponVerb(Verb verb)
        {
            return verb?.EquipmentCompSource != null;
        }

        // HitPawn
        [HarmonyPatch(typeof(Verb_MeleeAttack), "SoundHitPawn")]
        [HarmonyPostfix]
        static void Postfix_HitPawn(Verb_MeleeAttack __instance, ref SoundDef __result)
        {
            if (__instance == null || IsWeaponVerb(__instance)) return;

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
            if (__instance == null || IsWeaponVerb(__instance)) return;

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
            if (__instance == null || IsWeaponVerb(__instance)) return;

            var pawn = __instance.CasterPawn;
            var form = pawn?.TryGetComp<ShapeshifterFramework.Comps.CompShapeshifter>()?.currentForm;
            if (form != null && form.soundMeleeMiss != null)
                __result = form.soundMeleeMiss;
        }
    }
}
