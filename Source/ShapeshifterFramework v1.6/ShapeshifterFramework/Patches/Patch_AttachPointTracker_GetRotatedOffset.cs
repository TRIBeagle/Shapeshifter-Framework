// ShapeshifterFramework | Patches | Patch_AttachPointTracker_GetRotatedOffset.cs
// 목적 : 폰이 무기나 장비를 들고 있을 때, 손(Attach Point)의 위치를 변신 폼의 크기에 맞춰 보정.
// 용도 : Postfix를 통해 바닐라의 attachPointScaleFactor와 폼의 타겟 배율을 비교하여 무기가 공중에 붕 뜨거나 파묻히지 않도록 Offset(Vector3)에 배수(Multiplier)를 곱해줌.
// 주의 : Priority.Last를 적용하여 다른 애니메이션 모드의 오프셋 연산이 끝난 후 최종적으로 크기를 뻥튀기함.

using HarmonyLib;
using RimWorld;
using ShapeshifterFramework.Utilities;
using System;
using UnityEngine;
using Verse;

namespace ShapeshifterFramework.Patches
{
    // 정확한 시그니처 지정: (AttachPointType, Rot4)
    [HarmonyPatch(typeof(AttachPointTracker), nameof(AttachPointTracker.GetRotatedOffset),
                  new Type[] { typeof(AttachPointType), typeof(Rot4) })]
    [HarmonyPriority(Priority.Last)]
    public static class Patch_AttachPointTracker_GetRotatedOffset
    {
        static void Postfix(AttachPointTracker __instance, AttachPointType type, Rot4 rot, ref Vector3 __result)
        {
            var pawn = ShapeshiftReflectionCache.GetAttachParent(__instance) as Pawn;
            if (pawn == null) return;

            var ls = pawn.ageTracker != null ? pawn.ageTracker.CurLifeStage : null;
            float vanilla = ls != null ? ls.attachPointScaleFactor : 1f;
            float target = ShapeshiftSizeFactorResolver.Effective(pawn).attachPointScaleFactor;

            if (Mathf.Approximately(target, vanilla)) return;

            float mul = target / (Mathf.Approximately(vanilla, 0f) ? 1f : vanilla);
            __result *= mul;
        }
    }
}
