// ShapeshifterFramework | Patches | Patch_Pawn_EquipmentTracker_AddEquipment.cs
// 목적 : 변신 중 무기 장착을 막는 장착 잠금(EquipLock) 기능.
// 용도 : AddEquipment 메서드에 Prefix로 개입하여, 내부 시스템에 의한 자동 복구(suppressEquipLock)가 아닌 플레이어/AI의 강제 무기 장착 시도를 철저히 차단(return false)함.

using HarmonyLib;
using RimWorld;
using ShapeshifterFramework.Comps;
using ShapeshifterFramework.Utilities; // ShapeshiftEquipRules
using Verse;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch(typeof(Pawn_EquipmentTracker))]
    [HarmonyPatch(nameof(Pawn_EquipmentTracker.AddEquipment))]
    [HarmonyPatch(new[] { typeof(ThingWithComps) })]
    [HarmonyPriority(Priority.First)]
    public static class Patch_Pawn_EquipmentTracker_AddEquipment
    {
        static bool Prefix(Pawn_EquipmentTracker __instance, ThingWithComps newEq)
        {
            Pawn pawn = __instance.pawn; // public
            if (pawn == null) return true;

            var comp = pawn.TryGetComp<CompShapeshifter>();
            if (comp != null && comp.isTransformed && ShapeshiftEquipRules.LockWeapons(comp))
            {
                if (!comp.suppressEquipLock && pawn.IsColonistPlayerControlled)
                {
                    Messages.Message("Shapeshift_CannotEquipWhileTransformed".Translate(pawn.Named("PAWN")),
                                     pawn, MessageTypeDefOf.RejectInput, false);
                }
                return false; // 원본 AddEquipment 실행 취소
            }
            return true;
        }
    }
}
