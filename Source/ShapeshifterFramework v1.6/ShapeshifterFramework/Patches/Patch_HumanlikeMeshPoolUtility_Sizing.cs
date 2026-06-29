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
    internal static class Patch_HumanlikeMeshPoolUtility_Sizing
    {
        /// <summary>바닐라 기본 머리 폭 (HumanlikeMeshPoolUtility 기본값).</summary>
        private const float VanillaBaseHeadWidth = 1.5f;

        // Body 폭(및 높이 기준) 치환
        [HarmonyPatch(nameof(HumanlikeMeshPoolUtility.HumanlikeBodyWidthForPawn))]
        [HarmonyPostfix, HarmonyPriority(Priority.Last)]
        static void HumanlikeBodyWidthForPawn_Postfix(Pawn pawn, ref float __result)
        {
            if (!ShapeshiftRegistry.IsActive(pawn)) return;
            __result = ShapeshiftSizeFactorResolver.Effective(pawn).bodyWidth;
        }

        /// <summary>공통: 활성 폼의 headSizeFactor와 바닐라 값을 비교하여 차이가 있으면 (targetHead, vanillaHead) 반환, 없으면 false.</summary>
        static bool TryGetHeadFactors(Pawn pawn, out float targetHead, out float vanillaHead)
        {
            targetHead = 1f;
            vanillaHead = 1f;

            if (!ShapeshiftRegistry.IsActive(pawn)) return false;

            var story = pawn?.story;
            if (story == null || story.headType == null) return false;

            var ls = pawn.ageTracker?.CurLifeStage;
            vanillaHead = (ls != null && ls.headSizeFactor.HasValue) ? ls.headSizeFactor.Value : 1f;
            targetHead = ShapeshiftSizeFactorResolver.Effective(pawn).headSizeFactor;
            return !Mathf.Approximately(targetHead, vanillaHead);
        }

        // 머리 MeshSet
        [HarmonyPatch(nameof(HumanlikeMeshPoolUtility.GetHumanlikeHeadSetForPawn))]
        [HarmonyPostfix, HarmonyPriority(Priority.Last)]
        static void GetHumanlikeHeadSetForPawn_Postfix(Pawn pawn, float wFactor, float hFactor, ref GraphicMeshSet __result)
        {
            if (!TryGetHeadFactors(pawn, out float targetHead, out _)) return;

            float headWidth = VanillaBaseHeadWidth * targetHead;
            __result = MeshPool.GetMeshSetForSize(headWidth * wFactor, headWidth * hFactor);
        }

        // 헤어 MeshSet
        [HarmonyPatch(nameof(HumanlikeMeshPoolUtility.GetHumanlikeHairSetForPawn))]
        [HarmonyPostfix, HarmonyPriority(Priority.Last)]
        static void GetHumanlikeHairSetForPawn_Postfix(Pawn pawn, float wFactor, float hFactor, ref GraphicMeshSet __result)
        {
            if (!TryGetHeadFactors(pawn, out float targetHead, out _)) return;

            Vector2 baseSz = pawn.story.headType.hairMeshSize * new Vector2(wFactor, hFactor);
            Vector2 final = baseSz * targetHead;
            __result = MeshPool.GetMeshSetForSize(final.x, final.y);
        }

        // 수염 MeshSet
        [HarmonyPatch(nameof(HumanlikeMeshPoolUtility.GetHumanlikeBeardSetForPawn))]
        [HarmonyPostfix, HarmonyPriority(Priority.Last)]
        static void GetHumanlikeBeardSetForPawn_Postfix(Pawn pawn, float wFactor, float hFactor, ref GraphicMeshSet __result)
        {
            if (!TryGetHeadFactors(pawn, out float targetHead, out _)) return;

            Vector2 baseSz = pawn.story.headType.beardMeshSize * new Vector2(wFactor, hFactor);
            Vector2 final = baseSz * targetHead;
            __result = MeshPool.GetMeshSetForSize(final.x, final.y);
        }
    }
}
