// ShapeshifterFramework | Patches | Patch_Pawn_TryGetAttackVerb.cs
// 목적 : 유저가 강제 공격을 지시하거나 자동 사격(Auto-attack)이 발동할 때, 변신 폼의 무기가 의도대로 선택되도록 통제.
// 용도 : 유저가 특정 Verb로 강제 공격 중일 때는 다른 Verb가 섞여 나가지 않도록 고정하며, 자동 사격 시에는 UI 지즈모에서 꺼둔(Toggle OFF) Verb가 발사되지 않도록 필터링함.
//        근접 도구(tools)가 정의된 폼의 경우, replaceNativeTools 여부에 따라 바닐라 근접 선택을 교체하거나 보완함.

using HarmonyLib;
using RimWorld;
using ShapeshifterFramework.Comps;
using ShapeshifterFramework.Utilities;
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

                // 2) 변신 중이면 토글 상태에 따라 verb 선택 (allowManualCastWeapons 무관)
                var comp = __instance.TryGetComp<CompShapeshifter>();
                if (comp != null && comp.isTransformed)
                {
                    var vt = comp.ShapeshiftVerbTracker;
                    if (vt != null)
                    {
                        // 2a) 배타적 토글: 활성화된(ON) 원거리 verb 중 첫 번째 반환
                        var verbs = vt.AllVerbs;
                        for (int i = 0; i < verbs.Count; i++)
                        {
                            var v = verbs[i];
                            if (v == null || v.verbProps == null) continue;
                            if (!v.verbProps.Ranged) continue;
                            if (!v.Available()) continue;
                            if (!comp.IsAutoAttackEnabled(i, v)) continue;
                            if (target != null && !v.CanHitTarget(target)) continue;

                            __result = v;
                            return false;
                        }

                        // 2b) replaceNativeTools=true: 폼 도구만 사용 (바닐라 근접 완전 차단)
                        var form = comp.currentForm;
                        if (form != null && form.replaceNativeTools.HasValue && form.replaceNativeTools.Value
                            && form.tools != null && form.tools.Count > 0)
                        {
                            var bestMelee = FindBestFormMelee(verbs);
                            if (bestMelee != null)
                            {
                                ShapeshiftDiagnostics.Info($"TryGetAttackVerb(replace): form melee tool={((bestMelee as Verb_MeleeAttack)?.tool?.label ?? "?")}");
                                __result = bestMelee;
                                return false;
                            }
                            else
                            {
                                ShapeshiftDiagnostics.Info($"TryGetAttackVerb(replace): FindBestFormMelee returned null! verbs.Count={verbs.Count}, melee verbs:");
                                for (int j = 0; j < verbs.Count; j++)
                                {
                                    var vj = verbs[j];
                                    if (vj == null) continue;
                                    ShapeshiftDiagnostics.Info($"  [{j}] {vj.GetType().Name} isMelee={vj.verbProps?.IsMeleeAttack} tool={((vj as Verb_MeleeAttack)?.tool?.label ?? "null")} caster={vj.caster?.ToString() ?? "null"}");
                                }
                            }
                        }

                        // 2c) replaceNativeTools=false (또는 null) + 폼 도구 있음:
                        //     바닐라 실행 후 Postfix에서 비교 (Prefix에서는 처리 안 함)
                    }
                }
            }
            catch (Exception e)
            {
                Log.Warning($"[SSF] TryGetAttackVerb Prefix failed: {e}");
            }
            return true; // 기본 원본 실행
        }

        /// <summary>바닐라 결과보다 강한 폼 근접 도구가 있으면 교체 (replaceNativeTools=false 대응).</summary>
        public static void Postfix(Pawn __instance, ref Verb __result, Thing target)
        {
            try
            {
                var comp = __instance.TryGetComp<CompShapeshifter>();
                if (comp == null || !comp.isTransformed) return;

                var form = comp.currentForm;
                if (form == null || form.tools == null || form.tools.Count == 0) return;

                // replaceNativeTools=true는 Prefix에서 이미 처리했으므로 스킵
                if (form.replaceNativeTools.HasValue && form.replaceNativeTools.Value) return;

                var vt = comp.ShapeshiftVerbTracker;
                if (vt == null) return;

                var verbs = vt.AllVerbs;
                var bestFormMelee = FindBestFormMelee(verbs);
                if (bestFormMelee == null) return;

                // 바닐라 결과가 없거나, 바닐라 근접보다 폼 도구가 강하면 교체
                if (__result == null)
                {
                    ShapeshiftDiagnostics.Info($"TryGetAttackVerb(add): no vanilla result, using form melee tool={((bestFormMelee as Verb_MeleeAttack)?.tool?.label ?? "?")}");
                    __result = bestFormMelee;
                    return;
                }

                // 바닐라 결과가 원거리면 건드리지 않음
                if (__result.verbProps != null && __result.verbProps.Ranged) return;

                // 바닐라 근접 vs 폼 근접: 파워 비교
                var vanillaMelee = __result as Verb_MeleeAttack;
                var formMelee = bestFormMelee as Verb_MeleeAttack;
                float vanillaPower = (vanillaMelee?.tool != null) ? vanillaMelee.tool.power : 0f;
                float formPower = (formMelee?.tool != null) ? formMelee.tool.power : 0f;

                if (formPower > vanillaPower)
                {
                    ShapeshiftDiagnostics.Info($"TryGetAttackVerb(add): form melee({formMelee?.tool?.label}={formPower}) > vanilla({vanillaMelee?.tool?.label}={vanillaPower})");
                    __result = bestFormMelee;
                }
            }
            catch (Exception e)
            {
                Log.Warning($"[SSF] TryGetAttackVerb Postfix failed: {e}");
            }
        }

        /// <summary>verbs 리스트에서 가장 파워 높은 근접 verb 반환.</summary>
        private static Verb FindBestFormMelee(System.Collections.Generic.List<Verb> verbs)
        {
            Verb best = null;
            float bestPower = -1f;
            for (int i = 0; i < verbs.Count; i++)
            {
                var v = verbs[i];
                if (v == null || v.verbProps == null) continue;
                if (!v.verbProps.IsMeleeAttack) continue;
                // 근접 verb는 Available() 체크 생략 — shapeshiftVerbTracker verb가
                // 바닐라 초기화 경로를 완전히 거치지 않아 false를 반환할 수 있음

                var vma = v as Verb_MeleeAttack;
                float power = (vma?.tool != null) ? vma.tool.power : 0f;
                if (best == null || power > bestPower)
                {
                    best = v;
                    bestPower = power;
                }
            }
            return best;
        }
    }
}
