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
            return SafeReequipInternal(pawn, eq, (p, item) =>
            {
                p.equipment?.AddEquipment(item as ThingWithComps);
                return p.equipment != null &&
                       p.equipment.AllEquipmentListForReading != null &&
                       p.equipment.AllEquipmentListForReading.Contains(item as ThingWithComps);
            });
        }

        // 의복: 인벤에서 꺼내 착용.
        public static bool SafeWearFromInventory(Pawn pawn, Apparel ap, bool dropReplaced = true)
        {
            return SafeReequipInternal(pawn, ap, (p, item) =>
            {
                p.apparel?.Wear(item as Apparel, dropReplaced, locked: false);
                return p.apparel != null &&
                       p.apparel.WornApparel != null &&
                       p.apparel.WornApparel.Contains(item as Apparel);
            });
        }

        /// <summary>공통 로직: 인벤토리에서 꺼내 장착 시도, 실패 시 복구.</summary>
        static bool SafeReequipInternal(Pawn pawn, Thing item, System.Func<Pawn, Thing, bool> tryEquip)
        {
            if (pawn == null || item == null) return false;
            if (pawn.Destroyed || pawn.Dead) return false;

            var inv = pawn.inventory?.innerContainer;
            if (inv == null || !inv.Contains(item)) return false;

            inv.Remove(item);
            bool success = false;
            try
            {
                success = tryEquip(pawn, item);
            }
            catch { success = false; /* 장착 시도 실패 — 복구 경로로 진입 */ }

            if (!success)
            {
                ShapeshiftDiagnostics.Info($"Reequip failed for {pawn.LabelShort}: {item.Label} — returning to inventory or dropping.");
                if (!inv.TryAdd(item, canMergeWithExistingStacks: true))
                {
                    if (pawn.MapHeld != null)
                    {
                        // 맵이 있으면 주변에 드랍
                        GenPlace.TryPlaceThing(item, pawn.PositionHeld, pawn.MapHeld, ThingPlaceMode.Near);
                    }
                    else
                    {
                        // 강제 삽입 시도 후, 실패하면 Error 로그 출력
                        if (inv.TryAdd(item, false))
                        {
                            Log.Warning($"[SSF] Item void prevention! Forced {item.Label} back into inventory for off-map pawn {pawn.LabelShort}.");
                        }
                        else
                        {
                            // 인벤토리 용량 초과 등으로 강제 삽입조차 실패하여 아이템이 증발하는 최악의 케이스
                            Log.Error($"[SSF] CRITICAL: Failed to recover {item.Label} for off-map pawn {pawn.LabelShort}. Item is permanently lost due to inventory limits.");
                        }
                    }
                }
            }
            return success;
        }
    }
}
