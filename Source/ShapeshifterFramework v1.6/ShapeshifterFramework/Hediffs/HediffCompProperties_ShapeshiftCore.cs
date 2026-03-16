// ShapeshifterFramework | Hediffs | HediffCompProperties_ShapeshiftCore.cs
// 목적 : HediffComp_ShapeshiftCore를 XML HediffDef에 연결하기 위한 속성 클래스.
// 용도 : XML의 <comps> 리스트에 이 클래스가 포함되면, 해당 Hediff 인스턴스에 HediffComp_ShapeshiftCore가 부착됨.

using Verse;

namespace ShapeshifterFramework.Hediffs
{
    /// <summary>HediffComp_ShapeshiftCore 연결용 속성 정의.</summary>
    public class HediffCompProperties_ShapeshiftCore : HediffCompProperties
    {
        public HediffCompProperties_ShapeshiftCore()
        {
            compClass = typeof(HediffComp_ShapeshiftCore);
        }
    }
}
