// .NET 4.8 / C# 7.3
// 변신 중, 폼에 지정된 작업을 "추가로" 불가 처리
//  - 대상: Pawn의 컴파일러 생성 GetDisabledWorkTypes(List<WorkTypeDef>) 메서드
//  - 방식: 바닐라가 채워주는 list에 추가로 WorkTypeDef들을 넣는다(원래 비활성은 유지).
// 성능: TargetMethod는 로딩 1회 탐색. LINQ 없이 루프로 처리.

using HarmonyLib;
using ShapeshifterFramework.Comps;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Verse;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch]
    public static class Patch_Pawn_GetDisabledWorkTypes
    {
        // [성능] WorkTags → WorkTypeDef 매칭 캐시(초기화 후 재사용)
        private static readonly Dictionary<WorkTags, List<WorkTypeDef>> _workTypesByTagsCache
            = new Dictionary<WorkTags, List<WorkTypeDef>>(16);

        [HarmonyTargetMethod]
        public static MethodBase TargetMethod()
        {
            // Pawn 내부의 컴파일러 생성 메서드 중 이름에 "GetDisabledWorkTypes"가 포함된 것을 선택.
            var methods = AccessTools.GetDeclaredMethods(typeof(Pawn));
            for (int i = 0; i < methods.Count; i++)
            {
                var m = methods[i];
                if (m == null) continue;
                if (!m.HasAttribute<CompilerGeneratedAttribute>()) continue;
                if (m.Name != null && m.Name.Contains("GetDisabledWorkTypes")) return m;
            }
            return null;
        }

        static void Prefix(Pawn __instance, List<WorkTypeDef> list)
        {
            if (__instance == null || list == null) return;

            var comp = __instance.TryGetComp<CompShapeshifter>();
            var form = (comp != null && comp.isTransformed) ? comp.currentForm as ShapeshiftFormDef : null;
            if (form == null) return;

            // 1) 폼 지정 WorkTypeDef들을 추가
            var extra = form.disabledWorkTypesOnTransform;
            if (extra != null)
            {
                for (int i = 0; i < extra.Count; i++)
                {
                    var w = extra[i];
                    if (w != null && !list.Contains(w)) list.Add(w);
                }
            }

            // 2) 폼 지정 WorkTags로 태그 매칭되는 WorkType들을 추가
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
