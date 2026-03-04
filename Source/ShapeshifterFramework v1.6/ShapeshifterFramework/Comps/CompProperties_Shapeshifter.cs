// ShapeshifterFramework | Comps | CompProperties_Shapeshifter.cs
// 목적 : Pawn(또는 ThingDef)에 변신 능력을 부여하기 위한 XML 연결용 속성 클래스.
// 용도 : XML의 <comps> 리스트에 이 클래스가 포함되면, 게임 시작 시 해당 개체에 CompShapeshifter 컴포넌트 인스턴스가 생성되어 부착됨.

using Verse;

namespace ShapeshifterFramework.Comps
{
    /// <summary>
    /// ThingDef에 CompShapeshifter를 연결하기 위한 속성 정의.
    /// - 컴포넌트 동작은 모두 <see cref="CompShapeshifter"/> 내부에 구현됨.
    /// - Def에 이 속성을 선언하면 해당 Pawn/Thing이 변신 가능 개체가 됨.
    /// </summary>
    public class CompProperties_Shapeshifter : CompProperties
    {
        /// <summary>
        /// 생성자: 연결된 실행 클래스 <see cref="CompShapeshifter"/>를 지정한다.
        /// </summary>
        public CompProperties_Shapeshifter()
        {
            compClass = typeof(CompShapeshifter);
        }
    }
}
