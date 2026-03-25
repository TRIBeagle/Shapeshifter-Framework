// ShapeshifterFramework | Compat | HARFormExtension.cs
// 목적 : Humanoid Alien Races 연동 필드를 ShapeshiftFormDef에서 분리한 DefModExtension.
// 용도 : FormDef의 <modExtensions>에 추가하여 HAR 모드 설치 시에만 활성화.

using Verse;

namespace ShapeshifterFramework.Compat
{
    /// <summary>HAR 폼 오버라이드 확장.</summary>
    public class HARFormExtension : DefModExtension
    {
        public bool showHarAddons = false;
    }
}
