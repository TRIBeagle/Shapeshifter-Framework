// ShapeshifterFramework | Patches | Patch_Pawn_TryGetAttackVerb.cs
// 목적 : 유저가 강제 공격을 지시하거나 자동 사격(Auto-attack)이 발동할 때, 변신 폼의 무기가 의도대로 선택되도록 통제.
// 용도 : 유저가 특정 Verb로 강제 공격 중일 때는 다른 Verb가 섞여 나가지 않도록 고정하며, 자동 사격 시에는 UI 지즈모에서 꺼둔(Toggle OFF) Verb가 발사되지 않도록 필터링함.

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

                // 2) 변신 중이고 자동사격 경로(allowManualCastWeapons == false)라면, 라운드 로빈으로 verb 선택
                var comp = __instance.TryGetComp<CompShapeshifter>();
                if (comp != null && comp.isTransformed && !allowManualCastWeapons)
                {
                    var vt = comp.ShapeshiftVerbTracker;
                    if (vt != null)
                    {
                        // 라운드 로빈: 마지막 인덱스 다음부터 순회
                        var verbs = vt.AllVerbs;
                        int count = verbs.Count;
                        if (count > 0)
                        {
                            int start = (comp.lastAutoVerbIndex + 1) % count;
                            for (int offset = 0; offset < count; offset++)
                            {
                                int i = (start + offset) % count;
                                var v = verbs[i];
                                if (v == null || v.verbProps == null) continue;
                                if (!v.verbProps.Ranged) continue;
                                if (!v.Available()) continue;

                                // 토글 OFF면 자동사격 후보에서 제외
                                if (!comp.IsAutoAttackEnabled(i, v)) continue;

                                // 타겟 적합성 (간단 체크)
                                if (target != null && !v.CanHitTarget(target)) continue;

                                comp.lastAutoVerbIndex = i;
                                __result = v;
                                return false;
                            }
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
