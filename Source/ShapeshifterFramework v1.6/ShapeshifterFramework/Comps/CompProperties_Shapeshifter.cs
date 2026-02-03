// ShapeshifterFramework | Comps | CompProperties_Shapeshifter.cs
// 목적   : 변신 로직 컴포넌트(CompShapeshifter)와 연결되는 설정 컨테이너.
// 용도   : ThingDef에 compClass로 지정하여 CompShapeshifter를 활성화.
// 변경   : 2025-09-22 v1.0 — 프로젝트 주석 규칙에 따라 정리 (주석만 수정, 로직 변경 없음).
// 주의   : 실제 동작은 CompShapeshifter에 구현됨. Def 수정 시 재로드 필요.

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
