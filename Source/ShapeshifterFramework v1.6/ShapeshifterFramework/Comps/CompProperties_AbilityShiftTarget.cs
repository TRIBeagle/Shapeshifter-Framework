// ShapeshifterFramework | Comps | CompProperties_AbilityShiftTarget.cs
// 목적 : XML에서 Ability(초능력/마법)의 변신 효과를 정의하기 위한 속성(Properties) 클래스.
// 용도 : 대상에게 적용할 폼의 defName(formDefName)과 변신 성공 확률(successChance, 기본값 1.0)을 설정값으로 보관하며, CompAbilityEffect_ShiftTarget과 연결(compClass)됨.

using RimWorld;

namespace ShapeshifterFramework.Comps
{
    /// <summary>
    /// Ability 사용 시 특정 Pawn을 변신시키는 속성 정의.
    /// - 필수: 적용할 폼(defName)
    /// - 선택: 성공 확률 (0~1)
    /// </summary>
    public class CompProperties_AbilityShiftTarget : CompProperties_AbilityEffect
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
        /// 생성자: 본 속성의 실행 클래스(CompAbilityEffect_ShiftTarget)를 지정.
        /// </summary>
        public CompProperties_AbilityShiftTarget()
        {
            compClass = typeof(CompAbilityEffect_ShiftTarget);
        }
    }
}
