// ShapeshifterFramework | Patches | Patch_PawnRenderUtility_DrawEquipmentAiming_Hide.cs
// 목적 : 시각적 필터(renderHide)에 걸린 무기나 장비가 전투/조준 중일 때 허공에 그려지는 것을 원천 차단.
// 용도 : DrawEquipmentAiming 메서드 시작 직전(Prefix)에 개입하여, 숨겨야 할 장비라면 렌더링 자체를 완전히 생략(return false)시킴.

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
