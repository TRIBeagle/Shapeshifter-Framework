// ShapeshifterFramework | Utilities | ShapeshiftInventoryReequipUtility.cs
// 목적 : 인벤토리에 보관된 장비/무기를 안전하게 꺼내어 착용시키고, 실패 시 복구하는 로직.
// 용도 : 착용(Wear/AddEquipment) 시도 후 실패하면 다시 인벤토리로 되돌리며, 인벤토리 용량 초과 시 맵 바닥에 안전하게 드랍(TryPlaceThing)함.
// 주의 : 카라반(월드맵)이나 수송 포드 등 폰이 맵에 존재하지 않는(Off-map) 예외 상황에서 아이템이 허공으로 증발하는 것(Void)을 막기 위해, 인벤토리 강제 삽입(Force) 및 크리티컬 에러 로그 방어가 적용됨.

using RimWorld;
using Verse;

namespace ShapeshifterFramework.Utilities
{
    public static class ShapeshiftInventoryReequipUtility
    {
        // 무기: 인벤에서 꺼내 장비.
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
                pawn.equipment?.AddEquipment(eq);
                success = pawn.equipment != null &&
                          pawn.equipment.AllEquipmentListForReading != null &&
                          pawn.equipment.AllEquipmentListForReading.Contains(eq);
            }
            catch { success = false; }

            if (!success)
            {
                if (!inv.TryAdd(eq, canMergeWithExistingStacks: true))
                {
                    if (pawn.MapHeld != null)
                    {
                        // 맵이 있으면 주변에 드랍
                        GenPlace.TryPlaceThing(eq, pawn.PositionHeld, pawn.MapHeld, ThingPlaceMode.Near);
                    }
                    else
                    {
                        // 강제 삽입 시도 후, 실패하면 Error 로그 출력
                        if (inv.TryAdd(eq, false))
                        {
                            Log.Warning($"[SSF] Item void prevention! Forced {eq.Label} back into inventory for off-map pawn {pawn.Name}.");
                        }
                        else
                        {
                            // 인벤토리 용량 초과 등으로 강제 삽입조차 실패하여 아이템이 증발하는 최악의 케이스
                            Log.Error($"[SSF] CRITICAL: Failed to recover {eq.Label} for off-map pawn {pawn.Name}. Item is permanently lost due to inventory limits.");
                        }
                    }
                }
            }
            return success;
        }

        // 의복: 인벤에서 꺼내 착용.
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
            catch { success = false; }

            if (!success)
            {
                if (!inv.TryAdd(ap, canMergeWithExistingStacks: true))
                {
                    if (pawn.MapHeld != null)
                    {
                        // 맵이 있으면 주변에 드랍
                        GenPlace.TryPlaceThing(ap, pawn.PositionHeld, pawn.MapHeld, ThingPlaceMode.Near);
                    }
                    else
                    {
                        // 강제 삽입 시도 후, 실패하면 Error 로그 출력
                        if (inv.TryAdd(ap, false))
                        {
                            Log.Warning($"[SSF] Item void prevention! Forced {ap.Label} back into inventory for off-map pawn {pawn.Name}.");
                        }
                        else
                        {
                            // 인벤토리 용량 초과 등으로 강제 삽입조차 실패하여 아이템이 증발하는 최악의 케이스
                            Log.Error($"[SSF] CRITICAL: Failed to recover {ap.Label} for off-map pawn {pawn.Name}. Item is permanently lost due to inventory limits.");
                        }
                    }
                }
            }
            return success;
        }
    }
}