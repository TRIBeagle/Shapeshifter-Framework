// ShapeshifterFramework | Patches | Patch_JobGiver_OptimizeApparel_TryGiveJob.cs
// 목적 : 의류 잠금(LockApparel) 상태인 변신 폰의 의류 최적화 Job 생성을 차단.
// 용도 : 바닐라 JobGiver_OptimizeApparel이 잠금 폰에게 Wear Job을 주면, 폰이 옷까지 걸어갔다가
//        Wear Prefix에 거부당하는 헛걸음 + "cannot wear" 메시지 반복이 발생 — Job 생성 자체를 막음.
// 주의 : suppressEquipLock(내부 재장착 스코프) 중에는 LockApparel이 false를 반환하므로 자동 통과.

using HarmonyLib;
using RimWorld;
using ShapeshifterFramework.Utilities;
using Verse;
using Verse.AI;

namespace ShapeshifterFramework.Patches
{
    /// <summary>의류 잠금 변신 폰은 의류 최적화 Job을 받지 않음.</summary>
    [HarmonyPatch(typeof(JobGiver_OptimizeApparel), "TryGiveJob")]
    internal static class Patch_JobGiver_OptimizeApparel_TryGiveJob
    {
        static bool Prefix(Pawn pawn, ref Job __result)
        {
            if (pawn != null
                && ShapeshiftRegistry.TryGet(pawn, out var comp, out _)
                && ShapeshiftEquipRules.LockApparel(comp))
            {
                __result = null;
                return false; // 원본 스킵 — 잠금 중 의류 최적화 없음
            }
            return true;
        }
    }
}
