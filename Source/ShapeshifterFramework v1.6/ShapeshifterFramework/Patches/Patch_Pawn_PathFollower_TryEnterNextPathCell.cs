// ShapeshifterFramework | Patches | Patch_Pawn_PathFollower_TryEnterNextPathCell.cs
// 목적 : 폰이 이동(Pathing)하여 한 칸 전진할 때마다 물(Water) 타일 진입/이탈 여부를 정밀 감지하여 그래픽을 갱신.
// 용도 : 이동 전(Prefix)과 후(Postfix)의 바닥 타일 IsWater 상태(__state)를 비교하여, 상태가 변했고 수영 텍스처가 있는 폼이라면 SetAllGraphicsDirty를 호출해 즉각적인 수영 그래픽 전환을 유도함.

using HarmonyLib;
using ShapeshifterFramework.Utilities;
using Verse;
using Verse.AI;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch(typeof(Pawn_PathFollower), "TryEnterNextPathCell")]
    internal static class Patch_Pawn_PathFollower_TryEnterNextPathCell
    {
        static void Prefix(Pawn ___pawn, out bool __state)
        {
            __state = false;

            Pawn pawn = ___pawn;
            if (pawn == null || !pawn.Spawned || pawn.Map == null) return;

            // 비변신 폰 즉시 스킵 — 이동하는 모든 폰의 terrain 조회 회피 (Postfix도 ShouldRun으로 동일 가드)
            if (!ShapeshiftRegistry.IsActive(pawn)) return;

            var terr = pawn.Position.GetTerrain(pawn.Map);
            __state = terr != null && terr.IsWater;
        }

        static void Postfix(Pawn ___pawn, bool __state)
        {
            Pawn pawn = ___pawn;
            if (pawn == null || !pawn.Spawned || pawn.Map == null) return;

            ShapeshiftFormDef form;
            if (!ShapeshiftPartControlUtility.ShouldRun(pawn, out form) || form == null) return;

            // 수영 replacement가 있는 폼만 추적(없으면 SetAllGraphicsDirty 필요 없음)
            string swimPath;
            if (!ShapeshiftPartControlUtility.TryGetBodySwimmingReplacementPath(pawn, form, out swimPath))
                return;

            var terr = pawn.Position.GetTerrain(pawn.Map);
            bool now = terr != null && terr.IsWater;

            if (now != __state)
                pawn.Drawer?.renderer?.SetAllGraphicsDirty();
        }
    }
}
