// Patches/Patch_Pawn_PathFollower_TryEnterNextPathCell.cs
// 목적: 이동 1칸 시점에 물 타일(IsWater) 진입/이탈 감지 → SetAllGraphicsDirty()
// 핵심: SelectByGender 제거. 유틸의 merge 기반 TryGetBodySwimmingReplacementPath 사용.

using HarmonyLib;
using ShapeshifterFramework.Utilities;
using Verse;
using Verse.AI;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch(typeof(Pawn_PathFollower), "TryEnterNextPathCell")]
    internal static class Patch_Pawn_PathFollower_TryEnterNextPathCell
    {
        private static readonly AccessTools.FieldRef<Pawn_PathFollower, Pawn> PawnRef =
            AccessTools.FieldRefAccess<Pawn_PathFollower, Pawn>("pawn");

        static void Prefix(Pawn_PathFollower __instance, out bool __state)
        {
            __state = false;

            Pawn pawn = null;
            try { pawn = PawnRef(__instance); } catch { pawn = null; }
            if (pawn == null || !pawn.Spawned || pawn.Map == null) return;

            var terr = pawn.Position.GetTerrain(pawn.Map);
            __state = terr != null && terr.IsWater;
        }

        static void Postfix(Pawn_PathFollower __instance, bool __state)
        {
            Pawn pawn = null;
            try { pawn = PawnRef(__instance); } catch { pawn = null; }
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
