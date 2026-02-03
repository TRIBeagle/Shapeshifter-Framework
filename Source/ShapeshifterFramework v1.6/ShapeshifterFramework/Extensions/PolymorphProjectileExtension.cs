// ShapeshifterFramework | Extensions | PolymorphProjectileExtension.cs
// 목적  : 투사체(Projectile) Def에 “명중 시 폴리모프” 동작을 부여하기 위한 확장 데이터 컨테이너.
// 용도  : ThingDef(Projectile) 혹은 Verb/Ability의 Def에 <modExtensions>로 붙여서
//         명중 대상에게 폼 적용(확률/범위 설정)을 지시한다.
// 변경  : 2025-09-23 v1.0 — 프로젝트 주석 규칙 적용(주석만 정리, 로직 변경 없음).

using Verse;

namespace ShapeshifterFramework.Extensions
{
    /// <summary>
    /// 투사체가 명중했을 때 대상에게 변신 폼을 적용하기 위한 확장 데이터.
    /// 실제 효과 적용은 별도 처리기(예: <see cref="ShapeshifterFramework.Utilities.ShapeshiftTargetUtility"/> 호출부)에서 수행한다.
    /// </summary>
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
