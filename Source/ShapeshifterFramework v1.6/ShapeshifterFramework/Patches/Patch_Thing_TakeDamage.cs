// ShapeshifterFramework | Patches | Patch_Thing_TakeDamage.cs
// 목적 : 변신 중 맨손 공격 시 상처 라벨에서 종족명("인간") 제거.
// 용도 : 바닐라는 DamageInfo.Weapon = CasterPawn.def (예: Human)로 설정하여
//        상처 라벨이 "인간 teeth"로 표시됨. 변신 중이면 weapon을 null로 비워
//        "teeth"만 표시되도록 함. damageSourceDef가 명시적으로 지정된 경우 해당 ThingDef 사용.

using HarmonyLib;
using ShapeshifterFramework.Utilities;
using System.Reflection;
using Verse;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch(typeof(Thing), nameof(Thing.TakeDamage))]
    internal static class Patch_Thing_TakeDamage
    {
        private static readonly FieldInfo weaponIntField = AccessTools.Field(typeof(DamageInfo), "weaponInt");

        static Patch_Thing_TakeDamage()
        {
            if (weaponIntField == null)
                Log.Warning("[SSF] Reflection failed: DamageInfo.weaponInt not found. TakeDamage wound-label patch disabled — vanilla version mismatch?");
        }

        static void Prefix(ref DamageInfo dinfo)
        {
            if (weaponIntField == null) return;

            var instigator = dinfo.Instigator;
            if (instigator == null) return;

            var pawn = instigator as Pawn;
            if (pawn == null) return;

            if (!ShapeshiftRegistry.TryGet(pawn, out var comp, out var form)) return;
            if (form.tools == null || form.tools.Count == 0) return;

            // 장비로 공격 중이면 건드리지 않음
            if (dinfo.Weapon != null && dinfo.Weapon != pawn.def) return;

            // damageSourceDef가 명시적으로 지정되면 해당 ThingDef, 아니면 null (종족명 제거)
            object boxed = dinfo;
            weaponIntField.SetValue(boxed, form.damageSourceDef);
            dinfo = (DamageInfo)boxed;
        }
    }
}
