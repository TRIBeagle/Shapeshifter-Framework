// ShapeshifterFramework | Utilities | ShapeshiftVisualFilter.cs
// 목적   : 폼 기반 시각 필터(의상/유전자/무기/헤디프) 공통 판정 및 일관성 강화.
// 정책   : 화이트리스트(Show) 매치 시 항상 표시(블랙리스트 예외). 그 외 블랙리스트(Hide) 매치 시 숨김.
// 특징   : "All" 키워드 및 '*' 와일드카드 지원. 데이터가 없는 파츠도 "All" 정책 시 필터링됨.

using RimWorld;
using ShapeshifterFramework.Comps;
using System;
using System.Collections.Generic;
using Verse;

namespace ShapeshifterFramework.Utilities
{
    internal static class ShapeshiftVisualFilter
    {
        // comp와 form을 한 번에 가져오는 내부 헬퍼 — 렌더 경로에서 TryGetComp 중복 호출 방지
        private static bool TryGetFormAndComp(Pawn pawn, out CompShapeshifter comp, out ShapeshiftFormDef form)
        {
            comp = pawn != null ? pawn.TryGetComp<CompShapeshifter>() : null;
            if (comp != null && comp.isTransformed && comp.currentForm != null)
            {
                form = comp.currentForm;
                return true;
            }
            form = null;
            return false;
        }

        // ─── 공통 유틸리티 ───

        private static bool WildcardMatch(string value, string pattern)
        {
            if (string.IsNullOrEmpty(pattern)) return false;
            if (string.Equals(pattern, "All", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.IsNullOrEmpty(value)) return false;

            int star = pattern.IndexOf('*');
            if (star < 0) return string.Equals(value, pattern, StringComparison.OrdinalIgnoreCase);

            string core = pattern.Replace("*", "");
            if (core.Length == 0) return true;
            bool starts = pattern.StartsWith("*");
            bool ends = pattern.EndsWith("*");

            if (starts && ends) return value.IndexOf(core, StringComparison.OrdinalIgnoreCase) >= 0;
            if (starts) return value.EndsWith(core, StringComparison.OrdinalIgnoreCase);
            if (ends) return value.StartsWith(core, StringComparison.OrdinalIgnoreCase);
            return value.IndexOf(core, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool MatchesAny(string value, List<string> patterns)
        {
            if (patterns == null || patterns.Count == 0) return false;
            for (int i = 0; i < patterns.Count; i++)
                if (WildcardMatch(value, patterns[i])) return true;
            return false;
        }

        private static bool ListHasAll(List<string> patterns)
        {
            if (patterns == null) return false;
            for (int i = 0; i < patterns.Count; i++)
                if (patterns[i] != null && patterns[i].Equals("All", StringComparison.OrdinalIgnoreCase))
                    return true;
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
            if (MatchesAny(def.defName, form.renderShowApparelDefNames)) return false;
            if (layers != null)
            {
                for (int i = 0; i < layers.Count; i++)
                    if (MatchesAny(layers[i].defName, form.renderShowApparelLayers)) return false;
            }

            // 2) 블랙리스트 - 이름/전체 (Hide Defs)
            var hideDefs = form.renderHideApparelDefNames;
            if (ListHasAll(hideDefs) || MatchesAny(def.defName, hideDefs)) return true;

            // 3) 블랙리스트 - 레이어 (Hide Layers)
            var hideLayers = form.renderHideApparelLayers;
            if (ListHasAll(hideLayers)) return true; // 레이어 정보가 없어도 All이면 숨김
            if (layers != null)
            {
                for (int i = 0; i < layers.Count; i++)
                    if (MatchesAny(layers[i].defName, hideLayers)) return true;
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
            if (MatchesAny(def.defName, form.renderShowWeaponDefNames)) return false;
            if (tags != null)
            {
                for (int i = 0; i < tags.Count; i++)
                    if (MatchesAny(tags[i], form.renderShowWeaponTags)) return false;
            }

            // 2) 블랙리스트 - 이름/전체 (Hide Defs)
            var hideDefs = form.renderHideWeaponDefNames;
            if (ListHasAll(hideDefs) || MatchesAny(def.defName, hideDefs)) return true;

            // 3) 블랙리스트 - 태그 (Hide Tags)
            var hideTags = form.renderHideWeaponTags;
            if (ListHasAll(hideTags)) return true; // 태그가 없는 무기도 All이면 숨김
            if (tags != null)
            {
                for (int i = 0; i < tags.Count; i++)
                    if (MatchesAny(tags[i], hideTags)) return true;
            }

            return false;
        }

        // ─── 유전자 (Gene) ───

        internal static bool ShouldHideGeneByDefOrTags(Pawn pawn, Gene gene, IList<string> tagsFromNodeOrDef)
        {
            if (gene == null || gene.def == null) return false;
            if (!TryGetFormAndComp(pawn, out _, out var form)) return false;

            // 1) 화이트리스트 (Show)
            if (MatchesAny(gene.def.defName, form.renderShowGeneDefNames)) return false;
            if (tagsFromNodeOrDef != null)
            {
                for (int i = 0; i < tagsFromNodeOrDef.Count; i++)
                    if (MatchesAny(tagsFromNodeOrDef[i], form.renderShowGeneExclusionTags)) return false;
            }

            // 2) 블랙리스트 - 이름/전체 (Hide Defs)
            var hideDefs = form.renderHideGeneDefNames;
            if (ListHasAll(hideDefs) || MatchesAny(gene.def.defName, hideDefs)) return true;

            // 3) 블랙리스트 - 태그 (Hide Tags)
            var hideTags = form.renderHideGeneExclusionTags;
            if (ListHasAll(hideTags)) return true;
            if (tagsFromNodeOrDef != null)
            {
                for (int i = 0; i < tagsFromNodeOrDef.Count; i++)
                    if (MatchesAny(tagsFromNodeOrDef[i], hideTags)) return true;
            }

            return false;
        }

        // ─── 헤디프/변이 (Hediff) ───

        internal static bool ShouldHideHediffGraphic(Pawn pawn, Hediff hediff)
        {
            if (hediff == null || hediff.def == null) return false;
            if (!TryGetFormAndComp(pawn, out _, out var form)) return false;

            // 1) 화이트리스트 (Show)
            if (MatchesAny(hediff.def.defName, form.renderShowHediffDefNames)) return false;

            // 2) 블랙리스트 - 이름/전체 (Hide Defs)
            var hideDefs = form.renderHideHediffDefNames;
            if (ListHasAll(hideDefs) || MatchesAny(hediff.def.defName, hideDefs)) return true;

            return false;
        }

        internal static bool ShouldHideGeneForUI(Pawn pawn, Gene gene)
        {
            var tags = (gene?.def != null) ? (IList<string>)gene.def.exclusionTags : null;
            return ShouldHideGeneByDefOrTags(pawn, gene, tags);
        }
    }
}