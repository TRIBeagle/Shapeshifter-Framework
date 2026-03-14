// ShapeshifterFramework | Patches | Patch_VerbTracker_InitVerbsFromZero.cs
// 목적 : 폼에 지정된 근접 공격(Tools) 데이터를 런타임에 폰의 기본 공격 목록(NativeVerb)에 동적으로 주입.
// 용도 : 장비나 헤디프가 아닌 순수 폰(NativeVerb) 소유일 때만 작동하며, replaceNativeTools 옵션에 따라 기존 종족의 맨손 공격을 지우고 폼 전용 발톱/이빨 공격 등을 주입함 (근접 공격이 0개가 되는 상황을 철저히 방어).
// 주의 : ManeuverDef를 requiredCapacity별로 정적 캐시하여 O(N) 전체 스캔을 O(1) 조회로 최적화. 기존 PredictAddCount 2패스 구조를 수집-후-주입 단일 패스로 통합.

using HarmonyLib;
using RimWorld;
using ShapeshifterFramework.Comps;
using ShapeshifterFramework.Utilities;
using System;
using System.Collections.Generic;
using Verse;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch(typeof(VerbTracker), "InitVerbsFromZero")]
    internal static class Patch_VerbTracker_InitVerbsFromZero
    {
        // ManeuverDef를 requiredCapacity별로 그룹화한 정적 캐시 (DefDatabase는 로드 후 불변)
        private static Dictionary<ToolCapacityDef, List<ManeuverDef>> _maneuversByCapacity;

        private static Dictionary<ToolCapacityDef, List<ManeuverDef>> ManeuversByCapacity
        {
            get
            {
                if (_maneuversByCapacity == null)
                {
                    _maneuversByCapacity = new Dictionary<ToolCapacityDef, List<ManeuverDef>>();
                    var all = DefDatabase<ManeuverDef>.AllDefsListForReading;
                    for (int i = 0; i < all.Count; i++)
                    {
                        var m = all[i];
                        if (m?.requiredCapacity == null) continue;
                        if (!_maneuversByCapacity.TryGetValue(m.requiredCapacity, out var list))
                        {
                            list = new List<ManeuverDef>();
                            _maneuversByCapacity[m.requiredCapacity] = list;
                        }
                        list.Add(m);
                    }
                }
                return _maneuversByCapacity;
            }
        }

        // 수집 단계에서 사용하는 임시 구조체 (힙 할당 최소화)
        private struct VerbEntry
        {
            public Tool tool;
            public ManeuverDef maneuver;
            public VerbProperties verbProps;
        }

        // 수집용 재사용 리스트 (스레드 안전: RimWorld는 단일 스레드)
        private static readonly List<VerbEntry> _collected = new List<VerbEntry>(16);

        static void Postfix(VerbTracker __instance, ref List<Verb> ___verbs, IVerbOwner ___directOwner)
        {
            try
            {
                if (__instance == null || ___verbs == null)
                {
                    ShapeshiftDiagnostics.Info($"InitVerbsFromZero Postfix: early return — instance={__instance != null}, verbs={___verbs != null}");
                    return;
                }

                var owner = ___directOwner;
                if (owner == null) return;

                // 1) 네이티브(pawn) 트래커만 처리. 장비/헤디프 트래커는 스킵.
                //    (중요: 장비 트래커를 건드리면 무기 Verb까지 지워질 수 있음)
                if (owner.ImplementOwnerTypeDef != ImplementOwnerTypeDefOf.NativeVerb) return;

                var pawn = owner.ConstantCaster as Pawn;
                if (pawn == null) return;

                // Pawn 직접 소유 트래커만 처리. ShapeshiftVerbOwner 등 다른 NativeVerb 소유자는 스킵.
                if (!(owner is Pawn)) return;

                if (!ShapeshiftRegistry.TryGet(pawn, out var comp, out var form))
                {
                    ShapeshiftDiagnostics.Info($"InitVerbsFromZero Postfix: pawn={pawn.LabelShort}, no active form");
                    return;
                }

                // 2) Replace/Tools 정보 취득
                bool replaceNative = form.replaceNativeTools.HasValue && form.replaceNativeTools.Value;
                var tools = form.tools;

                // 3) 단일 패스로 주입 대상을 수집 (Verb 객체 생성 전에 개수 확정)
                _collected.Clear();
                if (tools != null && tools.Count > 0)
                {
                    var cache = ManeuversByCapacity;
                    for (int ti = 0; ti < tools.Count; ti++)
                    {
                        var tool = tools[ti]; if (tool == null) continue;
                        var caps = tool.capacities; if (caps == null || caps.Count == 0) continue;

                        for (int ci = 0; ci < caps.Count; ci++)
                        {
                            var cap = caps[ci]; if (cap == null) continue;
                            if (!cache.TryGetValue(cap, out var maneuvers)) continue;

                            for (int mi = 0; mi < maneuvers.Count; mi++)
                            {
                                var man = maneuvers[mi];
                                if (man == null) continue;
                                var vp = man.verb; if (vp == null) continue;
                                if (!vp.IsMeleeAttack) continue;

                                _collected.Add(new VerbEntry { tool = tool, maneuver = man, verbProps = vp });
                            }
                        }
                    }
                }

                int willAdd = _collected.Count;
                ShapeshiftDiagnostics.Info($"InitVerbsFromZero Postfix: pawn={pawn.LabelShort}, form={form.defName}, replaceNative={replaceNative}, willAdd={willAdd}, verbsBefore={___verbs.Count}");

                // 4) Replace더라도 willAdd==0이면 제거하지 않는다 (안전 가드)
                int removedCount = 0;
                if (replaceNative && willAdd > 0)
                {
                    for (int i = ___verbs.Count - 1; i >= 0; i--)
                    {
                        var v = ___verbs[i];
                        if (v != null && v.verbProps != null && v.verbProps.IsMeleeAttack)
                        {
                            if (ShapeshiftDiagnostics.DebugLog)
                                ShapeshiftDiagnostics.Info($"  Removing native melee: {v.GetType().Name} tool={((v as Verb_MeleeAttack)?.tool?.label ?? "null")}");
                            ___verbs.RemoveAt(i);
                            removedCount++;
                        }
                    }
                }

                // 5) 수집된 항목으로 Verb_MeleeAttack 생성 및 주입
                int addedCount = 0;
                for (int i = 0; i < _collected.Count; i++)
                {
                    var entry = _collected[i];
                    var verb = CreateVerb(__instance, owner, entry.verbProps, pawn);
                    if (verb == null) continue;

                    var vma = verb as Verb_MeleeAttack;
                    if (vma != null)
                    {
                        vma.tool = entry.tool;
                        vma.maneuver = entry.maneuver;
                    }

                    // 바닐라 규칙에 맞는 loadID 부여
                    verb.loadID = Verb.CalculateUniqueLoadID(owner, ___verbs.Count);
                    ___verbs.Add(verb);
                    addedCount++;
                }

                _collected.Clear();

                ShapeshiftDiagnostics.Info($"  Result: removed={removedCount}, added={addedCount}, verbsAfter={___verbs.Count}");
            }
            catch (Exception e)
            {
                Log.Error("[SSF] InitVerbsFromZero Postfix failed: " + e);
            }
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