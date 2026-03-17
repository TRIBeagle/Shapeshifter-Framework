// ShapeshifterFramework | Patches | Patch_PawnRenderer_BaseHeadOffsetAt.cs
// 목적 : 변신 시 목(Neck) 위치가 바뀌어 머리가 붕 뜨거나 파묻히는 것을 방지하기 위한 정밀 위치(Offset) 조정.
// 용도 : Postfix에서 바닐라의 bodySizeFactor 제곱근 비율 계산식을 재현한 뒤, 폼에 지정된 커스텀 헤드 오프셋(headOffset)을 폰이 바라보는 방향(Rotation)에 맞춰 적용함.

using HarmonyLib;
using ShapeshifterFramework.Utilities;
using UnityEngine;
using Verse;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch(typeof(PawnRenderer), "BaseHeadOffsetAt")]
    public static class Patch_PawnRenderer_BaseHeadOffsetAt
    {
        [HarmonyPostfix, HarmonyPriority(Priority.Last)]
        static void Postfix(PawnRenderer __instance, Rot4 rotation, ref Vector3 __result)
        {
            Pawn pawn = ShapeshiftReflectionCache.GetPawn(__instance);
            if (pawn == null) return;

            // 비변신 폰 즉시 스킵 — 렌더 핫패스에서 SizeFactorResolver/리플렉션 방지
            if (!ShapeshiftRegistry.IsActive(pawn)) return;

            // 1) bodySizeFactor 보정
            var ls = pawn.ageTracker != null ? pawn.ageTracker.CurLifeStage : null;
            float vanilla = Mathf.Max(0.01f, ls != null ? ls.bodySizeFactor : 1f);
            float target = Mathf.Max(0.01f, ShapeshiftSizeFactorResolver.Effective(pawn).bodySizeFactor);
            if (!Mathf.Approximately(target, vanilla))
            {
                float mul = Mathf.Sqrt(target / vanilla);
                __result = new Vector3(__result.x * mul, __result.y, __result.z * mul);
            }

            // 2) 헤드 오프셋 적용
            if (!ShapeshiftRegistry.TryGet(pawn, out var comp, out var form)) return;

            Vector2 add2 = form.headOffset.HasValue ? form.headOffset.Value : Vector2.zero;
            if (add2 == Vector2.zero) return;

            Vector3 add = new Vector3(add2.x, 0f, add2.y);
            if (rotation == Rot4.West) add.x = -add.x;

            __result = new Vector3(__result.x + add.x, __result.y, __result.z + add.z);
        }
    }
}
