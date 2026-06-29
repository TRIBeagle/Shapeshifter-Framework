// ShapeshifterFramework | Hediffs | HediffComp_ShapeshiftCore.Tick.cs
// 목적 : 매 틱 처리 — 지연 초기화, 타이머/사망/downed/sustain 검사, 앰비언트 VFX, VerbTracker 틱.
// 용도 : CompPostTick에서 변신 상태 유지 조건을 주기적으로 검사하고,
//        조건 미충족 시 자동 해제. 인스펙터 UI 문자열도 담당.

using RimWorld;
using ShapeshifterFramework.Utilities;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse.Sound;
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
                    ShapeshiftDiagnostics.Info("HediffComp_ShapeshiftCore: needsInit but no formDef. Use ApplyForm() for dynamic forms.");
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
                    if (CheckSourceItemLost(pawn)) return;
                    if (!CheckSustainConditions(pawn, out string failReason) && !(pawn.stances?.curStance is Stance_Warmup))
                    {
                        string msg = failReason != null
                            ? "SSF_Message_RevertDueToConditionLostDetail".Translate(pawn.LabelShortCap, failReason)
                            : "SSF_Message_RevertDueToConditionLost".Translate(pawn.LabelShortCap);
                        Messages.Message(msg, pawn, MessageTypeDefOf.NegativeEvent, false);
                        RemoveForm();
                        return;
                    }
                }

                TickAmbientVfx(pawn);

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

        /// <summary>소스 아이템(변신 트리거 장비) 유실 검사. 유실 시 RemoveForm 호출 후 true 반환.</summary>
        private bool CheckSourceItemLost(Pawn pawn)
        {
            if (this.sourceItems == null || this.sourceItems.Count == 0) return false;
            for (int i = this.sourceItems.Count - 1; i >= 0; i--)
            {
                Thing item = this.sourceItems[i];
                if (item == null) continue;
                bool isEquipped =
                    (item is Apparel ap && pawn.apparel != null && pawn.apparel.Contains(ap)) ||
                    (item is ThingWithComps tc && pawn.equipment != null && pawn.equipment.Contains(tc));
                if (item.Destroyed || !isEquipped)
                {
                    ShapeshiftDiagnostics.Info("Source item lost. Forcing shapeshift revert.");
                    Messages.Message("SSF_Message_RevertDueToItemLost".Translate(pawn.LabelShortCap, item.Label), pawn, MessageTypeDefOf.NegativeEvent, false);
                    RemoveForm();
                    return true;
                }
            }
            return false;
        }

        /// <summary>앰비언트 VFX 틱 처리 (이펙터 + 주기적 Fleck).</summary>
        private void TickAmbientVfx(Pawn pawn)
        {
            // 방어: 호출 직전 RemoveForm이 동기 트리거됐다면 currentForm이 null일 수 있음
            if (currentForm == null) return;
            if ((currentForm.ambientEffecter == null && currentForm.ambientFleck == null && currentForm.ambientSound == null)
                || !pawn.Spawned) return;
            if (currentForm.ambientEffecter != null)
            {
                if (ambientEffecterInstance == null)
                    ambientEffecterInstance = currentForm.ambientEffecter.Spawn();
                ambientEffecterInstance.EffectTick(pawn, pawn);
            }
            if (Find.TickManager.TicksGame >= ambientFleckNextTick)
            {
                if (currentForm.ambientFleck != null)
                {
                    FleckMaker.Static(pawn.DrawPos, pawn.Map, currentForm.ambientFleck,
                        Mathf.Max(0.01f, currentForm.ambientFleckScale));
                }
                if (currentForm.ambientSound != null)
                {
                    // Spawned 가드(위) 하라 Position/Map 안전 — Fleck(pawn.DrawPos/pawn.Map)과 좌표 소스 통일
                    currentForm.ambientSound.PlayOneShot(SoundInfo.InMap(new TargetInfo(pawn.Position, pawn.Map)));
                }
                ambientFleckNextTick = Find.TickManager.TicksGame + Mathf.Max(1, currentForm.ambientFleckIntervalTicks);
            }
        }

        /// <summary>sustain 조건 충족 여부 검사. Props 오버라이드 반영. 실패 시 failReason에 구체적 사유.</summary>
        private bool CheckSustainConditions(Pawn pawn, out string failReason)
        {
            failReason = null;
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
                if (!apparelMet) failReason = "SSF_Sustain_Apparels".Translate();
                else if (!weaponMet) failReason = "SSF_Sustain_Weapons".Translate();
                else if (!hediffMet) failReason = "SSF_Sustain_Hediffs".Translate();
                else if (!geneMet) failReason = "SSF_Sustain_Genes".Translate();
                return apparelMet && weaponMet && hediffMet && geneMet;
            }

            // Any 모드: 요구사항이 있는 카테고리 중 하나라도 충족하면 유지
            if (hasApparels && CheckSustainApparels(pawn, apparels)) return true;
            if (hasWeapons && CheckSustainWeapons(pawn, weapons)) return true;
            if (hasHediffs && CheckSustainHediffs(pawn, hediffs)) return true;
            if (hasGenes && CheckSustainGenes(pawn, genes)) return true;
            failReason = "SSF_Sustain_AllCategories".Translate();
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
