// ShapeshifterFramework | Hediffs | Hediff_ShapeshiftForm.cs
// 목적 : 변신 상태를 나타내는 커스텀 헤디프. 기즈모 위임 + 건강 탭 툴팁 표시.
// 용도 : GetGizmos → Core에 위임, TipStringExtra → 남은 시간/무제한 표시.
//        수명 관리는 severity 기반(바닐라 기본). 정리는 CompPostPostRemoved에서 수행.

using RimWorld;
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
    }
}
