// ShapeshifterFramework | Utilities | ShapeshiftEquipRules.cs
// 목적 : 현재 변신 중인 폼의 정책(EquipLockMode)에 따라 폰의 의복 및 무기 착용/해제 가능 여부를 판정.
// 용도 : 폼 설정(Always, Never, Auto)을 확인하여 바닐라 장착 로직(Job)을 차단할지 결정. suppressEquipLock이 켜져 있다면 내부 시스템에 의한 착용이므로 즉시 락을 해제함.

using ShapeshifterFramework.Comps;

namespace ShapeshifterFramework.Utilities
{
    public static class ShapeshiftEquipRules
    {
        public static bool LockApparel(CompShapeshifter comp)
        {
            if (comp == null) return false;
            if (comp.suppressEquipLock) return false;
            var def = comp.currentForm as ShapeshiftFormDef;
            if (def == null) return false;

            switch (def.apparelEquipLock)
            {
                case EquipLockMode.Locked: return true;  // 잠금 상태이므로 true
                case EquipLockMode.Unlocked: return false; // 잠금 해제 상태이므로 false
                default:
                    // Auto: 겉옷을 벗어 던지는 폼이면 자동으로 잠금(true)
                    return def.apparelOnTransform != GearHandling.Keep;
            }
        }

        public static bool LockWeapons(CompShapeshifter comp)
        {
            if (comp == null) return false;
            if (comp.suppressEquipLock) return false; // 내부 복구 중엔 잠금 해제
            var def = comp.currentForm as ShapeshiftFormDef;
            if (def == null) return false;

            switch (def.weaponEquipLock)
            {
                case EquipLockMode.Locked: return true;  // 잠금 상태이므로 true
                case EquipLockMode.Unlocked: return false; // 잠금 해제 상태이므로 false
                default: return def.weaponsOnTransform != GearHandling.Keep;
            }
        }
    }
}
