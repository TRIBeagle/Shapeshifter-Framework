// ShapeshifterFramework | Patches | Patch_Pawn_TryGetAttackVerb.cs
// 목적 : 유저가 강제 공격을 지시하거나 자동 사격(Auto-attack)이 발동할 때, 변신 폼의 무기가 의도대로 선택되도록 통제.
// 용도 : 유저가 특정 Verb로 강제 공격 중일 때는 다른 Verb가 섞여 나가지 않도록 고정하며, 자동 사격 시에는 UI 지즈모에서 꺼둔(Toggle OFF) Verb가 발사되지 않도록 필터링함.
//        근접 도구(tools)는 shapeshiftVerbTracker에서 관리하며, pawn.verbTracker에서 네이티브가 제거된 후 Postfix에서 보완.
//
// ── [Verb 선택 흐름 — 3개 패치 협력 구조] ──
//   1. Patch_VerbTracker_InitVerbsFromZero  : 폼 교체 시 tools를 NativeVerb 풀에 주입/제거 (구성 시점)
//   2. ★ 이 파일 (TryGetAttackVerb)        : 공격 시 원거리 우선 → 근접 폴백으로 최적 verb 선정 (선택 시점)
//   3. Patch_Pawn_MeleeVerbs_TryGetMeleeVerb : 바닐라 근접 경로 안전망 + power 비교 (폴백 시점)
//   공유 헬퍼: FindBestFormMelee() — 이 파일에 정의, TryGetMeleeVerb 패치에서 재사용

using HarmonyLib;
using RimWorld;
using ShapeshifterFramework.Utilities;
using System;
using System.Collections.Generic;
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
                //    변신 폰 전용 — 비변신 폰에서는 바닐라 로직에 맡김
                if (ShapeshiftRegistry.IsActive(__instance))
                {
                    var curJob = __instance.CurJob;
                    if (curJob != null && curJob.def == JobDefOf.AttackStatic && curJob.playerForced && curJob.verbToUse != null)
                    {
                        __result = curJob.verbToUse;
                        return false; // 원본 스킵
                    }
                }

                // 2) 변신 중이면 토글 상태에 따라 verb 선택
                if (ShapeshiftRegistry.TryGet(__instance, out var comp, out var form))
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

                        // 2b) 원거리가 없으면 폼 근접 도구 공급 (replaceNativeTools 시 네이티브가 제거된 상태)
                        if (form.tools != null && form.tools.Count > 0)
                        {
                            var bestMelee = FindBestFormMelee(verbs);
                            if (bestMelee != null)
                            {
                                __result = bestMelee;
                                return false;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Warning($"[SSF] TryGetAttackVerb Prefix failed: {e}");
            }
            return true; // 기본 원본 실행
        }

        /// <summary>verbs 리스트에서 가장 파워 높은 근접 verb 반환. TryGetMeleeVerb 패치에서도 공유.</summary>
        internal static Verb FindBestFormMelee(List<Verb> verbs)
        {
            Verb best = null;
            float bestPower = -1f;
            for (int i = 0; i < verbs.Count; i++)
            {
                var v = verbs[i];
                if (v == null || v.verbProps == null) continue;
                if (!v.verbProps.IsMeleeAttack) continue;

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
