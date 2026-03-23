// ShapeshifterFramework | Patches | Patch_DrugPolicyTracker_AllowedToTakeScheduledEver.cs
// 목적 : DrugPolicy 내부 배열에 등록되지 않은 ThingDef 조회 시 발생하는 ArgumentException 방어.
// 용도 : 변신 약물 등 커스텀 Drug ThingDef가 DrugPolicy 엔트리에 없을 때 FloatMenuOptionProvider_PickUpItem →
//        ShouldKeepDrugInInventory → AllowedToTakeScheduledEver 경로에서 예외가 터지는 것을 방지.
//        예외 발생 시 false(스케줄 복용 불가)를 반환하여 안전하게 폴백.

using HarmonyLib;
using RimWorld;
using System;
using Verse;

namespace ShapeshifterFramework.Patches
{
    /// <summary>DrugPolicy.get_Item에서 발생하는 ArgumentException을 Finalizer로 흡수하는 패치.</summary>
    [HarmonyPatch(typeof(Pawn_DrugPolicyTracker), nameof(Pawn_DrugPolicyTracker.AllowedToTakeScheduledEver))]
    public static class Patch_DrugPolicyTracker_AllowedToTakeScheduledEver
    {
        /// <summary>원본 메서드에서 ArgumentException 발생 시 false를 반환하고 예외를 삼킴.</summary>
        static Exception Finalizer(Exception __exception, ref bool __result)
        {
            if (__exception is ArgumentException)
            {
                // DrugPolicy 엔트리에 없는 ThingDef → 스케줄 복용 불가로 안전하게 폴백
                __result = false;
                return null; // 예외 삼킴
            }
            return __exception; // 다른 예외는 그대로 전파
        }
    }
}
