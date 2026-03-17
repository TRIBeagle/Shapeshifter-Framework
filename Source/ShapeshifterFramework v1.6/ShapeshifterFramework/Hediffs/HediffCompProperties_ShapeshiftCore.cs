// ShapeshifterFramework | Hediffs | HediffCompProperties_ShapeshiftCore.cs
// 목적 : HediffComp_ShapeshiftCore를 XML HediffDef에 연결하기 위한 속성 클래스.
// 용도 : <comps> 리스트에 이 클래스를 추가하면 해당 Hediff 인스턴스에 HediffComp_ShapeshiftCore가 부착됨.
//        formDef로 대상 폼을 지정하고, 나머지 행동 필드는 null이면 FormDef 기본값을 사용.
// 주의 : nullable 필드(int?, bool?)는 null = "FormDef 기본값 사용"을 의미. 명시적 값이 있으면 오버라이드.

using RimWorld;
using System.Collections.Generic;
using Verse;

namespace ShapeshifterFramework.Hediffs
{
    /// <summary>HediffComp_ShapeshiftCore 연결용 속성 정의. HediffDef → FormDef 매핑 + 행동 오버라이드.</summary>
    public class HediffCompProperties_ShapeshiftCore : HediffCompProperties
    {
        // ── 핵심 연결: HediffDef → FormDef ──
        /// <summary>이 HediffDef가 적용할 변신 폼. null이면 런타임에 SetFormDef()로 지정 (디버그/범용).</summary>
        public ShapeshiftFormDef formDef;

        // ── 행동 오버라이드 (null = FormDef 기본값 사용) ──

        /// <summary>변신 지속 틱. null이면 FormDef.durationTicks 사용.</summary>
        public int? durationTicks;

        /// <summary>유저가 기즈모로 변신 해제 가능 여부. null이면 FormDef.canRevertVoluntarily 사용.</summary>
        public bool? canRevertVoluntarily;

        /// <summary>Downed 시 자동 해제 여부. null이면 FormDef.revertOnDowned 사용.</summary>
        public bool? revertOnDowned;

        // ── 유지 조건 오버라이드 (null = FormDef 것 사용) ──

        /// <summary>변신 유지에 필요한 의류. null이면 FormDef.sustainApparels 사용.</summary>
        public List<ThingDef> sustainApparels;

        /// <summary>변신 유지에 필요한 무기. null이면 FormDef.sustainWeapons 사용.</summary>
        public List<ThingDef> sustainWeapons;

        /// <summary>변신 유지에 필요한 hediff. null이면 FormDef.sustainHediffs 사용.</summary>
        public List<HediffDef> sustainHediffs;

        /// <summary>변신 유지에 필요한 유전자 (Biotech). null이면 FormDef.sustainGenes 사용.</summary>
        [MayRequire("Ludeon.RimWorld.Biotech")]
        public List<GeneDef> sustainGenes;

        /// <summary>유지 조건 집계 모드. null이면 FormDef.sustainMode 사용.</summary>
        public SustainMode? sustainMode;

        // ── 해제 시 부산물 오버라이드 (null = FormDef 것 사용) ──

        /// <summary>변신 해제 시 드랍할 아이템. null이면 FormDef.revertDrops 사용.</summary>
        public List<ThingDefCountClass> revertDrops;

        /// <summary>변신 해제 시 부여할 hediff. null이면 FormDef.revertAddHediffs 사용.</summary>
        public List<HediffAddEntry> revertAddHediffs;

        public HediffCompProperties_ShapeshiftCore()
        {
            compClass = typeof(HediffComp_ShapeshiftCore);
        }
    }
}
