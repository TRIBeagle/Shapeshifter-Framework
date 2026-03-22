// ShapeshifterFramework | Comps | CompProperties_AbilityGiveHediff_Shapeshift.cs
// 목적 : 바닐라 CompProperties_AbilityGiveHediff를 확장하여 SSF 전용 캐스트 조건을 추가하는 속성 클래스.
// 용도 : hediffDef는 바닐라에서 상속. 종족/뮤턴트 필터, 폼 시전 허용, 적대 전용 등 SSF 고유 조건을 정의.
//        ExtraStatSummary로 어빌리티 툴팁에 폼 이름/지속시간 표시.

using RimWorld;
using ShapeshifterFramework.Hediffs;
using System.Collections.Generic;
using Verse;

namespace ShapeshifterFramework.Comps
{
    /// <summary>바닐라 GiveHediff를 확장한 SSF 어빌리티 속성. hediffDef는 바닐라에서 상속.</summary>
    public class CompProperties_AbilityGiveHediff_Shapeshift : CompProperties_AbilityGiveHediff
    {
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

        // ── 변신 중 시전 허용 폼 (defName 문자열) ──
        // null/비어있으면: 변신 중 시전 불가 (기즈모 비활성)
        public List<string> allowedFromForms;

        // 캐시: hediffDef → formDef / durationTicks
        private bool _resolved;
        private ShapeshiftFormDef _formDef;
        private int? _durationTicks;

        public CompProperties_AbilityGiveHediff_Shapeshift()
        {
            compClass = typeof(CompAbilityEffect_GiveHediff_Shapeshift);
        }

        /// <summary>hediffDef에서 폼/지속시간 정보를 캐시.</summary>
        private void ResolveFormInfo()
        {
            if (_resolved) return;
            _resolved = true;
            if (hediffDef == null) return;

            var coreProps = hediffDef.CompProps<HediffCompProperties_ShapeshiftCore>();
            if (coreProps == null) return;

            _formDef = coreProps.formDef;
            // HediffCompProperties 오버라이드 → FormDef 기본값 순서
            _durationTicks = coreProps.durationTicks ?? _formDef?.durationTicks;
        }

        /// <summary>어빌리티 툴팁 StatSummary에 폼 이름/지속시간 표시.</summary>
        public override IEnumerable<string> ExtraStatSummary()
        {
            ResolveFormInfo();

            if (_formDef != null)
            {
                string formName = _formDef.LabelCap.NullOrEmpty() ? _formDef.defName : _formDef.LabelCap.Resolve();
                yield return "SSF_Tooltip_FormName".Translate() + ": " + formName;
            }

            if (_durationTicks.HasValue && _durationTicks.Value > 0)
            {
                string durStr = GenDate.ToStringTicksToPeriod(_durationTicks.Value, allowSeconds: false, shortForm: false);
                yield return "SSF_Tooltip_Duration".Translate() + ": " + durStr;
            }
            else if (_formDef != null)
            {
                yield return "SSF_Tooltip_Duration".Translate() + ": " + "SSF_Inspect_Permanent_Short".Translate();
            }
        }
    }
}
