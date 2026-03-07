// ShapeshifterFramework | Utilities | ShapeshiftFilthScope.cs
// 목적 : 바닐라 혈흔/오물 생성 메서드(TryMakeFilth)의 시그니처를 건드리지 않고, 현재 출혈 중인 Pawn의 컨텍스트(참조)를 패치 내부로 안전하게 전달.
// 용도 : [System.ThreadStatic]을 활용해 찰나의 순간에만 Pawn 객체를 캐싱하여 공유하므로, 리플렉션 비용 없이 폼 전용 혈흔(Blood/Smear) 데이터를 생성할 수 있게 함.

using Verse;

namespace ShapeshifterFramework.Utilities
{
    /// <summary>혈흔/스미어 생성 시점에 Pawn 컨텍스트를 전달하기 위한 스코프.</summary>
    internal static class ShapeshiftFilthScope
    {
        [System.ThreadStatic]
        public static Pawn CurrentPawn;
    }
}
