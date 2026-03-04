// ShapeshifterFramework | Patches | Patch_PawnRenderer_GetDrawParms.cs
// 목적 : 변신 폼의 바디 오프셋, UI 초상화 전용 스케일 배율을 적용하고 수영 관련 치명적 렌더링 버그를 수정.
// 용도 : GetDrawParms 결과 매트릭스에 폼 오프셋 및 UI(Portrait) 스케일을 주입. 머리가 없는(Hidden) 폼이 수영할 때 폰 전체가 투명화되는 바닐라 NoBody 플래그 버그를 조건부로 해제함.

using HarmonyLib;
using ShapeshifterFramework.Utilities;
using UnityEngine;
using Verse;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch(typeof(PawnRenderer), "GetDrawParms")]
    public static class Patch_PawnRenderer_GetDrawParms
    {
        [HarmonyPostfix, HarmonyPriority(Priority.Last)]
        static void Postfix(
            PawnRenderer __instance,
            Vector3 rootLoc, float angle, Rot4 bodyFacing, RotDrawMode bodyDrawType, PawnRenderFlags flags,
            ref PawnDrawParms __result)
        {
            // Pawn
            Pawn pawn = ShapeshiftReflectionCache.GetPawn(__instance);
            if (pawn == null) return;

            // ShapeShift Comp/Form
            var comp = ShapeshiftUtility.GetShapeShiftComp(pawn);
            if (comp == null || !comp.isTransformed || comp.currentForm == null) return;
            var form = comp.currentForm;

            // ── A) 수영 중 NoBody 해제(헤드 숨김 폼이 완전 투명 되는 문제 방지)
            // 원인: 바닐라가 swimming일 때 PawnRenderFlags.NoBody를 켬.
            // 해결: GetDrawParms 단계에서 __result.flags에서 NoBody를 조건부로 빼야 PreDraw에 반영됨.
            if (pawn.Swimming && (flags & PawnRenderFlags.NoBody) != 0)
            {
                ShapeshiftFormDef runForm;
                if (ShapeshiftPartControlUtility.ShouldRun(pawn, out runForm) && runForm != null)
                {
                    if (ShapeshiftPartControlUtility.IsHeadHiddenForGender(pawn, runForm))
                    {
                        __result.flags &= ~PawnRenderFlags.NoBody;
                    }
                }
            }

            // ── B) 공통: 바디 오프셋(맵/포트레잇 동일)
            Vector2 add2 = form.bodyOffset.HasValue ? form.bodyOffset.Value : Vector2.zero;
            if (add2 != Vector2.zero)
            {
                Vector3 add = new Vector3(add2.x, 0f, add2.y);
                if (bodyFacing == Rot4.West) add.x = -add.x;

                Matrix4x4 m = __result.matrix;
                m.m03 += add.x; // x
                m.m23 += add.z; // z
                __result.matrix = m;
            }

            // ── C) 포트레잇 전용: 전체 균등 스케일 (옵션 허용 시)
            if ((flags & PawnRenderFlags.Portrait) != 0)
            {
                var settings = ShapeshifterFrameworkMod.Settings;
                if (settings == null || settings.enablePortraitScale)
                {
                    float s = form.portraitDrawScale.HasValue ? form.portraitDrawScale.Value : 1f;
                    if (!Mathf.Approximately(s, 1f))
                    {
                        Matrix4x4 m = __result.matrix;
                        // 루트 TRS의 기저 벡터에 배수 곱 → 모든 파츠/의상/무기/HAR 오버레이가 동일 비율(※ HAR는 별도 패치에서 동기화)
                        m.m00 *= s; // X
                        m.m11 *= s; // Y
                        m.m22 *= s; // Z
                        __result.matrix = m;
                    }
                }
            }
        }
    }
}
