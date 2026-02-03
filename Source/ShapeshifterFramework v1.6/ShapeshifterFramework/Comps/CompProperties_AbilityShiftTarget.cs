// ShapeshifterFramework | Comps | CompProperties_AbilityShiftTarget.cs
// 목적   : 대상 Pawn을 지정된 폼(defName)으로 변신시키는 Ability 속성 정의
// 용도   : CompAbilityEffect_ShiftTarget에서 참조하여 변신 로직 실행
// 변경   : 2025-09-22 신규 작성 (주석 규칙 일원화 적용)

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
