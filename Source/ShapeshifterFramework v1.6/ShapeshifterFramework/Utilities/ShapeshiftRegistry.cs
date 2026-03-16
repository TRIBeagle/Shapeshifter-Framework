// ShapeshifterFramework | Utilities | ShapeshiftRegistry.cs
// 목적 : 현재 변신 중인 폰(Pawn)을 전역 Dictionary로 관리하여 O(1) 속도로 CompShapeshifter를 조회.
// 용도 : 44개 Harmony 패치에서 매 프레임/틱마다 수행하던 TryGetComp<CompShapeshifter>()(AllComps 선형 탐색)을
//        단일 Dictionary.TryGetValue 호출로 대체. 비변신 폰은 ContainsKey 한 번으로 즉시 스킵.
// 주의 : Register/Unregister는 ApplyForm, RemoveForm, PostLoadInit, PostSpawnSetup, PostDestroy에서만 호출.
//        PostDeSpawn에서는 호출하지 않음 (상단/동면관/포드 진입 시 레지스트리 누락 방지).

using ShapeshifterFramework.Comps;
using ShapeshifterFramework.Hediffs;
using System.Collections.Generic;
using Verse;

namespace ShapeshifterFramework.Utilities
{
    internal static class ShapeshiftRegistry
    {
        // Phase 1+2에서 CompShapeshifter → HediffComp_ShapeshiftCore로 완전 전환 예정
        private static readonly Dictionary<Pawn, CompShapeshifter> _active = new Dictionary<Pawn, CompShapeshifter>(32);
        private static readonly Dictionary<Pawn, HediffComp_ShapeshiftCore> _activeCore = new Dictionary<Pawn, HediffComp_ShapeshiftCore>(32);

        /// <summary>변신 중인 폰을 레지스트리에 등록 (기존 CompShapeshifter).</summary>
        internal static void Register(Pawn pawn, CompShapeshifter comp)
        {
            if (pawn != null && comp != null)
                _active[pawn] = comp;
        }

        /// <summary>변신 중인 폰을 레지스트리에 등록 (HediffComp_ShapeshiftCore).</summary>
        internal static void Register(Pawn pawn, HediffComp_ShapeshiftCore comp)
        {
            if (pawn != null && comp != null)
                _activeCore[pawn] = comp;
        }

        /// <summary>폰을 레지스트리에서 제거.</summary>
        internal static void Unregister(Pawn pawn)
        {
            if (pawn != null)
            {
                _active.Remove(pawn);
                _activeCore.Remove(pawn);
            }
        }

        /// <summary>핵심 조회 — comp와 form을 동시에 반환. O(1). (기존 CompShapeshifter)</summary>
        internal static bool TryGet(Pawn pawn, out CompShapeshifter comp, out ShapeshiftFormDef form)
        {
            if (pawn != null && _active.TryGetValue(pawn, out comp))
            {
                form = comp.currentForm;
                if (form != null) return true;
            }
            comp = null;
            form = null;
            return false;
        }

        /// <summary>핵심 조회 — HediffComp_ShapeshiftCore와 form을 동시에 반환. O(1).</summary>
        internal static bool TryGet(Pawn pawn, out HediffComp_ShapeshiftCore comp, out ShapeshiftFormDef form)
        {
            if (pawn != null && _activeCore.TryGetValue(pawn, out comp))
            {
                form = comp.currentForm;
                if (form != null) return true;
            }
            comp = null;
            form = null;
            return false;
        }

        /// <summary>변신 중인지 여부만 판정. O(1).</summary>
        internal static bool IsActive(Pawn pawn)
        {
            return pawn != null && (_active.ContainsKey(pawn) || _activeCore.ContainsKey(pawn));
        }

        /// <summary>활성 변신 폰이 하나라도 있는지 O(1) 확인.</summary>
        internal static bool HasAny()
        {
            return _active.Count > 0 || _activeCore.Count > 0;
        }

        /// <summary>활성 딕셔너리 직접 참조 (기존). Phase 1+2에서 ActiveCoreDict로 전환 예정.</summary>
        internal static Dictionary<Pawn, CompShapeshifter> ActiveDict => _active;

        /// <summary>활성 딕셔너리 직접 참조 (HediffComp).</summary>
        internal static Dictionary<Pawn, HediffComp_ShapeshiftCore> ActiveCoreDict => _activeCore;

        /// <summary>게임 리셋/맵 전환 시 전체 초기화.</summary>
        internal static void Clear()
        {
            _active.Clear();
            _activeCore.Clear();
        }
    }
}
