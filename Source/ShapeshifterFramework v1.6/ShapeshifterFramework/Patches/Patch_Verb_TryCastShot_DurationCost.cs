// ShapeshifterFramework | Patches | Patch_Verb_TryCastShot_DurationCost.cs
// 목적 : 변신 폼 verb 사용 시 durationCostTicks만큼 변신 잔여 시간을 차감.
// 용도 : verbGizmoOptions에 durationCostTicks가 설정된 verb를 성공적으로 발사하면
//        해당 틱만큼 변신 타이머를 깎는다. 버스트 무기는 첫 발에만 1회 차감.

using HarmonyLib;
using ShapeshifterFramework.Hediffs;
using ShapeshifterFramework.Utilities;
using Verse;

namespace ShapeshifterFramework.Patches
{
    /// <summary>Verb.TryCastShot 성공 시 변신 시간 차감.</summary>
    [HarmonyPatch(typeof(Verb), "TryCastShot")]
    public static class Patch_Verb_TryCastShot_DurationCost
    {
        [HarmonyPostfix]
        static void Postfix(Verb __instance, bool __result, int ___burstShotsLeft)
        {
            // 발사 실패 시 비용 없음
            if (!__result) return;

            // 버스트 무기: 첫 발에만 차감 (burstShotsLeft == ShotsPerBurst-1일 때가 첫 발 직후)
            int shotsPerBurst = __instance.verbProps?.burstShotCount ?? 1;
            if (shotsPerBurst > 1 && ___burstShotsLeft != shotsPerBurst - 1)
                return;

            var pawn = __instance.CasterPawn;
            if (pawn == null) return;

            if (!ShapeshiftRegistry.TryGet(pawn, out var core, out _)) return;

            // 폼 전용 VerbTracker에 속한 verb인지 확인
            int idx = core.FindVerbIndex(__instance);
            if (idx < 0) return;

            int cost = core.GetVerbDurationCost(idx, __instance);
            if (cost <= 0) return;

            core.ExtendDuration(-cost, true);

            if (ShapeshifterFrameworkMod.Settings?.enableDebugLog == true)
            {
                string verbName = __instance.verbProps?.label ?? __instance.GetType().Name;
                Log.Message($"[SSF] Verb '{verbName}' 사용 — 변신 시간 {cost}틱 차감 (남은: {core.RemainingShapeshiftTicks}틱)");
            }
        }
    }
}
