// ShapeshifterFramework | Utilities | ShapeshiftEquipLockScope.cs
// 목적 : 변신 해제 시 프레임워크가 폰의 장비를 강제로 재착용시킬 때, 모드 자체의 장비 장착 금지 락(Equip Lock)을 임시로 우회하기 위한 C# 스코프.
// 용도 : using (new ShapeshiftEquipLockScope(comp)) { ... } 블록 내에서만 컴포넌트의 suppressEquipLock을 true로 만들어 착용을 허용하고, 블록이 끝나면 안전하게 원래 상태(Dispose)로 복구함.

using ShapeshifterFramework.Hediffs;
using System;

namespace ShapeshifterFramework.Utilities
{
    /// <summary>using 블록 안에서만 장비 잠금 해제하는 스코프.</summary>
    public readonly struct ShapeshiftEquipLockScope : IDisposable
    {
        private readonly HediffComp_ShapeshiftCore _comp;
        private readonly bool _prev;

        public ShapeshiftEquipLockScope(HediffComp_ShapeshiftCore comp)
        {
            _comp = comp;
            _prev = comp != null && comp.suppressEquipLock;
            if (_comp != null) _comp.suppressEquipLock = true;
        }

        public void Dispose()
        {
            if (_comp != null) _comp.suppressEquipLock = _prev;
        }
    }
}
