// ShapeshifterFramework | Patches | Patch_PawnRenderer_GetDrawParms.cs
// 목적 : 변신 폼의 바디 오프셋, UI 초상화 스케일 배율을 적용하고 수영 관련 치명적 렌더링 버그를 수정.
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
            PawnRenderer __instance, Pawn ___pawn,
            Vector3 rootLoc, float angle, Rot4 bodyFacing, RotDrawMode bodyDrawType, PawnRenderFlags flags,
            ref PawnDrawParms __result)
        {
            Pawn pawn = ___pawn;
            if (pawn == null) return;

            // 변신 컴프/폼 조회
            if (!ShapeshiftRegistry.TryGet(pawn, out var comp, out var form)) return;

            // A) 수영 중 NoBody 해제 (헤드 숨김 폼 투명화 방지)
            if (pawn.Swimming && (flags & PawnRenderFlags.NoBody) != 0)
            {
                if (ShapeshiftPartControlUtility.IsHeadHiddenForGender(pawn, form))
                {
                    __result.flags &= ~PawnRenderFlags.NoBody;
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

        }
    }
}
