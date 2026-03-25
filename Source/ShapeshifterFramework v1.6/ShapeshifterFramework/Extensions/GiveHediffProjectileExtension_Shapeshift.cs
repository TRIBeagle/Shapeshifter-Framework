// ShapeshifterFramework | Extensions | GiveHediffProjectileExtension_Shapeshift.cs
// 목적 : 투사체(Projectile) 명중 시 대상에게 HediffDef를 부여하기 위한 속성(Data Container) 확장 클래스.
// 용도 : ThingDef의 <modExtensions>에 부착되어, 타겟에게 적용할 hediffDef와 광역 적용을 위한 반경(aoeRadius) 설정값을 제공함.

using Verse;

namespace ShapeshifterFramework.Extensions
{
    /// <summary>투사체 명중 시 대상에게 HediffDef를 부여하기 위한 확장 데이터.</summary>
    public class GiveHediffProjectileExtension_Shapeshift : DefModExtension
    {
        #region 설정 필드

        /// <summary>부여할 HediffDef.</summary>
        public Verse.HediffDef hediffDef;

        // AoE 반경. 0 이하면 단일 타겟만 적용
        public float aoeRadius = 0f;

        // true면 AoE가 아군 포함 모든 폰에 적용. false(기본)면 시전자에게 적대적인 폰만.
        public bool affectAllies = false;

        #endregion
    }
}
