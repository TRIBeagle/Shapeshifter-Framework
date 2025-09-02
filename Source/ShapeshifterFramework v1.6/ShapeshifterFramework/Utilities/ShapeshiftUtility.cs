// ShapeshiftUtility.cs
// 목적: 변신 관련 보조 유틸(컴포 조회/상태 판정).
// 용도: GetShapeShiftComp/IsShapeShifting로 반복 리플렉션/링크 방지.
// 주의: 널가드 철저. 성능상 단순 루프 우선.

using ShapeshifterFramework.Comps;
using Verse;

namespace ShapeshifterFramework.Utilities
{
    public static class ShapeshiftUtility
    {
        /// <summary>
        /// Pawn에 CompShapeshifter가 붙어 있으면 반환, 없으면 null.
        /// </summary>
        public static CompShapeshifter GetShapeShiftComp(Pawn pawn)
        {
            if (pawn == null) return null;
            var comps = pawn.AllComps;
            if (comps == null) return null;

            // 순회하여 첫 CompShapeshifter를 찾는다.
            for (int i = 0; i < comps.Count; i++)
            {
                if (comps[i] is CompShapeshifter cs)
                    return cs;
            }
            return null;
        }

        /// <summary>
        /// Pawn이 변신 중인지(Comp 존재 + currentForm 보유) 여부.
        /// </summary>
        public static bool IsShapeShifting(Pawn pawn)
        {
            var comp = GetShapeShiftComp(pawn);
            return comp != null && comp.isTransformed;
        }
    }
}
