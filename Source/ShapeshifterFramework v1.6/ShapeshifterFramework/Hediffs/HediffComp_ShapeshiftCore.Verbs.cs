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

        /// <summary>verb 키 생성. 동일 이름 verb가 복수일 때만 인덱스 부여.</summary>
        string AutoKey(Verb v)
        {
            var f = currentForm?.defName ?? "None";
            string vName = v?.verbProps?.label ?? v?.GetType().Name ?? "UnknownVerb";

            // 동일 이름 verb 중복 여부 확인
            int dupeCount = 0;
            int myIndex = 0;
            var vt = shapeshiftVerbTracker;
            if (vt != null)
            {
                var verbs = vt.AllVerbs;
                for (int i = 0; i < verbs.Count; i++)
                {
                    string otherName = verbs[i]?.verbProps?.label ?? verbs[i]?.GetType().Name ?? "UnknownVerb";
                    if (otherName == vName)
                    {
                        if (verbs[i] == v) myIndex = dupeCount;
                        dupeCount++;
                    }
                }
            }

            return dupeCount > 1 ? f + "#" + vName + "#" + myIndex : f + "#" + vName;
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
            var vp = v?.verbProps;
            var o = FindGizmoOption(index, v);
            if (o != null)
            {
                string s = preferToggleLabel ? (o.toggleLabel ?? o.label) : o.label;
                if (!string.IsNullOrEmpty(s)) return s.Translate().CapitalizeFirst();
            }

            string __label = string.IsNullOrEmpty(vp?.label) ? "SSF_Verb_Attack".Translate() : vp.label.Translate();
            return __label.CapitalizeFirst();
        }

        /// <summary>verb 명령/토글 설명 반환.</summary>
        public string GetVerbDesc(int index, Verb v, bool forToggle)
        {
            var o = FindGizmoOption(index, v);
            if (o != null)
            {
                string s = forToggle ? (o.toggleDesc ?? o.desc) : o.desc;
                if (!string.IsNullOrEmpty(s)) return s.Translate();
            }

            if (forToggle) return "SSF_Verb_ToggleDesc".Translate();
            return "SSF_Verb_OrderDesc".Translate();
        }

        /// <summary>verbGizmoOptions의 iconPath에서 아이콘 로드.</summary>
        private Texture2D GetVerbIcon(int index, Verb v)
        {
            var o = FindGizmoOption(index, v);
            if (o != null)
            {
                string path = o.iconPath;
                if (!string.IsNullOrEmpty(path))
                    return ContentFinder<Texture2D>.Get(path, reportFailure: false);
            }
            return null;
        }
    }
}
