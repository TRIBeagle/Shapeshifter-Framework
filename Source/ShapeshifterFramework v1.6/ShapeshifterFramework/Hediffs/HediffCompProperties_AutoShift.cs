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
        /// <summary>변신할 ShapeshiftFormDef의 defName.</summary>
        public string formDefName;

        /// <summary>체력 비율이 이 값 미만이면 트리거. null이면 미사용. 예: 0.3 = 30%.</summary>
        public float? healthThreshold;

        /// <summary>이 정신 상태 중 하나 발동 시 트리거. null이면 미사용.</summary>
        public List<MentalStateDef> triggerMentalStates;

        /// <summary>SunGlow가 이 값 미만이면 트리거. null이면 미사용. 예: 0.5=밤, 0.3=깊은 밤.</summary>
        public float? triggerSunGlowBelow;

        /// <summary>전투 중(징집 + 근처 적대 폰) 트리거. false면 미사용.</summary>
        public bool triggerInCombat = false;

        /// <summary>조건 검사 간격 (틱). 기본 120 = 2초.</summary>
        public int checkIntervalTicks = 120;

        /// <summary>조건 충족 시 변신 성공 확률. 기본 1.0 = 100%.</summary>
        public float successChance = 1f;

        /// <summary>true면 발동 후 이 hediff 자체를 제거 (1회성).</summary>
        public bool triggerOnce = false;

        public HediffCompProperties_AutoShift()
        {
            compClass = typeof(HediffComp_AutoShift);
        }
    }
}
