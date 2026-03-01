// .NET Framework 4.8 / C# 7.3
// 목적: 변신 중 작업 결격 사유에 폼 정보를 추가.
//       작업 탭 툴팁 및 캐릭터 창에 "변신 (XXX): 작업 불가" 표시.

using HarmonyLib;
using RimWorld;
using ShapeshifterFramework.Comps;
using System.Collections.Generic;
using Verse;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch(typeof(Pawn), "GetReasonsForDisabledWorkType")]
    public static class Patch_Pawn_GetReasonsForDisabledWorkType
    {
        static void Postfix(Pawn __instance, WorkTypeDef workType, ref List<string> __result)
        {
            if (__instance == null || workType == null || __result == null) return;

            var comp = __instance.TryGetComp<CompShapeshifter>();
            var form = (comp != null && comp.isTransformed) ? comp.currentForm : null;
            if (form == null) return;

            bool disabledByType = form.disabledWorkTypesOnTransform != null
                && form.disabledWorkTypesOnTransform.Contains(workType);

            bool disabledByTag = form.disabledWorkTagsOnTransform != WorkTags.None
                && (workType.workTags & form.disabledWorkTagsOnTransform) != WorkTags.None;

            if (!disabledByType && !disabledByTag) return;

            // 이미 같은 이유가 추가되어 있으면 스킵 (중복 방지)
            string reason = "Shapeshift_WorkDisabled".Translate(form.LabelCap);
            if (!__result.Contains(reason))
                __result.Add(reason);
        }
    }
}