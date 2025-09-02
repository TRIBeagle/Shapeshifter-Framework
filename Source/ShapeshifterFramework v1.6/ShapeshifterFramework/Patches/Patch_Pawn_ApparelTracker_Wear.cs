// .NET 4.8 / C# 7.3
// 변신 중 의복 착용 금지(직접 시그니처 지정)
// - 대상 메서드: RimWorld.Pawn_ApparelTracker.Wear(Apparel newApparel, bool dropReplacedApparel = true, bool locked = false)
// - 이유: 최종 장착 지점 차단이 가장 안전. 메뉴/AI/타 모드 호출 모두 커버.
// - 성능: 리플렉션/Traverse 미사용.

using HarmonyLib;
using RimWorld;
using ShapeshifterFramework.Comps;
using ShapeshifterFramework.Utilities; // ShapeshiftEquipRules
using Verse;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch(typeof(Pawn_ApparelTracker))]
    [HarmonyPatch(nameof(Pawn_ApparelTracker.Wear))]
    [HarmonyPatch(new[] { typeof(Apparel), typeof(bool), typeof(bool) })]
    [HarmonyPriority(Priority.First)]
    public static class Patch_Pawn_ApparelTracker_Wear
    {
        static bool Prefix(Pawn_ApparelTracker __instance, Apparel newApparel, bool dropReplacedApparel, bool locked)
        {
            Pawn pawn = __instance.pawn; // public 필드라 Traverse 불필요
            if (pawn == null) return true;

            var comp = pawn.TryGetComp<CompShapeshifter>();
            if (comp != null && comp.isTransformed && ShapeshiftEquipRules.LockApparel(comp))
            {
                if (!comp.suppressEquipLock && pawn.IsColonistPlayerControlled)
                {
                    Messages.Message("Shapeshift_CannotWearWhileTransformed".Translate(pawn.Named("PAWN")),
                                     pawn, MessageTypeDefOf.RejectInput, false);
                }
                return false; // 원본 Wear 실행 취소
            }
            return true;
        }
    }
}
