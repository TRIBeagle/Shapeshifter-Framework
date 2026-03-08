// ShapeshifterFramework | Comps | CompProperties_AbilityShiftTarget.cs
// 목적 : XML에서 Ability(초능력/마법)의 변신 효과를 정의하기 위한 속성(Properties) 클래스.
// 용도 : 대상에게 적용할 폼의 defName(formDefName)과 변신 성공 확률(successChance, 기본값 1.0)을 설정값으로 보관하며, CompAbilityEffect_ShiftTarget과 연결(compClass)됨.

using RimWorld;

namespace ShapeshifterFramework.Comps
{
    /// <summary>Ability 변신 효과 속성 정의.</summary>
    public class CompProperties_AbilityShiftTarget : CompProperties_AbilityEffect
    {
        public string formDefName;
        public float successChance = 1.0f;

        public CompProperties_AbilityShiftTarget()
        {
            compClass = typeof(CompAbilityEffect_ShiftTarget);
        }
    }
}
