// ShapeshifterFramework | Patches | Patch_HumanlikeMeshPoolUtility_Sizing.cs
// 목적 : 폰의 몸통, 머리, 머리카락, 수염의 '실제 렌더링 메쉬(Mesh) 크기'를 변신 폼의 스케일 배율에 맞춰 동적으로 생성.
// 용도 : GraphicMeshSet을 가져오는 4개의 바닐라 메서드에 Postfix로 개입하여, ShapeshiftSizeFactorResolver에서 계산된 배수(Width, SizeFactor)를 적용한 새로운 크기의 메쉬 셋을 반환함.

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
