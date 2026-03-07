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

        /// <summary>
        /// 필수: 적용할 폼의 <c>defName</c>.
        /// 유효하지 않은 이름이면 아무 일도 하지 않도록 처리하는 것이 권장된다.
        /// </summary>
        public string formDefName;

        /// <summary>
        /// 성공 확률(0~1). 기본값 1.0f.
        /// 처리부에서 [0,1] 범위로 클램프해 사용하는 것을 권장.
        /// </summary>
        public float successChance = 1f;

        /// <summary>
        /// 선택: AoE(타일) 반경. 0 이하이면 단일 타겟만 적용.
        /// 반경 &gt; 0이면 중심 명중 지점 주변의 Pawn들에게도 동일 효과 적용.
        /// </summary>
        public float aoeRadius = 0f;

        // (확장 포인트) 이후 필요 시 추가:
        // public bool excludeMechanoids = true;
        // public List<string> requiredTags;

        #endregion
    }
}
