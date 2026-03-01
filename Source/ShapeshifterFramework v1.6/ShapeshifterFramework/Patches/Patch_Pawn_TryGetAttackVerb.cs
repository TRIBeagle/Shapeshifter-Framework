// .NET Framework 4.8 / C# 7.3
// Patch_Pawn_TryGetAttackVerb.cs
// 목적:
//  - 플레이어 강제 사격(AttackStatic + playerForced) 중에는 해당 Job의 verbToUse만 사용하도록 고정.
//    → 수동 사격 시 다른 shapeshift verb가 동시에 발사되는 현상 방지
//  - (옵션) 자동사격 경로에서는 우리 토글을 반영(원거리 verb만 토글 적용)

using HarmonyLib;
using RimWorld;
using ShapeshifterFramework.Comps;
using System;
using Verse;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.TryGetAttackVerb), new Type[] { typeof(Thing), typeof(bool), typeof(bool) })]
    public static class Patch_Pawn_TryGetAttackVerb
    {
        public static bool Prefix(Pawn __instance, ref Verb __result, Thing target, bool allowManualCastWeapons, bool allowTurrets)
        {
            try
            {
                // 1) 플레이어 강제 사격 중이면 현재 Job의 verbToUse만 반환 (동시 발사 방지)
                var curJob = __instance.CurJob;
                if (curJob != null && curJob.def == JobDefOf.AttackStatic && curJob.playerForced && curJob.verbToUse != null)
                {
                    __result = curJob.verbToUse;
                    return false; // 원본 스킵
                }

                // 2) 변신 중이고 자동사격 경로(allowManualCastWeapons == false)라면, 우리 토글/필터를 반영
                var comp = __instance.TryGetComp<CompShapeshifter>();
                if (comp != null && comp.isTransformed && !allowManualCastWeapons)
                {
                    var vt = comp.ShapeshiftVerbTracker;
                    if (vt != null)
                    {
                        // 원거리 verb만 토글 적용. (근접은 바닐라 로직 유지)
                        var verbs = vt.AllVerbs;
                        for (int i = 0; i < verbs.Count; i++)
                        {
                            var v = verbs[i];
                            if (v == null || v.verbProps == null) continue;
                            if (!v.verbProps.Ranged) continue;
                            if (!v.Available()) continue;

                            // 토글 OFF면 자동사격 후보에서 제외
                            if (!comp.IsAutoAttackEnabled(i, v)) continue;

                            // 타겟 적합성 (간단 체크)
                            if (target != null && !v.CanHitTarget(target)) continue;

                            // 가장 먼저 만족하는 verb를 바로 선택(바닐라에 넘기지 않음)
                            __result = v;
                            return false;
                        }

                        // shapeshift 원거리 verb 중 자동사격 가능한 것이 없으면, 바닐라에 맡김
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Warning($"[SSF] TryGetAttackVerb Prefix failed: {e}");
            }
            return true; // 기본 원본 실행
        }
    }
}
