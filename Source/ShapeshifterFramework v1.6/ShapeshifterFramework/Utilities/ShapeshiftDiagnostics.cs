// ShapeshifterFramework | Utilities | ShapeshiftDiagnostics.cs
// 목적 : 프레임워크 전역에서 사용하는 중앙 집중식 디버그 로거.
// 용도 : 모드 설정(ShapeshifterFrameworkMod.Settings.enableDebugLog)이 켜져 있을 때만 Info 메시지를 콘솔에 출력함.
// 주의 : 잦은 로그 출력으로 인한 렉(Spam)을 방지하기 위해 일반 정보는 반드시 이 클래스의 Info()를 거치며, 실제 크래시 위험이 있는 Error/Warning은 바닐라 Log 클래스를 직접 사용함.

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