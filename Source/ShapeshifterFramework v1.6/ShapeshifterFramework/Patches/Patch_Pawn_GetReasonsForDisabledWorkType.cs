// ShapeshifterFramework | Patches | Patch_Pawn_GetReasonsForDisabledWorkType.cs
// 목적 : 플레이어가 인게임 UI(작업 탭 툴팁 등)에서 왜 이 폰이 해당 작업을 할 수 없는지 직관적으로 알 수 있도록 사유를 제공.
// 용도 : Postfix에서 폼으로 인해 차단된 작업일 경우 지역화 문자열(Translate)을 결격 사유 리스트에 추가함.

using HarmonyLib;
using RimWorld;
using ShapeshifterFramework.Comps;
using ShapeshifterFramework.Utilities;
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

            if (!ShapeshiftRegistry.TryGet(__instance, out var comp, out var form)) return;

            bool disabledByType = form.disabledWorkTypesOnTransform != null
                && form.disabledWorkTypesOnTransform.Contains(workType);

            bool disabledByTag = form.disabledWorkTagsOnTransform != WorkTags.None
                && (workType.workTags & form.disabledWorkTagsOnTransform) != WorkTags.None;

            if (!disabledByType && !disabledByTag) return;

            // 이미 같은 이유가 추가되어 있으면 스킵 (중복 방지)
            string reason = "SSF_Message_WorkDisabled".Translate(form.LabelCap);
            if (!__result.Contains(reason))
                __result.Add(reason);
        }
    }
}