// .NET 4.8 / C# 7.3
using System;
using ShapeshifterFramework.Comps;

namespace ShapeshifterFramework.Utilities
{
    // using(new EquipLockScope(comp)) { … } 블록 안에서만 잠금 해제
    public readonly struct ShapeshiftEquipLockScope : IDisposable
    {
        private readonly CompShapeshifter _comp;
        private readonly bool _prev;
        public ShapeshiftEquipLockScope(CompShapeshifter comp)
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
