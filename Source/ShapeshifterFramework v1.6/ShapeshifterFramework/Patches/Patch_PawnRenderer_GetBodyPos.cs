// ShapeshifterFramework | Patches | Patch_PawnRenderer_GetBodyPos.cs
// 목적 : 변신 폼의 거대한 몸집(또는 특수 외형)이 침대나 이불 밑에 렌더링되어 사라지는 시각적 어색함을 해결.
// 용도 : 플레이어 소속 폰이 침대에 누워있을 때, 바닐라가 바디 렌더링을 끄려는 것을 후처리(Postfix)로 무시하고 showBody를 강제로 true로 덮어씌움.

using HarmonyLib;
using RimWorld;
using ShapeshifterFramework.Utilities;
using Verse;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch(typeof(PawnRenderer))]
    [HarmonyPatch("GetBodyPos")]
    internal class Patch_PawnRenderer_GetBodyPos
    {
        private static void Postfix(ref bool showBody, Pawn ___pawn)
        {
            // 인간형 + 플레이어 소속 + 비수감자
            if (___pawn == null || !___pawn.RaceProps.Humanlike
                || ___pawn.Faction == null || ___pawn.Faction != Faction.OfPlayer
                || ___pawn.IsPrisoner)
                return;

            var comp = ShapeshiftUtility.GetShapeShiftComp(___pawn);
            if (comp == null || comp.currentForm == null || !ShapeshiftUtility.IsShapeShifting(___pawn))
                return;

            // 이미 보이거나 침대가 아니면 스킵
            if (showBody || ___pawn.CurrentBed() == null)
                return;

            // 침대에서도 바디 보이도록 강제
            showBody = true;
        }
    }
}
