// ShapeshifterFramework | Patches | Patch_VerbTracker_ExposeData_StripFormVerbs.cs
// 목적 : 변신 중인 폰의 VerbTracker.ExposeData에서 ResolvingCrossRefs 시
//        "Replaced verb" 경고를 방지.
// 용도 : ResolvingCrossRefs 단계에서 바닐라 InitVerbs가 directOwner.Tools와
//        저장된 verb loadID를 매칭하는데, 변신 폼 verb의 loadID가
//        현재 ThingDef.tools 상태와 불일치할 수 있음 (게임 재시작 시 ThingDef 초기화).
//        Prefix에서 verbs를 null로 설정하여 매칭 로직 자체를 스킵하고,
//        이후 AllVerbs 접근 시 InitVerbsFromZero가 올바르게 재구축하도록 위임.
// 주의 : LookMode.Deep 원소의 크로스 레퍼런스 해소가 스킵되나,
//        InitVerbsFromZero가 verb를 완전히 재생성하므로 문제 없음.

using HarmonyLib;
using ShapeshifterFramework.Utilities;
using System.Collections.Generic;
using Verse;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch(typeof(VerbTracker), "ExposeData")]
    internal static class Patch_VerbTracker_ExposeData_StripFormVerbs
    {
        [HarmonyPrefix, HarmonyPriority(Priority.High)]
        static void Prefix(ref List<Verb> ___verbs, IVerbOwner ___directOwner)
        {
            // ResolvingCrossRefs 단계에서만 개입
            if (Scribe.mode != LoadSaveMode.ResolvingCrossRefs) return;
            if (___verbs == null || ___verbs.Count == 0) return;

            Pawn pawn = ___directOwner as Pawn;
            if (pawn == null) return;

            // 레지스트리는 PostLoadInit 이전이므로 hediff 직접 탐색
            if (!HasShapeshiftForm(pawn)) return;

            ShapeshiftDiagnostics.Info($"[StripFormVerbs] pawn={pawn.LabelShort}: nulling verbs for ResolvingCrossRefs to skip InitVerbs matching");

            // verbs를 null로 설정 → ExposeData 내부에서
            // Scribe_Collections.Look 후 verbs == null 체크로 조기 리턴
            // → InitVerbs 매칭 스킵 → "Replaced verb" 경고 방지
            // → 이후 AllVerbs 접근 시 InitVerbsFromZero로 재구축
            ___verbs = null;
        }

        /// <summary>폰의 hediff에서 활성 ShapeshiftCore를 직접 탐색.</summary>
        private static bool HasShapeshiftForm(Pawn pawn)
        {
            var hediffs = pawn.health?.hediffSet?.hediffs;
            if (hediffs == null) return false;
            for (int i = 0; i < hediffs.Count; i++)
            {
                var hwc = hediffs[i] as HediffWithComps;
                if (hwc?.comps == null) continue;
                for (int c = 0; c < hwc.comps.Count; c++)
                {
                    if (hwc.comps[c] is Hediffs.HediffComp_ShapeshiftCore core
                        && core.currentForm != null)
                        return true;
                }
            }
            return false;
        }
    }
}
