// Patch_PawnRenderUtility_DrawEquipmentAiming_Hide.cs  (C# 7.3)
using HarmonyLib;
using ShapeshifterFramework.Utilities;
using UnityEngine;
using Verse;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch(typeof(PawnRenderUtility), "DrawEquipmentAiming")]
    public static class Patch_PawnRenderUtility_DrawEquipmentAiming_Hide
    {
        [HarmonyPrefix]
        static bool Prefix(Thing eq, Vector3 drawLoc, float aimAngle)
        {
            if (eq == null) return true;
            Pawn pawn = ShapeshiftReflectionCache.TryGetHolderPawn(eq);
            if (pawn == null) return true;

            if (ShapeshiftVisualFilter.ShouldHideEquipmentGraphic(pawn, eq))
                return false; // 스킵

            return true;
        }
    }
}
