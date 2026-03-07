// ShapeshifterFramework | Utilities | ShapeshiftPatchStatus.cs
// 목적 : 특정 Harmony 패치들의 성공 및 적용 여부를 런타임 전역에서 공유하기 위한 상태 저장소.
// 용도 : Transpiler 등 크리티컬한 패치가 정상 적용되었는지를 bool 플래그로 저장하여, 다른 유틸리티나 패치에서 이를 참조해 폴백(Fallback) 동작을 수행할지 결정함.

namespace ShapeshifterFramework.Utilities
{
    /// <summary>패치 적용/실패 여부 플래그 저장소.</summary>
    internal static class ShapeshiftPatchStatus
    {
        // WoundDrawer FleshType 치환이 실제로 적용됐는지
        public static bool WoundDrawerTranspiled;
    }
}
