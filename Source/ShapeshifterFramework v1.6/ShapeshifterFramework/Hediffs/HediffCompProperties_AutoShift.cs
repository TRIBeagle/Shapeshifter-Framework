// ShapeshifterFramework | Hediffs | HediffCompProperties_AutoShift.cs
// 목적 : 조건부 자동 변신 HediffComp의 XML 프로퍼티 정의.
// 용도 : - 특정 hediff를 보유한 Pawn이 체력/정신상태/밝기/전투 조건 충족 시 자동 변신.
//        - 모더가 XML에서 늑대인간 저주, 버서커 유전자 등 수동 변신 패턴을 구성하는 데이터 클래스.

using RimWorld;
using System.Collections.Generic;
using Verse;

namespace ShapeshifterFramework.Hediffs
{
    /// <summary>조건부 자동 변신 프로퍼티.</summary>
    public class HediffCompProperties_AutoShift : HediffCompProperties
    {
        /// <summary>변신 적용에 사용할 HediffDef (HediffComp_ShapeshiftCore 포함 필수).</summary>
        public HediffDef hediffDef;

        /// <summary>체력 비율이 이 값 미만이면 트리거. null이면 미사용. 예: 0.3 = 30%.</summary>
        public float? healthThreshold;

        /// <summary>이 정신 상태 중 하나 발동 시 트리거. null이면 미사용.</summary>
        public List<MentalStateDef> triggerMentalStates;

        /// <summary>SunGlow가 이 값 미만이면 트리거. null이면 미사용. 예: 0.5=밤, 0.3=깊은 밤.</summary>
        public float? triggerSunGlowBelow;

        /// <summary>이 hediff의 severity가 해당 값 이상이면 트리거. null이면 미사용. 예: 0.7 = severity 70% 이상.</summary>
        public float? severityThreshold;

        /// <summary>전투 중(징집 + 근처 적대 폰) 트리거. false면 미사용.</summary>
        public bool triggerInCombat = false;

        /// <summary>조건 검사 간격 (틱). 기본 120 = 2초.</summary>
        public int checkIntervalTicks = 120;

        /// <summary>true면 발동 후 이 hediff 자체를 제거 (1회성).</summary>
        public bool triggerOnce = false;

        /// <summary>true면 모든 조건이 동시 충족되어야 발동 (AND). false면 하나만 충족해도 발동 (OR, 기본값).</summary>
        public bool requireAllConditions = false;

        public HediffCompProperties_AutoShift()
        {
            compClass = typeof(HediffComp_AutoShift);
        }

        public override IEnumerable<string> ConfigErrors(HediffDef parentDef)
        {
            foreach (var e in base.ConfigErrors(parentDef)) yield return e;

            if (hediffDef == null)
                yield return $"{parentDef.defName}: HediffCompProperties_AutoShift.hediffDef is null — AutoShift will not fire";
            else if (hediffDef.CompProps<HediffCompProperties_ShapeshiftCore>() == null)
                yield return $"{parentDef.defName}: AutoShift.hediffDef '{hediffDef.defName}' has no HediffComp_ShapeshiftCore comp — transform will not occur";

            bool anyTrigger = healthThreshold.HasValue
                || (triggerMentalStates != null && triggerMentalStates.Count > 0)
                || triggerSunGlowBelow.HasValue || severityThreshold.HasValue || triggerInCombat;
            if (!anyTrigger)
                yield return $"{parentDef.defName}: HediffCompProperties_AutoShift has no trigger condition defined — it will never fire";
        }
    }
}
