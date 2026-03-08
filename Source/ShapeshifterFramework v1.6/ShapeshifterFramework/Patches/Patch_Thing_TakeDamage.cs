// ShapeshifterFramework | Patches | Patch_Thing_TakeDamage.cs
// 목적 : 변신 중 근접 공격 시 상처 라벨에 폼의 종족명이 표시되도록 DamageInfo.Weapon 교체.
// 용도 : 바닐라는 맨손 공격 시 DamageInfo.Weapon = CasterPawn.def (예: "Human")을 사용하여
//        상처 라벨이 "인간 teeth"로 표시됨. 변신 폼에 damageSourceDef가 지정되어 있으면
//        이를 Weapon으로 교체하여 "Warg teeth" 등으로 표시되도록 함.

using HarmonyLib;
using ShapeshifterFramework.Comps;
using System.Reflection;
using Verse;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch(typeof(Thing), nameof(Thing.TakeDamage))]
    internal static class Patch_Thing_TakeDamage
    {
        private static readonly FieldInfo weaponIntField = AccessTools.Field(typeof(DamageInfo), "weaponInt");

        static void Prefix(ref DamageInfo dinfo)
        {
            if (weaponIntField == null) return;

            var instigator = dinfo.Instigator;
            if (instigator == null) return;

            var pawn = instigator as Pawn;
            if (pawn == null) return;

            var comp = pawn.TryGetComp<CompShapeshifter>();
            if (comp == null || !comp.isTransformed) return;

            var form = comp.currentForm;
            if (form == null || form.damageSourceDef == null) return;

            // 장비로 공격 중이면 건드리지 않음 (무기의 ThingDef가 이미 올바름)
            if (dinfo.Weapon != null && dinfo.Weapon != pawn.def) return;

            // DamageInfo는 struct이므로 box → field set → unbox
            object boxed = dinfo;
            weaponIntField.SetValue(boxed, form.damageSourceDef);
            dinfo = (DamageInfo)boxed;
        }
    }
}
