// ShapeshifterFramework | Patches | Patch_TooltipUtility_ShotCalculationTipString.cs
// 목적 : 손에 든 무기가 없더라도 변신 폼 자체가 원거리 공격(Verb)을 가졌다면, 타겟에 마우스를 올렸을 때 명중률 툴팁이 정상적으로 출력되도록 수정.
// 용도 : Postfix에서 폼의 원거리 Verb를 탐색하여 명중률(ShotReport)을 계산해 텍스트에 추가하며, 비폭력(Non-violent) 결격 폰일 경우 계산 중 발생하는 에러를 막기 위해 조기 종료(return)함.

using HarmonyLib;
using RimWorld;
using ShapeshifterFramework.Utilities;
using System;
using System.Text;
using Verse;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch(typeof(TooltipUtility), nameof(TooltipUtility.ShotCalculationTipString))]
    public static class Patch_TooltipUtility_ShotCalculationTipString
    {
        public static void Postfix(Thing target, ref string __result)
        {
            try
            {
                // 바닐라 결과 있으면 유지
                if (!string.IsNullOrEmpty(__result)) return;

                var sel = Find.Selector?.SingleSelectedThing as Pawn;
                if (sel == null || sel == target) return;

                // 플레이어 조종 + 드래프트 상태만 (비플레이어 폰이면 스킵)
                if (!sel.IsPlayerControlled || !sel.Drafted) return;

                // 비폭력 Pawn이면 계산 스킵
                if (sel.WorkTagIsDisabled(WorkTags.Violent)) return;

                if (!ShapeshiftRegistry.TryGet(sel, out var comp, out var form)) return;
                var vt = comp.ShapeshiftVerbTracker;
                if (vt == null) return;

                Verb picked = null;
                var verbs = vt.AllVerbs;
                for (int i = 0; i < verbs.Count; i++)
                {
                    var v = verbs[i];
                    if (v == null || v.verbProps == null) continue;
                    if (!v.verbProps.Ranged) continue;
                    if (!v.Available()) continue;

                    // 시야/사거리 체크
                    if (!v.CanHitTarget(target)) continue;

                    if (v.caster == null) v.caster = sel;
                    picked = v;
                    break;
                }
                if (picked == null) return;

                // 변신 폼의 race에서 ShootingAccuracyPawn 스탯이 비활성이면 HitReport 계산 불가
                if (StatDefOf.ShootingAccuracyPawn.Worker.IsDisabledFor(sel)) return;

                var sb = new StringBuilder();
                sb.Append("ShotBy".Translate(sel.LabelShort, sel) + ": ");

                if (picked.CanHitTarget(target))
                {
                    var report = ShotReport.HitReportFor(picked.caster, picked, target);
                    sb.Append(report.GetTextReadout());
                }
                else
                {
                    sb.AppendLine("CannotHit".Translate());
                }

                // 야생동물 격분 경고
                if (target is Pawn tp && tp.Faction == null && !tp.InAggroMentalState && tp.AnimalOrWildMan())
                {
                    float chance = PawnUtility.GetManhunterOnDamageChance(tp, sel);
                    if (chance > 0f)
                    {
                        sb.AppendLine();
                        sb.AppendLine(string.Format("{0}: {1}", "ManhunterPerHit".Translate(), chance.ToStringPercent()));
                    }
                }

                __result = sb.ToString();
            }
            catch (Exception e)
            {
                ShapeshiftDiagnostics.Info($"ShotCalcTip exception: {e.Message}");
            }
        }
    }
}
