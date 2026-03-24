// ShapeshifterFramework | Hediffs | HediffComp_ShapeshiftCore.Verbs.cs
// 목적 : 변신 폼 전용 IVerbOwner 구현과 VerbTracker 관리, verb 자동공격 토글 유틸.
// 용도 : 폼의 verbs/tools를 전용 VerbTracker로 관리하고, 기즈모용 라벨·설명·아이콘 헬퍼를 제공.

using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ShapeshifterFramework.Hediffs
{
    public partial class HediffComp_ShapeshiftCore
    {
        /// <summary>현재 폼 verbs/tools IVerbOwner 구현.</summary>
        private class ShapeshiftVerbOwner : IVerbOwner
        {
            private readonly HediffComp_ShapeshiftCore comp;
            private static readonly List<VerbProperties> EmptyVerbProperties = new List<VerbProperties>(0);
            private static readonly List<Tool> EmptyTools = new List<Tool>(0);
            public ShapeshiftVerbOwner(HediffComp_ShapeshiftCore c) { comp = c; }

            VerbTracker IVerbOwner.VerbTracker => comp.shapeshiftVerbTracker;

            ImplementOwnerTypeDef IVerbOwner.ImplementOwnerTypeDef => ImplementOwnerTypeDefOf.NativeVerb;

            string IVerbOwner.UniqueVerbOwnerID()
            {
                var p = comp.Pawn;
                return p != null ? "Shapeshift_" + p.ThingID : "Shapeshift_Unknown";
            }

            bool IVerbOwner.VerbsStillUsableBy(Pawn p)
            {
                return comp.isTransformed && comp.Pawn == p;
            }

            Thing IVerbOwner.ConstantCaster => comp.Pawn;

            public List<VerbProperties> VerbProperties
            {
                get
                {
                    var f = comp.currentForm;
                    return (f != null && f.verbs != null) ? f.verbs : EmptyVerbProperties;
                }
            }

            public List<Tool> Tools
            {
                get
                {
                    var f = comp.currentForm;
                    return (f != null && f.tools != null) ? f.tools : EmptyTools;
                }
            }
        }

        /// <summary>현재 폼 전용 VerbTracker. 없으면 null.</summary>
        public VerbTracker ShapeshiftVerbTracker
        {
            get
            {
                if (!isTransformed || currentForm == null) return null;

                bool hasVerbs = currentForm.verbs != null && currentForm.verbs.Count > 0;
                bool hasTools = currentForm.tools != null && currentForm.tools.Count > 0;
                if (!hasVerbs && !hasTools) return null;

                if (shapeshiftVerbTracker == null)
                {
                    shapeshiftVerbTracker = new VerbTracker(new ShapeshiftVerbOwner(this));
                    var pawn = Pawn;
                    if (pawn != null)
                    {
                        try
                        {
                            var verbs = shapeshiftVerbTracker.AllVerbs;
                            for (int i = 0; i < verbs.Count; i++)
                            {
                                var v = verbs[i];
                                if (v != null) v.caster = pawn;
                            }
                        }
                        catch (Exception ex) { Log.Error($"[SSF] VerbTracker init error: {ex}"); }
                    }
                }
                return shapeshiftVerbTracker;
            }
        }

        /// <summary>verb에 대응하는 VerbGizmoOption 검색.</summary>
        private VerbGizmoOption FindGizmoOption(int index, Verb v)
        {
            var opt = currentForm?.verbGizmoOptions;
            if (opt == null || opt.Count == 0) return null;

            string vLabel = v?.verbProps?.label;
            if (!string.IsNullOrEmpty(vLabel))
            {
                for (int i = 0; i < opt.Count; i++)
                {
                    var o = opt[i];
                    if (o != null && string.Equals(o.verbLabel, vLabel, StringComparison.OrdinalIgnoreCase))
                        return o;
                }
            }

            if (index >= 0 && index < opt.Count)
            {
                var o = opt[index];
                if (o != null && string.IsNullOrEmpty(o.verbLabel))
                    return o;
            }

            return null;
        }

        /// <summary>폼 전환 시 빌드되는 verb별 키 캐시. AutoKey O(N²) → O(1) 최적화.</summary>
        private Dictionary<Verb, string> _verbKeyCache;

        /// <summary>verb 키 캐시 초기화. 폼 전환 시 호출.</summary>
        private void BuildVerbKeyCache()
        {
            var vt = shapeshiftVerbTracker;
            if (vt == null) { _verbKeyCache = null; return; }

            var verbs = vt.AllVerbs;
            var f = currentForm?.defName ?? "None";

            // 이름별 총 개수 수집
            var nameCount = new Dictionary<string, int>(verbs.Count);
            for (int i = 0; i < verbs.Count; i++)
            {
                string vName = verbs[i]?.verbProps?.label ?? verbs[i]?.GetType().Name ?? "UnknownVerb";
                if (nameCount.ContainsKey(vName)) nameCount[vName]++;
                else nameCount[vName] = 1;
            }

            // verb → 키 문자열 매핑
            _verbKeyCache = new Dictionary<Verb, string>(verbs.Count);
            var idx = new Dictionary<string, int>(verbs.Count);
            for (int i = 0; i < verbs.Count; i++)
            {
                var v = verbs[i];
                if (v == null) continue;
                string vName = v.verbProps?.label ?? v.GetType().Name ?? "UnknownVerb";
                if (!idx.ContainsKey(vName)) idx[vName] = 0;
                int myIdx = idx[vName]++;
                _verbKeyCache[v] = nameCount[vName] > 1
                    ? f + "#" + vName + "#" + myIdx
                    : f + "#" + vName;
            }
        }

        /// <summary>verb 키 생성. 캐시 우선 조회, 미스 시 폴백.</summary>
        string AutoKey(Verb v)
        {
            // 캐시 히트
            if (_verbKeyCache != null && v != null)
            {
                string key;
                if (_verbKeyCache.TryGetValue(v, out key)) return key;
            }
            // 폴백: 캐시 없을 때 기존 로직 (인덱스 없이 단순 키)
            var f = currentForm?.defName ?? "None";
            string vName = v?.verbProps?.label ?? v?.GetType().Name ?? "UnknownVerb";
            return f + "#" + vName;
        }

        /// <summary>verb 자동공격 활성 여부.</summary>
        public bool IsAutoAttackEnabled(int index, Verb v)
        {
            if (v == null) return true;
            bool val;
            if (verbAutoToggle.TryGetValue(AutoKey(v), out val)) return val;
            return true;
        }

        /// <summary>자동공격 토글 전환 (배타적: ON 시 다른 ranged verb 전부 OFF).</summary>
        public void ToggleAutoAttack(int index, Verb v)
        {
            bool now = IsAutoAttackEnabled(index, v);
            if (now)
            {
                verbAutoToggle[AutoKey(v)] = false;
            }
            else
            {
                var vt = ShapeshiftVerbTracker;
                if (vt != null)
                {
                    var verbs = vt.AllVerbs;
                    for (int i = 0; i < verbs.Count; i++)
                    {
                        var other = verbs[i];
                        if (other == null || other.verbProps == null) continue;
                        if (!other.verbProps.Ranged) continue;
                        verbAutoToggle[AutoKey(other)] = false;
                    }
                }
                verbAutoToggle[AutoKey(v)] = true;
            }
        }

        /// <summary>폼 적용 시 배타적 토글 초기화.</summary>
        private void InitAutoToggleForForm()
        {
            var vt = ShapeshiftVerbTracker;
            BuildVerbKeyCache();
            if (vt == null) return;

            bool toggleEnabled = ShapeshifterFrameworkMod.Settings?.showVerbAutoToggle ?? true;

            bool firstSet = false;
            var verbs = vt.AllVerbs;
            for (int i = 0; i < verbs.Count; i++)
            {
                var v = verbs[i];
                if (v == null || v.verbProps == null) continue;
                if (!v.verbProps.Ranged) continue;

                bool on = toggleEnabled && !firstSet;
                verbAutoToggle[AutoKey(v)] = on;
                if (on) firstSet = true;
            }
        }

        /// <summary>verb 명령 라벨 반환.</summary>
        public string GetVerbLabel(int index, Verb v, bool preferToggleLabel)
        {
            return GetVerbLabel(index, v, preferToggleLabel, FindGizmoOption(index, v));
        }

        /// <summary>verb 명령 라벨 반환 (조회 결과 재사용).</summary>
        private string GetVerbLabel(int index, Verb v, bool preferToggleLabel, VerbGizmoOption o)
        {
            var vp = v?.verbProps;
            if (o != null)
            {
                string s = preferToggleLabel ? (o.toggleLabel ?? o.label) : o.label;
                if (!string.IsNullOrEmpty(s)) return s.Translate().CapitalizeFirst();
            }

            string label = string.IsNullOrEmpty(vp?.label) ? "SSF_Verb_Attack".Translate() : vp.label.Translate();
            return label.CapitalizeFirst();
        }

        /// <summary>verb 명령/토글 설명 반환.</summary>
        public string GetVerbDesc(int index, Verb v, bool forToggle)
        {
            return GetVerbDesc(index, v, forToggle, FindGizmoOption(index, v));
        }

        /// <summary>verb 명령/토글 설명 반환 (조회 결과 재사용).</summary>
        private string GetVerbDesc(int index, Verb v, bool forToggle, VerbGizmoOption o)
        {
            string desc = null;
            if (o != null)
            {
                string s = forToggle ? (o.toggleDesc ?? o.desc) : o.desc;
                desc = !string.IsNullOrEmpty(s) ? s.Translate() : null;
            }

            if (desc == null)
                desc = forToggle ? "SSF_Verb_ToggleDesc".Translate() : "SSF_Verb_OrderDesc".Translate();

            // durationCostTicks 비용 표시
            int cost = o != null ? o.durationCostTicks : 0;
            if (cost > 0)
            {
                string costStr = GenDate.ToStringTicksToPeriod(cost, allowSeconds: false, shortForm: false);
                desc += "\n\n" + "SSF_DurationCost".Translate(costStr);
            }

            return desc;
        }

        /// <summary>verb 사용 시 차감할 변신 잔여 틱 반환. 0이면 비용 없음.</summary>
        public int GetVerbDurationCost(int index, Verb v)
        {
            var o = FindGizmoOption(index, v);
            return o != null ? o.durationCostTicks : 0;
        }

        /// <summary>verb 인덱스를 빠르게 조회 (패치용).</summary>
        public int FindVerbIndex(Verb v)
        {
            var vt = ShapeshiftVerbTracker;
            if (vt == null) return -1;
            var verbs = vt.AllVerbs;
            for (int i = 0; i < verbs.Count; i++)
            {
                if (verbs[i] == v) return i;
            }
            return -1;
        }

        // verb 아이콘 텍스처 캐시 — iconPath별 1회만 ContentFinder 호출
        private static readonly Dictionary<string, Texture2D> _verbIconCache =
            new Dictionary<string, Texture2D>();

        /// <summary>verbGizmoOptions의 iconPath에서 아이콘 로드 (캐시).</summary>
        private Texture2D GetVerbIcon(int index, Verb v)
        {
            return GetVerbIcon(index, v, FindGizmoOption(index, v));
        }

        /// <summary>verbGizmoOptions의 iconPath에서 아이콘 로드 (조회 결과 재사용, 캐시).</summary>
        private Texture2D GetVerbIcon(int index, Verb v, VerbGizmoOption o)
        {
            if (o == null) return null;
            string path = o.iconPath;
            if (string.IsNullOrEmpty(path)) return null;

            Texture2D tex;
            if (!_verbIconCache.TryGetValue(path, out tex))
            {
                tex = ContentFinder<Texture2D>.Get(path, reportFailure: false);
                _verbIconCache[path] = tex;
            }
            return tex;
        }
    }
}
