// ShapeshifterFramework | Hediffs | Hediff_ShapeshiftForm.cs
// 목적 : 변신 상태를 증명하고, 바닐라 헤디프 시스템을 통해 건강 탭에 폼의 스탯/능력치 변동을 직관적으로 표시하기 위한 커스텀 헤디프.
// 용도 : 실제 스탯 연산은 바닐라 엔진에 전적으로 위임하고, UI 툴팁에 '폼 이름'과 '남은 시간'만 가볍게 덧그려 성능 저하 없이 바닐라 UI에 녹아들게 함.
//        기즈모(해제/verb)는 Core HediffComp에 위임.

using RimWorld;
using ShapeshifterFramework.Utilities;
using System.Collections.Generic;
using System.Text;
using Verse;

namespace ShapeshifterFramework.Hediffs
{
    /// <summary>변신 폼 활성 상태를 나타내는 헤디프. 건강 탭에 스탯 변동과 남은 시간을 표시.</summary>
    public class Hediff_ShapeshiftForm : HediffWithComps
    {
        /// <summary>이 Hediff에 부착된 HediffComp_ShapeshiftCore 접근.</summary>
        public HediffComp_ShapeshiftCore Core =>
            this.TryGetComp<HediffComp_ShapeshiftCore>();

        /// <summary>기즈모 생성 — Core에 위임.</summary>
        public override IEnumerable<Gizmo> GetGizmos()
        {
            // 바닐라 기즈모 먼저
            foreach (var g in base.GetGizmos())
                yield return g;

            // Core 기즈모(해제/verb 등)
            var core = Core;
            if (core != null)
            {
                foreach (var g in core.GetGizmosExtra())
                    yield return g;
            }
        }

        /// <summary>건강 탭 툴팁에 남은 시간 추가.</summary>
        public override string TipStringExtra
        {
            get
            {
                var sb = new StringBuilder();

                var core = Core;
                if (core != null && core.isTransformed && core.currentForm != null)
                {
                    // 1. 지속 시간이 있는 폼일 경우
                    var resolvedDuration = core.ResolvedDurationTicks;
                    if (resolvedDuration.HasValue && resolvedDuration.Value > 0)
                    {
                        int remain = core.RemainingShapeshiftTicks;
                        if (remain > 0)
                        {
                            string timeStr = GenDate.ToStringTicksToPeriod(remain,
                                allowSeconds: false, shortForm: false);
                            sb.AppendLine("SSF_Inspect_Remaining".Translate(timeStr));
                        }
                    }
                    // 2. 무제한(지속 시간이 없는) 폼일 경우
                    else
                    {
                        sb.AppendLine("SSF_Inspect_Permanent".Translate());
                    }
                }

                // 바닐라가 생성한 스탯 설명을 가져옴(statOffsets, statFactors, capMods)
                string baseTip = base.TipStringExtra;
                if (!string.IsNullOrEmpty(baseTip))
                    sb.Append(baseTip);

                return sb.ToString().TrimEnd();
            }
        }

        // 자동 소멸 로직 — Core 상태 기반
        public override bool ShouldRemove
        {
            get
            {
                var core = Core;

                // 초기화 진행 중이면 보호 (자동 소멸 방지)
                if (core?.needsInit == true)
                    return false;

                // Core가 없거나 변신 상태가 아니면 삭제
                if (core == null || !core.isTransformed || core.currentForm == null)
                    return true;

                // 정식 변신 상태라면 바닐라 엔진이 멋대로 지우지 못하게 보호
                return false;
            }
        }

        /// <summary>Hediff 제거 시 레지스트리 해제.</summary>
        public override void PostRemoved()
        {
            base.PostRemoved();

            // 방어적 레지스트리 해제
            if (pawn != null)
                ShapeshiftRegistry.Unregister(pawn);
        }

        /// <summary>세이브/로드.</summary>
        public override void ExposeData()
        {
            base.ExposeData();
        }
    }
}
