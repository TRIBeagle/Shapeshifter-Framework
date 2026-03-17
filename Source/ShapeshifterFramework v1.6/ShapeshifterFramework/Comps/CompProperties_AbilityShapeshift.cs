// ShapeshifterFramework | Comps | CompProperties_AbilityShapeshift.cs
// 목적 : XML에서 Ability(초능력/마법)의 변신 효과를 정의하기 위한 속성(Properties) 클래스.
// 용도 : 대상에게 적용할 hediffDef를 보관.
//        캐스트 조건(종족/뮤턴트)도 여기서 정의하여 ShouldHideGizmo/CanApplyOn에서 검사.

using RimWorld;
using ShapeshifterFramework.Utilities;
using System.Collections.Generic;
using Verse;

namespace ShapeshifterFramework.Comps
{
    /// <summary>Ability 변신 효과 속성 정의.</summary>
    public class CompProperties_AbilityShapeshift : CompProperties_AbilityEffect
    {
        /// <summary>변신 적용에 사용할 HediffDef (HediffComp_ShapeshiftCore 포함 필수).</summary>
        public Verse.HediffDef hediffDef;

        // ── 캐스트 조건: 종족/뮤턴트 필터 ──
        // 조건 미충족 시 ShouldHideGizmo → true, CanApplyOn → false
        public List<ThingDef> allowedRaces;
        public List<ThingDef> disallowedRaces;

        [MayRequire("Ludeon.RimWorld.Anomaly")]
        public List<MutantDef> allowedMutants;
        [MayRequire("Ludeon.RimWorld.Anomaly")]
        public List<MutantDef> disallowedMutants;

        // ── AoE 팩션 필터 ──
        // true 시 캐스터에 적대인 폰만 Apply, 아군/중립 스킵
        public bool affectHostileOnly;

        // ── 저항 판정 (적대 대상 전용) ──
        // 최종 성공률 = baseSuccessChance × target.GetStatValue(resistStat)
        // resistStat이 null이면 저항 체크 없이 항상 성공.
        // 아군 대상은 바닐라 Psycast 패턴에 따라 저항 체크 생략.

        /// <summary>기본 성공 확률 (0~1). 기본값 1 = 항상 성공.</summary>
        public float baseSuccessChance = 1f;

        /// <summary>저항에 사용할 StatDef. 예: PsychicSensitivity, ToxicResistance 등. null이면 저항 체크 생략.</summary>
        public StatDef resistStat;

        /// <summary>스탯 방향성. Sensitivity=높을수록 취약, Resistance=높을수록 면역.</summary>
        public ResistMode resistMode = ResistMode.Sensitivity;

        // ── 변신 중 시전 허용 폼 (defName 문자열) ──
        // null/비어있으면: 변신 중 시전 불가 (기즈모 비활성)
        public List<string> allowedFromForms;

        public CompProperties_AbilityShapeshift()
        {
            compClass = typeof(CompAbilityEffect_Shapeshift);
        }
    }
}
