// ShapeshifterFramework | Patches | Patch_EquipmentUtility_CanEquip.cs
// 목적 : 무기 잠금(LockWeapons) 상태인 변신 폰의 무기 장착 판정을 원천 차단.
// 용도 : 바닐라 JobGiver_PickUpOpportunisticWeapon(자동 픽업)·장비 UI가 CanEquip으로 사전 판정하므로,
//        여기서 false를 반환하면 Equip Job 자체가 생성되지 않아 아이템 증발 경로가 차단됨.
//        (JobDriver_Equip은 무기를 먼저 DeSpawn한 뒤 AddEquipment를 호출하기 때문에,
//         AddEquipment Prefix 차단만으로는 무기가 허공에서 소실됨 — 그쪽은 place-back 백스톱으로 방어.)
// 주의 : suppressEquipLock(내부 재장착 스코프) 중에는 LockWeapons가 false를 반환하므로 자동 통과.

using HarmonyLib;
using RimWorld;
using ShapeshifterFramework.Utilities;
using Verse;

namespace ShapeshifterFramework.Patches
{
    /// <summary>무기 잠금 변신 폰은 CanEquip 판정에서 사전 거부 — AI 자동 픽업 Job 생성 차단.</summary>
    [HarmonyPatch(typeof(EquipmentUtility), nameof(EquipmentUtility.CanEquip),
        new[] { typeof(Thing), typeof(Pawn), typeof(string), typeof(bool) },
        new[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out, ArgumentType.Normal })]
    internal static class Patch_EquipmentUtility_CanEquip
    {
        static void Postfix(Thing thing, Pawn pawn, ref string cantReason, ref bool __result)
        {
            if (!__result) return;
            if (pawn == null) return;

            if (ShapeshiftRegistry.TryGet(pawn, out var comp, out _)
                && ShapeshiftEquipRules.LockWeapons(comp))
            {
                __result = false;
                cantReason = "SSF_Message_CannotEquip".Translate(pawn.Named("PAWN"));
            }
        }
    }
}
