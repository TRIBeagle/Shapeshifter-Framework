// ShapeshifterFramework | Patches | Patch_Pawn_EquipmentTracker_TryDropEquipment.cs
// 목적 : 폰이 무기를 바닥에 버리거나 다른 무기로 교체할 때 호출되는 바닐라 로직(TryDropEquipment)을 가로채어 통제.
// 용도 : 프레임워크가 강제로 소환한 폼 전용 무기(generatedWeapons)를 유저가 꼼수로 해제하거나 폰 사망 시 드랍되어 아이템이 무한 복사되는 버그를 원천 차단.

using HarmonyLib;
using RimWorld;
using Verse;
using ShapeshifterFramework.Utilities;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch(typeof(Pawn_EquipmentTracker), "TryDropEquipment")]
    public static class Patch_Pawn_EquipmentTracker_TryDropEquipment
    {
        public static bool Prefix(Pawn_EquipmentTracker __instance, ThingWithComps eq, ref ThingWithComps resultingEq, ref bool __result)
        {
            if (eq == null) return true;
            if (__instance.pawn != null)
            {
                if (ShapeshiftRegistry.TryGet(__instance.pawn, out var comp, out var form) && !comp.suppressEquipLock)
                {
                    if (comp.IsGeneratedWeapon(eq))
                    {
                        // 1. 유저 직접 드랍 시도 차단
                        if (eq.holdingOwner == __instance.pawn.equipment.GetDirectlyHeldThings() && __instance.pawn.IsColonistPlayerControlled && !__instance.pawn.Dead)
                        {
                            Messages.Message("SSF_Message_CannotDropGeneratedWeapon".Translate(), __instance.pawn, MessageTypeDefOf.RejectInput, false);
                            resultingEq = null;
                            __result = false;
                            return false;
                        }

                        // 2. 시스템 강제 드랍 시 소멸 처리 (복사 방지)
                        //    ThingOwner에서 먼저 제거해야 "Destroy but holdingOwner still set" 에러 방지
                        resultingEq = null;
                        if (eq.holdingOwner != null)
                            eq.holdingOwner.Remove(eq);
                        eq.Destroy(DestroyMode.Vanish);
                        __result = true; // 처리 완료로 보고
                        return false;    // 바닐라 드랍 스킵
                    }
                }
            }
            return true;
        }
    }
}