// ShapeshifterFramework | Utilities | ShapeshiftApplyHediffUtility.cs
// 목적 : 변신 시 폼에 정의된 헤디프(Hediff)와 인공 신체(AddedPart)를 폰에게 안전하게 부여하고 관리하는 전담 유틸리티.
// 용도 : 대상 부위(전신, 특정 파츠, 그룹)를 탐색하여 헤디프를 추가하며, 인공 장기의 경우 정책(StrictFleshOnly, RegrowFleshOnly, ForceAdd)에 따라 기존 결손/인공 부위와의 충돌을 정밀하게 제어함.
// 주의 : 원상 복구를 위해 추가/교체된 파츠의 원래 상태(WasMissingBefore, PreExistingAdded)를 ShapeshiftPartRestoreRecord에 기록하며, 헤디프 삭제 시 불필요한 캐시 갱신(DirtyCache)이 발생하지 않도록 최적화됨.

using System.Collections.Generic;
using Verse;

namespace ShapeshifterFramework.Utilities
{
    public static class ShapeshiftApplyHediffUtility
    {
        private static readonly List<BodyPartRecord> EmptyParts = new List<BodyPartRecord>(0);

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

            // prevDefCache가 null이면 정리 스킵
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
                            if (partMissing || childMissing) { try { pawn.health.RestorePart(part); } catch (System.Exception ex) { Log.Warning($"[SSF] RestorePart failed (RegrowFleshOnly): {part.Label} — {ex.Message}"); } }
                            break;

                        case AddedPartPolicy.ForceAdd:
                            if (partMissing || childMissing) { try { pawn.health.RestorePart(part); } catch (System.Exception ex) { Log.Warning($"[SSF] RestorePart failed (ForceAdd): {part.Label} — {ex.Message}"); } }
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
                    try { existing.Severity = opt.severity.Value; } catch (System.Exception ex) { Log.Warning($"[SSF] Set severity failed (existing): {opt.hediff.defName} — {ex.Message}"); }
                }
                // 기존 hediff는 폼이 생성하지 않았으므로 추적하지 않음 — 해제 시 건드리지 않음
                ShapeshiftDiagnostics.Info($"Update existing (not tracked): {opt.hediff.defName} {(part?.Label ?? "FullBody")}");
                return false;
            }

            Hediff created = pawn.health.AddHediff(opt.hediff, part, null);
            if (created != null)
            {
                if (opt.severity.HasValue)
                {
                    try { created.Severity = opt.severity.Value; } catch (System.Exception ex) { Log.Warning($"[SSF] Set severity failed (created): {opt.hediff.defName} — {ex.Message}"); }
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
                if (h.Part == null) continue;

                // 대상 파츠 자신 또는 하위 파츠의 인공장기도 제거
                bool isTargetOrChild = false;
                BodyPartRecord current = h.Part;
                while (current != null)
                {
                    if (current == part) { isTargetOrChild = true; break; }
                    current = current.parent;
                }
                if (!isTargetOrChild) continue;

                try { pawn.health.RemoveHediff(h); } catch (System.Exception ex) { Log.Warning($"[SSF] RemoveHediff failed: {h.def?.defName ?? "null"} — {ex.Message}"); }
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

        private static List<BodyPartRecord> ResolveTargetParts(Pawn pawn, HediffAddEntry opt)
        {
            if (opt.targetPart != null)
            {
                var all = pawn?.RaceProps?.body?.AllParts;
                if (all != null)
                {
                    var results = new List<BodyPartRecord>(4);
                    for (int i = 0; i < all.Count; i++)
                        if (all[i].def == opt.targetPart) results.Add(all[i]);
                    return results.Count == 0 ? EmptyParts : results;
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
                return set.Count == 0 ? EmptyParts : new List<BodyPartRecord>(set);
            }

            return null;
        }

        // RemoveHediff 대상을 수집하기 위한 재사용 버퍼 — GC 할당 방지
        private static readonly List<Hediff> _cleanupRemoveBuffer = new List<Hediff>(8);

        static void CleanupNullPartHediffs(Pawn pawn, List<HediffDef> prevDefCache)
        {
            var list = pawn.health?.hediffSet?.hediffs;
            if (list == null || list.Count == 0) return;

            bool requiresDirtyCache = false;

            // 1단계: null 엔트리 제거 (RemoveAt은 리스트 내부 조작만 하므로 안전)
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i] == null)
                {
                    list.RemoveAt(i);
                    requiresDirtyCache = true;
                }
            }

            // 2단계: Part가 null인 AddedPart/MissingPart 수집 (순회 중 리스트를 수정하지 않음)
            _cleanupRemoveBuffer.Clear();
            for (int i = 0; i < list.Count; i++)
            {
                var h = list[i];
                if (h == null || h.def == null) continue;
                if ((h.def.addedPartProps != null || h is Hediff_MissingPart) && h.Part == null)
                {
                    if (prevDefCache == null || !prevDefCache.Contains(h.def)) continue;
                    _cleanupRemoveBuffer.Add(h);
                }
            }

            // 3단계: 수집된 hediff를 안전하게 제거
            for (int i = 0; i < _cleanupRemoveBuffer.Count; i++)
            {
                try
                {
                    // RemoveHediff는 내부에서 DirtyCache 호출
                    pawn.health.RemoveHediff(_cleanupRemoveBuffer[i]);
                    ShapeshiftDiagnostics.Info($"Cleanup null-part hediff: {_cleanupRemoveBuffer[i].def?.defName ?? "null"}");
                }
                catch (System.Exception ex) { Log.Warning($"[SSF] RemoveHediff failed (cleanup): {_cleanupRemoveBuffer[i].def?.defName ?? "null"} — {ex.Message}"); }
            }
            _cleanupRemoveBuffer.Clear();

            // RemoveAt 실행 시 수동 캐시 갱신
            if (requiresDirtyCache)
            {
                pawn.health.hediffSet.DirtyCache();
                ShapeshiftDiagnostics.Info("Cleaned up literal null hediffs and dirtied cache.");
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
                if (h == null || h.def == null) continue;
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

            while (current != null)
            {
                for (int i = 0; i < hediffs.Count; i++)
                {
                    if (hediffs[i] == null || hediffs[i].def == null) continue;
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