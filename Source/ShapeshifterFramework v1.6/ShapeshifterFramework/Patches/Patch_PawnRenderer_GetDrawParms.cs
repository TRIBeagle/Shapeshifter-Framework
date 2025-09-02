// Patch_PawnRenderer_GetDrawParms.cs
// 목적: 폼 오프셋 + (포트레잇 전용) 전체 스케일 적용
// 조건: C# 7.3 호환

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
        static void Postfix(PawnRenderer __instance,
                            Vector3 rootLoc, float angle, Rot4 bodyFacing, RotDrawMode bodyDrawType, PawnRenderFlags flags,
                            ref PawnDrawParms __result)
        {
            // Pawn/Comp/Form
            Pawn pawn = ShapeshiftReflectionCache.GetPawn(__instance);
            if (pawn == null) return;
            var comp = ShapeshiftUtility.GetShapeShiftComp(pawn);
            if (comp == null || !comp.isTransformed || comp.currentForm == null) return;
            var form = comp.currentForm;

            // ── 1) 공통: 바디 오프셋(맵/포트레잇 동일)
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

            // ── 2) 포트레잇 전용: 전체 균등 스케일 (옵션 허용 시)
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
