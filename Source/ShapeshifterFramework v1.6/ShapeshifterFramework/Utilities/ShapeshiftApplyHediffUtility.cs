// .NET 4.8 / C# 7.3
using System.Collections.Generic;
using Verse;

namespace ShapeshifterFramework.Utilities
{
    public static class ShapeshiftApplyHediffUtility
    {
        public static void ApplyHediffEntries(
            Pawn pawn,
            List<HediffAddEntry> entries,
            List<Hediff> outTempAddedHediffs,
            List<HediffDef> outTempAddedHediffsDefCache,
            List<ShapeshiftPartRestoreRecord> outPartRestoreRecords,
            List<HediffDef> prevDefCache = null)
        {
            if (pawn == null || pawn.health == null) return;
            if (entries == null || entries.Count == 0) return;

            // prevDefCache: 이전 변신에서 우리가 추가한 헤디프 목록
            // null이면 우리가 추가한 것을 알 수 없으므로 정리 스킵
            CleanupNullPartHediffs(pawn, prevDefCache);

            int applied = 0;

            for (int i = 0; i < entries.Count; i++)
            {
                var opt = entries[i];
                if (opt == null || opt.hediff == null) continue;

                bool isAddedPart = (opt.hediff.addedPartProps != null);
                var targets = ResolveTargetParts(pawn, opt); // null => 전신

                // 전신(null)에 AddedPart는 불가
                if (isAddedPart && (targets == null || targets.Count == 0))
                {
                    ShapeshiftDiagnostics.Info($"Skip addedPart on FullBody: {opt.hediff.defName}");
                    continue;
                }

                if (targets == null || targets.Count == 0)
                {
                    if (TryAddOrUpdateSingle(pawn, opt, null, outTempAddedHediffs, outTempAddedHediffsDefCache, outPartRestoreRecords))
                        applied++;
                }
                else
                {
                    for (int t = 0; t < targets.Count; t++)
                    {
                        if (TryAddOrUpdateSingle(pawn, opt, targets[t], outTempAddedHediffs, outTempAddedHediffsDefCache, outPartRestoreRecords))
                            applied++;
                    }
                }
            }

            ShapeshiftDiagnostics.Info($"Apply: +{applied} hediff(s)");
        }

        static bool TryAddOrUpdateSingle(
            Pawn pawn,
            HediffAddEntry opt,
            BodyPartRecord part,
            List<Hediff> outTempAddedHediffs,
            List<HediffDef> outTempAddedHediffsDefCache,
            List<ShapeshiftPartRestoreRecord> outPartRestoreRecords)
        {
            bool isAddedPart = (opt.hediff.addedPartProps != null);

            if (part != null)
            {
                bool partMissing = pawn.health.hediffSet.PartIsMissing(part);

                if (isAddedPart)
                {
                    var record = new ShapeshiftPartRestoreRecord
                    {
                        Part = part,
                        WasMissingBefore = partMissing,
                        PreExistingAdded = CollectExistingAddedParts(pawn, part)
                    };

                    CheckChildIssues(pawn, part, out bool childMissing, out bool childArtificial);

                    if (HasArtificialParentPart(pawn, part))
                    {
                        ShapeshiftDiagnostics.Info($"Skip addedPart (Parent is Artificial): {opt.hediff.defName} @ {part.Label}");
                        return false;
                    }

                    switch (opt.addedPartPolicy)
                    {
                        case AddedPartPolicy.StrictFleshOnly:
                            if (partMissing || childMissing) return false;
                            if ((record.PreExistingAdded != null && record.PreExistingAdded.Count > 0) || childArtificial) return false;
                            break;

                        case AddedPartPolicy.RegrowFleshOnly:
                            if ((record.PreExistingAdded != null && record.PreExistingAdded.Count > 0) || childArtificial) return false;
                            if (partMissing || childMissing) { try { pawn.health.RestorePart(part); } catch { } }
                            break;

                        case AddedPartPolicy.ForceAdd:
                            if (partMissing || childMissing) { try { pawn.health.RestorePart(part); } catch { } }
                            RemoveExistingAddedParts(pawn, part, record.PreExistingAdded);
                            break;
                    }

                    if (outPartRestoreRecords != null) outPartRestoreRecords.Add(record);
                }
                else
                {
                    if (partMissing)
                    {
                        ShapeshiftDiagnostics.Info($"Skip non-added on missing part: {opt.hediff.defName} @ {part.Label}");
                        return false;
                    }
                }
            }
            else
            {
                if (isAddedPart)
                {
                    ShapeshiftDiagnostics.Info($"Skip addedPart on FullBody: {opt.hediff.defName}");
                    return false;
                }
            }

            Hediff existing = FindExisting(pawn, opt.hediff, part);
            if (existing != null)
            {
                if (opt.severity.HasValue)
                {
                    try { existing.Severity = opt.severity.Value; } catch { }
                }
                ShapeshiftDiagnostics.Info($"Update existing: {opt.hediff.defName} {(part?.Label ?? "FullBody")}");
                return false;
            }

            Hediff created = pawn.health.AddHediff(opt.hediff, part, null);
            if (created != null)
            {
                if (opt.severity.HasValue)
                {
                    try { created.Severity = opt.severity.Value; } catch { }
                }
                if (outTempAddedHediffs != null) outTempAddedHediffs.Add(created);
                if (outTempAddedHediffsDefCache != null) outTempAddedHediffsDefCache.Add(opt.hediff);
                return true;
            }
            return false;
        }

