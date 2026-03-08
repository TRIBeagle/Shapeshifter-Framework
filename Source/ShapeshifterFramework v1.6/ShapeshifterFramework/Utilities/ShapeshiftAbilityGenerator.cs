// ShapeshifterFramework | Utilities | ShapeshiftAbilityGenerator.cs
// 목적 : AbilityMode.Auto인 ShapeshiftFormDef에 대해 런타임에 동적 AbilityDef를 자동 생성함.
// 용도 : ShapeshiftStatHediffGenerator와 동일한 패턴으로 ResolveReferences 시점에 호출되어 DefDatabase에 등록.

using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ShapeshifterFramework.Utilities
{
    /// <summary>ShapeshiftFormDef별 동적 AbilityDef 생성기.</summary>
    internal static class ShapeshiftAbilityGenerator
    {
        // 중복 생성 방지용
        private static readonly HashSet<string> _generated = new HashSet<string>();

        /// <summary>단일 폼에 대해 동적 AbilityDef 생성. 중복 호출 안전.</summary>
        internal static void TryGenerateFor(ShapeshiftFormDef form)
        {
            if (form == null) return;
            if (form.abilityMode != AbilityMode.Auto) return;

            string defName = "SSF_AutoAbility_" + form.defName;
            if (_generated.Contains(defName)) return;

            // 다른 모드의 동일 defName 중복 Add 방어
            var existing = DefDatabase<AbilityDef>.GetNamedSilentFail(defName);
            if (existing != null)
            {
                form.generatedAbility = existing;
                _generated.Add(defName);
                return;
            }

            var abilityDef = CreateAbilityDef(form, defName);
            if (abilityDef == null) return;

            DefDatabase<AbilityDef>.Add(abilityDef);

            form.generatedAbility = abilityDef;
            _generated.Add(defName);
        }

        /// <summary>모든 폼의 누락분 일괄 생성. HarmonyInit 보험용.</summary>
        internal static void GenerateAll()
        {
            int generated = 0;
            var allForms = DefDatabase<ShapeshiftFormDef>.AllDefsListForReading;

            for (int i = 0; i < allForms.Count; i++)
            {
                var form = allForms[i];
                if (form == null || form.abilityMode != AbilityMode.Auto) continue;
                if (form.generatedAbility != null) continue;

                int before = _generated.Count;
                TryGenerateFor(form);
                if (_generated.Count > before) generated++;
            }

            if (generated > 0)
                Log.Message($"[SSF] Generated {generated} additional ability def(s) for shapeshift forms.");
        }

        private static AbilityDef CreateAbilityDef(ShapeshiftFormDef form, string defName)
        {
            var abilityDef = new AbilityDef();

            abilityDef.defName = defName;
            abilityDef.label = form.label ?? form.defName;
            abilityDef.description = form.description ?? "";

            // 아이콘: gizmoIconPathEnter 사용 (없으면 기본 아이콘)
            if (!string.IsNullOrEmpty(form.gizmoIconPathEnter))
            {
                abilityDef.iconPath = form.gizmoIconPathEnter;
            }

            // 쿨다운/신경열 없음
            abilityDef.cooldownTicksRange = new FloatRange(0f, 0f);
            abilityDef.hostile = false;

            // 자기변신 VerbProperties 설정
            abilityDef.verbProperties = new VerbProperties
            {
                verbClass = typeof(Verb_CastAbility),
                range = 0f,
                warmupTime = 0f,
                targetParams = new TargetingParameters
                {
                    canTargetSelf = true,
                    canTargetPawns = false,
                    canTargetBuildings = false,
                }
            };

            // 카테고리 설정 (SSF_Shapeshift)
            var category = DefDatabase<AbilityCategoryDef>.GetNamedSilentFail("SSF_Shapeshift");
            if (category != null)
                abilityDef.category = category;

            // CompAbilityEffect_ShiftTarget 연결
            var compProps = new Comps.CompProperties_AbilityShiftTarget();
            compProps.formDefName = form.defName;
            compProps.successChance = 1.0f;

            abilityDef.comps = new List<AbilityCompProperties> { compProps };

            return abilityDef;
        }
    }
}
