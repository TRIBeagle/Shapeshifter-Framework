// ShapeshifterFramework | Hediffs | Hediff_ShapeshiftForm.cs
// 목적 : 변신 상태를 증명하고, 바닐라 헤디프 시스템을 통해 건강 탭에 폼의 스탯/능력치 변동을 직관적으로 표시하기 위한 커스텀 헤디프.
// 용도 : 실제 스탯 연산은 바닐라 엔진에 전적으로 위임하고, UI 툴팁에 '폼 이름'과 '남은 시간'만 가볍게 덧그려 성능 저하 없이 바닐라 UI에 녹아들게 함.

using RimWorld;
using ShapeshifterFramework.Comps;
using System.Text;
using Verse;

namespace ShapeshifterFramework.Hediffs
{
    /// <summary>변신 폼 활성 상태를 나타내는 헤디프. 건강 탭에 스탯 변동과 남은 시간을 표시.</summary>
    public class Hediff_ShapeshiftForm : HediffWithComps
    {
        // CompShapeshifter 캐시(매 프레임 TryGetComp 호출 방지)
        private CompShapeshifter _cachedComp;

        private CompShapeshifter Comp
        {
            get
            {
                if (_cachedComp == null && pawn != null)
                    _cachedComp = pawn.TryGetComp<CompShapeshifter>();
                return _cachedComp;
            }
        }

        /// <summary>건강 탭 툴팁에 남은 시간 추가.</summary>
        public override string TipStringExtra
        {
            get
            {
                var sb = new StringBuilder();

                var comp = Comp;
                if (comp != null && comp.isTransformed && comp.currentForm != null)
                {
                    var form = comp.currentForm;
                    // 1. 지속 시간이 있는 폼일 경우
                    if (form.durationTicks.HasValue && form.durationTicks.Value > 0)
                    {
                        int remain = comp.RemainingShapeshiftTicks;
                        if (remain > 0)
                        {
                            string timeStr = GenDate.ToStringTicksToPeriod(remain,
                                allowSeconds: false, shortForm: true);
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

        // 디버그 부여 방어 및 자동 소멸 로직
        public override bool ShouldRemove
        {
            get
            {
                var comp = Comp;
                // 1. 폰에게 Shapeshifter 컴프가 없거나
                // 2. 변신 상태가 아니거나
                // 3. 부여된 헤디프가 현재 폼의 공식 헤디프가 아니면 삭제
                if (comp == null || !comp.isTransformed || comp.currentForm == null || comp.currentForm.generatedStatHediff != this.def)
                {
                    return true;
                }

                // 정식 변신 상태라면 바닐라 엔진이 멋대로 지우지 못하게 보호
                return false;
            }
        }

        /// <summary>세이브/로드.</summary>
        public override void ExposeData()
        {
            base.ExposeData();
        }
    }
}