// ShapeshifterFramework | Patches | Patch_PawnRenderNode_GraphicFor_Parts.cs
// 목적 : 폰의 각 신체 부위(머리, 몸통, 수염, 머리카락, 문신 등) 그래픽 렌더링에 폼 설정(Default/Hidden/Replace)을 강제 적용.
// 용도 : 각종 PawnRenderNode_* 클래스의 GraphicFor 메서드 결과값(__result)을 후처리(Postfix)로 가로채어, 투명화시키거나 지정된 커스텀 텍스처(Graphic)로 완전히 교체함.
// 주의 : Harmony 2에서 하나의 메서드에 [HarmonyPatch] 어트리뷰트를 여러 개 쌓으면 Merge 시 마지막 declaringType만 남음.
//        반드시 TargetMethods()로 모든 대상 메서드를 명시적으로 열거해야 함.
//        VEF 등 외부 모드가 동일 메서드를 Postfix할 수 있으므로 Priority.Last로 SSF가 최종 결과를 보장.
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
        // Harmony TargetMethods용 — public override이지만 MethodBase 반환이 필요 (1.6 DLL 대조 감사 완료)
        static IEnumerable<MethodBase> TargetMethods()
        {
            var types = new[]
            {
                typeof(PawnRenderNode_Body),
                typeof(PawnRenderNode_Head),
                typeof(PawnRenderNode_Hair),
                typeof(PawnRenderNode_Beard),
                typeof(PawnRenderNode_Tattoo_Head),
                typeof(PawnRenderNode_Tattoo_Body),
                typeof(PawnRenderNode_AnimalPart),
            };
            for (int i = 0; i < types.Length; i++)
            {
                // AccessTools.Method가 null(향후 RimWorld 버전에서 메서드 제거/리네임)인데 그대로 yield하면
                // Harmony PatchAll이 전체 중단되어 SSF 모든 패치가 무력화되므로, null은 경고 후 스킵.
                var m = AccessTools.Method(types[i], "GraphicFor");
                if (m != null) yield return m;
                else Log.Warning($"[SSF] PawnRenderNode patch: {types[i].Name}.GraphicFor not found - skipped (RimWorld version change?)");
            }
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        static void Postfix(object __instance, Pawn pawn, ref Graphic __result)
        {
            ShapeshiftFormDef form;
            if (!ShapeshiftPartControlUtility.ShouldRun(pawn, out form)) return;
            ShapeshiftPartControlUtility.TryApplyPartControl(__instance, pawn, form, ref __result);
        }
    }
}
