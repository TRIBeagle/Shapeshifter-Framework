// ShapeshifterFramework | Utilities | ShapeshiftEligibility.cs
// 목적 : 특정 폰(Pawn)이 지정된 폼(Form)으로 변신할 수 있는 자격(Eligibility)이 되는지 판정.
// 용도 : 1단계 게이트 검사(종족, 뮤턴트, 제노타입, 이전 폼 제한)와 2단계 조건 집계(유전자, 아이템, 장비, 능력, 헤디프 보유 여부)를 requirementsMode(All/Any)에 맞춰 철저하게 검증함.
// 주의 : Anomaly DLC의 뮤턴트 판정은 성능을 위해 HediffDef 기준의 내부 캐시(Dictionary)를 구축하여 O(1) 속도로 탐색하도록 최적화됨.

using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace ShapeshifterFramework.Utilities
{
    internal static class ShapeshiftEligibility
    {
        // ─────────────────────────────────────────────────────────────
        // Mutant 캐시: HediffDef → MutantDef (Anomaly)
        private static Dictionary<HediffDef, MutantDef> _mutantByHediff;

        private static void EnsureMutantCache()
        {
            if (_mutantByHediff != null) return;
            _mutantByHediff = new Dictionary<HediffDef, MutantDef>(64);
            if (!ModLister.AnomalyInstalled) return;

            var list = DefDatabase<MutantDef>.AllDefsListForReading;
            for (int i = 0; i < list.Count; i++)
            {
                var m = list[i];
                if (m == null || m.hediff == null) continue;
                if (!_mutantByHediff.TryGetValue(m.hediff, out _))
                    _mutantByHediff.Add(m.hediff, m);
            }
        }

        private static bool Active<T>(List<T> list) => list != null && list.Count > 0;

        // 단일 값 allow/disallow 필터
        private static bool PassAllowDisallowSingle<T>(T value, List<T> allow, List<T> disallow)
            where T : class
        {
            bool hasAllow = Active(allow);
            bool hasDisallow = Active(disallow);

            if (!hasAllow && !hasDisallow) return true;
            if (hasAllow && !hasDisallow)
            {
                for (int i = 0; i < allow.Count; i++)
                    if (allow[i] == value) return true;
                return false;
            }
            if (!hasAllow && hasDisallow)
            {
                for (int i = 0; i < disallow.Count; i++)
                    if (disallow[i] == value) return false;
                return true;
            }
            // allow - disallow
            bool inAllow = false;
            for (int i = 0; i < allow.Count; i++) if (allow[i] == value) { inAllow = true; break; }
            if (!inAllow) return false;
            for (int i = 0; i < disallow.Count; i++) if (disallow[i] == value) return false;
            return true;
        }

        // 다중 값 allow/disallow 필터
        private static bool PassAllowDisallowMulti<T>(List<T> have, List<T> allow, List<T> disallow)
            where T : class
        {
            bool hasAllow = Active(allow);
            bool hasDisallow = Active(disallow);

            if (!hasAllow && !hasDisallow) return true;

            if (hasAllow && !hasDisallow)
            {
                // have ∩ allow ≠ ∅
                for (int i = 0; i < have.Count; i++)
                    for (int j = 0; j < allow.Count; j++)
                        if (have[i] == allow[j]) return true;
                return false;
            }
            if (!hasAllow && hasDisallow)
            {
                // have ∩ disallow = ∅
                for (int i = 0; i < have.Count; i++)
                    for (int j = 0; j < disallow.Count; j++)
                        if (have[i] == disallow[j]) return false;
                return true;
            }

            // have ∩ (allow - disallow)
            for (int i = 0; i < have.Count; i++)
            {
                bool inAllow = false;
                for (int j = 0; j < allow.Count; j++) if (have[i] == allow[j]) { inAllow = true; break; }
                if (!inAllow) continue;
                bool inDisallow = false;
                for (int j = 0; j < disallow.Count; j++) if (have[i] == disallow[j]) { inDisallow = true; break; }
                if (!inDisallow) return true;
            }
            return false;
        }

        private static XenotypeDef TryGetXenotype(Pawn pawn)
        {
            try { return pawn?.genes?.Xenotype; } catch { return null; }
        }

        private static List<MutantDef> GetPawnMutants(Pawn pawn)
        {
            List<MutantDef> list = new List<MutantDef>(2);
            EnsureMutantCache();
            if (_mutantByHediff == null || pawn?.health?.hediffSet == null) return list;

            var hediffs = pawn.health.hediffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                var h = hediffs[i];
                if (h == null || h.def == null) continue;
                MutantDef md;
                if (_mutantByHediff.TryGetValue(h.def, out md) && md != null)
                    list.Add(md);
            }
            return list;
        }

        // ─────────────────────────────────────────────────────────────
        // 게이트(집계 제외): Race / Mutant / Xenotype / FromForm
        public static bool PassBasicFilters(Pawn pawn, ShapeshiftFormDef form, string prevDefName)
        {
            if (pawn == null || form == null) return false;

            // Race
            if (!PassAllowDisallowSingle(pawn.def, form.allowedRaces, form.disallowedRaces))
                return false;

            // Mutant (Anomaly)
            if (Active(form.allowedMutants) || Active(form.disallowedMutants))
            {
                if (!ModLister.AnomalyInstalled)
                {
                    if (Active(form.allowedMutants)) return false;
                }
                else
                {
                    var have = GetPawnMutants(pawn);
                    if (!PassAllowDisallowMulti(have, form.allowedMutants, form.disallowedMutants))
                        return false;
                }
            }

            // Xenotype (Biotech)
            if (Active(form.allowedXenotypes) || Active(form.disallowedXenotypes))
            {
                if (!ModsConfig.BiotechActive)
                {
                    if (Active(form.allowedXenotypes)) return false;
                }
                else
                {
                    var x = TryGetXenotype(pawn);
                    if (!PassAllowDisallowSingle(x, form.allowedXenotypes, form.disallowedXenotypes))
                        return false;
                }
            }

            // FromForm 게이트
            if (Active(form.allowedFromForms))
            {
                bool noneAllowed = false;
                for (int i = 0; i < form.allowedFromForms.Count; i++)
                {
                    var s = form.allowedFromForms[i];
                    if (!string.IsNullOrEmpty(s) && s.Equals("None", StringComparison.OrdinalIgnoreCase))
                    { noneAllowed = true; break; }
                }

                if (prevDefName == null)
                {
                    if (!noneAllowed) return false;
                }
                else
                {
                    bool listed = false;
                    for (int i = 0; i < form.allowedFromForms.Count; i++)
                        if (string.Equals(form.allowedFromForms[i], prevDefName, StringComparison.Ordinal))
                        { listed = true; break; }
                    if (!listed) return false;
                }
            }
            // allowedFromForms 미지정: 변신 중이 아닐 때만 진입 허용
            // (폼 체이닝을 원하면 allowedFromForms에 명시적으로 이전 폼을 나열해야 함)
            if (prevDefName != null) return false;

            return true;
        }

        // ─────────────────────────────────────────────────────────────
        // required* 카테고리 집계(RequirementsMode 전용)
        public static bool PassConditional(Pawn pawn, ShapeshiftFormDef form)
        {
            if (pawn == null || form == null) return false;

            var mode = form.requirementsMode.HasValue ? form.requirementsMode.Value : RequirementMatchMode.All;

            bool anyActive = false;
            bool anyPass = false;

            // Items in inventory (ALL-of)
            if (Active(form.requiredItems))
            {
                anyActive = true;
                bool pass = false;
                var inv = pawn.inventory?.innerContainer;
                if (inv != null)
                {
                    pass = true;
                    for (int i = 0; i < form.requiredItems.Count; i++)
                    {
                        var need = form.requiredItems[i]; if (need == null) continue;
                        bool has = false;
                        for (int j = 0; j < inv.Count; j++)
                        {
                            var t = inv[j];
                            if (t != null && t.def == need && t.stackCount > 0) { has = true; break; }
                        }
                        if (!has) { pass = false; break; }
                    }
                }
                if (pass) anyPass = true; else if (mode == RequirementMatchMode.All) return false;
            }

            // Apparel worn (ALL-of)
            if (Active(form.requiredApparels))
            {
                anyActive = true;
                bool pass = false;
                var worn = pawn.apparel?.WornApparel;
                if (worn != null)
                {
                    pass = true;
                    for (int i = 0; i < form.requiredApparels.Count; i++)
                    {
                        var need = form.requiredApparels[i]; if (need == null) continue;
                        bool has = false;
                        for (int j = 0; j < worn.Count; j++)
                        {
                            var a = worn[j];
                            if (a != null && a.def == need) { has = true; break; }
                        }
                        if (!has) { pass = false; break; }
                    }
                }
                if (pass) anyPass = true; else if (mode == RequirementMatchMode.All) return false;
            }

            // Weapons equipped (ALL-of) — 다중 장비 모드 고려
            if (Active(form.requiredWeapons))
            {
                anyActive = true;
                bool pass = false;
                var eqs = pawn.equipment?.AllEquipmentListForReading;
                if (eqs != null)
                {
                    pass = true;
                    for (int i = 0; i < form.requiredWeapons.Count; i++)
                    {
                        var need = form.requiredWeapons[i]; if (need == null) continue;
                        bool has = false;
                        for (int j = 0; j < eqs.Count; j++)
                        {
                            var e = eqs[j];
                            if (e != null && e.def == need) { has = true; break; }
                        }
                        if (!has) { pass = false; break; }
                    }
                }
                if (pass) anyPass = true; else if (mode == RequirementMatchMode.All) return false;
            }

            // Hediffs (ALL-of)
            if (Active(form.requiredHediffs))
            {
                anyActive = true;
                bool pass = false;
                var set = pawn.health?.hediffSet;
                if (set != null)
                {
                    pass = true;
                    for (int i = 0; i < form.requiredHediffs.Count; i++)
                    {
                        var need = form.requiredHediffs[i]; if (need == null) continue;
                        if (set.GetFirstHediffOfDef(need) == null) { pass = false; break; }
                    }
                }
                if (pass) anyPass = true; else if (mode == RequirementMatchMode.All) return false;
            }

            if (!anyActive) return true;                   // 요건 없음 → 통과
            if (mode == RequirementMatchMode.Any) return anyPass;
            return true;                                   // All은 위에서 미통과시 이미 false 반환
        }
    }
}
