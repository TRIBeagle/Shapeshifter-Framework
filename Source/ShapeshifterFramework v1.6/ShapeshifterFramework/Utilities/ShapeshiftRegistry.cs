// ShapeshifterFramework | Utilities | ShapeshiftRegistry.cs
// 목적 : 현재 변신 중인 폰(Pawn)을 전역 Dictionary로 관리하여 O(1) 속도로 HediffComp_ShapeshiftCore를 조회.
// 용도 : Harmony 패치에서 매 프레임/틱마다 수행하던 hediff 탐색을
//        단일 Dictionary.TryGetValue 호출로 대체. 비변신 폰은 ContainsKey 한 번으로 즉시 스킵.
// 주의 : Register/Unregister는 ApplyForm, RemoveForm, PostLoadInit, PostSpawnSetup, PostDestroy에서만 호출.
//        PostDeSpawn에서는 호출하지 않음 (상단/동면관/포드 진입 시 레지스트리 누락 방지).
//        FinalizeInit 중 ClearAll → 재등록 사이에 _reinitializing 가드로 hediff 기반 폴백 조회 제공.

using ShapeshifterFramework.Hediffs;
using System.Collections.Generic;
using Verse;

namespace ShapeshifterFramework.Utilities
{
    internal static class ShapeshiftRegistry
    {
        // HediffComp_ShapeshiftCore 기반 전역 레지스트리
        private static readonly Dictionary<Pawn, HediffComp_ShapeshiftCore> _active = new Dictionary<Pawn, HediffComp_ShapeshiftCore>(32);

        // FinalizeInit 중 ClearAll → 재등록 사이 타이밍 이슈 방어
        private static bool _reinitializing;

        /// <summary>변신 중인 폰을 레지스트리에 등록 (HediffComp_ShapeshiftCore).</summary>
        internal static void Register(Pawn pawn, HediffComp_ShapeshiftCore comp)
        {
            if (pawn != null && comp != null)
                _active[pawn] = comp;
        }

        /// <summary>폰을 레지스트리에서 제거.</summary>
        internal static void Unregister(Pawn pawn)
        {
            if (pawn != null)
            {
                _active.Remove(pawn);
            }
        }

        /// <summary>핵심 조회 — HediffComp_ShapeshiftCore와 form을 동시에 반환. O(1).</summary>
        internal static bool TryGet(Pawn pawn, out HediffComp_ShapeshiftCore comp, out ShapeshiftFormDef form)
        {
            if (pawn != null && _active.TryGetValue(pawn, out comp))
            {
                form = comp.currentForm;
                if (form != null) return true;
            }

            // FinalizeInit 중 레지스트리가 비어 있을 때 hediff 기반 폴백
            if (_reinitializing && pawn != null)
                return TryGetFallback(pawn, out comp, out form);

            comp = null;
            form = null;
            return false;
        }

        /// <summary>변신 중인지 여부만 판정. O(1).</summary>
        internal static bool IsActive(Pawn pawn)
        {
            if (pawn != null && _active.ContainsKey(pawn))
                return true;

            // FinalizeInit 중 hediff 기반 폴백
            if (_reinitializing && pawn != null)
                return TryGetFallback(pawn, out _, out _);

            return false;
        }

        /// <summary>FinalizeInit 재초기화 시작 — 레지스트리 비어있을 때 hediff 폴백 활성화.</summary>
        internal static void BeginReInit() { _reinitializing = true; }

        /// <summary>FinalizeInit 재초기화 완료 — hediff 폴백 비활성화.</summary>
        internal static void EndReInit() { _reinitializing = false; }

        /// <summary>hediff 리스트 직접 탐색 폴백. FinalizeInit 중에만 사용.</summary>
        private static bool TryGetFallback(Pawn pawn, out HediffComp_ShapeshiftCore comp, out ShapeshiftFormDef form)
        {
            comp = null;
            form = null;
            var hediffs = pawn.health?.hediffSet?.hediffs;
            if (hediffs == null) return false;
            for (int i = 0; i < hediffs.Count; i++)
            {
                if (hediffs[i] == null) continue;
                var comps = hediffs[i].comps;
                if (comps == null) continue;
                for (int c = 0; c < comps.Count; c++)
                {
                    if (comps[c] is HediffComp_ShapeshiftCore core && core.currentForm != null)
                    {
                        comp = core;
                        form = core.currentForm;
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>활성 변신 폰이 하나라도 있는지 O(1) 확인.</summary>
        internal static bool HasAny()
        {
            return _active.Count > 0;
        }

        /// <summary>활성 딕셔너리 직접 참조 (HediffComp_ShapeshiftCore). 순회 중 수정이 없는 경우만 사용.</summary>
        internal static Dictionary<Pawn, HediffComp_ShapeshiftCore> ActiveDict => _active;

        // 순회 중 수정이 발생할 수 있는 경우를 위한 스냅샷 리스트
        // _snapshotInUse 플래그로 중첩 호출 시 새 리스트 할당
        private static readonly List<KeyValuePair<Pawn, HediffComp_ShapeshiftCore>> _snapshot
            = new List<KeyValuePair<Pawn, HediffComp_ShapeshiftCore>>(32);
        private static bool _snapshotInUse;

        /// <summary>활성 딕셔너리 스냅샷 반환. 순회 중 Register/Unregister 안전.</summary>
        /// <remarks>중첩 호출 감지 시 새 리스트를 할당하여 데이터 손상을 방지.</remarks>
        internal static List<KeyValuePair<Pawn, HediffComp_ShapeshiftCore>> GetSnapshot()
        {
            // 이미 스냅샷 순회 중이면 새 리스트를 반환하여 데이터 손상 방지
            if (_snapshotInUse)
            {
                var copy = new List<KeyValuePair<Pawn, HediffComp_ShapeshiftCore>>(_active.Count);
                foreach (var kv in _active)
                    copy.Add(kv);
                return copy;
            }

            _snapshotInUse = true;
            _snapshot.Clear();
            foreach (var kv in _active)
                _snapshot.Add(kv);
            return _snapshot;
        }

        /// <summary>스냅샷 순회 완료 후 호출 — 재사용 리스트 반환 허용.</summary>
        internal static void ReleaseSnapshot() { _snapshotInUse = false; }

        /// <summary>게임 리셋/맵 전환 시 전체 초기화.</summary>
        internal static void Clear()
        {
            _active.Clear();
        }
    }
}
