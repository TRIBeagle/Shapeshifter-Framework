// ShapeshifterFramework | Patches | Patch_PawnRenderNodeWorker_AttachmentBody_ScaleFor.cs
// 목적 : 바디 애드온(Body Attachment)의 크기를 변신 폼의 스케일에 맞춰 확대/축소.
// 용도 : ScaleFor에 Postfix로 개입해 ShapeshiftRenderUtility.ApplyDrawScale(useHeadScale: false)을 호출함.

using HarmonyLib;
using ShapeshifterFramework.Utilities;
using UnityEngine;
using Verse;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch(typeof(PawnRenderNodeWorker_AttachmentBody), "ScaleFor")]
    [HarmonyPriority(Priority.Last)]
    public static class Patch_PawnRenderNodeWorker_AttachmentBody_ScaleFor
    {
        public static void Postfix(PawnDrawParms parms, ref Vector3 __result)
        {
            // 몸통 기준 스케일 적용
            ShapeshiftRenderUtility.ApplyDrawScale(parms, ref __result, useHeadScale: false);
        }
    }
}