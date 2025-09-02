// ShapeshiftVisualFilter.cs
// 목적: 폼 기반 시각 필터(의상/유전자/무기) 공통 판정.
// 정책: allowed(화이트리스트) 매치 시 "항상 표시" (블랙리스트 예외). 그 외 hidden(블랙리스트) 매치 시 숨김.
// 특수: "All" 지원, '*' 와일드카드(부분 매치) 지원.
// 메모: C# 7.3 호환.

using RimWorld;
using ShapeshifterFramework.Comps;
using System;
using System.Collections.Generic;
using Verse;

namespace ShapeshifterFramework.Utilities
{
    internal static class ShapeshiftVisualFilter
    {
        private static ShapeshiftFormDef CurForm(Pawn pawn)
        {
            if (pawn == null) return null;
            var comp = pawn.TryGetComp<CompShapeshifter>();
            return (comp != null && comp.isTransformed) ? comp.currentForm : null;
        }

        // ─── 공통 유틸 ───

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

        private static bool AnyMatchFromList(IList<string> values, List<string> patterns)
        {
            if (values == null || values.Count == 0) return false;
            if (patterns == null || patterns.Count == 0) return false;
            for (int i = 0; i < values.Count; i++)
                if (MatchesAny(values[i], patterns)) return true;
            return false;
        }

        // ─── 의상 ───

        internal static bool ShouldHideApparelGraphic(Pawn pawn, Apparel apparel)
        {
            var form = CurForm(pawn);
            if (form == null || apparel == null) return false;

            var def = apparel.def;
            if (def == null) return false;

            // 1) 화이트리스트 예외(매치 시 항상 표시)
            if (MatchesAny(def.defName, form.renderShowApparelDefNames))
                return false;

            var layers = def.apparel != null ? def.apparel.layers : null;
            if (layers != null && layers.Count > 0)
            {
                for (int i = 0; i < layers.Count; i++)
                {
                    var l = layers[i];
                    if (l != null && MatchesAny(l.defName, form.renderShowApparelLayers))
                        return false;
                }
            }

            // 2) 블랙리스트
            if (MatchesAny(def.defName, form.renderHideApparelDefNames))
                return true;

            var wantedLayers = form.renderHideApparelLayers;
            if (ListHasAll(wantedLayers))
                return true;

            if (layers != null && wantedLayers != null && wantedLayers.Count > 0)
            {
                for (int i = 0; i < layers.Count; i++)
                {
                    var l = layers[i];
                    if (l != null && MatchesAny(l.defName, wantedLayers))
                        return true;
                }
            }

            return false;
        }

        // ─── 무기 ───

        internal static bool ShouldHideEquipmentGraphic(Pawn pawn, Thing eq)
        {
            var form = CurForm(pawn);
            if (form == null || eq == null) return false;

            var def = eq.def;
            if (def == null) return false;

            // 1) 화이트리스트 예외
            if (MatchesAny(def.defName, form.renderShowWeaponDefNames))
                return false;

            if (def.weaponTags != null && def.weaponTags.Count > 0)
            {
                for (int i = 0; i < def.weaponTags.Count; i++)
                    if (MatchesAny(def.weaponTags[i], form.renderShowWeaponTags))
                        return false;
            }

            // 2) 블랙리스트
            if (MatchesAny(def.defName, form.renderHideWeaponDefNames))
                return true;

            if (def.weaponTags != null && def.weaponTags.Count > 0 && form.renderHideWeaponTags != null && form.renderHideWeaponTags.Count > 0)
            {
                for (int i = 0; i < def.weaponTags.Count; i++)
                    if (MatchesAny(def.weaponTags[i], form.renderHideWeaponTags))
                        return true;
            }

            return false;
        }

        // ─── 유전자 ───

        internal static bool ShouldHideGeneByDefOrTags(Pawn pawn, Gene gene, IList<string> tagsFromNodeOrDef)
        {
            var form = CurForm(pawn);
            if (form == null || gene == null || gene.def == null) return false;

            // 1) 화이트리스트 예외
            if (MatchesAny(gene.def.defName, form.renderShowGeneDefNames))
                return false;

            if (tagsFromNodeOrDef != null && tagsFromNodeOrDef.Count > 0)
            {
                for (int i = 0; i < tagsFromNodeOrDef.Count; i++)
                    if (MatchesAny(tagsFromNodeOrDef[i], form.renderShowGeneExclusionTags))
                        return false;
            }

            // 2) 블랙리스트
            if (MatchesAny(gene.def.defName, form.renderHideGeneDefNames))
                return true;

            var wanted = form.renderHideGeneExclusionTags;
            if (ListHasAll(wanted))
                return true;

            if (tagsFromNodeOrDef != null && wanted != null && wanted.Count > 0)
            {
                for (int i = 0; i < tagsFromNodeOrDef.Count; i++)
                    if (MatchesAny(tagsFromNodeOrDef[i], wanted))
                        return true;
            }

            return false;
        }

        internal static bool ShouldHideGeneForUI(Pawn pawn, Gene gene)
        {
            var tags = (gene != null && gene.def != null) ? (IList<string>)gene.def.exclusionTags : null;
            return ShouldHideGeneByDefOrTags(pawn, gene, tags);
        }
    }
}
