// ShapeshifterFramework | Compat | Compat_HAR_AddComp.cs
// 목적 : Humanoid Alien Races (HAR) 기반의 커스텀 외계 종족들에게도 변신 능력을 부여하기 위한 컴포넌트 자동 주입.
// 용도 : 게임 로딩(StaticConstructorOnStartup) 시점에 HAR이 활성화되어 있다면, XML에 정의된 모든 ThingDef_AlienRace (Human 제외)의 comps 리스트에 CompProperties_Shapeshifter를 동적으로 추가.
// 주의 : 이미 컴포넌트가 추가되어 있을 경우를 대비해 중복을 제거(deduped)하는 로직이 포함되어 있으며, 주입 결과는 CompatManager를 통해 메트릭으로 보고됨.

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
