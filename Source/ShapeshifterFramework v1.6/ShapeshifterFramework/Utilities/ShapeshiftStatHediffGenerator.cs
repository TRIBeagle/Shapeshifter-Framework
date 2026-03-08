// ShapeshifterFramework | Utilities | ShapeshiftStatHediffGenerator.cs
// 목적 : ShapeshiftFormDef에 정의된 스탯 및 능력치 변동 데이터를 바닐라 시스템이 인식할 수 있도록 런타임에 동적 HediffDef로 자동 생성함.
// 용도 : 세이브 파일 파괴를 막기 위해 게임 로딩 극초반(ResolveReferences)에 호출되어, 바닐라 엔진이 알아서 안전한 ShortHash를 부여하도록 유도함.

using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace ShapeshifterFramework.Utilities
{
    /// <summary>ShapeshiftFormDef별 동적 HediffDef 생성기.</summary>
    internal static class ShapeshiftStatHediffGenerator
    {
        // 중복 생성 방지용
        private static readonly HashSet<string> _generated = new HashSet<string>();

        /// <summary>단일 폼에 대해 동적 HediffDef 생성. 중복 호출 안전.</summary>
        internal static void TryGenerateFor(ShapeshiftFormDef form)
        {
            if (form == null) return;

            string defName = "SSF_FormStats_" + form.defName;
            if (_generated.Contains(defName)) return;

            // 다른 모드의 동일 defName 중복 Add 방어
            var existing = DefDatabase<HediffDef>.GetNamedSilentFail(defName);
            if (existing != null)
            {
                form.generatedStatHediff = existing;
                _generated.Add(defName);
                return;
            }

            var hediffDef = CreateHediffDef(form, defName);
            if (hediffDef == null) return;

            // DefDatabase에만 쓱 밀어 넣으면, 바닐라 엔진이 알아서 ShortHash를 부여함!
            DefDatabase<HediffDef>.Add(hediffDef);

            form.generatedStatHediff = hediffDef;
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
                if (form == null || form.generatedStatHediff != null) continue;

                int before = _generated.Count;
                TryGenerateFor(form);
                if (_generated.Count > before) generated++;
            }

            if (generated > 0)
                Log.Message($"[SSF] Generated {generated} additional stat hediff(s) for shapeshift forms.");
        }

        private static HediffDef CreateHediffDef(ShapeshiftFormDef form, string defName)
        {
            var hediffDef = new HediffDef();

            hediffDef.defName = defName;
            hediffDef.label = "SSF_Hediff_FormLabel".Translate(form.label ?? form.defName);
            hediffDef.description = "SSF_Hediff_FormDesc".Translate(form.label ?? form.defName);
            hediffDef.hediffClass = typeof(Hediffs.Hediff_ShapeshiftForm);

            hediffDef.defaultLabelColor = new UnityEngine.Color(0.35f, 0.75f, 0.35f);
            hediffDef.isBad = false;
            hediffDef.scenarioCanAdd = false;
            hediffDef.maxSeverity = 1f;
            hediffDef.initialSeverity = 0.5f;

            hediffDef.makesSickThought = false;
            hediffDef.tendable = false;
            hediffDef.makesAlert = false;

            var stage = new HediffStage();
            stage.becomeVisible = true;

            if (form.statOffsets != null && form.statOffsets.Count > 0)
                stage.statOffsets = form.statOffsets;

            if (form.statFactors != null && form.statFactors.Count > 0)
                stage.statFactors = form.statFactors;

            if (form.capMods != null && form.capMods.Count > 0)
                stage.capMods = form.capMods;

            hediffDef.stages = new List<HediffStage>(1) { stage };

            return hediffDef;
        }
    }
}