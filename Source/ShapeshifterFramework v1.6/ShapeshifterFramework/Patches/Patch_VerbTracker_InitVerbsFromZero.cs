// ShapeshifterFramework | Patches | Patch_VerbTracker_InitVerbsFromZero.cs
// 목적 : 폼에 지정된 근접 공격(Tools) 데이터를 런타임에 폰의 기본 공격 목록(NativeVerb)에 동적으로 주입.
// 용도 : 장비나 헤디프가 아닌 순수 폰(NativeVerb) 소유일 때만 작동하며, replaceNativeTools 옵션에 따라 기존 종족의 맨손 공격을 지우고 폼 전용 발톱/이빨 공격 등을 주입함 (근접 공격이 0개가 되는 상황을 철저히 방어).

using HarmonyLib;
using RimWorld;
using ShapeshifterFramework.Comps;
using System;
using System.Collections.Generic;
using Verse;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch(typeof(VerbTracker), "InitVerbsFromZero")]
    internal static class Patch_VerbTracker_InitVerbsFromZero
    {
        static void Postfix(VerbTracker __instance, ref List<Verb> ___verbs, IVerbOwner ___directOwner)
        {
            try
            {
                if (__instance == null || ___verbs == null) return;

                var owner = ___directOwner;
                if (owner == null) return;

                // 1) 네이티브(pawn) 트래커만 처리. 장비/헤디프 트래커는 스킵.
                //    (중요: 장비 트래커를 건드리면 무기 Verb까지 지워질 수 있음)
                if (owner.ImplementOwnerTypeDef != ImplementOwnerTypeDefOf.NativeVerb) return;

                var pawn = owner.ConstantCaster as Pawn;
                if (pawn == null) return;

                var comp = pawn.TryGetComp<CompShapeshifter>();
                var form = (comp != null && comp.isTransformed) ? comp.currentForm : null;
                if (form == null) return;

                // 2) Replace/Tools 정보 취득
                bool replaceNative = form.replaceNativeTools.HasValue && form.replaceNativeTools.Value;
                var tools = form.tools;

                // 3) 이번 주입에서 "실제로 추가될 근접 Verb 수"를 예측
                int willAdd = PredictAddCount(tools);

                // 4) Replace더라도 willAdd==0이면 제거하지 않는다 (안전 가드)
                if (replaceNative && willAdd > 0)
                {
                    // 네이티브 근접 verb 제거 (이 트래커는 네이티브 전용임)
                    for (int i = ___verbs.Count - 1; i >= 0; i--)
                    {
                        var v = ___verbs[i];
                        if (v != null && v.verbProps != null && v.verbProps.IsMeleeAttack)
                            ___verbs.RemoveAt(i);
                    }
                }

                // 5) Tool -> Maneuver -> Verb_MeleeAttack 주입
                if (willAdd > 0)
                {
                    // Maneuver 목록 캐시
                    var maneuvers = DefDatabase<ManeuverDef>.AllDefsListForReading;
                    for (int ti = 0; ti < tools.Count; ti++)
                    {
                        var tool = tools[ti]; if (tool == null) continue;
                        var caps = tool.capacities; if (caps == null || caps.Count == 0) continue;

                        for (int ci = 0; ci < caps.Count; ci++)
                        {
                            var cap = caps[ci]; if (cap == null) continue;

                            for (int mi = 0; mi < maneuvers.Count; mi++)
                            {
                                var man = maneuvers[mi];
                                if (man == null || man.requiredCapacity != cap) continue;

                                var vp = man.verb; if (vp == null) continue;
                                if (!vp.IsMeleeAttack) continue; // 근접만

                                var verb = CreateVerb(__instance, owner, vp, pawn);
                                if (verb == null) continue;

                                var vma = verb as Verb_MeleeAttack;
                                if (vma != null)
                                {
                                    vma.tool = tool;
                                    vma.maneuver = man;
                                }

                                // 바닐라 규칙에 맞는 loadID 부여
                                verb.loadID = Verb.CalculateUniqueLoadID(owner, ___verbs.Count);
                                ___verbs.Add(verb);
                            }
                        }
                    }
                }
                // willAdd==0이고 replaceNative==true였더라도, 4)에서 제거 자체를 하지 않았기 때문에
                // 근접 Verb 0개 상태는 발생하지 않는다.
            }
            catch (Exception e)
            {
                Log.Error("[SSF] InitVerbsFromZero Postfix failed: " + e);
            }
        }

        // tools로부터 실제로 추가될 "근접" Verb 개수를 예측
        private static int PredictAddCount(List<Tool> tools)
        {
            if (tools == null || tools.Count == 0) return 0;

            int add = 0;
            var maneuvers = DefDatabase<ManeuverDef>.AllDefsListForReading;
            for (int ti = 0; ti < tools.Count; ti++)
            {
                var tool = tools[ti]; if (tool == null) continue;
                var caps = tool.capacities; if (caps == null || caps.Count == 0) continue;

                for (int ci = 0; ci < caps.Count; ci++)
                {
                    var cap = caps[ci]; if (cap == null) continue;
                    for (int mi = 0; mi < maneuvers.Count; mi++)
                    {
                        var man = maneuvers[mi];
                        if (man == null || man.requiredCapacity != cap) continue;
                        var vp = man.verb; if (vp == null) continue;
                        if (vp.IsMeleeAttack) add++;
                    }
                }
            }
            return add;
        }

        // 생성 실패한 verbClass 캐시 — 같은 타입을 매번 재시도하지 않도록
        private static readonly HashSet<Type> FailedVerbClasses = new HashSet<Type>();

        private static Verb CreateVerb(VerbTracker tracker, IVerbOwner owner, VerbProperties vp, Pawn ownerPawn)
        {
            try
            {
                if (tracker == null || owner == null || vp == null || ownerPawn == null) return null;

                var cls = vp.verbClass != null ? vp.verbClass : typeof(Verb);

                // 이전에 실패한 타입은 재시도하지 않음
                if (FailedVerbClasses.Contains(cls)) return null;

                var verb = (Verb)Activator.CreateInstance(cls);

                verb.verbProps = vp;
                verb.verbTracker = tracker;

                // caster: owner.ConstantCaster 우선, 없으면 pawn
                var constCaster = owner.ConstantCaster;
                verb.caster = constCaster ?? ownerPawn;

                return verb;
            }
            catch (Exception e)
            {
                var cls = vp?.verbClass;
                if (cls != null) FailedVerbClasses.Add(cls);
                Log.Warning("[SSF] CreateVerb failed for " + (vp != null ? vp.ToString() : "null") + " : " + e);
                return null;
            }
        }
    }
}