        static List<ShapeshiftPartRestoreRecord.PreExistingAddedEntry> CollectExistingAddedParts(Pawn pawn, BodyPartRecord rootPart)
        {
            var hediffs = pawn.health.hediffSet.hediffs;
            List<ShapeshiftPartRestoreRecord.PreExistingAddedEntry> results = null;

            for (int i = 0; i < hediffs.Count; i++)
            {
                Hediff h = hediffs[i];
                if (h?.def?.addedPartProps == null || h.Part == null) continue;

                BodyPartRecord current = h.Part;
                bool isTargetOrChild = false;
                while (current != null)
                {
                    if (current == rootPart) { isTargetOrChild = true; break; }
                    current = current.parent;
                }

                if (isTargetOrChild)
                {
                    if (results == null) results = new List<ShapeshiftPartRestoreRecord.PreExistingAddedEntry>();
                    results.Add(new ShapeshiftPartRestoreRecord.PreExistingAddedEntry
                    {
                        Def = h.def,
                        Severity = (h is HediffWithComps) ? (float?)h.Severity : null,
                        PartDef = h.Part.def
                    });
                }
            }
            return results;
        }

        static void RemoveExistingAddedParts(Pawn pawn, BodyPartRecord part, List<ShapeshiftPartRestoreRecord.PreExistingAddedEntry> cache)
        {
            if (cache == null || cache.Count == 0) return;
            var list = pawn.health.hediffSet.hediffs;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                Hediff h = list[i];
                if (h?.def?.addedPartProps == null) continue;
                if (h.Part != part) continue;
                try { pawn.health.RemoveHediff(h); } catch { }
            }
        }

        static Hediff FindExisting(Pawn pawn, HediffDef def, BodyPartRecord part)
        {
            if (pawn == null || pawn.health == null || def == null) return null;
            List<Hediff> list = pawn.health.hediffSet.hediffs;
            for (int i = 0; i < list.Count; i++)
            {
                var h = list[i];
                if (h == null || h.def != def) continue;
                if (part == null) return h;
                if (h.Part == part) return h;
            }
            return null;
        }

        public static List<BodyPartRecord> ResolveTargetParts(Pawn pawn, HediffAddEntry opt)
        {
            if (opt.targetPart != null)
            {
                var all = pawn?.RaceProps?.body?.AllParts;
                if (all != null)
                {
                    var results = new List<BodyPartRecord>(4);
                    for (int i = 0; i < all.Count; i++)
                        if (all[i].def == opt.targetPart) results.Add(all[i]);
                    return (results.Count == 0) ? new List<BodyPartRecord>() : results;
                }
                return new List<BodyPartRecord>();
            }

            if (opt.targetGroups != null && opt.targetGroups.Count > 0)
            {
                var all = pawn?.RaceProps?.body?.AllParts;
                if (all == null || all.Count == 0) return new List<BodyPartRecord>();

                var set = new HashSet<BodyPartRecord>();
                for (int i = 0; i < all.Count; i++)
                {
                    var p = all[i];
                    var groups = p?.groups;
                    if (groups == null || groups.Count == 0) continue;

                    for (int g = 0; g < groups.Count; g++)
                    {
                        if (opt.targetGroups.Contains(groups[g]))
                        {
                            set.Add(p);
                            break;
                        }
                    }
                }
                return (set.Count == 0) ? new List<BodyPartRecord>() : new List<BodyPartRecord>(set);
            }

            return null;
        }

        static void CleanupNullPartHediffs(Pawn pawn, List<HediffDef> prevDefCache)
        {
            var list = pawn.health?.hediffSet?.hediffs;
            if (list == null || list.Count == 0) return;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                var h = list[i];
                if (h == null) { list.RemoveAt(i); continue; }
                if ((h.def?.addedPartProps != null || h is Hediff_MissingPart) && h.Part == null)
                {
                    // prevDefCache가 null이면 우리가 추가한 것인지 알 수 없으므로 스킵
                    // prevDefCache가 있으면 우리가 추가한 것으로 알려진 것만 제거
                    if (prevDefCache == null || !prevDefCache.Contains(h.def)) continue;
                    try
                    {
                        pawn.health.RemoveHediff(h);
                        ShapeshiftDiagnostics.Info($"Cleanup null-part hediff: {h.def?.defName ?? "null"}");
                    }
                    catch { }
                }
            }
        }

        static void CheckChildIssues(Pawn pawn, BodyPartRecord rootPart, out bool hasMissing, out bool hasArtificial)
        {
            hasMissing = false;
            hasArtificial = false;
            if (pawn?.health?.hediffSet == null || rootPart == null) return;

            var hediffs = pawn.health.hediffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                var h = hediffs[i];
                if (h.Part == null || h.Part == rootPart) continue;

                bool isMissing = h is Hediff_MissingPart;
                bool isArtificial = h.def.addedPartProps != null;

                if (isMissing || isArtificial)
                {
                    BodyPartRecord current = h.Part.parent;
                    while (current != null)
                    {
                        if (current == rootPart)
                        {
                            if (isMissing) hasMissing = true;
                            if (isArtificial) hasArtificial = true;
                            break;
                        }
                        current = current.parent;
                    }
                }
            }
        }

        static bool HasArtificialParentPart(Pawn pawn, BodyPartRecord part)
        {
            if (pawn?.health?.hediffSet == null || part == null) return false;

            var hediffs = pawn.health.hediffSet.hediffs;
            BodyPartRecord current = part.parent;

            while (current != null && current.parent != null)
            {
                for (int i = 0; i < hediffs.Count; i++)
                {
                    if (hediffs[i].Part == current && hediffs[i].def.addedPartProps != null)
                    {
                        return true;
                    }
                }
                current = current.parent;
            }
            return false;
        }
    }
}