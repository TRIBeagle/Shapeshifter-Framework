// ShapeshifterFramework | Patches | Patch_PawnRenderNode_GraphicFor_Parts.cs
// 목적 : 폰의 각 신체 부위(머리, 몸통, 수염, 머리카락, 문신 등) 그래픽 렌더링에 폼 설정(Default/Hidden/Replace)을 강제 적용.
// 용도 : 각종 PawnRenderNode_* 클래스의 GraphicFor 메서드 결과값(__result)을 후처리(Postfix)로 가로채어, 투명화시키거나 지정된 커스텀 텍스처(Graphic)로 완전히 교체함.
// 렌더링 핫패스이므로 LINQ 미사용.

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
