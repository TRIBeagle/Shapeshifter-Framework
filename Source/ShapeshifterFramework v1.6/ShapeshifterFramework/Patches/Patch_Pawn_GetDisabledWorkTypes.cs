// .NET 4.8 / C# 7.3
// 변신 중, 폼에 지정된 작업을 "추가로" 불가 처리
//  - 대상: Pawn의 컴파일러 생성 GetDisabledWorkTypes(List<WorkTypeDef>) 메서드

using HarmonyLib;
using ShapeshifterFramework.Comps;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Verse;

namespace ShapeshifterFramework.Patches
{
    // 게임 로드/시작 시 캐시를 비워주는 초기화 클래스 추가
    [StaticConstructorOnStartup]
    public static class Patch_Pawn_GetDisabledWorkTypes_CacheClearer
    {
        static Patch_Pawn_GetDisabledWorkTypes_CacheClearer()
        {
            Patch_Pawn_GetDisabledWorkTypes.ClearCache();
        }
    }

    [HarmonyPatch]
    public static class Patch_Pawn_GetDisabledWorkTypes
    {
        private static readonly Dictionary<WorkTags, List<WorkTypeDef>> _workTypesByTagsCache
            = new Dictionary<WorkTags, List<WorkTypeDef>>(16);

        // 캐시 청소용 메서드
        public static void ClearCache()
        {
            _workTypesByTagsCache.Clear();
        }

        [HarmonyTargetMethod]
        public static MethodBase TargetMethod()
        {
            var methods = AccessTools.GetDeclaredMethods(typeof(Pawn));
            for (int i = 0; i < methods.Count; i++)
            {
                var m = methods[i];
                if (m == null) continue;
                if (!m.HasAttribute<CompilerGeneratedAttribute>()) continue;
                if (m.Name != null && m.Name.Contains("GetDisabledWorkTypes")) return m;
            }
            Log.Error("[ShapeshifterFramework] Critical: 'GetDisabledWorkTypes' hidden method not found! The patch will be ignored. RimWorld might have been updated.");
            return null;
        }

        static void Prefix(Pawn __instance, List<WorkTypeDef> list)
        {
            if (__instance == null || list == null) return;

            var comp = __instance.TryGetComp<CompShapeshifter>();
            var form = (comp != null && comp.isTransformed) ? comp.currentForm as ShapeshiftFormDef : null;
            if (form == null) return;

            var extra = form.disabledWorkTypesOnTransform;
            if (extra != null)
            {
                for (int i = 0; i < extra.Count; i++)
                {
                    var w = extra[i];
                    if (w != null && !list.Contains(w)) list.Add(w);
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
                    if (wt != null && !list.Contains(wt))
                        list.Add(wt);
                }
            }
        }
    }
}