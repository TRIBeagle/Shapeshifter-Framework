// ShapeshifterFramework | Extensions | PolymorphProjectileExtension.cs
// 목적 : 총알이나 마법 등 투사체(Projectile) 명중 시 대상을 변신시키기 위한 속성(Data Container) 확장 클래스.
// 용도 : ThingDef의 <modExtensions>에 부착되어, 타겟에게 적용할 폼(formDefName), 변신 성공 확률(successChance), 그리고 광역 적용을 위한 반경(aoeRadius) 설정값을 제공함.

using Verse;

namespace ShapeshifterFramework.Extensions
{
    /// <summary>투사체 명중 시 대상에게 변신 폼을 적용하기 위한 확장 데이터.</summary>
    public class PolymorphProjectileExtension : DefModExtension
    {
        #region 설정 필드

        // 적용할 폼의 defName (필수)
        public string formDefName;

        // 성공 확률(0~1), 기본 1.0
        public float successChance = 1f;

        // AoE 반경. 0 이하면 단일 타겟만 적용
        public float aoeRadius = 0f;

        // true면 AoE가 아군 포함 모든 폰에 적용. false(기본)면 시전자에게 적대적인 폰만.
        public bool affectAllies = false;

        #endregion
    }
}
