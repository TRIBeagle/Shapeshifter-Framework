// ShapeshifterFramework | Utilities | ShapeshiftOverlayUtility.cs
// 목적 : 상처 오버레이(Wound Overlay) 렌더링 시 폰의 살점 타입(FleshTypeDef)을 에러 없이 안전하게 가져오는 유틸리티.
// 용도 : 프레임워크의 O(1) 런타임 캐시를 최우선으로 조회하고, 없을 경우 바닐라의 def.race.FleshType → RaceProps.FleshType 순으로 폴백(Fallback) 탐색함.
// 주의 : 찾을 수 없는 폰(예: 특수 구조물 등)인 경우 안전하게 null을 반환하여 바닐라 렌더러가 예외(NRE)를 뿜지 않고 자연스럽게 오버레이를 스킵하게 함.

using RimWorld;
using Verse;

namespace ShapeshifterFramework.Utilities
{
    internal static class ShapeshiftOverlayUtility
    {
        /// <summary>FleshTypeDef를 캐시 우선으로 안전 조회, 없으면 null 반환.</summary>
        public static FleshTypeDef GetEffectiveFleshType(Pawn pawn)
        {
            if (pawn == null)
                return null;

            // 1) 캐시 우선 (O(1))
            FleshTypeDef cached;
            if (ShapeshiftRuntimeCaches.FleshTypeByPawn.TryGetValue(pawn, out cached) && cached != null)
                return cached;

            // 2) 안전 접근: def.race.FleshType (Pawn.RaceProps와 동일 객체)
            var race = pawn.def?.race;
            if (race != null && race.FleshType != null)
                return race.FleshType;

            // 3) 못 찾으면 null 반환(상처 오버레이 생략)
            return null;
        }
    }
}
