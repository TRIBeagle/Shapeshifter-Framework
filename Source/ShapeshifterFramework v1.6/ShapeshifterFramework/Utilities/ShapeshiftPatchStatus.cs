// .NET Framework 4.8 / C# 7.3
namespace ShapeshifterFramework.Utilities
{
    /// <summary>
    /// 패치 적용/실패 여부 플래그 저장소.
    /// - 다른 곳에서 진단/폴백 판단에 사용.
    /// </summary>
    internal static class ShapeshiftPatchStatus
    {
        // WoundDrawer FleshType 치환이 실제로 적용됐는지
        public static bool WoundDrawerTranspiled;
    }
}
