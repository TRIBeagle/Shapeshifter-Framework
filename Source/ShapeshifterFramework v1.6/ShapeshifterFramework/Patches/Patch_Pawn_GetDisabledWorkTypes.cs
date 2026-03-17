// ShapeshifterFramework | Patches | Patch_Pawn_GetDisabledWorkTypes.cs
// 목적 : 변신 중일 때만 특정 작업(WorkType)이나 작업 태그(예: 운반 불가, 요리 불가)를 동적으로 결격 사유로 추가.
// 용도 : GetDisabledWorkTypes에 Postfix로 개입하여 폼에 지정된 불가 작업을 결과 리스트(__result)에 밀어 넣으며, 태그 기반 파싱은 성능을 위해 Dictionary에 런타임 캐싱됨.

using HarmonyLib;
using ShapeshifterFramework.Utilities;
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

        // 재진입 안전을 위해 static HashSet 제거, 호출 빈도가 낮아 로컬 할당 허용
        [System.ThreadStatic]
        private static HashSet<WorkTypeDef> _tmpExisting;

        public static void ClearCache()
        {
            _workTypesByTagsCache.Clear();
        }

        static void Postfix(Pawn __instance, ref List<WorkTypeDef> __result)
        {
            if (__instance == null || __result == null) return;

            if (!ShapeshiftRegistry.TryGet(__instance, out var comp, out var form)) return;

            bool hasExtra = form.disabledWorkTypesOnTransform != null && form.disabledWorkTypesOnTransform.Count > 0;
            bool hasTags = form.disabledWorkTagsOnTransform != WorkTags.None;
            if (!hasExtra && !hasTags) return;

            // ThreadStatic으로 재진입 안전 + GC 절감
            if (_tmpExisting == null) _tmpExisting = new HashSet<WorkTypeDef>();
            else _tmpExisting.Clear();
            for (int i = 0; i < __result.Count; i++)
                _tmpExisting.Add(__result[i]);

            if (hasExtra)
            {
                var extra = form.disabledWorkTypesOnTransform;
                for (int i = 0; i < extra.Count; i++)
                {
                    var w = extra[i];
                    if (w != null && _tmpExisting.Add(w)) __result.Add(w);
                }
            }

            if (hasTags)
            {
                var tags = form.disabledWorkTagsOnTransform;
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
                    if (wt != null && _tmpExisting.Add(wt))
                        __result.Add(wt);
                }
            }
        }
    }
}