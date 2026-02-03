// ShapeshifterFramework | Compat | Compat_HAR_AddComp.cs
// 목적   : HAR(AlienRace) 계열 ThingDef에 CompProperties_Shapeshifter를 자동 주입한다.
// 용도   : 게임 시작 시(StaticConstructorOnStartup) HAR 활성 여부를 확인하고,
//          AlienRace.ThingDef_AlienRace 파생 ThingDef 중 Human을 제외한 대상에 컴프를 주입한다.
// 변경   : 2025-09-22 v1.0 — 프로젝트 주석 규칙 적용(주석 정리만, 로직 변경 없음).
// 주의   : 모드 비활성 시 무동작/무로그. 동일 경고는 CompatManager.HAR.ReportOnce 계열로 억제.
// 비고   : 즉시 로그 대신 메트릭 적재 후 ReportOnce() 시점에서 요약 출력(added/deduped).

using ShapeshifterFramework.Comps;
using ShapeshifterFramework.Utilities;
using System.Collections.Generic;
using Verse;

namespace ShapeshifterFramework.Compat
{
    /// <summary>
    /// HAR(AlienRace) 연동: AlienRace.ThingDef_AlienRace 파생 ThingDef에
    /// <see cref="CompProperties_Shapeshifter"/>를 자동 주입한다.
    /// - 대상: ThingDef_AlienRace 파생이며 defName != "Human"
    /// - 중복 방지: 기존 동일 컴프 제거 후 1회만 추가
    /// - 보고: 즉시 로그 대신 Metric(added/deduped) 누적, 이후 ReportOnce에서 출력
    /// 전제:
    /// - HAR 모드 활성(CompatManager.HAR.IsActive)
    /// 부작용:
    /// - 없음(중복 제거 후 추가)
    /// </summary>
    [StaticConstructorOnStartup]
    internal static class Compat_HAR_AddComp
    {
        /// <summary>
        /// Static .cctor — 시작 시 1회 실행되어 대상 ThingDef에 컴프를 주입한다.
        /// </summary>
        static Compat_HAR_AddComp()
        {
            if (!CompatManager.HAR.IsActive) return;

            var harThingDefType = ShapeshiftReflectionCache.TryType("AlienRace.ThingDef_AlienRace");
            if (harThingDefType == null)
            {
                // [안전] 외부 타입 미탐지: 동일 id 1회만 실패 기록
                if (!CompatManager.HAR.HasFailed("AddComp:TypeMissing"))
                    CompatManager.HAR.Failed("AddComp:TypeMissing", "ThingDef_AlienRace type missing");
                return;
            }

            int added = 0, deduped = 0;
            var allDefs = DefDatabase<ThingDef>.AllDefsListForReading;

            for (int i = 0; i < allDefs.Count; i++)
            {
                var def = allDefs[i];
                if (def == null) continue;

                var t = def.GetType();
                if (t == null || !harThingDefType.IsAssignableFrom(t)) continue;
                if (def.defName == "Human") continue; // Human은 XML로 처리

                if (def.comps == null) def.comps = new List<CompProperties>();

                // 중복 제거(동일 컴프가 여러 번 붙어 있는 경우 정리)
                int removed = def.comps.RemoveAll(c => c is CompProperties_Shapeshifter);
                if (removed > 0) deduped += removed;

                def.comps.Add(new CompProperties_Shapeshifter());
                added++;
            }

            // 즉시 로그 대신 메트릭 누적(ReportOnce에서 요약)
            CompatManager.HAR.MetricSet("AddComp", "added", added);
            CompatManager.HAR.MetricSet("AddComp", "deduped", deduped);
            CompatManager.HAR.Patched("AddComp");
        }

        /// <summary>
        /// 외부에서 .cctor 실행 보장을 강제하기 위한 no-op 트리거.
        /// </summary>
        internal static void EnsureInitialized() { /* .cctor 보장용 no-op */ }
    }
}
