// ShapeshifterFramework | Hediffs | Hediff_ShapeshiftForm.cs
// 목적 : 변신 상태를 나타내는 커스텀 헤디프. 기즈모 위임.
// 용도 : GetGizmos → Core에 위임. 툴팁은 HediffComp_ShapeshiftCore.CompTipStringExtra에서 처리.
//        수명 관리는 severity 기반(바닐라 기본). 정리는 CompPostPostRemoved에서 수행.

using System.Collections.Generic;
using Verse;

namespace ShapeshifterFramework.Hediffs
{
    /// <summary>변신 폼 활성 상태를 나타내는 헤디프. 기즈모를 Core에 위임.</summary>
    public class Hediff_ShapeshiftForm : HediffWithComps
    {
        // TryGetComp는 comps 리스트 선형 탐색 → 매 GetGizmos 호출마다 반복되므로 캐시
        private HediffComp_ShapeshiftCore _cachedCore;

        /// <summary>이 Hediff에 부착된 HediffComp_ShapeshiftCore 접근 (캐시).</summary>
        public HediffComp_ShapeshiftCore Core =>
            _cachedCore ?? (_cachedCore = this.TryGetComp<HediffComp_ShapeshiftCore>());

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
    }
}
