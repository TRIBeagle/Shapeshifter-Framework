// ShapeshifterFramework | Patches | Patch_Pawn_EquipmentTracker.cs
// 목적 : 폰이 무기를 바닥에 버리거나 다른 무기로 교체할 때 호출되는 바닐라 로직(TryDropEquipment)을 가로채어 통제.
// 용도 : 프레임워크가 강제로 소환한 폼 전용 무기(generatedWeapons)를 유저가 꼼수로 해제하거나 폰 사망 시 드랍되어 아이템이 무한 복사되는 버그를 원천 차단.

using HarmonyLib;
using RimWorld;
using Verse;
using ShapeshifterFramework.Comps;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch(typeof(Pawn_EquipmentTracker), "TryDropEquipment")]
    public static class Patch_Pawn_EquipmentTracker_TryDropEquipment
    {
        public static bool Prefix(Pawn_EquipmentTracker __instance, ThingWithComps eq, ref ThingWithComps resultingEq, ref bool __result)
        {
            if (__instance.pawn != null)
            {
                var comp = __instance.pawn.TryGetComp<CompShapeshifter>();
                if (comp != null && comp.isTransformed && !comp.suppressEquipLock)
                {
                    if (comp.IsGeneratedWeapon(eq))
                    {
                        // 1. 유저가 직접 마우스로 버리기/교체를 시도한 경우 (꼼수 방지)
                        if (eq.holdingOwner == __instance.pawn.equipment.GetDirectlyHeldThings() && __instance.pawn.IsColonistPlayerControlled && !__instance.pawn.Dead)
                        {
                            Messages.Message("SSF_Message_CannotDropGeneratedWeapon".Translate(), __instance.pawn, MessageTypeDefOf.RejectInput, false);
                            resultingEq = null;
                            __result = false;
                            return false;
                        }

                        // 2. 팔이 잘리거나, 사망 등 '불가항력'으로 시스템이 드랍을 강제하는 경우 (무한 루프 방지)
                        // 바닥에 떨어져 템이 복사되는 것을 막기 위해 강제로 소멸(Destroy)시킵니다.
                        resultingEq = null;
                        eq.Destroy(DestroyMode.Vanish);
                        __result = true; // "드랍(사실은 소멸) 처리 완료했어!" 라고 시스템을 안심시킴
                        return false;    // 바닐라의 바닥 드랍 로직은 스킵
                    }
                }
            }
            return true;
        }
    }
}