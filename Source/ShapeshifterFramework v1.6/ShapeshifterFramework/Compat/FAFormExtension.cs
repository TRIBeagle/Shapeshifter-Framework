// ShapeshifterFramework | Compat | FAFormExtension.cs
// 목적 : Facial Animation 연동 필드를 ShapeshiftFormDef에서 분리한 DefModExtension.
// 용도 : FormDef의 <modExtensions>에 추가하여 FA 모드 설치 시에만 활성화.

using Verse;

namespace ShapeshifterFramework.Compat
{
    /// <summary>Facial Animation 폼 오버라이드 확장.
    /// 주의: fa*TypeDef는 typed Def가 아닌 string defName이라 FA 미설치/오타 시 로드 에러 없이 조용히 무시됨 — 값 확인은 인게임 외형으로 검증할 것.</summary>
    public class FAFormExtension : DefModExtension
    {
        /// <summary>FA 머리(Head) 타입 defName. 변신 시 적용할 FacialAnimation HeadType.</summary>
        public string faHeadTypeDef;
        /// <summary>FA 눈알(Eyeball) 타입 defName.</summary>
        public string faEyeballTypeDef;
        /// <summary>FA 눈꺼풀(Lid) 타입 defName.</summary>
        public string faLidTypeDef;
        /// <summary>FA 눈썹(Brow) 타입 defName.</summary>
        public string faBrowTypeDef;
        /// <summary>FA 입(Mouth) 타입 defName.</summary>
        public string faMouthTypeDef;
        /// <summary>FA 피부(Skin) 타입 defName.</summary>
        public string faSkinTypeDef;
        /// <summary>FA 눈 색상(주). null이면 기존 색 유지.</summary>
        public ColorInt? faEyeColor;
        /// <summary>FA 눈 색상(보조, 두 번째 눈). null이면 기존 색 유지.</summary>
        public ColorInt? faEyeColor2;
    }
}
