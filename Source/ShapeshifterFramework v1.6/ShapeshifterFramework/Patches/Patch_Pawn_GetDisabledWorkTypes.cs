// .NET 4.8 / C# 7.3
// 변신 중, 폼에 지정된 작업을 "추가로" 불가 처리
//  - 대상: Pawn의 컴파일러 생성 GetDisabledWorkTypes(List<WorkTypeDef>) 메서드
//  - 참고: 1.5 예시도 같은 타겟팅을 사용(CompilerGenerated + 이름 매칭) :contentReference[oaicite:2]{index=2}
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using ShapeshifterFramework.Comps;
using Verse;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch]
    public static class Patch_Pawn_GetDisabledWorkTypes
    {
        [HarmonyTargetMethod]
        public static MethodBase TargetMethod()
        {
            // Pawn 내부의 컴파일러 생성 메서드 중 "GetDisabledWorkTypes" 포함 메서드 선택
            return AccessTools.GetDeclaredMethods(typeof(Pawn))
                .First(m => m.HasAttribute<CompilerGeneratedAttribute>() && m.Name.Contains("GetDisabledWorkTypes"));
        }

        // 바닐라가 채워주는 list에 "추가로" 넣는 방식(원래 비활성은 유지)
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
                var all = DefDatabase<WorkTypeDef>.AllDefsListForReading;
                for (int i = 0; i < all.Count; i++)
                {
                    var wt = all[i];
                    if (wt == null) continue;
                    // 해당 WorkType의 tags가 폼 태그와 교집합이면 추가
                    if ((wt.workTags & tags) != WorkTags.None && !list.Contains(wt))
                        list.Add(wt);
                }
            }
        }
    }
}
