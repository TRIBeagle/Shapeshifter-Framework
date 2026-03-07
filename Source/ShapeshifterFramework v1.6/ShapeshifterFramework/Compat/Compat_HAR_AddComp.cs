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
    /// <summary>HAR ThingDef_AlienRace에 CompProperties_Shapeshifter 자동 주입.</summary>
    [StaticConstructorOnStartup]
    internal static class Compat_HAR_AddComp
    {
        /// <summary>시작 시 1회 실행, 대상 ThingDef에 컴프 주입.</summary>
        static Compat_HAR_AddComp()
        {
            if (!CompatManager.HAR.IsActive) return;

            var harThingDefType = ShapeshiftReflectionCache.TryType("AlienRace.ThingDef_AlienRace");
            if (harThingDefType == null)
            {
                // 외부 타입 미탐지 시 1회만 로그
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

                // 중복 제거
                int removed = def.comps.RemoveAll(c => c is CompProperties_Shapeshifter);
                if (removed > 0) deduped += removed;

                def.comps.Add(new CompProperties_Shapeshifter());
                added++;
            }

            // 메트릭 누적, ReportOnce에서 요약
            CompatManager.HAR.MetricSet("AddComp", "added", added);
            CompatManager.HAR.MetricSet("AddComp", "deduped", deduped);
            CompatManager.HAR.Patched("AddComp");
        }

        /// <summary>cctor 실행 보장용 no-op.</summary>
        internal static void EnsureInitialized() { /* .cctor 보장용 no-op */ }
    }
}
