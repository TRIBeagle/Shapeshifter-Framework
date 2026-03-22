// ShapeshifterFramework | Hediffs | HediffComp_ShapeshiftCore.Tick.cs
// 목적 : 매 틱 처리 — 지연 초기화, 타이머/사망/downed/sustain 검사, 앰비언트 VFX, VerbTracker 틱.
// 용도 : CompPostTick에서 변신 상태 유지 조건을 주기적으로 검사하고,
//        조건 미충족 시 자동 해제. 인스펙터 UI 문자열도 담당.

using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ShapeshifterFramework.Hediffs
{
    public partial class HediffComp_ShapeshiftCore
    {
        #region Ticking/Inspect

        /// <summary>sustain 조건 검사 주기 (틱). 60틱 ≈ 1초.</summary>
        private const int SustainCheckIntervalTicks = 60;

        /// <summary>매 틱: 지연 초기화 + 타이머/사망/downed/sustain 검사/VerbTracker 처리.</summary>
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            var pawn = Pawn;
            if (pawn == null) return;

            // 지연 초기화: CompPostPostAdd에서 needsInit=true → 첫 Tick에서 ApplyForm 실행
            if (needsInit)
            {
                needsInit = false;

                var formDef = currentForm ?? Props.formDef;
                if (formDef != null)
                {
                    ApplyForm(formDef);
                }
                else
                {
                    ShapeshiftDiagnostics.Info("HediffComp_ShapeshiftCore: needsInit but no formDef. Use SetFormDef() for dynamic forms.");
                }
                return;
            }

            // 로드 후 장비 참조 복원 (상세 로직은 ExposeData.cs의 ResolveGearFromIds)
            if (needsGearResolve)
            {
                ResolveGearFromIds(pawn);
            }

            if (isTransformed && currentForm != null)
            {
                if (pawn.Dead)
                {
                    RemoveForm();
                    return;
                }
                if (ResolvedRevertOnDowned && pawn.Downed)
                {
                    RemoveForm();
                    return;
                }

                var resolvedDuration = ResolvedDurationTicks;
                if (resolvedDuration.HasValue && resolvedDuration.Value > 0)
                {
                    transformTimer--;
                    if (transformTimer <= 0) { RemoveForm(); return; }
                }

                // 주기적 유지 요건(sustain) 검사
                if (pawn.IsHashIntervalTick(SustainCheckIntervalTicks))
                {
                    // 코어 아이템 유실 검사
                    if (this.sourceItems != null && this.sourceItems.Count > 0)
                    {
                        for (int i = this.sourceItems.Count - 1; i >= 0; i--)
                        {
                            Thing item = this.sourceItems[i];
                            if (item == null) continue;

                            bool isEquipped =
                                (item is Apparel ap && pawn.apparel != null && pawn.apparel.Contains(ap)) ||
                                (item is ThingWithComps tc && pawn.equipment != null && pawn.equipment.Contains(tc));

                            if (item.Destroyed || item.Spawned || !isEquipped)
                            {
                                ShapeshiftDiagnostics.Info("Source item lost. Forcing shapeshift revert.");
                                Messages.Message("SSF_Message_RevertDueToItemLost".Translate(pawn.LabelShortCap, item.Label), pawn, MessageTypeDefOf.NegativeEvent, false);
                                RemoveForm();
                                return;
                            }
                        }
                    }

                    // sustain 조건 검사 (Props 오버라이드 반영)
                    if (!CheckSustainConditions(pawn)
                        && !(pawn.stances?.curStance is Stance_Warmup))
                    {
                        Messages.Message("SSF_Message_RevertDueToConditionLost".Translate(pawn.LabelShortCap), pawn, MessageTypeDefOf.NegativeEvent, false);
                        RemoveForm();
                        return;
                    }
                }

                // 앰비언트 VFX
                if ((currentForm.ambientEffecter != null || currentForm.ambientFleck != null)
                    && pawn.Spawned)
                {
                    if (currentForm.ambientEffecter != null)
                    {
                        if (ambientEffecterInstance == null)
                            ambientEffecterInstance = currentForm.ambientEffecter.Spawn();
                        ambientEffecterInstance.EffectTick(pawn, pawn);
                    }
                    if (currentForm.ambientFleck != null
                        && Find.TickManager.TicksGame >= ambientFleckNextTick)
                    {
                        FleckMaker.Static(pawn.DrawPos, pawn.Map, currentForm.ambientFleck,
                            Mathf.Max(0.01f, currentForm.ambientFleckScale));
                        ambientFleckNextTick = Find.TickManager.TicksGame
                            + Mathf.Max(1, currentForm.ambientFleckIntervalTicks);
                    }
                }

                try
                {
                    ShapeshiftVerbTracker?.VerbsTick();
                }
                catch (Exception ex)
                {
                    if (!verbTickErrorLogged)
                    {
                        Log.Error($"[SSF] VerbsTick error on pawn {pawn?.Name} (Logging once to prevent spam): {ex}");
                        verbTickErrorLogged = true;
                    }
                }
            }
        }

        /// <summary>sustain 조건 충족 여부 검사. Props 오버라이드 반영.</summary>
        private bool CheckSustainConditions(Pawn pawn)
        {
            var apparels = ResolvedSustainApparels;
            var weapons = ResolvedSustainWeapons;
            var hediffs = ResolvedSustainHediffs;
            var genes = ResolvedSustainGenes;

            bool hasApparels = apparels != null && apparels.Count > 0;
            bool hasWeapons = weapons != null && weapons.Count > 0;
            bool hasHediffs = hediffs != null && hediffs.Count > 0;
            bool hasGenes = ModsConfig.BiotechActive && genes != null && genes.Count > 0;

            if (!hasApparels && !hasWeapons && !hasHediffs && !hasGenes) return true;

            var mode = ResolvedSustainMode;

            if (mode == SustainMode.All)
            {
                // All 모드: 모든 카테고리 충족 필요
                bool apparelMet = !hasApparels || CheckSustainApparels(pawn, apparels);
                bool weaponMet = !hasWeapons || CheckSustainWeapons(pawn, weapons);
                bool hediffMet = !hasHediffs || CheckSustainHediffs(pawn, hediffs);
                bool geneMet = !hasGenes || CheckSustainGenes(pawn, genes);
                return apparelMet && weaponMet && hediffMet && geneMet;
            }

            // Any 모드: 요구사항이 있는 카테고리 중 하나라도 충족하면 유지
            if (hasApparels && CheckSustainApparels(pawn, apparels)) return true;
            if (hasWeapons && CheckSustainWeapons(pawn, weapons)) return true;
            if (hasHediffs && CheckSustainHediffs(pawn, hediffs)) return true;
            if (hasGenes && CheckSustainGenes(pawn, genes)) return true;
            return false;
        }

        // sustain 체크용 재활용 HashSet — 재진입 안전을 위해 ThreadStatic
        [ThreadStatic] private static HashSet<ThingDef> _tmpSustainDefs;

        private static bool CheckSustainApparels(Pawn pawn, List<ThingDef> required)
        {
            if (pawn.apparel == null) return false;
            if (_tmpSustainDefs == null) _tmpSustainDefs = new HashSet<ThingDef>();
            var worn = pawn.apparel.WornApparel;
            _tmpSustainDefs.Clear();
            for (int j = 0; j < worn.Count; j++)
            {
                if (worn[j] == null) continue;
                _tmpSustainDefs.Add(worn[j].def);
            }
            for (int i = 0; i < required.Count; i++)
            {
                if (!_tmpSustainDefs.Contains(required[i])) return false;
            }
            return true;
        }

        private static bool CheckSustainWeapons(Pawn pawn, List<ThingDef> required)
        {
            if (pawn.equipment == null) return false;
            if (_tmpSustainDefs == null) _tmpSustainDefs = new HashSet<ThingDef>();
            var eqs = pawn.equipment.AllEquipmentListForReading;
            _tmpSustainDefs.Clear();
            for (int j = 0; j < eqs.Count; j++)
            {
                if (eqs[j] == null) continue;
                _tmpSustainDefs.Add(eqs[j].def);
            }
            for (int i = 0; i < required.Count; i++)
            {
                if (!_tmpSustainDefs.Contains(required[i])) return false;
            }
            return true;
        }

        private static bool CheckSustainHediffs(Pawn pawn, List<HediffDef> required)
        {
            if (pawn.health == null) return false;
            for (int i = 0; i < required.Count; i++)
            {
                if (pawn.health.hediffSet.GetFirstHediffOfDef(required[i]) == null)
                    return false;
            }
            return true;
        }

        private static bool CheckSustainGenes(Pawn pawn, List<GeneDef> required)
        {
            if (pawn.genes == null) return false;
            for (int i = 0; i < required.Count; i++)
            {
                if (pawn.genes.GetGene(required[i]) == null)
                    return false;
            }
            return true;
        }

        // 남은 변신 틱
        public int RemainingShapeshiftTicks
        {
            get
            {
                int t = transformTimer;
                return t > 0 ? t : 0;
            }
        }

        /// <summary>헤디프 툴팁 추가 문자열 (1.6: CompTipStringExtra 프로퍼티).</summary>
        public override string CompTipStringExtra
        {
            get
            {
                if (!isTransformed || currentForm == null)
                    return null;

                var resolvedDuration = ResolvedDurationTicks;
                if (!resolvedDuration.HasValue || resolvedDuration.Value <= 0)
                    return "SSF_Inspect_Permanent".Translate();

                int remain = transformTimer;
                if (remain <= 0) return null;

                string timeStr = GenDate.ToStringTicksToPeriod(remain, allowSeconds: false, shortForm: false);
                return "SSF_Inspect_Remaining".Translate(timeStr);
            }
        }

        #endregion
    }
}
