// ShapeshifterFramework | Compat | Compat_HAR_AddComp.cs
// 목적 : HAR ThingDef_AlienRace에 CompProperties_Shapeshifter 자동 주입
// 용도 : Human 제외, 중복 제거 포함. 결과는 CompatManager 메트릭으로 보고

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
