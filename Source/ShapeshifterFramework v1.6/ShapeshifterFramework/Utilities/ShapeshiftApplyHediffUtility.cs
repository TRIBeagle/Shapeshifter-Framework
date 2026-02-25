// .NET 4.8 / C# 7.3
using System.Collections.Generic;
using Verse;

namespace ShapeshifterFramework.Utilities
{
    public static class ShapeshiftApplyHediffUtility
    {
        public static bool DebugLog = true; // 디버그 토글

        public static void ApplyHediffEntries(
            Pawn pawn,
            List<HediffAddEntry> entries,
            List<Hediff> outTempAddedHediffs,
            List<HediffDef> outTempAddedHediffsDefCache,
            List<ShapeshiftPartRestoreRecord> outPartRestoreRecords)
        {
            if (pawn == null || pawn.health == null) return;
            if (entries == null || entries.Count == 0) return;

            CleanupNullPartHediffs(pawn);

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
                    if (DebugLog) Log.Message($"[SSF] Skip addedPart on FullBody: {opt.hediff.defName}");
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

            if (DebugLog) Log.Message($"SSF Apply: +{applied} hediff(s)");
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
                    // 레코드를 미리 리스트에 넣지 않고, 일단 변수만 생성
                    var record = new ShapeshiftPartRestoreRecord
                    {
                        Part = part,
                        WasMissingBefore = partMissing,
                        PreExistingAdded = CollectExistingAddedParts(pawn, part)
                    };

                    CheckChildIssues(pawn, part, out bool childMissing, out bool childArtificial);

                    if (HasArtificialParentPart(pawn, part))
                    {
                        if (DebugLog) Log.Message($"[SSF] Skip addedPart (Parent is Artificial): {opt.hediff.defName} @ {part.Label}");
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
                            RemoveExistingAddedParts(pawn, part, CollectExistingAddedParts(pawn, part));
                            break;
                    }

                    // 모든 정책 검사를 통과하고 "실제로 변신이 확정"된 순간에만 복원 레코드를 추가
                    if (outPartRestoreRecords != null) outPartRestoreRecords.Add(record);
                }
                else
                {
                    // 비대체형은 결손 파츠에 부착 불가
                    if (partMissing)
                    {
                        if (DebugLog) Log.Message($"[SSF] Skip non-added on missing part: {opt.hediff.defName} @ {part.Label}");
                        return false;
                    }
                }
            }
            else
            {
                // 전신인데 AddedPart면 불가
                if (isAddedPart)
                {
                    if (DebugLog) Log.Message($"[SSF] Skip addedPart on FullBody: {opt.hediff.defName}");
                    return false;
                }
            }

            // 중복 검사
            Hediff existing = FindExisting(pawn, opt.hediff, part);
            if (existing != null)
            {
                if (opt.severity.HasValue)
                {
                    try { existing.Severity = opt.severity.Value; } catch { }
                }
                if (DebugLog) Log.Message($"[SSF] Update existing: {opt.hediff.defName} {(part?.Label ?? "FullBody")}");
                return false;
            }

            // 추가
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

        // 변신 전 해당 파츠 및 모든 하위 파츠에 있던 AddedPart 목록 수집(복원용)
        static List<ShapeshiftPartRestoreRecord.PreExistingAddedEntry> CollectExistingAddedParts(Pawn pawn, BodyPartRecord rootPart)
        {
            var hediffs = pawn.health.hediffSet.hediffs;
            List<ShapeshiftPartRestoreRecord.PreExistingAddedEntry> results = null;

            for (int i = 0; i < hediffs.Count; i++)
            {
                Hediff h = hediffs[i];
                if (h?.def?.addedPartProps == null || h.Part == null) continue;

                // 해당 Hediff가 붙은 부위가 rootPart(예: 팔)이거나 그 하위 부위인지 확인
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

        // 기존 AddedPart 제거(교체 설치 시)
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
                if (part == null) return h;      // 전신
                if (h.Part == part) return h;    // 동일 파츠
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
                    return (results.Count == 0) ? EmptyParts : results;
                }
                return EmptyParts;
            }

            if (opt.targetGroups != null && opt.targetGroups.Count > 0)
            {
                var all = pawn?.RaceProps?.body?.AllParts;
                if (all == null || all.Count == 0) return EmptyParts;

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
                return (set.Count == 0) ? EmptyParts : new List<BodyPartRecord>(set);
            }

            return null; // 전신
        }

        // 방어적 정리: null-Part hediff 제거
        static void CleanupNullPartHediffs(Pawn pawn)
        {
            var list = pawn.health?.hediffSet?.hediffs;
            if (list == null || list.Count == 0) return;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                var h = list[i];
                if (h == null) { list.RemoveAt(i); continue; }
                if ((h.def?.addedPartProps != null || h is Hediff_MissingPart) && h.Part == null)
                {
                    try
                    {
                        pawn.health.RemoveHediff(h);
                        if (DebugLog) Log.Message($"[SSF] Cleanup null-part hediff: {h.def?.defName ?? "null"}");
                    }
                    catch { }
                }
            }
        }

        // 타겟 파츠의 하위 파츠(손, 손가락 등)에 결손이나 인공장기가 있는지 각각 분리해서 딥스캔
        static void CheckChildIssues(Pawn pawn, BodyPartRecord rootPart, out bool hasMissing, out bool hasArtificial)
        {
            hasMissing = false;
            hasArtificial = false;
            if (pawn?.health?.hediffSet == null || rootPart == null) return;

            var hediffs = pawn.health.hediffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                var h = hediffs[i];
                if (h.Part == null || h.Part == rootPart) continue; // 본체는 제외

                bool isMissing = h is Hediff_MissingPart;
                bool isArtificial = h.def.addedPartProps != null;

                if (isMissing || isArtificial)
                {
                    // 이 Hediff가 붙은 부위가 rootPart(예: 팔)의 하위 부위인지 역추적
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

        // 타겟 파츠의 상위 파츠에 인공장기(기계팔 등)가 있는지 스캔하되, '몸통(Torso)' 같은 최상위 루트 노드는 제외
        static bool HasArtificialParentPart(Pawn pawn, BodyPartRecord part)
        {
            if (pawn?.health?.hediffSet == null || part == null) return false;

            var hediffs = pawn.health.hediffSet.hediffs;
            BodyPartRecord current = part.parent;

            // 현재 부위의 부모가 없다면(즉, 몸통이라면) 반복문을 즉시 중지
            while (current != null && current.parent != null)
            {
                for (int i = 0; i < hediffs.Count; i++)
                {
                    // 상위 부위(어깨 등)에 기계 부품이 발견되면 true 반환
                    if (hediffs[i].Part == current && hediffs[i].def.addedPartProps != null)
                    {
                        return true;
                    }
                }
                current = current.parent; // 위로 한 칸 이동
            }
            return false;
        }

        static readonly List<BodyPartRecord> EmptyParts = new List<BodyPartRecord>(0);
    }
}
