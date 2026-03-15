// ShapeshifterFramework | Utilities | ShapeshiftUtility.cs
// 목적 : 프레임워크 내에서 가장 자주 사용되는 기본 판정 및 조회 기능들을 모아둔 공용 유틸.
// 용도 : 폰의 CompShapeshifter 획득(레지스트리 O(1) → AllComps 폴백) 기능을 제공함.

using ShapeshifterFramework.Comps;
using Verse;

namespace ShapeshifterFramework.Utilities
{
    public static class ShapeshiftUtility
    {
        /// <summary>Pawn의 CompShapeshifter를 조회. 레지스트리 O(1) 우선, AllComps 폴백.</summary>
        public static bool TryGetShapeshiftComp(Pawn pawn, out CompShapeshifter comp)
        {
            comp = null;
            if (pawn == null) return false;

            // 레지스트리 O(1) 조회 (변신 중인 폰은 즉시 반환)
            if (ShapeshiftRegistry.TryGet(pawn, out comp, out _))
                return true;

            // 폴백: 비변신 상태에서도 Comp 접근이 필요한 경우 (CanTransform 판정 등)
            var comps = pawn.AllComps;
            if (comps == null) return false;
            for (int i = 0; i < comps.Count; i++)
            {
                if (comps[i] is CompShapeshifter cs)
                {
                    comp = cs;
                    return true;
                }
            }
            return false;
        }
    }
}
