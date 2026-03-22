// ShapeshifterFramework | Patches | Patch_PawnRenderNode_GraphicFor_Parts.cs
// 목적 : 폰의 각 신체 부위(머리, 몸통, 수염, 머리카락, 문신 등) 그래픽 렌더링에 폼 설정(Default/Hidden/Replace)을 강제 적용.
// 용도 : 각종 PawnRenderNode_* 클래스의 GraphicFor 메서드 결과값(__result)을 후처리(Postfix)로 가로채어, 투명화시키거나 지정된 커스텀 텍스처(Graphic)로 완전히 교체함.
// 주의 : Harmony 2에서 하나의 메서드에 [HarmonyPatch] 어트리뷰트를 여러 개 쌓으면 Merge 시 마지막 declaringType만 남음.
//        반드시 TargetMethods()로 모든 대상 메서드를 명시적으로 열거해야 함.
// 렌더링 핫패스이므로 LINQ 미사용.

using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using ShapeshifterFramework.Utilities;
using Verse;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch]
    internal static class Patch_GraphicFor_Parts
    {
        /// <summary>패치 대상: 각 PawnRenderNode 서브클래스의 GraphicFor(Pawn) 메서드.</summary>
        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(PawnRenderNode_Body), "GraphicFor");
            yield return AccessTools.Method(typeof(PawnRenderNode_Head), "GraphicFor");
            yield return AccessTools.Method(typeof(PawnRenderNode_Hair), "GraphicFor");
            yield return AccessTools.Method(typeof(PawnRenderNode_Beard), "GraphicFor");
            yield return AccessTools.Method(typeof(PawnRenderNode_Tattoo_Head), "GraphicFor");
            yield return AccessTools.Method(typeof(PawnRenderNode_Tattoo_Body), "GraphicFor");
        }

        [HarmonyPostfix]
        static void Postfix(object __instance, Pawn pawn, ref Graphic __result)
        {
            ShapeshiftFormDef form;
            if (!ShapeshiftPartControlUtility.ShouldRun(pawn, out form)) return;
            ShapeshiftPartControlUtility.TryApplyPartControl(__instance, pawn, form, ref __result);
        }
    }
}
