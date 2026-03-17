// ShapeshifterFramework | Extensions | PolymorphProjectileExtension.cs
// 목적 : 총알이나 마법 등 투사체(Projectile) 명중 시 대상을 변신시키기 위한 속성(Data Container) 확장 클래스.
// 용도 : ThingDef의 <modExtensions>에 부착되어, 타겟에게 적용할 hediffDef와 광역 적용을 위한 반경(aoeRadius) 설정값을 제공함.

using RimWorld;
using ShapeshifterFramework.Utilities;
using Verse;

namespace ShapeshifterFramework.Extensions
{
    /// <summary>투사체 명중 시 대상에게 변신 폼을 적용하기 위한 확장 데이터.</summary>
    public class PolymorphProjectileExtension : DefModExtension
    {
        #region 설정 필드

        /// <summary>변신 적용에 사용할 HediffDef (HediffComp_ShapeshiftCore 포함 필수).</summary>
        public Verse.HediffDef hediffDef;

        // AoE 반경. 0 이하면 단일 타겟만 적용
        public float aoeRadius = 0f;

        // true면 AoE가 아군 포함 모든 폰에 적용. false(기본)면 시전자에게 적대적인 폰만.
        public bool affectAllies = false;

        // ── 저항 판정 ──
        // 최종 성공률 = baseSuccessChance × target.GetStatValue(resistStat)
        // resistStat이 null이면 저항 체크 없이 항상 성공.
        // 투사체는 부정적 효과이므로 아군 면제 없이 모든 대상에게 저항 체크 수행.

        /// <summary>기본 성공 확률 (0~1). 기본값 1 = 항상 성공.</summary>
        public float baseSuccessChance = 1f;

        /// <summary>저항에 사용할 StatDef. 예: PsychicSensitivity, ToxicResistance 등. null이면 저항 체크 생략.</summary>
        public StatDef resistStat;

        /// <summary>스탯 방향성. Sensitivity=높을수록 취약, Resistance=높을수록 면역.</summary>
        public ResistMode resistMode = ResistMode.Sensitivity;

        #endregion
    }
}
