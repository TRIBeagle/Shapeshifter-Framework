// ShapeshifterFramework | Patches | Patch_AttachPointTracker_GetRotatedOffset.cs
// 목적  : AttachPointTracker.GetRotatedOffset 결과를 변신 크기에 맞춰 보정.
// 용도  : Pawn의 attach point(무기/장비 위치 등) 오프셋이 변신 크기에 맞도록 조정.
// 변경  : 2025-09-23 v1.0 — 초기 작성.

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
