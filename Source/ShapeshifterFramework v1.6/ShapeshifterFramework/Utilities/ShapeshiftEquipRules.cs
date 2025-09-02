using Verse;
using ShapeshifterFramework;
using ShapeshifterFramework.Comps;

namespace ShapeshifterFramework.Utilities
{
    public static class ShapeshiftEquipRules
    {
        public static bool LockApparel(CompShapeshifter comp)
        {
            if (comp == null) return false;
            if (comp.suppressEquipLock) return false; // ★ 내부 복구 중엔 잠금 해제
            var def = comp.currentForm as ShapeshiftFormDef;
            if (def == null) return false;

            switch (def.apparelEquipLock)
            {
                case EquipLockMode.Always: return true;
                case EquipLockMode.Never: return false;
                default: return def.apparelOnTransform != GearHandling.None;
            }
        }

        public static bool LockWeapons(CompShapeshifter comp)
        {
            if (comp == null) return false;
            if (comp.suppressEquipLock) return false; // ★ 내부 복구 중엔 잠금 해제
            var def = comp.currentForm as ShapeshiftFormDef;
            if (def == null) return false;

            switch (def.weaponEquipLock)
            {
                case EquipLockMode.Always: return true;
                case EquipLockMode.Never: return false;
                default: return def.weaponsOnTransform != GearHandling.None;
            }
        }
    }
}
