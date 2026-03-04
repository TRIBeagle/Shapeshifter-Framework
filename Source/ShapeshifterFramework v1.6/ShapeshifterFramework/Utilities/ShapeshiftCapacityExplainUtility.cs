// ShapeshifterFramework | Utilities | ShapeshiftCapacityExplainUtility.cs
// 목적 : 인게임 건강 탭(Health Card) 툴팁에 변신 폼이 신체 능력(Capacity)에 미치는 영향을 표시.
// 용도 : 폼의 capMods에 정의된 특정 능력치의 배수(postFactor)와 가산(offset) 수치를 집계하여 툴팁 문자열을 생성함.
// 주의 : 변화량이 없는 경우(배수 1f, 가산 0f) null을 반환하여 바닐라 UI에 불필요한 빈 줄이 생기지 않도록 비침습적(Non-invasive)으로 설계됨.

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
            var comp = pawn.TryGetComp<CompShapeshifter>();
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
