// ShapeshifterFramework | Patches | Patch_Pawn_ApparelTracker_Wear.cs
// 목적 : 변신 중 옷을 갈아입거나 입지 못하게 막는 장착 잠금(EquipLock) 기능.
// 용도 : ApparelTracker.Wear 최하단 관문에 Prefix로 개입하여, 내부 스크립트 복구 목적(suppressEquipLock)이 아닌 일반적인 착용 시도를 완전히 취소(return false)시키고 유저에게 거부 메시지를 띄움.

using HarmonyLib;
using RimWorld;
using ShapeshifterFramework.Utilities; // ShapeshiftEquipRules
using Verse;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch(typeof(Pawn_ApparelTracker))]
    [HarmonyPatch(nameof(Pawn_ApparelTracker.Wear))]
    [HarmonyPatch(new[] { typeof(Apparel), typeof(bool), typeof(bool) })]
    [HarmonyPriority(Priority.First)]
    internal static class Patch_Pawn_ApparelTracker_Wear
    {
        static bool Prefix(Pawn_ApparelTracker __instance, Apparel newApparel, bool dropReplacedApparel, bool locked)
        {
            Pawn pawn = __instance.pawn;
            if (pawn == null) return true;

            if (!ShapeshiftRegistry.TryGet(pawn, out var comp, out _)) return true;
            if (ShapeshiftEquipRules.LockApparel(comp))
            {
                // 내부 시스템(suppressEquipLock)에 의한 복구는 허용
                if (comp.suppressEquipLock) return true;

                if (pawn.IsColonistPlayerControlled)
                {
                    Messages.Message("SSF_Message_CannotWear".Translate(pawn.Named("PAWN")),
                                     pawn, MessageTypeDefOf.RejectInput, false);
                }
                // 백스톱: 아웃핏 스탠드 경유 Wear는 스탠드에서 먼저 제거된 뒤 여기를 호출 —
                // 그냥 차단하면 의류가 허공에서 소실되므로 폰 옆에 되살림 (바닥 의류는 Spawned라 no-op)
                ShapeshiftEquipRules.RecoverBlockedEquipItem(newApparel, pawn);
                return false; // 원본 스킵
            }
            return true;
        }
    }
}
