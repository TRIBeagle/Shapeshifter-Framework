// ShapeshifterFramework | Patches | Patch_PawnRenderNode_GraphicFor_Parts.cs
// 목적   : 변신 폼(ShapeshiftFormDef)의 파츠 제어(Default/Hidden/Replace) 적용
// 용도   : PawnRenderNode_*::GraphicFor 결과를 후처리(Postfix)로 교체/숨김
// 성능   : 렌더 핫패스. LINQ/할당 없음. 조건 충족 시에만 GraphicDatabase 조회.

using HarmonyLib;
using ShapeshifterFramework.Utilities;
using Verse;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch(typeof(PawnRenderNode_Body), "GraphicFor")]
    internal static class Patch_GraphicFor_Body
    {
        static void Postfix(object __instance, Pawn pawn, ref Graphic __result)
        {
            ShapeshiftFormDef form;
            if (!ShapeshiftPartControlUtility.ShouldRun(pawn, out form)) return;
            ShapeshiftPartControlUtility.TryApplyPartControl(__instance, pawn, form, ref __result);
        }
    }

    [HarmonyPatch(typeof(PawnRenderNode_Head), "GraphicFor")]
    internal static class Patch_GraphicFor_Head
    {
        static void Postfix(object __instance, Pawn pawn, ref Graphic __result)
        {
            ShapeshiftFormDef form;
            if (!ShapeshiftPartControlUtility.ShouldRun(pawn, out form)) return;
            ShapeshiftPartControlUtility.TryApplyPartControl(__instance, pawn, form, ref __result);
        }
    }

    [HarmonyPatch(typeof(PawnRenderNode_Hair), "GraphicFor")]
    internal static class Patch_GraphicFor_Hair
    {
        static void Postfix(object __instance, Pawn pawn, ref Graphic __result)
        {
            ShapeshiftFormDef form;
            if (!ShapeshiftPartControlUtility.ShouldRun(pawn, out form)) return;
            ShapeshiftPartControlUtility.TryApplyPartControl(__instance, pawn, form, ref __result);
        }
    }

    [HarmonyPatch(typeof(PawnRenderNode_Beard), "GraphicFor")]
    internal static class Patch_GraphicFor_Beard
    {
        static void Postfix(object __instance, Pawn pawn, ref Graphic __result)
        {
            ShapeshiftFormDef form;
            if (!ShapeshiftPartControlUtility.ShouldRun(pawn, out form)) return;
            ShapeshiftPartControlUtility.TryApplyPartControl(__instance, pawn, form, ref __result);
        }
    }

    [HarmonyPatch(typeof(PawnRenderNode_Tattoo_Head), "GraphicFor")]
    internal static class Patch_GraphicFor_TattooHead
    {
        static void Postfix(object __instance, Pawn pawn, ref Graphic __result)
        {
            ShapeshiftFormDef form;
            if (!ShapeshiftPartControlUtility.ShouldRun(pawn, out form)) return;
            ShapeshiftPartControlUtility.TryApplyPartControl(__instance, pawn, form, ref __result);
        }
    }

    [HarmonyPatch(typeof(PawnRenderNode_Tattoo_Body), "GraphicFor")]
    internal static class Patch_GraphicFor_TattooBody
    {
        static void Postfix(object __instance, Pawn pawn, ref Graphic __result)
        {
            ShapeshiftFormDef form;
            if (!ShapeshiftPartControlUtility.ShouldRun(pawn, out form)) return;
            ShapeshiftPartControlUtility.TryApplyPartControl(__instance, pawn, form, ref __result);
        }
    }
}
