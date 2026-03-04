// ShapeshifterFramework | Utilities | ShapeshiftVerbShootProvider.cs
// 목적 : 드래프트된(Drafted) 변신 폰이 타겟을 우클릭했을 때 나타나는 '플로트 메뉴(Float Menu)'에 폼 전용 원거리 공격 명령을 추가.
// 용도 : 자동공격 토글(Auto-Attack)이 꺼져 있더라도 플레이어가 강제로 특정 Verb를 사용해 사격(AttackStatic Job)하도록 명령을 생성하며, 발사체가 없는 버그 무기 사용 시도(NRE)를 방어함.

using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace ShapeshifterFramework.Utilities
{
    public class ShapeshiftVerbShootProvider : FloatMenuOptionProvider
    {
        protected override bool Drafted => true;
        protected override bool Undrafted => false;
        protected override bool Multiselect => false;

        public override bool Applies(FloatMenuContext context)
        {
            var p = context?.FirstSelectedPawn;
            if (p == null || !p.Drafted) return false;

            var comp = p.TryGetComp<ShapeshifterFramework.Comps.CompShapeshifter>();
            return comp != null && comp.ShapeshiftVerbTracker != null;
        }

        public override bool TargetPawnValid(Pawn target, FloatMenuContext context)
        {
            var sel = context?.FirstSelectedPawn;
            if (target == null || sel == null) return false;
            if (target == sel) return false;

            if (GenHostility.HostileTo(target, sel)) return true;
            if (target.RaceProps?.Humanlike == true) return false; // 비적대 인간형 금지
            return true; // 동물/메카 허용
        }

        public override IEnumerable<FloatMenuOption> GetOptionsFor(Pawn target, FloatMenuContext context)
        {
            var sel = context?.FirstSelectedPawn;
            if (sel == null || target == null) yield break;

            var comp = sel.TryGetComp<ShapeshifterFramework.Comps.CompShapeshifter>();
            var vt = comp?.ShapeshiftVerbTracker;
            if (vt == null) yield break;

            var verbs = vt.AllVerbs;
            for (int i = 0; i < verbs.Count; i++)
            {
                var v = verbs[i];
                if (v == null || v.verbProps == null) continue;
                if (!v.verbProps.Ranged) continue;
                if (!v.Available()) continue;

                // LaunchProjectile류는 defaultProjectile 필수 (장비 없음이므로 CompChangeableProjectile이 없음)
                if ((v is Verb_LaunchProjectile) && v.verbProps.defaultProjectile == null) continue;

                if (!v.CanHitTargetFrom(sel.Position, target)) continue;

                if (v.caster == null) v.caster = sel;

                int idx = i; // 클로저 안전
                string label = comp.GetVerbLabel(idx, v, preferToggleLabel: false);
                string menuLabel = $"{label}: {target.LabelShortCap}";

                yield return new FloatMenuOption(menuLabel, () =>
                {
                    try
                    {
                        sel.stances?.CancelBusyStanceSoft(); // 이전 시전/조준 중첩 방지

                        var job = JobMaker.MakeJob(JobDefOf.AttackStatic, target);
                        job.verbToUse = v;         // 강제: 선택한 verb만 사용
                        job.playerForced = true;   // 토글 OFF 무시
                        sel.jobs?.TryTakeOrderedJob(job, JobTag.Misc);
                    }
                    catch (Exception e)
                    {
                        Log.Error($"[SSF] Shoot provider exec failed (verb={v?.ToString() ?? "null"}): {e}");
                    }
                })
                {
                    autoTakeable = false,
                    iconThing = target
                };
            }
        }
    }
}
