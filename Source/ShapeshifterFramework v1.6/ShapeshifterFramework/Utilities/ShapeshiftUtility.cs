// ShapeshifterFramework | Utilities | ShapeshiftUtility.cs
// 목적 : 프레임워크 내에서 가장 자주 사용되는 기본 판정 및 조회 기능들을 모아둔 공용 유틸.
// 용도 : 폰의 CompShapeshifter 획득, 변신 중인지 여부(IsShapeShifting) 판정, 특정 Def의 존재 유무를 예외 없이 확인하는 안전한 조회(GetDefSafe) 기능을 제공함.

using ShapeshifterFramework.Comps;
using Verse;

namespace ShapeshifterFramework.Utilities
{
    public static class ShapeshiftUtility
    {
        /// <summary>Pawn에 CompShapeshifter가 붙어 있으면 반환, 없으면 null. 레지스트리 우선 조회 후 AllComps 폴백.</summary>
        public static CompShapeshifter GetShapeShiftComp(Pawn pawn)
        {
            if (pawn == null) return null;

            // 레지스트리 O(1) 조회 (변신 중인 폰은 즉시 반환)
            if (ShapeshiftRegistry.TryGet(pawn, out var regComp, out _))
                return regComp;

            // 폴백: 비변신 상태에서도 Comp 접근이 필요한 경우 (CanTransform 판정 등)
            var comps = pawn.AllComps;
            if (comps == null) return null;
            for (int i = 0; i < comps.Count; i++)
                if (comps[i] is CompShapeshifter cs) return cs;
            return null;
        }

        /// <summary>TryGet 패턴 오버로드: 컴프 존재 여부 + out 반환. GetShapeShiftComp와 동일한 조회 로직.</summary>
        public static bool TryGetComp(Pawn pawn, out CompShapeshifter comp)
        {
            comp = GetShapeShiftComp(pawn);
            return comp != null;
        }

        /// <summary>Pawn이 변신 중인지(레지스트리 O(1) 판정) 여부.</summary>
        public static bool IsShapeShifting(Pawn pawn)
        {
            return ShapeshiftRegistry.IsActive(pawn);
        }

        /// <summary>DefDatabase에서 안전 조회. defName null/오타 시 null 반환.</summary>
        public static T GetDefSafe<T>(string defName) where T : Def
        {
            if (string.IsNullOrEmpty(defName)) return null;
            return DefDatabase<T>.GetNamedSilentFail(defName);
        }
    }
}
