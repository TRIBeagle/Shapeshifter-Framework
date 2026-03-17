// ShapeshifterFramework | Comps | CompProperties_UseEffect_Shapeshift.cs
// 목적 : 소비형 아이템(Use/Ingest)을 사용했을 때 발생하는 변신 효과를 XML에 정의하기 위한 속성 클래스.
// 용도 : 적용할 hediffDef를 보관하며, CompUseEffect_Shapeshift과 연결됨.

using RimWorld;

namespace ShapeshifterFramework.Comps
{
    /// <summary>아이템 사용 변신 효과 속성 정의.</summary>
    public class CompProperties_UseEffect_Shapeshift : CompProperties_UseEffect
    {
        /// <summary>변신 적용에 사용할 HediffDef (HediffComp_ShapeshiftCore 포함 필수).</summary>
        public Verse.HediffDef hediffDef;

        public CompProperties_UseEffect_Shapeshift()
        {
            compClass = typeof(CompUseEffect_Shapeshift);
        }
    }
}
