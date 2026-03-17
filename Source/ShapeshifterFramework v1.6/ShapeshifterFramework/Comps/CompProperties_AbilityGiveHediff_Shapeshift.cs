// ShapeshifterFramework | Comps | CompProperties_AbilityGiveHediff_Shapeshift.cs
// 목적 : 바닐라 CompProperties_AbilityGiveHediff를 확장하여 SSF 전용 캐스트 조건을 추가하는 속성 클래스.
// 용도 : hediffDef는 바닐라에서 상속. 종족/뮤턴트 필터, 폼 시전 허용, 적대 전용 등 SSF 고유 조건을 정의.

using RimWorld;
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

        public CompProperties_AbilityGiveHediff_Shapeshift()
        {
            compClass = typeof(CompAbilityEffect_GiveHediff_Shapeshift);
        }
    }
}
