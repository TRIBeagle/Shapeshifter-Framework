// ShapeshifterFramework | Compat | FAFormExtension.cs
// 목적 : Facial Animation 연동 필드를 ShapeshiftFormDef에서 분리한 DefModExtension.
// 용도 : FormDef의 <modExtensions>에 추가하여 FA 모드 설치 시에만 활성화.

using Verse;

namespace ShapeshifterFramework.Compat
{
    /// <summary>Facial Animation 폼 오버라이드 확장.</summary>
    public class FAFormExtension : DefModExtension
    {
        public string faHeadTypeDef;
        public string faEyeballTypeDef;
        public string faLidTypeDef;
        public string faBrowTypeDef;
        public string faMouthTypeDef;
        public string faSkinTypeDef;
        public ColorInt? faEyeColor;
        public ColorInt? faEyeColor2;
    }
}
