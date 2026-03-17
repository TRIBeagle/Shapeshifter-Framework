// ShapeshifterFramework | Patches | Patch_Pawn_SpawnSetup.cs
// 목적 : Pawn 스폰(맵 진입) 시 변신 중인 HediffComp_ShapeshiftCore를 레지스트리에 재등록하고 그래픽을 복원.
// 용도 : HediffComp는 PostSpawnSetup이 없으므로, Pawn.SpawnSetup Harmony Postfix로 대체.
//        Priority.Low로 실행하여 Drawer 초기화 이후에 안전하게 그래픽 dirty 처리.
// 주의 : PostDeSpawn에서는 해제하지 않음 (상단/동면관/포드 진입 시 레지스트리 누락 방지).

using HarmonyLib;
using ShapeshifterFramework.Hediffs;
using ShapeshifterFramework.Utilities;
using RimWorld;
using Verse;

namespace ShapeshifterFramework.Patches
{
    /// <summary>Pawn.SpawnSetup Postfix — 변신 중이면 레지스트리 재등록 + 그래픽 복원.</summary>
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.SpawnSetup))]
    internal static class Patch_Pawn_SpawnSetup
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Low)]
        static void Postfix(Pawn __instance, bool respawningAfterLoad)
        {
            if (__instance == null || __instance.health?.hediffSet == null) return;

            // HediffComp_ShapeshiftCore를 보유한 pawn만 처리
            var hediffs = __instance.health.hediffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                var core = (hediffs[i] as HediffWithComps)?.TryGetComp<HediffComp_ShapeshiftCore>();
                if (core != null)
                {
                    if (core.isTransformed && core.currentForm != null)
                    {
                        core.OnPawnSpawned(respawningAfterLoad);
                    }
                    return; // 변신 hediff는 동시에 1개만 존재
                }
            }
        }
    }
}
