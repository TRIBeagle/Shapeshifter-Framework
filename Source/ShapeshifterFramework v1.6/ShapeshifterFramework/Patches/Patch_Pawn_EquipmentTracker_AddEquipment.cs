// 변신 중 무기/장비 장착 금지(직접 시그니처 지정)
// - 대상 메서드: Verse.Pawn_EquipmentTracker.AddEquipment(ThingWithComps newEq)
// - 주의: 1.6 바닐라엔 TryAddEquipment가 없고 AddEquipment가 최종 관문.

using HarmonyLib;
using RimWorld;
using ShapeshifterFramework.Comps;
using ShapeshifterFramework.Utilities; // ShapeshiftEquipRules
using Verse;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch(typeof(Pawn_EquipmentTracker))]
    [HarmonyPatch(nameof(Pawn_EquipmentTracker.AddEquipment))]
    [HarmonyPatch(new[] { typeof(ThingWithComps) })]
    [HarmonyPriority(Priority.First)]
    public static class Patch_Pawn_EquipmentTracker_AddEquipment
    {
        static bool Prefix(Pawn_EquipmentTracker __instance, ThingWithComps newEq)
        {
            Pawn pawn = __instance.pawn; // public
            if (pawn == null) return true;

            var comp = pawn.TryGetComp<CompShapeshifter>();
            if (comp != null && comp.isTransformed && ShapeshiftEquipRules.LockWeapons(comp))
            {
                if (!comp.suppressEquipLock && pawn.IsColonistPlayerControlled)
                {
                    Messages.Message("Shapeshift_CannotEquipWhileTransformed".Translate(pawn.Named("PAWN")),
                                     pawn, MessageTypeDefOf.RejectInput, false);
                }
                return false; // 원본 AddEquipment 실행 취소
            }
            return true;
        }
    }
}
