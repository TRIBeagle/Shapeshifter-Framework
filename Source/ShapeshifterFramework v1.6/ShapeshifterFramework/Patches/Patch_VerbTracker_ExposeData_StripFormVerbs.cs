// ShapeshifterFramework | Patches | Patch_VerbTracker_ExposeData_StripFormVerbs.cs
// 목적 : 세이브 시 pawn.verbTracker에 주입된 폼 전용 근접 verb를 임시 제거하여
//        로드 시 "Replaced verb" 경고를 방지.
// 용도 : InitVerbsFromZero 패치가 폼의 tools(Smash/Bite 등)를 pawn.verbTracker에 주입하는데,
//        이 verb들은 Human ThingDef에 정의되어 있지 않아 로드 시 VerbTracker.ExposeData가
//        verb ID 불일치를 감지하고 "Replaced verb" 경고를 출력함.
//        Prefix에서 세이브 직전에 제거하고, Postfix에서 세이브 직후 복원하여 경고를 원천 차단.
// 주의 : 세이브 전용. 로드/ResolvingCrossRefs 시에는 아무 작업 안 함.

using HarmonyLib;
using RimWorld;
using ShapeshifterFramework.Utilities;
using System.Collections.Generic;
using Verse;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch(typeof(VerbTracker), "ExposeData")]
    internal static class Patch_VerbTracker_ExposeData_StripFormVerbs
    {
        // 임시 보관용 — 단일 스레드이므로 static 안전
        private static readonly List<Verb> _strippedVerbs = new List<Verb>(8);

        [HarmonyPrefix, HarmonyPriority(Priority.High)]
        static void Prefix(ref List<Verb> ___verbs, IVerbOwner ___directOwner)
        {
            if (Scribe.mode != LoadSaveMode.Saving) return;
            if (___verbs == null || ___verbs.Count == 0) return;

            Pawn pawn = ___directOwner as Pawn;
            if (pawn == null)
            {
                ShapeshiftDiagnostics.Info("[StripFormVerbs] directOwner is not Pawn, skipping");
                return;
            }

            if (!ShapeshiftRegistry.TryGet(pawn, out var comp, out var form))
            {
                ShapeshiftDiagnostics.Info($"[StripFormVerbs] pawn={pawn.LabelShort} not in registry, skipping");
                return;
            }

            var tools = form.tools;
            if (tools == null || tools.Count == 0)
            {
                ShapeshiftDiagnostics.Info($"[StripFormVerbs] pawn={pawn.LabelShort} form={form.defName} has no tools, skipping");
                return;
            }

            _strippedVerbs.Clear();

            // 폼 tool과 일치하는 verb를 제거하여 세이브에서 배제
            // ReferenceEquals 우선, 실패 시 tool.label 비교로 폴백
            for (int i = ___verbs.Count - 1; i >= 0; i--)
            {
                var v = ___verbs[i];
                if (v == null) continue;

                var vma = v as Verb_MeleeAttack;
                if (vma == null || vma.tool == null) continue;

                bool matched = false;
                for (int j = 0; j < tools.Count; j++)
                {
                    if (tools[j] == null) continue;

                    if (ReferenceEquals(vma.tool, tools[j])
                        || vma.tool.label == tools[j].label)
                    {
                        matched = true;
                        break;
                    }
                }

                if (matched)
                {
                    ShapeshiftDiagnostics.Info($"[StripFormVerbs] stripping verb: {v.loadID} tool={vma.tool.label}");
                    _strippedVerbs.Add(v);
                    ___verbs.RemoveAt(i);
                }
            }

            ShapeshiftDiagnostics.Info($"[StripFormVerbs] pawn={pawn.LabelShort} form={form.defName}, stripped={_strippedVerbs.Count}, remaining={___verbs.Count}");
        }

        /// <summary>Finalizer로 예외 발생 시에도 제거된 verb를 반드시 복원.</summary>
        [HarmonyFinalizer, HarmonyPriority(Priority.High)]
        static void Finalizer(ref List<Verb> ___verbs)
        {
            if (_strippedVerbs.Count == 0) return;

            // 세이브 완료(또는 예외) 후 제거한 verb 복원
            if (___verbs == null)
                ___verbs = new List<Verb>();

            for (int i = 0; i < _strippedVerbs.Count; i++)
                ___verbs.Add(_strippedVerbs[i]);

            _strippedVerbs.Clear();
        }
    }
}
