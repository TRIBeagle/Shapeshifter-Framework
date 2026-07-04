// ShapeshifterFramework | Utilities | ShapeshiftEquipRules.cs
// 목적 : 현재 변신 중인 폼의 정책(EquipLockMode)에 따라 폰의 의복 및 무기 착용/해제 가능 여부를 판정.
// 용도 : 폼 설정(Always, Never, Auto)을 확인하여 바닐라 장착 로직(Job)을 차단할지 결정. suppressEquipLock이 켜져 있다면 내부 시스템에 의한 착용이므로 즉시 락을 해제함.

using ShapeshifterFramework.Hediffs;
using Verse;

namespace ShapeshifterFramework.Utilities
{
    public static class ShapeshiftEquipRules
    {
        /// <summary>장착 차단으로 갈 곳을 잃은 아이템 복구.
        /// 바닐라 JobDriver_Equip(무기)·아웃핏 스탠드 Wear(의류)는 아이템을 먼저 DeSpawn/홀더 제거한 뒤
        /// AddEquipment/Wear를 호출하므로, Prefix 차단만 하면 아이템이 허공에서 영구 소실됨.
        /// despawn 상태 + 홀더 없음이면 폰 옆에 되살리고, 맵이 없으면 인벤토리로 강제 삽입.</summary>
        public static void RecoverBlockedEquipItem(Thing item, Pawn pawn)
        {
            if (item == null || pawn == null) return;
            if (item.Destroyed || item.Spawned || item.holdingOwner != null) return;

            var map = pawn.MapHeld;
            if (map != null && pawn.PositionHeld.IsValid)
            {
                GenPlace.TryPlaceThing(item, pawn.PositionHeld, map, ThingPlaceMode.Near);
            }
            else if (pawn.inventory?.innerContainer == null
                     || !pawn.inventory.innerContainer.TryAdd(item, false))
            {
                Log.Error($"[SSF] CRITICAL: Failed to recover blocked equip item {item.Label} for off-map pawn {pawn.LabelShort}. Item is permanently lost.");
            }
        }

        /// <summary>HediffComp_ShapeshiftCore 기반 의복 잠금 판정.</summary>
        public static bool LockApparel(HediffComp_ShapeshiftCore core)
        {
            if (core == null) return false;
            if (core.suppressEquipLock) return false;
            var def = core.currentForm;
            if (def == null) return false;

            switch (def.apparelEquipLock)
            {
                case EquipLockMode.Locked: return true;
                case EquipLockMode.Unlocked: return false;
                default:
                    return def.apparelOnTransform != GearHandling.Keep;
            }
        }

        /// <summary>HediffComp_ShapeshiftCore 기반 무기 잠금 판정.</summary>
        public static bool LockWeapons(HediffComp_ShapeshiftCore core)
        {
            if (core == null) return false;
            if (core.suppressEquipLock) return false;
            var def = core.currentForm;
            if (def == null) return false;

            switch (def.weaponEquipLock)
            {
                case EquipLockMode.Locked: return true;
                case EquipLockMode.Unlocked: return false;
                default: return def.weaponsOnTransform != GearHandling.Keep;
            }
        }
    }
}
