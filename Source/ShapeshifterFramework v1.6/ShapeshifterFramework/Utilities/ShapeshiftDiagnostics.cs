// ShapeshifterFramework/Utilities/ShapeshiftDiagnostics.cs
// .NET Framework 4.8 / C# 7.3
using Verse;

namespace ShapeshifterFramework.Utilities
{
    /// <summary>
    /// 공용 디버그 유틸리티. 
    /// 단순 정보(Info)는 DebugLog 설정에 따라 노이즈를 제어하고,
    /// 경고 및 에러는 바닐라 Log.Warning / Log.Error를 직접 사용한다.
    /// </summary>
    internal static class ShapeshiftDiagnostics
    {
        // 필요 시 디버그 출력 켜기
        public static bool DebugLog => ShapeshifterFrameworkMod.Settings != null && ShapeshifterFrameworkMod.Settings.enableDebugLog;

        public static void Info(string msg)
        {
            if (DebugLog) Log.Message("[SSF] " + msg);
        }
    }
}