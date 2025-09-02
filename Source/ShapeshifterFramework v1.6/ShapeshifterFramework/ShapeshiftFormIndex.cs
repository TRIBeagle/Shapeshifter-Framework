// ShapeshiftFormIndex.cs
// 목적: 모든 폼의 renderNodeProperties 화이트리스트 인덱스.
// 용도: “폼이 추가한 노드인지” 빠른 판정에 사용(참조 비교 기반).
// 주의: 게임 로드시 1회 빌드. Def 변경 반영에는 재로드 필요.

using System.Collections.Generic;
using Verse;

namespace ShapeshifterFramework
{
    [StaticConstructorOnStartup]
    public static class ShapeshiftFormIndex
    {
        // 폼이 추가한 모든 PawnRenderNodeProperties 집합(참조 비교 기반)
        public static readonly HashSet<PawnRenderNodeProperties> AllFormProps =
            new HashSet<PawnRenderNodeProperties>();

        // 초기화: 모든 ShapeshiftFormDef를 순회하며 renderNodeProperties를 수집
        static ShapeshiftFormIndex()
        {
            foreach (var def in DefDatabase<ShapeshiftFormDef>.AllDefsListForReading)
            {
                if (def?.renderNodeProperties == null) continue;
                for (int i = 0; i < def.renderNodeProperties.Count; i++)
                {
                    var p = def.renderNodeProperties[i];
                    if (p != null) AllFormProps.Add(p);
                }
            }
        }
    }
}
