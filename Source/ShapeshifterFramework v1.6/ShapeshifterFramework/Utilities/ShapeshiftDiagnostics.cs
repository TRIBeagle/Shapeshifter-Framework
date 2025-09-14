// .NET Framework 4.8 / C# 7.3
using Verse;

namespace ShapeshifterFramework.Utilities
{
    /// <summary>
    /// 공용 로그/디버그 유틸. Verbose 끄고 켜서 노이즈 제어.
    /// </summary>
    internal static class ShapeshiftDiagnostics
    {
        // 필요 시 디버그 출력 켜기
        public static bool Verbose = false;

        public static void Info(string msg)
        {
            if (Verbose) Log.Message("[SSF] " + msg);
        }

        public static void Warn(string msg)
        {
            Log.Warning("[SSF] " + msg);
        }

        public static void Error(string msg)
        {
            Log.Error("[SSF] " + msg);
        }
    }
}
