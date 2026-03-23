// ShapeshifterFramework | Comps | CompProperties_UseEffect_ExtendShapeshift.cs
// 목적 : 아이템 사용 시 변신 지속 시간을 연장하는 CompUseEffect의 속성 클래스.
// 용도 : extendTicks, targetFormDef, allowExtendBeyondMax를 XML에서 정의.

using Verse;

namespace ShapeshifterFramework.Comps
{
    /// <summary>아이템 사용 변신 연장 효과 속성 정의.</summary>
    public class CompProperties_UseEffect_ExtendShapeshift : CompProperties
    {
        /// <summary>연장할 틱 수 (양수: 연장, 음수: 단축).</summary>
        public int extendTicks;

        /// <summary>특정 폼에서만 연장 허용. null이면 현재 폼 종류 무관.</summary>
        public string targetFormDef;

        /// <summary>true면 원래 폼 최대 시간을 초과하여 연장 가능. 기본 false.</summary>
        public bool allowExtendBeyondMax;

        public CompProperties_UseEffect_ExtendShapeshift()
        {
            compClass = typeof(CompUseEffect_ExtendShapeshift);
        }
    }
}
