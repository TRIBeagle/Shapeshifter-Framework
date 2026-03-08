// ShapeshifterFramework | Patches | Patch_Pawn_GetDisabledWorkTypes.cs
// 목적 : 변신 중일 때만 특정 작업(WorkType)이나 작업 태그(예: 운반 불가, 요리 불가)를 동적으로 결격 사유로 추가.
// 용도 : GetDisabledWorkTypes에 Postfix로 개입하여 폼에 지정된 불가 작업을 결과 리스트(__result)에 밀어 넣으며, 태그 기반 파싱은 성능을 위해 Dictionary에 런타임 캐싱됨.

using HarmonyLib;
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

            var tags = form.resolvedDisabledWorkTags;
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