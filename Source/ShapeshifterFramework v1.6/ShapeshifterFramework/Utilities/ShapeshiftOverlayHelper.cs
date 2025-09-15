// .NET Framework 4.8 / C# 7.3
using RimWorld;
using Verse;

namespace ShapeshifterFramework.Utilities
{
    internal static class ShapeshiftOverlayHelper
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
