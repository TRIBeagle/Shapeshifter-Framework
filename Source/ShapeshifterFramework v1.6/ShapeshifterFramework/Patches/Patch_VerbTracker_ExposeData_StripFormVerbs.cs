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
            if (pawn == null) return;

            if (!ShapeshiftRegistry.TryGet(pawn, out var comp, out var form)) return;

            var tools = form.tools;
            if (tools == null || tools.Count == 0) return;

            _strippedVerbs.Clear();

            // 폼 tool 참조와 일치하는 verb를 제거하여 세이브에서 배제
            for (int i = ___verbs.Count - 1; i >= 0; i--)
            {
                var v = ___verbs[i];
                if (v == null) continue;

                var vma = v as Verb_MeleeAttack;
                if (vma == null || vma.tool == null) continue;

                for (int j = 0; j < tools.Count; j++)
                {
                    if (ReferenceEquals(vma.tool, tools[j]))
                    {
                        _strippedVerbs.Add(v);
                        ___verbs.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        [HarmonyPostfix, HarmonyPriority(Priority.High)]
        static void Postfix(ref List<Verb> ___verbs, IVerbOwner ___directOwner)
        {
            if (_strippedVerbs.Count == 0) return;

            // 세이브 완료 후 제거한 verb 복원
            if (___verbs == null)
                ___verbs = new List<Verb>();

            for (int i = 0; i < _strippedVerbs.Count; i++)
                ___verbs.Add(_strippedVerbs[i]);

            _strippedVerbs.Clear();
        }
    }
}
