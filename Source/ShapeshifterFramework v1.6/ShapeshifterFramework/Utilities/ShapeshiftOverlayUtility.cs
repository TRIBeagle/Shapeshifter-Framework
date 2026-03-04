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
        /// <summary>
        /// FleshTypeDef 조회(안전 가드 강화)
        /// - 변신 중이면 캐시 FleshType 사용
        /// - 아니면 Pawn.def.race.FleshType → Pawn.RaceProps.FleshType 순으로 안전 조회
        /// - 못 찾으면 null (바닐라도 null이면 상처 오버레이를 생략함)
        /// 성능: TryGetValue 사용, 예외 미발생 보장
        /// </summary>
        public static FleshTypeDef GetEffectiveFleshType(Pawn pawn)
        {
            if (pawn == null)
                return null;

            // 1) 캐시 우선 (O(1))
            FleshTypeDef cached;
            if (ShapeshiftRuntimeCaches.FleshTypeByPawn.TryGetValue(pawn, out cached) && cached != null)
                return cached;

            // 2) 안전 접근: Pawn.def → def.race.FleshType
            var def = pawn.def;
            if (def != null)
            {
                var raceFromDef = def.race;
                if (raceFromDef != null && raceFromDef.FleshType != null)
                    return raceFromDef.FleshType;
            }

            // 3) 보조 경로: Pawn.RaceProps.FleshType
            var raceProps = pawn.RaceProps;
            if (raceProps != null && raceProps.FleshType != null)
                return raceProps.FleshType;

            // 4) 못 찾으면 null 반환(상처 오버레이 생략)
            return null;
        }
    }
}
