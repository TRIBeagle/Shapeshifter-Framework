// ShapeshifterFramework | Patches | Patch_Verb_TryCastNextBurstShot_DurationCost.cs
// 목적 : 변신 폼 verb 사용 시 durationCostTicks 차감 및 entropyCost 신경열 추가.
// 용도 : verbGizmoOptions에 설정된 비용을 성공적으로 발사 후 적용.
//        durationCostTicks → 변신 타이머 차감, entropyCost → 신경열 추가 (로열티 DLC).
//        버스트 무기는 첫 발에만 1회 차감.
// 주의 : TryCastShot()은 abstract라 Harmony 패치 불가 → concrete 메서드 TryCastNextBurstShot() 패치.
//        Prefix에서 burstShotsLeft를 __state에 저장, Postfix에서 감소 여부로 발사 성공 판별.

using HarmonyLib;
using ShapeshifterFramework.Hediffs;
using ShapeshifterFramework.Utilities;
using Verse;

namespace ShapeshifterFramework.Patches
{
    /// <summary>Verb.TryCastNextBurstShot 성공 시 변신 시간 차감.</summary>
    [HarmonyPatch(typeof(Verb), "TryCastNextBurstShot")]
    public static class Patch_Verb_TryCastNextBurstShot_DurationCost
    {
        /// <summary>발사 전 burstShotsLeft를 __state에 저장.</summary>
        [HarmonyPrefix]
        static void Prefix(int ___burstShotsLeft, ref int __state)
        {
            __state = ___burstShotsLeft;
        }

        /// <summary>발사 후 burstShotsLeft 감소 여부로 성공 판별, 변신 시간 차감.</summary>
        [HarmonyPostfix]
        static void Postfix(Verb __instance, int ___burstShotsLeft, int __state)
        {
            // burstShotsLeft가 감소하지 않았으면 발사 실패
            if (___burstShotsLeft >= __state) return;

            // 변신 폰이 없으면 즉시 반환
            if (!ShapeshiftRegistry.HasAny()) return;

            // 버스트 무기: 첫 발에만 차감 (__state == burstShotCount일 때가 첫 발)
            int shotsPerBurst = __instance.verbProps?.burstShotCount ?? 1;
            if (shotsPerBurst > 1 && __state != shotsPerBurst)
                return;

            var pawn = __instance.CasterPawn;
            if (pawn == null) return;

            if (!ShapeshiftRegistry.TryGet(pawn, out var core, out _)) return;

            // 폼 전용 VerbTracker에 속한 verb인지 확인
            int idx = core.FindVerbIndex(__instance);
            if (idx < 0) return;

            int cost = core.GetVerbDurationCost(idx, __instance);
            float entropy = core.GetVerbEntropyCost(idx, __instance);

            // 비용이 아무것도 없으면 즉시 반환
            if (cost <= 0 && entropy <= 0f) return;

            // 변신 시간 차감
            if (cost > 0)
                core.ExtendDuration(-cost, true);

            // 신경열 추가 (로열티 DLC 비활성 또는 EntropyTracker 없으면 자연 스킵)
            if (entropy > 0f)
            {
                var tracker = pawn.psychicEntropy;
                if (tracker != null)
                {
                    tracker.TryAddEntropy(entropy);
                }
            }

            if (ShapeshifterFrameworkMod.Settings?.enableDebugLog == true)
            {
                string verbName = __instance.verbProps?.label ?? __instance.GetType().Name;
                Log.Message($"[SSF] Verb '{verbName}' used — deducted {cost} ticks (remaining: {core.RemainingShapeshiftTicks})" +
                    (entropy > 0f ? $", entropy +{entropy}" : ""));
            }
        }
    }
}
