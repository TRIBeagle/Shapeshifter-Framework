// ShapeshifterFramework | Utilities | ShapeshiftVisualFilter.cs
// 목적 : 변신 폼의 렌더링 설정(renderShow/Hide)에 따라 바닐라 의상, 무기, 유전자, 헤디프 그래픽의 노출 여부를 판정.
// 용도 : 와일드카드('*') 및 "All" 키워드를 사전 컴파일(CompiledFilterSet)하여 렌더 경로에서 HashSet O(1) 조회와 최소한의 문자열 비교만 수행.
// 주의 : CompiledFilterSet은 ShapeshiftFormDef.ResolveReferences()에서 한 번만 빌드되며, 렌더 루프에서는 컴파일된 필터만 사용.

using RimWorld;
using ShapeshifterFramework.Comps;
using System;
using System.Collections.Generic;
using Verse;

namespace ShapeshifterFramework.Utilities
{
    // ─── 사전 컴파일된 와일드카드 필터 ───

    internal enum FilterMode { Exact, Prefix, Suffix, Contains }

    internal struct CompiledFilter
    {
        public FilterMode mode;
        public string core; // 소문자 변환된 핵심 문자열 (*제거)
    }

    /// <summary>와일드카드 패턴 리스트를 사전 파싱하여 O(1) 조회 및 최소 문자열 비교를 지원.</summary>
    internal class CompiledFilterSet
    {
        public readonly bool hasAll;               // "All" 키워드 포함 여부
        private readonly HashSet<string> _exact;   // 와일드카드 없는 정확 매칭 (OrdinalIgnoreCase)
        private readonly CompiledFilter[] _wild;   // 와일드카드 패턴들

        public bool IsEmpty => !hasAll && _exact == null && _wild == null;

        private CompiledFilterSet(bool hasAll, HashSet<string> exact, CompiledFilter[] wild)
        {
            this.hasAll = hasAll;
            _exact = exact;
            _wild = wild;
        }

        /// <summary>값이 이 필터셋의 패턴 중 하나라도 매칭되는지 판정.</summary>
        public bool Matches(string value)
        {
            if (hasAll) return true;
            if (string.IsNullOrEmpty(value)) return false;

            // HashSet O(1) 정확 매칭
            if (_exact != null && _exact.Contains(value)) return true;

            // 와일드카드 폴백
            if (_wild != null)
            {
                for (int i = 0; i < _wild.Length; i++)
                {
                    ref var f = ref _wild[i];
                    switch (f.mode)
                    {
                        case FilterMode.Prefix:
                            if (value.StartsWith(f.core, StringComparison.OrdinalIgnoreCase)) return true;
                            break;
                        case FilterMode.Suffix:
                            if (value.EndsWith(f.core, StringComparison.OrdinalIgnoreCase)) return true;
                            break;
                        case FilterMode.Contains:
                            if (value.IndexOf(f.core, StringComparison.OrdinalIgnoreCase) >= 0) return true;
                            break;
                    }
                }
            }

            return false;
        }

        // 빈 필터 싱글톤
        private static readonly CompiledFilterSet _empty = new CompiledFilterSet(false, null, null);

        /// <summary>원본 패턴 리스트를 컴파일.</summary>
        public static CompiledFilterSet Compile(List<string> patterns)
        {
            if (patterns == null || patterns.Count == 0) return _empty;

            bool hasAll = false;
            HashSet<string> exact = null;
            List<CompiledFilter> wild = null;

            for (int i = 0; i < patterns.Count; i++)
            {
                var p = patterns[i];
                if (string.IsNullOrEmpty(p)) continue;

                if (string.Equals(p, "All", StringComparison.OrdinalIgnoreCase))
                {
                    hasAll = true;
                    continue; // "All"이면 다른 패턴은 의미 없지만 호환성 위해 계속 파싱
                }

                int star = p.IndexOf('*');
                if (star < 0)
                {
                    // 정확 매칭 → HashSet
                    if (exact == null) exact = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    exact.Add(p);
                    continue;
                }

                // 와일드카드 패턴 분류
                string core = p.Replace("*", "");
                if (core.Length == 0)
                {
                    // "*"만 있으면 사실상 All
                    hasAll = true;
                    continue;
                }

                bool startsW = p[0] == '*';
                bool endsW = p[p.Length - 1] == '*';

                FilterMode mode;
                if (startsW && endsW) mode = FilterMode.Contains;
                else if (startsW) mode = FilterMode.Suffix;
                else if (endsW) mode = FilterMode.Prefix;
                else mode = FilterMode.Contains; // 중간 *는 contains로 폴백

                if (wild == null) wild = new List<CompiledFilter>();
                wild.Add(new CompiledFilter { mode = mode, core = core });
            }

            if (!hasAll && exact == null && wild == null) return _empty;
            return new CompiledFilterSet(hasAll, exact, wild?.ToArray());
        }
    }

    internal static class ShapeshiftVisualFilter
    {
        // comp와 form을 한 번에 가져오는 내부 헬퍼 — 렌더 경로에서 TryGetComp 중복 호출 방지
        private static bool TryGetFormAndComp(Pawn pawn, out CompShapeshifter comp, out ShapeshiftFormDef form)
        {
            if (ShapeshiftRegistry.TryGet(pawn, out comp, out form))
                return true;
            form = null;
            return false;
        }

        // ─── 의상 (Apparel) ───

