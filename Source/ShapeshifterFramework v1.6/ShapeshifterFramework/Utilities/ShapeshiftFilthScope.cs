using Verse;

namespace ShapeshifterFramework.Utilities
{
    /// <summary>
    /// 혈흔/스미어 생성 시점에 Pawn 컨텍스트를 전달하기 위한 스코프.
    /// - DropBloodFilth/Smear 호출 Prefix에서 Pawn을 설정.
    /// - FilthMaker.TryMakeFilth Prefix에서 참조.
    /// - Postfix에서 null로 클리어.
    /// </summary>
    internal static class ShapeshiftFilthScope
    {
        [System.ThreadStatic]
        public static Pawn CurrentPawn;
    }
}
