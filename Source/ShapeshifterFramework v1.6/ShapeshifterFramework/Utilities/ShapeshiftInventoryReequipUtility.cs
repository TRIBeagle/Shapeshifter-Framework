// .NET 4.8 / C# 7.3
using RimWorld;
using Verse;

namespace ShapeshifterFramework.Utilities
{
    public static class ShapeshiftInventoryReequipUtility
    {
        // 무기: 인벤에서 꺼내 장비. 실패하면 반드시 인벤으로 되돌리되,
        // 인벤이 가득하면 근처에 드랍(유실 방지).
        public static bool SafeEquipFromInventory(Pawn pawn, ThingWithComps eq)
        {
            if (pawn == null || eq == null) return false;
            if (pawn.Destroyed || pawn.Dead) return false;

            var inv = pawn.inventory?.innerContainer;
            if (inv == null || !inv.Contains(eq)) return false;

            inv.Remove(eq);
            bool success = false;
            try
            {
                pawn.equipment?.AddEquipment(eq); // Harmony Prefix가 차단하면 실행 안 됨
                success = pawn.equipment != null &&
                          pawn.equipment.AllEquipmentListForReading != null &&
                          pawn.equipment.AllEquipmentListForReading.Contains(eq);
            }
            catch
            {
                success = false;
            }

            if (!success)
            {
                if (!inv.TryAdd(eq, canMergeWithExistingStacks: true))
                {
                    // 인벤이 가득한 드문 케이스 → 맵에 드랍
                    if (pawn.MapHeld != null)
                        GenPlace.TryPlaceThing(eq, pawn.PositionHeld, pawn.MapHeld, ThingPlaceMode.Near);
                }
            }
            return success;
        }

        // 의복: 인벤에서 꺼내 착용. 실패하면 반드시 인벤으로 되돌리되,
        // 인벤이 가득하면 근처에 드랍(유실 방지).
        public static bool SafeWearFromInventory(Pawn pawn, Apparel ap, bool dropReplaced = true)
        {
            if (pawn == null || ap == null) return false;
            if (pawn.Destroyed || pawn.Dead) return false;

            var inv = pawn.inventory?.innerContainer;
            if (inv == null || !inv.Contains(ap)) return false;

            inv.Remove(ap);
            bool success = false;
            try
            {
                pawn.apparel?.Wear(ap, dropReplaced, locked: false);
                success = pawn.apparel != null &&
                          pawn.apparel.WornApparel != null &&
                          pawn.apparel.WornApparel.Contains(ap);
            }
            catch
            {
                success = false;
            }

            if (!success)
            {
                if (!inv.TryAdd(ap, canMergeWithExistingStacks: true))
                {
                    if (pawn.MapHeld != null)
                        GenPlace.TryPlaceThing(ap, pawn.PositionHeld, pawn.MapHeld, ThingPlaceMode.Near);
                }
            }
            return success;
        }
    }
}
