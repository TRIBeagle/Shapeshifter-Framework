// ShapeshifterFramework | Patches | Patch_PawnRenderer_GetDrawParms.cs
// 목적 : 변신 폼의 바디 오프셋, UI 초상화/캐시 전용 스케일 배율을 적용하고 수영 관련 치명적 렌더링 버그를 수정.
// 용도 : GetDrawParms 결과 매트릭스에 폼 오프셋 및 UI(Portrait) 스케일을 주입. 머리가 없는(Hidden) 폼이 수영할 때 폰 전체가 투명화되는 바닐라 NoBody 플래그 버그를 조건부로 해제함.
//        캐시(Cache) 렌더링 시 역스케일을 적용하여 대형 메쉬가 고정 크기 아틀라스 프레임에 맞도록 축소.

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
            if (!ShapeshiftRegistry.TryGet(pawn, out var comp, out var form)) return;

            // A) 수영 중 NoBody 해제 (헤드 숨김 폼 투명화 방지)
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

            // B) 바디 오프셋
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

            // C) 포트레잇 전용 스케일
            if ((flags & PawnRenderFlags.Portrait) != 0)
            {
                var settings = ShapeshifterFrameworkMod.Settings;
                if (settings == null || settings.enablePortraitScale)
                {
                    float s = form.portraitDrawScale.HasValue ? form.portraitDrawScale.Value : 1f;
                    if (!Mathf.Approximately(s, 1f))
                    {
                        Matrix4x4 m = __result.matrix;
                        // TRS 기저 벡터에 배수 곱
                        m.m00 *= s; // X
                        m.m11 *= s; // Y
                        m.m22 *= s; // Z
                        __result.matrix = m;
                    }
                }
            }

            // D) 캐시(아틀라스) 렌더링 시 역스케일
            // 줌아웃 시 바닐라는 폰을 고정 크기 텍스처 프레임에 렌더링하여 캐시함.
            // bodyDrawScale > 1인 폼은 메쉬가 프레임을 넘치므로, 캐시 렌더링 시에만
            // 역스케일을 적용하여 1x 크기로 축소. Patch_Thing_DrawSize가 DrawSize를
            // bodyDrawScale 배수로 반환하므로, 캐시 프레임은 올바른 크기로 표시됨.
            if ((flags & PawnRenderFlags.Cache) != 0)
            {
                float sBody = form.bodyDrawScale.HasValue ? Mathf.Max(0.01f, form.bodyDrawScale.Value) : 1f;
                if (!Mathf.Approximately(sBody, 1f))
                {
                    float inv = 1f / sBody;
                    Matrix4x4 m = __result.matrix;
                    m.m00 *= inv; // X
                    m.m11 *= inv; // Y
                    m.m22 *= inv; // Z
                    __result.matrix = m;
                }
            }
        }
    }
}
