// ShapeshifterFramework | Patches | Patch_PawnRenderer_CachedFrameScaling.cs
// 목적 : bodyDrawScale ≠ 1인 변신 폼의 아틀라스 캐시 렌더링과 디스플레이 스케일링을 처리.
// 용도 : (1) GetBlitMeshUpdatedFrame 패치: PawnCacheRenderer.RenderPawn 호출 시 역스케일(1/bodyDrawScale)을
//            적용하여 대형 메쉬가 고정 크기 아틀라스 프레임에 맞도록 축소 캡처.
//        (2) RenderPawnAt Transpiler: GenDraw.DrawMeshNowOrLater 호출을 대체하여 캐시 디스플레이 시
//            bodyDrawScale 배수의 Matrix4x4로 확대 표시.
// 주의 : Transpiler가 RimWorld 1.6 PawnRenderer.RenderPawnAt 내의 GenDraw.DrawMeshNowOrLater 호출 위치에 의존.

using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using ShapeshifterFramework.Utilities;
using UnityEngine;
using Verse;

namespace ShapeshifterFramework.Patches
{
    /// <summary>캐시 렌더링 시 역스케일로 축소 캡처.</summary>
    [HarmonyPatch(typeof(PawnRenderer), "GetBlitMeshUpdatedFrame")]
    internal static class Patch_PawnRenderer_GetBlitMeshUpdatedFrame_InverseScale
    {
        [HarmonyPrefix]
        static bool Prefix(PawnRenderer __instance, PawnTextureAtlasFrameSet frameSet, Rot4 rotation, PawnDrawMode drawMode, ref Mesh __result)
        {
            Pawn pawn = ShapeshiftReflectionCache.GetPawn(__instance);
            if (pawn == null) return true;
            if (!ShapeshiftRegistry.TryGet(pawn, out _, out var form)) return true;

            float scale = form.bodyDrawScale.HasValue ? Mathf.Max(0.01f, form.bodyDrawScale.Value) : 1f;
            if (Mathf.Approximately(scale, 1f)) return true;

            int index = frameSet.GetIndex(rotation, drawMode);
            if (frameSet.isDirty[index])
            {
                // 아틀라스 프레임에 역스케일로 렌더링 — 대형 메쉬가 프레임 안에 수축
                Find.PawnCacheCamera.rect = frameSet.uvRects[index];
                float invScale = 1f / scale;
                Find.PawnCacheRenderer.RenderPawn(pawn, frameSet.atlas, Vector3.zero, invScale, 0f, rotation);
                Find.PawnCacheCamera.rect = new Rect(0f, 0f, 1f, 1f);
                frameSet.isDirty[index] = false;
            }

            __result = frameSet.meshes[index];
            return false;
        }
    }

    /// <summary>캐시 디스플레이 시 bodyDrawScale 배수로 확대 표시.</summary>
    [HarmonyPatch(typeof(PawnRenderer), nameof(PawnRenderer.RenderPawnAt))]
    internal static class Patch_PawnRenderer_RenderPawnAt_CacheScaleUp
    {
        /// <summary>
        /// GenDraw.DrawMeshNowOrLater 호출을 DrawCachedPawnScaled로 대체하여
        /// 변신 폰의 캐시 디스플레이를 bodyDrawScale 배수로 확대.
        /// </summary>
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            var drawMeshMethod = AccessTools.Method(typeof(GenDraw), "DrawMeshNowOrLater",
                new[] { typeof(Mesh), typeof(Vector3), typeof(Quaternion), typeof(Material), typeof(bool) });
            var helperMethod = AccessTools.Method(typeof(Patch_PawnRenderer_RenderPawnAt_CacheScaleUp), nameof(DrawCachedPawnScaled));

            bool patched = false;
            for (int i = 0; i < codes.Count; i++)
            {
                if (!patched && codes[i].Calls(drawMeshMethod))
                {
                    // 스택: Mesh, Vector3, Quaternion, Material, bool
                    // ldarg.0 (PawnRenderer this) 추가 후 helper 호출로 대체
                    codes.Insert(i, new CodeInstruction(OpCodes.Ldarg_0));
                    i++; // Insert 후 인덱스 보정
                    codes[i] = new CodeInstruction(OpCodes.Call, helperMethod);
                    patched = true;
                }
            }

            if (!patched)
            {
                Log.Warning("[SSF] Transpiler: RenderPawnAt에서 GenDraw.DrawMeshNowOrLater를 찾지 못함. 대형 폼 줌아웃 스케일 미적용.");
            }

            return codes;
        }

        /// <summary>
        /// 캐시 디스플레이 드로우 헬퍼 — 변신 폰은 bodyDrawScale 배수 매트릭스로 확대 표시.
        /// 바닐라 폰(비변신 또는 bodyDrawScale=1)은 원래 GenDraw.DrawMeshNowOrLater와 동일 동작.
        /// </summary>
        public static void DrawCachedPawnScaled(Mesh mesh, Vector3 loc, Quaternion quat, Material mat, bool drawNow, PawnRenderer renderer)
        {
            Pawn pawn = ShapeshiftReflectionCache.GetPawn(renderer);
            if (pawn != null && ShapeshiftRegistry.TryGet(pawn, out _, out var form))
            {
                float scale = form.bodyDrawScale.HasValue ? Mathf.Max(0.01f, form.bodyDrawScale.Value) : 1f;
                if (!Mathf.Approximately(scale, 1f))
                {
                    // bodyDrawScale 배수의 매트릭스로 확대 표시
                    Graphics.DrawMesh(mesh, Matrix4x4.TRS(loc, quat, Vector3.one * scale), mat, 0);
                    return;
                }
            }

            // 바닐라 동작 유지
            GenDraw.DrawMeshNowOrLater(mesh, loc, quat, mat, drawNow);
        }
    }
}
