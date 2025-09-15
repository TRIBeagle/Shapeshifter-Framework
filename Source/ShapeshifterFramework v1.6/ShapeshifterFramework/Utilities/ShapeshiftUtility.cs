// ShapeshiftUtility.cs
// 목적: 변신 관련 보조 유틸(컴포 조회/상태 판정/안전한 Def 조회).
// 기준: RimWorld 1.6 / .NET 4.8 / C# 7.3
using ShapeshifterFramework.Comps;
using Verse;

namespace ShapeshifterFramework.Utilities
{
    public static class ShapeshiftUtility
    {
        /// <summary>Pawn에 CompShapeshifter가 붙어 있으면 반환, 없으면 null.</summary>
        public static CompShapeshifter GetShapeShiftComp(Pawn pawn)
        {
            if (pawn == null) return null;
            var comps = pawn.AllComps;
            if (comps == null) return null;
            for (int i = 0; i < comps.Count; i++)
                if (comps[i] is CompShapeshifter cs) return cs;
            return null;
        }

        /// <summary>Pawn이 변신 중인지(Comp 존재 + currentForm 보유) 여부.</summary>
        public static bool IsShapeShifting(Pawn pawn)
        {
            var comp = GetShapeShiftComp(pawn);
            return comp != null && comp.isTransformed;
        }

        /// <summary>TryGet 패턴 오버로드(오류 해소용): 컴프 존재 여부 + out 반환.</summary>
        public static bool TryGetComp(Pawn pawn, out CompShapeshifter comp)
        {
            comp = GetShapeShiftComp(pawn);
            return comp != null;
        }

        /// <summary>DefDatabase에서 안전 조회. defName null/오타 시 null 반환.</summary>
        public static T GetDefSafe<T>(string defName) where T : Def
        {
            if (string.IsNullOrEmpty(defName)) return null;
            return DefDatabase<T>.GetNamedSilentFail(defName);
        }
    }
}
