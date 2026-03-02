// .NET Framework 4.8 / C# 7.3
// Patch_TooltipUtility_ShotCalculationTipString.cs
// 목적: 총이 없어도 변신 폼의 원거리 Verb가 있으면 사격 명중률 툴팁을 표시
// 수정: 비폭력 Pawn일 때는 계산을 스킵해 오류 방지

using HarmonyLib;
using RimWorld;
using ShapeshifterFramework.Comps;
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
                // 바닐라가 이미 붙였으면 그대로 둠
                if (!string.IsNullOrEmpty(__result)) return;

                var sel = Find.Selector?.SingleSelectedThing as Pawn;
                if (sel == null || sel == target) return;

                // 바닐라와 동일: 플레이어 조종 + 드래프트 상태에서만
                if (sel.IsPlayerControlled && !sel.Drafted) return;

                // ★ 비폭력 Pawn이면 사격 스탯이 disable이므로 계산 자체를 스킵
                if (sel.WorkTagIsDisabled(WorkTags.Violent)) return;

                var comp = sel.TryGetComp<CompShapeshifter>();
                var vt = comp?.ShapeshiftVerbTracker;
                if (vt == null) return;

                Verb picked = null;
                var verbs = vt.AllVerbs;
                for (int i = 0; i < verbs.Count; i++)
                {
                    var v = verbs[i];
                    if (v == null || v.verbProps == null) continue;
                    if (!v.verbProps.Ranged) continue;
                    if (!v.Available()) continue;

                    // 시야/사거리 체크(히트리포트도 이 전제 필요)
                    if (!v.CanHitTarget(target)) continue;

                    if (v.caster == null) v.caster = sel;
                    picked = v;
                    break;
                }
                if (picked == null) return;

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

                // 야생동물 격분 경고(바닐라와 동일 개념)
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
