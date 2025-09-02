// Patch_HumanlikeMeshPoolUtility_Sizing.cs
// 목적: 바닐라 머리/몸 메쉬 크기 산출값을 폼 스케일로 치환.
// 용도: Postfix에서 Effective(...)로 얻은 target 값으로 __result 교체.
// 주의: MeshSet 캐시 구조 특성상 “대입” 방식 필요(곱셈 불가).

using HarmonyLib;
using ShapeshifterFramework.Utilities;
using UnityEngine;
using Verse;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch(typeof(HumanlikeMeshPoolUtility))]
    public static class Patch_HumanlikeMeshPoolUtility_Sizing
    {
        // Body 폭(및 높이 기준) 치환
        [HarmonyPatch(nameof(HumanlikeMeshPoolUtility.HumanlikeBodyWidthForPawn))]
        [HarmonyPostfix, HarmonyPriority(Priority.Last)]
        static void HumanlikeBodyWidthForPawn_Postfix(Pawn pawn, ref float __result)
        {
            __result = ShapeshiftSizeFactorResolver.Effective(pawn).bodyWidth;
        }

        // Head MeshSet
        [HarmonyPatch(nameof(HumanlikeMeshPoolUtility.GetHumanlikeHeadSetForPawn))]
        [HarmonyPostfix, HarmonyPriority(Priority.Last)]
        static void GetHumanlikeHeadSetForPawn_Postfix(Pawn pawn, float wFactor, float hFactor, ref GraphicMeshSet __result)
        {
            var story = pawn != null ? pawn.story : null;
            if (story == null || story.headType == null) return;

            var ls = pawn.ageTracker != null ? pawn.ageTracker.CurLifeStage : null;
            float vanillaHead = (ls != null && ls.headSizeFactor.HasValue) ? ls.headSizeFactor.Value : 1f;
            float targetHead = ShapeshiftSizeFactorResolver.Effective(pawn).headSizeFactor;
            if (Mathf.Approximately(targetHead, vanillaHead)) return;

            float num = 1.5f * targetHead; // 바닐라 기본폭 1.5f에 head factor 곱
            __result = MeshPool.GetMeshSetForSize(num * wFactor, num * hFactor);
        }

        // Hair MeshSet
        [HarmonyPatch(nameof(HumanlikeMeshPoolUtility.GetHumanlikeHairSetForPawn))]
        [HarmonyPostfix, HarmonyPriority(Priority.Last)]
        static void GetHumanlikeHairSetForPawn_Postfix(Pawn pawn, float wFactor, float hFactor, ref GraphicMeshSet __result)
        {
            var story = pawn != null ? pawn.story : null;
            if (story == null || story.headType == null) return;

            var ls = pawn.ageTracker != null ? pawn.ageTracker.CurLifeStage : null;
            float vanillaHead = (ls != null && ls.headSizeFactor.HasValue) ? ls.headSizeFactor.Value : 1f;
            float targetHead = ShapeshiftSizeFactorResolver.Effective(pawn).headSizeFactor;
            if (Mathf.Approximately(targetHead, vanillaHead)) return;

            Vector2 baseSz = story.headType.hairMeshSize * new Vector2(wFactor, hFactor);
            Vector2 final = baseSz * targetHead;
            __result = MeshPool.GetMeshSetForSize(final.x, final.y);
        }

        // Beard MeshSet
        [HarmonyPatch(nameof(HumanlikeMeshPoolUtility.GetHumanlikeBeardSetForPawn))]
        [HarmonyPostfix, HarmonyPriority(Priority.Last)]
        static void GetHumanlikeBeardSetForPawn_Postfix(Pawn pawn, float wFactor, float hFactor, ref GraphicMeshSet __result)
        {
            var story = pawn != null ? pawn.story : null;
            if (story == null || story.headType == null) return;

            var ls = pawn.ageTracker != null ? pawn.ageTracker.CurLifeStage : null;
            float vanillaHead = (ls != null && ls.headSizeFactor.HasValue) ? ls.headSizeFactor.Value : 1f;
            float targetHead = ShapeshiftSizeFactorResolver.Effective(pawn).headSizeFactor;
            if (Mathf.Approximately(targetHead, vanillaHead)) return;

            Vector2 baseSz = story.headType.beardMeshSize * new Vector2(wFactor, hFactor);
            Vector2 final = baseSz * targetHead;
            __result = MeshPool.GetMeshSetForSize(final.x, final.y);
        }
    }
}
