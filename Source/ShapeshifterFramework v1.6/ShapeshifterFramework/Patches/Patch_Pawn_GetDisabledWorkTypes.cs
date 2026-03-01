// .NET 4.8 / C# 7.3
// 변신 중, 폼에 지정된 작업을 "추가로" 불가 처리
//  - 대상: Pawn.GetDisabledWorkTypes 직접 패치 (Postfix)
//  - 기존: 컴파일러 생성 FillList 로컬 함수를 HarmonyTargetMethod로 탐색 → RimWorld 업데이트 시 취약
//  - 변경: GetDisabledWorkTypes 자체를 Postfix로 패치 → 반환된 리스트에 폼 작업 추가
//          cachedDisabledWorkTypes가 이미 채워진 경우도 올바르게 처리됨

using HarmonyLib;
using RimWorld;
using ShapeshifterFramework.Comps;
using System.Collections.Generic;
using Verse;

namespace ShapeshifterFramework.Patches
{
    // 게임 로드/시작 시 캐시를 비워주는 초기화 클래스
    [StaticConstructorOnStartup]
    public static class Patch_Pawn_GetDisabledWorkTypes_CacheClearer
    {
        static Patch_Pawn_GetDisabledWorkTypes_CacheClearer()
        {
            Patch_Pawn_GetDisabledWorkTypes.ClearCache();
        }
    }

    [HarmonyPatch(typeof(Pawn), "GetDisabledWorkTypes")]
    public static class Patch_Pawn_GetDisabledWorkTypes
    {
        private static readonly Dictionary<WorkTags, List<WorkTypeDef>> _workTypesByTagsCache
            = new Dictionary<WorkTags, List<WorkTypeDef>>(16);

        public static void ClearCache()
        {
            _workTypesByTagsCache.Clear();
        }

        static void Postfix(Pawn __instance, ref List<WorkTypeDef> __result)
        {
            if (__instance == null || __result == null) return;

            var comp = __instance.TryGetComp<CompShapeshifter>();
            var form = (comp != null && comp.isTransformed) ? comp.currentForm as ShapeshiftFormDef : null;
            if (form == null) return;

            var extra = form.disabledWorkTypesOnTransform;
            if (extra != null)
            {
                for (int i = 0; i < extra.Count; i++)
                {
                    var w = extra[i];
                    if (w != null && !__result.Contains(w)) __result.Add(w);
                }
            }

            var tags = form.disabledWorkTagsOnTransform;
            if (tags != WorkTags.None)
            {
                if (!_workTypesByTagsCache.TryGetValue(tags, out var matched))
                {
                    matched = new List<WorkTypeDef>(16);
                    var all = DefDatabase<WorkTypeDef>.AllDefsListForReading;
                    for (int i = 0; i < all.Count; i++)
                    {
                        var wt = all[i];
                        if (wt == null) continue;
                        if ((wt.workTags & tags) != WorkTags.None)
                            matched.Add(wt);
                    }
                    _workTypesByTagsCache[tags] = matched;
                }

                for (int i = 0; i < matched.Count; i++)
                {
                    var wt = matched[i];
                    if (wt != null && !__result.Contains(wt))
                        __result.Add(wt);
                }
            }
        }
    }
}