// ShapeshifterFramework | Utilities | ShapeshiftEquipRules.cs
// 목적 : 현재 변신 중인 폼의 정책(EquipLockMode)에 따라 폰의 의복 및 무기 착용/해제 가능 여부를 판정.
// 용도 : 폼 설정(Always, Never, Auto)을 확인하여 바닐라 장착 로직(Job)을 차단할지 결정. suppressEquipLock이 켜져 있다면 내부 시스템에 의한 착용이므로 즉시 락을 해제함.

using ShapeshifterFramework.Hediffs;

namespace ShapeshifterFramework.Utilities
{
    public static class ShapeshiftEquipRules
    {
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
