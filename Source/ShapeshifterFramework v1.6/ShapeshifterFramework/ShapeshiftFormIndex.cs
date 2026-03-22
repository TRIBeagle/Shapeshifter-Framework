// ShapeshifterFramework | Root | ShapeshiftFormIndex.cs
// 목적 : ShapeshiftFormDef → List<HediffDef> 역인덱스를 제공하여, 특정 폼을 사용하는 HediffDef를 O(1)로 조회.
// 용도 : 게임 로드(StaticConstructorOnStartup) 시 모든 HediffDef를 순회하여 역인덱스를 구성.

using ShapeshifterFramework.Hediffs;
using System.Collections.Generic;
using Verse;

namespace ShapeshifterFramework
{
    [StaticConstructorOnStartup]
    public static class ShapeshiftFormIndex
    {
        // 역인덱스: ShapeshiftFormDef → 해당 폼을 사용하는 HediffDef 목록
        public static readonly Dictionary<ShapeshiftFormDef, List<HediffDef>> FormToHediffDefs =
            new Dictionary<ShapeshiftFormDef, List<HediffDef>>();

        // 초기화: 모든 HediffDef를 순회하여 역인덱스 구성
        static ShapeshiftFormIndex()
        {
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
