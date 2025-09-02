// Compat_HAR_AddComp.cs
// 목적: HAR(AlienRace) ThingDef에 CompProperties_Shapeshifter 자동 주입
// 정책: 즉시 로그 X → 메트릭 적재 후 ReportOnce()에서 요약 출력
//      모드 비활성 시 무동작/무로그
//      반복 경고 억제는 ShapeshiftCompat(HAR.HasFailed)로 처리

using ShapeshifterFramework.Comps;
using ShapeshifterFramework.Utilities;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ShapeshifterFramework.Compat
{
    [StaticConstructorOnStartup]
    internal static class Compat_HAR_AddComp
    {
        static Compat_HAR_AddComp()
        {
            if (!ShapeshiftCompat.HAR.IsActive) return;

            var harThingDefType = ShapeshiftReflectionCache.TryType("AlienRace.ThingDef_AlienRace");
            if (harThingDefType == null)
            {
                if (!ShapeshiftCompat.HAR.HasFailed("AddComp:TypeMissing"))
                    ShapeshiftCompat.HAR.Failed("AddComp:TypeMissing", "ThingDef_AlienRace type missing");
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
                int before = def.comps.Count(c => c is CompProperties_Shapeshifter);
                if (before > 0)
                {
                    def.comps.RemoveAll(c => c is CompProperties_Shapeshifter);
                    deduped += before;
                }

                def.comps.Add(new CompProperties_Shapeshifter());
                added++;
            }

            ShapeshiftCompat.HAR.MetricSet("AddComp", "added", added);
            ShapeshiftCompat.HAR.MetricSet("AddComp", "deduped", deduped);
            ShapeshiftCompat.HAR.Patched("AddComp");
        }

        // ReportAllOnce() 전에 실행 보장 트리거
        internal static void EnsureInitialized() { /* .cctor 보장용 no-op */ }
    }
}
