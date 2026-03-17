// ShapeshifterFramework | Comps | CompProperties_UseEffect_Shapeshift.cs
// 목적 : 소비형 아이템(Use/Ingest)을 사용했을 때 발생하는 변신 효과를 XML에 정의하기 위한 속성 클래스.
// 용도 : 적용할 폼(formDefName)과 성공 확률(successChance)을 보관하며, CompUseEffect_Shapeshift과 연결됨.

using RimWorld;

namespace ShapeshifterFramework.Comps
{
    /// <summary>아이템 사용 변신 효과 속성 정의.</summary>
    public class CompProperties_UseEffect_Shapeshift : CompProperties_UseEffect
    {
        public string formDefName;
        /// <summary>HediffDef 기반 변신 (Phase 2). hediffDef가 지정되면 formDefName보다 우선 사용.</summary>
        public Verse.HediffDef hediffDef;
        public float successChance = 1.0f;

        public CompProperties_UseEffect_Shapeshift()
        {
            compClass = typeof(CompUseEffect_Shapeshift);
        }
    }
}
