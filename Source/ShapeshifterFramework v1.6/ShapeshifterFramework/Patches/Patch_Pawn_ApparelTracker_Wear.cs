// ShapeshifterFramework | Patches | Patch_Pawn_ApparelTracker_Wear.cs
// 목적 : 변신 중 옷을 갈아입거나 입지 못하게 막는 장착 잠금(EquipLock) 기능.
// 용도 : ApparelTracker.Wear 최하단 관문에 Prefix로 개입하여, 내부 스크립트 복구 목적(suppressEquipLock)이 아닌 일반적인 착용 시도를 완전히 취소(return false)시키고 유저에게 거부 메시지를 띄움.

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
