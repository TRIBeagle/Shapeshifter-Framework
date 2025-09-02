// Patch_PawnRenderNode_GraphicFor_Parts.cs
// 목적: 파츠별 제어(기본/숨김/교체)를 적용. 성별 전용은 PartOverrideOption.male/female 우선.
// 메모: C# 7.3 호환. 6개 파츠 지원(Body, Head, Hair, Beard, Tattoo_Head, Tattoo_Body)

using HarmonyLib;
using ShapeshifterFramework.Utilities;
using UnityEngine;
using Verse;

namespace ShapeshifterFramework.Patches
{
    internal static class PartControlUtil
    {
        // 현재 폼 얻기
        internal static bool ShouldRun(Pawn pawn, out ShapeshiftFormDef form)
        {
            form = null;
            if (pawn == null) return false;
            var comp = ShapeshiftUtility.GetShapeShiftComp(pawn);
            if (comp == null || !comp.isTransformed || comp.currentForm == null) return false;
            form = comp.currentForm;
            return true;
        }

        // 성별에 맞는 PartOverrideOption 선택: female/male 하위 옵션이 있으면 우선
        private static PartOverrideOption SelectByGender(PartOverrideOption baseOpt, Gender g)
        {
            if (baseOpt == null) return null;
            if (g == Gender.Female && baseOpt.female != null) return baseOpt.female;
            if (g == Gender.Male && baseOpt.male != null) return baseOpt.male;
            return baseOpt;
        }

        // Hidden/Replace만 처리(Default는 false 반환)
        internal static bool TryApplyPartControl(object node, Pawn pawn, ShapeshiftFormDef form, ref Graphic result)
        {
            PartOverrideOption opt = null;
            Shader shader = ShaderDatabase.Cutout;

            if (node is PawnRenderNode_Head) { opt = form.head; shader = ShaderDatabase.Cutout; }
            else if (node is PawnRenderNode_Hair) { opt = form.hair; shader = ShaderDatabase.CutoutHair; }
            else if (node is PawnRenderNode_Beard) { opt = form.beard; shader = ShaderDatabase.CutoutSkinOverlay; }
            else if (node is PawnRenderNode_Tattoo_Head) { opt = form.tattooHead; shader = ShaderDatabase.CutoutSkinOverlay; }
            else if (node is PawnRenderNode_Tattoo_Body) { opt = form.tattooBody; shader = ShaderDatabase.CutoutSkinOverlay; }
            else if (node is PawnRenderNode_Body) { opt = form.body; shader = ShaderDatabase.Cutout; }

            if (opt == null) return false;

            // 성별 전용 하위 옵션이 있으면 우선 적용
            Gender g = pawn != null ? pawn.gender : Gender.None;
            PartOverrideOption eff = SelectByGender(opt, g);
            if (eff == null || eff.mode == PartControlMode.Default)
                return false;

            // 숨김
            if (eff.mode == PartControlMode.Hidden)
            {
                result = null;
                return true;
            }

            // 교체
            if (eff.mode == PartControlMode.Replace)
            {
                string path = eff.replacementTexPath;
                if (!string.IsNullOrEmpty(path))
                {
                    // Graphic_Multi로 안전 생성
                    var newG = GraphicDatabase.Get<Graphic_Multi>(path, shader, Vector2.one, Color.white);
                    if (newG != null)
                        result = newG;
                }
                return true;
            }

            return false;
        }
    }

    // ───────────────────────── 바디
    [HarmonyPatch(typeof(PawnRenderNode_Body), "GraphicFor")]
    internal static class Patch_GraphicFor_Body
    {
        static void Postfix(object __instance, Pawn pawn, ref Graphic __result)
        {
            ShapeshiftFormDef form;
            if (!PartControlUtil.ShouldRun(pawn, out form)) return;
            PartControlUtil.TryApplyPartControl(__instance, pawn, form, ref __result);
        }
    }

    // ───────────────────────── 헤드
    [HarmonyPatch(typeof(PawnRenderNode_Head), "GraphicFor")]
    internal static class Patch_GraphicFor_Head
    {
        static void Postfix(object __instance, Pawn pawn, ref Graphic __result)
        {
            ShapeshiftFormDef form;
            if (!PartControlUtil.ShouldRun(pawn, out form)) return;
            PartControlUtil.TryApplyPartControl(__instance, pawn, form, ref __result);
        }
    }

    // ───────────────────────── 헤어
    [HarmonyPatch(typeof(PawnRenderNode_Hair), "GraphicFor")]
    internal static class Patch_GraphicFor_Hair
    {
        static void Postfix(object __instance, Pawn pawn, ref Graphic __result)
        {
            ShapeshiftFormDef form;
            if (!PartControlUtil.ShouldRun(pawn, out form)) return;
            PartControlUtil.TryApplyPartControl(__instance, pawn, form, ref __result);
        }
    }

    // ───────────────────────── 수염
    [HarmonyPatch(typeof(PawnRenderNode_Beard), "GraphicFor")]
    internal static class Patch_GraphicFor_Beard
    {
        static void Postfix(object __instance, Pawn pawn, ref Graphic __result)
        {
            ShapeshiftFormDef form;
            if (!PartControlUtil.ShouldRun(pawn, out form)) return;
            PartControlUtil.TryApplyPartControl(__instance, pawn, form, ref __result);
        }
    }

    // ───────────────────────── 타투(헤드)
    [HarmonyPatch(typeof(PawnRenderNode_Tattoo_Head), "GraphicFor")]
    internal static class Patch_GraphicFor_TattooHead
    {
        static void Postfix(object __instance, Pawn pawn, ref Graphic __result)
        {
            ShapeshiftFormDef form;
            if (!PartControlUtil.ShouldRun(pawn, out form)) return;
            PartControlUtil.TryApplyPartControl(__instance, pawn, form, ref __result);
        }
    }

    // ───────────────────────── 타투(바디)
    [HarmonyPatch(typeof(PawnRenderNode_Tattoo_Body), "GraphicFor")]
    internal static class Patch_GraphicFor_TattooBody
    {
        static void Postfix(object __instance, Pawn pawn, ref Graphic __result)
        {
            ShapeshiftFormDef form;
            if (!PartControlUtil.ShouldRun(pawn, out form)) return;
            PartControlUtil.TryApplyPartControl(__instance, pawn, form, ref __result);
        }
    }
}