        internal static bool ShouldHideApparelGraphic(Pawn pawn, Apparel apparel)
        {
            if (apparel == null || apparel.def == null) return false;
            if (!TryGetFormAndComp(pawn, out _, out var form)) return false;

            var def = apparel.def;
            var layers = def.apparel?.layers;

            // 1) 화이트리스트 (Show)
            if (form._showApparelDefNames.Matches(def.defName)) return false;
            if (layers != null)
            {
                for (int i = 0; i < layers.Count; i++)
                    if (form._showApparelLayers.Matches(layers[i].defName)) return false;
            }

            // 2) 블랙리스트 - 이름/전체 (Hide Defs)
            if (form._hideApparelDefNames.hasAll || form._hideApparelDefNames.Matches(def.defName)) return true;

            // 3) 블랙리스트 - 레이어 (Hide Layers)
            if (form._hideApparelLayers.hasAll) return true;
            if (layers != null)
            {
                for (int i = 0; i < layers.Count; i++)
                    if (form._hideApparelLayers.Matches(layers[i].defName)) return true;
            }

            return false;
        }

        // ─── 무기 (Equipment) ───

        internal static bool ShouldHideEquipmentGraphic(Pawn pawn, Thing eq)
        {
            if (eq == null || eq.def == null) return false;
            if (!TryGetFormAndComp(pawn, out _, out var form)) return false;

            var def = eq.def;
            var tags = def.weaponTags;

            // 1) 화이트리스트 (Show)
            if (form._showWeaponDefNames.Matches(def.defName)) return false;
            if (tags != null)
            {
                for (int i = 0; i < tags.Count; i++)
                    if (form._showWeaponTags.Matches(tags[i])) return false;
            }

            // 2) 블랙리스트 - 이름/전체 (Hide Defs)
            if (form._hideWeaponDefNames.hasAll || form._hideWeaponDefNames.Matches(def.defName)) return true;

            // 3) 블랙리스트 - 태그 (Hide Tags)
            if (form._hideWeaponTags.hasAll) return true;
            if (tags != null)
            {
                for (int i = 0; i < tags.Count; i++)
                    if (form._hideWeaponTags.Matches(tags[i])) return true;
            }

            return false;
        }

        // ─── 유전자 (Gene) ───

        internal static bool ShouldHideGeneByDefOrTags(Pawn pawn, Gene gene, IList<string> tagsFromNodeOrDef)
        {
            if (gene == null || gene.def == null) return false;
            if (!TryGetFormAndComp(pawn, out _, out var form)) return false;

            // 1) 화이트리스트 (Show)
            if (form._showGeneDefNames.Matches(gene.def.defName)) return false;
            if (tagsFromNodeOrDef != null)
            {
                for (int i = 0; i < tagsFromNodeOrDef.Count; i++)
                    if (form._showGeneExclusionTags.Matches(tagsFromNodeOrDef[i])) return false;
            }

            // 2) 블랙리스트 - 이름/전체 (Hide Defs)
            if (form._hideGeneDefNames.hasAll || form._hideGeneDefNames.Matches(gene.def.defName)) return true;

            // 3) 블랙리스트 - 태그 (Hide Tags)
            if (form._hideGeneExclusionTags.hasAll) return true;
            if (tagsFromNodeOrDef != null)
            {
                for (int i = 0; i < tagsFromNodeOrDef.Count; i++)
                    if (form._hideGeneExclusionTags.Matches(tagsFromNodeOrDef[i])) return true;
            }

            return false;
        }

        // ─── 헤디프/변이 (Hediff) ───

        internal static bool ShouldHideHediffGraphic(Pawn pawn, Hediff hediff)
        {
            if (hediff == null || hediff.def == null) return false;
            if (!TryGetFormAndComp(pawn, out _, out var form)) return false;

            // 1) 화이트리스트 (Show)
            if (form._showHediffDefNames.Matches(hediff.def.defName)) return false;

            // 2) 블랙리스트 - 이름/전체 (Hide Defs)
            if (form._hideHediffDefNames.hasAll || form._hideHediffDefNames.Matches(hediff.def.defName)) return true;

            return false;
        }

        /// <summary>GeneDef가 외형에 영향을 주는지 판별 (렌더 노드, 피부색, 머리색, 체형 등).</summary>
        private static bool HasAnyVisualEffect(GeneDef def)
        {
            if (!def.renderNodeProperties.NullOrEmpty()) return true;
            if (def.skinColorBase != null) return true;
            if (def.skinColorOverride.HasValue) return true;
            if (def.hairColorOverride.HasValue) return true;
            if (def.hairTagFilter != null) return true;
            if (def.beardTagFilter != null) return true;
            if (!def.forcedHeadTypes.NullOrEmpty()) return true;
            if (def.bodyType != null) return true;
            return false;
        }

        /// <summary>UI 탭에서 디밍 대상인지 판별. 외형에 영향이 없는 유전자(면역력, 대사 등)는 항상 false.</summary>
        internal static bool ShouldHideGeneForUI(Pawn pawn, Gene gene)
        {
            if (gene?.def == null) return false;

            // 외형에 영향이 없는 유전자는 디밍하지 않음
            if (!HasAnyVisualEffect(gene.def)) return false;

            var tags = (IList<string>)gene.def.exclusionTags;
            return ShouldHideGeneByDefOrTags(pawn, gene, tags);
        }
    }
}