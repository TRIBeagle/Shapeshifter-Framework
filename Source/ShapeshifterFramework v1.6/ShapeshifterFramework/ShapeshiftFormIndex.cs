// ShapeshifterFramework | Root | ShapeshiftFormIndex.cs
// 목적 : ShapeshiftFormDef → List<HediffDef> 역인덱스를 제공하여, 특정 폼을 사용하는 HediffDef를 O(1)로 조회.
// 용도 : 게임 로드(StaticConstructorOnStartup) 시 모든 HediffDef를 순회하여 역인덱스를 구성.

using ShapeshifterFramework.Hediffs;
using System.Collections.Generic;
using Verse;

namespace ShapeshifterFramework
{
    /// <summary>ShapeshiftFormDef → HediffDef 역인덱스. 디버그 전용이므로 첫 접근 시 lazy 초기화.</summary>
    public static class ShapeshiftFormIndex
    {
        private static Dictionary<ShapeshiftFormDef, List<HediffDef>> _formToHediffDefs;

        /// <summary>역인덱스: ShapeshiftFormDef → 해당 폼을 사용하는 HediffDef 목록. 첫 접근 시 빌드.</summary>
        public static Dictionary<ShapeshiftFormDef, List<HediffDef>> FormToHediffDefs
        {
            get
            {
                if (_formToHediffDefs == null)
                    Build();
                return _formToHediffDefs;
            }
        }

        private static void Build()
        {
            _formToHediffDefs = new Dictionary<ShapeshiftFormDef, List<HediffDef>>();
            foreach (var hediffDef in DefDatabase<HediffDef>.AllDefsListForReading)
            {
                if (hediffDef?.comps == null) continue;
                for (int i = 0; i < hediffDef.comps.Count; i++)
                {
                    if (hediffDef.comps[i] is HediffCompProperties_ShapeshiftCore coreProps && coreProps.formDef != null)
                    {
                        if (!_formToHediffDefs.TryGetValue(coreProps.formDef, out var list))
                        {
                            list = new List<HediffDef>(2);
                            _formToHediffDefs[coreProps.formDef] = list;
                        }
                        list.Add(hediffDef);
                    }
                }
            }
        }
    }
}
