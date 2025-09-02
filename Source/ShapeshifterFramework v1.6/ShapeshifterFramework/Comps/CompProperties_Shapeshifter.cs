// CompProperties_Shapeshifter.cs
// 목적: 변신 로직 컴포넌트(CompShapeshifter)와 연결되는 설정 컨테이너.
// 용도: ThingDef에 compClass로 붙여 CompShapeshifter를 활성화.
// 주의: 동작은 모두 CompShapeshifter에 구현됨. Def 변화는 재로드 필요.

using Verse;

namespace ShapeshifterFramework.Comps
{
    public class CompProperties_Shapeshifter : CompProperties
    {
        public CompProperties_Shapeshifter()
        {
            this.compClass = typeof(CompShapeshifter);
        }
    }
}
