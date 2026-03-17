// ShapeshifterFramework | Root | ShapeshiftFormIndex.cs
// 목적 : 게임 내에 로드된 모든 변신 폼이 추가한 렌더 노드(PawnRenderNodeProperties)들을 빠르게 식별하기 위한 글로벌 인덱스.
//        + ShapeshiftFormDef → List<HediffDef> 역인덱스를 제공하여, 특정 폼을 사용하는 HediffDef를 O(1)로 조회.
// 용도 : 게임 로드(StaticConstructorOnStartup) 시 모든 FormDef를 순회하여 노드 속성들의 참조(Reference)를 하나의 HashSet(AllFormProps)에 모아둠.
// 주의 : 폰 렌더링(Draw) 루프처럼 1초에 수십 번 호출되는 핫루프(Hot-loop) 구간에서 "이 노드가 폼 전용 노드인지" O(1) 속도로 판별하기 위한 핵심 최적화 장치임.

using ShapeshifterFramework.Hediffs;
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

        // 역인덱스: ShapeshiftFormDef → 해당 폼을 사용하는 HediffDef 목록
        public static readonly Dictionary<ShapeshiftFormDef, List<HediffDef>> FormToHediffDefs =
            new Dictionary<ShapeshiftFormDef, List<HediffDef>>();

        // 초기화: 모든 ShapeshiftFormDef를 순회하며 renderNodeProperties 수집 + 역인덱스 구성
        static ShapeshiftFormIndex()
        {
            foreach (var def in DefDatabase<ShapeshiftFormDef>.AllDefsListForReading)
            {
                if (def == null) continue;

                // 렌더 노드 인덱싱
                if (def.renderNodeProperties != null)
                {
                    for (int i = 0; i < def.renderNodeProperties.Count; i++)
                    {
                        var p = def.renderNodeProperties[i];
                        if (p != null) AllFormProps.Add(p);
                    }
                }

            }

            // 역인덱스 구성: 모든 HediffDef를 순회하여 HediffCompProperties_ShapeshiftCore.formDef 참조 수집
            foreach (var hediffDef in DefDatabase<HediffDef>.AllDefsListForReading)
            {
                if (hediffDef?.comps == null) continue;
                for (int i = 0; i < hediffDef.comps.Count; i++)
                {
                    if (hediffDef.comps[i] is HediffCompProperties_ShapeshiftCore coreProps && coreProps.formDef != null)
                    {
                        if (!FormToHediffDefs.TryGetValue(coreProps.formDef, out var list))
                        {
                            list = new List<HediffDef>(2);
                            FormToHediffDefs[coreProps.formDef] = list;
                        }
                        list.Add(hediffDef);
                    }
                }
            }
        }
    }
}
