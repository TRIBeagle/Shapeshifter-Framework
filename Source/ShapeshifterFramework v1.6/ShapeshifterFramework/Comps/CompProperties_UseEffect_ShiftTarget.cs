// ShapeshifterFramework | Comps | CompProperties_UseEffect_ShiftTarget.cs
// 목적 : 소비형 아이템(Use/Ingest)을 사용했을 때 발생하는 변신 효과를 XML에 정의하기 위한 속성 클래스.
// 용도 : 적용할 폼(formDefName)과 성공 확률(successChance)을 보관하며, CompUseEffect_ShiftTarget과 연결됨.

using RimWorld;

namespace ShapeshifterFramework.Comps
{
    /// <summary>
    /// 아이템 사용 시 Pawn을 변신시키는 속성 정의.
    /// - <see cref="formDefName"/> : 변신시킬 대상 폼(defName)
    /// - <see cref="successChance"/> : 성공 확률 (0~1, 기본 1.0)
    /// </summary>
    public class CompProperties_UseEffect_ShiftTarget : CompProperties_UseEffect
    {
        /// <summary>
        /// 변신시킬 대상 폼(defName).
        /// </summary>
        public string formDefName;

        /// <summary>
        /// 변신 성공 확률 (0~1). 기본값은 1.0.
        /// </summary>
        public float successChance = 1.0f;

        /// <summary>
        /// 생성자: 본 속성과 연결된 실행 클래스 <see cref="CompUseEffect_ShiftTarget"/> 지정.
        /// </summary>
        public CompProperties_UseEffect_ShiftTarget()
        {
            compClass = typeof(CompUseEffect_ShiftTarget);
        }
    }
}
