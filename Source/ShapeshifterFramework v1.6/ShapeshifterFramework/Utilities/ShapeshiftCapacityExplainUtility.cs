// ShapeshiftCapacityExplainUtility.cs
// 목적: 건강 카드 툴팁에 붙일 “폼 영향” 한 줄(배수/가산) 문자열 생성.
// 용도: capMods에서 대상 capacity의 postFactor/offset을 집계해 포맷 반환.
// 주의: 변화 없으면 null 반환(비침습).

using RimWorld;
using ShapeshifterFramework.Comps;
using System.Text;
using Verse;

namespace ShapeshifterFramework.Utilities
{
    public static class ShapeshiftCapacityExplainUtility
    {
        /// <summary>
        /// 주어진 Pawn/Capacity에 대해 변신 폼 영향 설명 문자열을 생성.
        /// 반환값:
        ///  - 영향 있을 때: "배수" 및/또는 "가산"을 포함한 한 줄 문자열
        ///  - 영향 없거나 대상 아님: null (호출처에서 아무 것도 추가하지 않도록)
        /// </summary>
        public static string BuildExplainLine(Pawn pawn, PawnCapacityDef capacity)
        {
            if (pawn == null) return null;

            // 변신 중이 아니면 설명 없음
            var comp = pawn.GetComp<CompShapeshifter>();
            if (comp == null || !comp.isTransformed) return null;

            var form = comp.currentForm;
            if (form == null || form.capMods == null || form.capMods.Count == 0) return null;

            // 해당 capacity의 배수/가산 합산
            float factor = 1f, offset = 0f;
            for (int i = 0; i < form.capMods.Count; i++)
            {
                var m = form.capMods[i];
                if (m == null || m.capacity != capacity) continue;
                factor *= m.postFactor;
                offset += m.offset;
            }

            // 변화가 전혀 없으면(null 반환해 호출측에서 미표시)
            if (factor == 1f && offset == 0f) return null;

            // 출력 구성(번역 키 사용)
            string label = form.LabelCap != null ? form.LabelCap.ToString() : "Shapeshift";
            var sb = new StringBuilder();

            // "ShapeshiftCapacityFactor": 예) "{0} 배수: x{1}"
            if (factor != 1f)
                sb.Append("ShapeshiftCapacityFactor".Translate(label, factor.ToString("0.##")));

            // "ShapeshiftCapacityOffset": 예) "{0} 가산: +1.2 / -0.8"
            if (offset != 0f)
            {
                if (sb.Length > 0) sb.Append("  "); // 배수/가산 둘 다 있을 때 공백 구분
                sb.Append("ShapeshiftCapacityOffset".Translate(label, offset.ToString("+0.##;-0.##")));
            }

            return sb.ToString();
        }
    }
}
