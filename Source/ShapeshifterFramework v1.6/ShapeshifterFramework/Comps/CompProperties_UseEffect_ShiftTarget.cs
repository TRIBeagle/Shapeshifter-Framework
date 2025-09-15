using RimWorld;

namespace ShapeshifterFramework.Comps
{
    // 타겟 필터링은 Ability/아이템 쪽 판정에 맡기고, 여기서는 성공 확률만 본다.
    public class CompProperties_UseEffect_ShiftTarget : CompProperties_UseEffect
    {
        // 필수: 적용할 폼(defName)
        public string formDefName;

        // 성공 확률(0~1). 기본 1.0
        public float successChance = 1.0f;

        public CompProperties_UseEffect_ShiftTarget()
        {
            compClass = typeof(CompUseEffect_ShiftTarget);
        }
    }
}
