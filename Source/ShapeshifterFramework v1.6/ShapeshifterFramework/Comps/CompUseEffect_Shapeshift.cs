// ShapeshifterFramework | Comps | CompUseEffect_Shapeshift.cs
// 목적 : 물약, 아티팩트 등 아이템(Use/Ingest) 사용 시 대상 폰을 변신시키는 실행 로직.
// 용도 : 같은 ThingDef에 CompTargetable(예: CompTargetable_SinglePawn)이 함께 정의되어 있으면
//        플레이어가 클릭으로 선택한 대상 Pawn을 변신시키고, CompTargetable이 없으면 사용자 자신을 변신시킴.
// 주의 : 이데올로기 금지 시 사용 차단, 이미 다른 폼 변신 중이면 사용 차단.

using RimWorld;
using ShapeshifterFramework.Utilities;
using Verse;

namespace ShapeshifterFramework.Comps
{
    /// <summary>아이템 사용 시 대상 Pawn을 폼으로 변신시키는 효과.</summary>
    public class CompUseEffect_Shapeshift : CompUseEffect
    {
        public CompProperties_UseEffect_Shapeshift Props => (CompProperties_UseEffect_Shapeshift)props;

        /// <summary>사용 가능 여부 판정. 이데올로기 금지, 사용자/대상 변신 중이면 차단.</summary>
        public override AcceptanceReport CanBeUsedBy(Pawn pawn)
        {
            if (ShapeshiftEligibility.IsIdeologyForbidden(pawn))
                return "IdeoligionForbids".Translate();

            // 사용자 본인이 이미 변신 중이면 차단
            if (ShapeshiftEligibility.IsAlreadyTransformed(pawn))
                return "SSF_Menu_Blocked".Translate();

            // 타겟 대상이 이미 변신 중이면 차단 (Job 실행 시점에서만 유효)
            var targetable = parent.GetComp<CompTargetable>();
            if (targetable != null)
            {
                var jobTarget = pawn.CurJob?.targetB.Thing as Pawn;
                if (jobTarget != null && ShapeshiftEligibility.IsAlreadyTransformed(jobTarget))
                    return "SSF_Menu_Blocked".Translate();
            }

            return base.CanBeUsedBy(pawn);
        }

        /// <summary>아이템 사용 시 변신 효과 실행.</summary>
        /// <param name="user">사용 Pawn</param>
        public override void DoEffect(Pawn user)
        {
            base.DoEffect(user);

            Pawn target = null;

            // CompTargetable이 있으면 UseItem Job의 targetB에서 대상을 가져옴
            var targetable = parent.GetComp<CompTargetable>();
            if (targetable != null)
            {
                // 1차: UseItem Job의 targetB (RimWorld이 타겟 선택 결과를 여기에 저장)
                var jobTarget = user.CurJob?.targetB.Thing as Pawn;
                if (jobTarget != null && !jobTarget.Dead)
                {
                    target = jobTarget;
                }

                // 2차 폴백: CompTargetable.GetTargets
                if (target == null)
                {
                    foreach (var t in targetable.GetTargets(parent))
                    {
                        if (t is Pawn p && !p.Dead)
                        {
                            target = p;
                            break;
                        }
                    }
                }
            }

            // CompTargetable이 없거나 유효한 대상이 없으면 자기 자신
            if (target == null)
            {
                target = user;
            }

            // 대상이 이미 변신 중이면 차단 (CanBeUsedBy 우회 시 방어)
            if (ShapeshiftEligibility.IsAlreadyTransformed(target))
            {
                Messages.Message("SSF_Message_AlreadyTransformed".Translate(), target, MessageTypeDefOf.RejectInput, false);
                return;
            }

            // HediffDef 기반 변신 진입
            if (Props.hediffDef == null)
            {
                Log.Error("[SSF] CompUseEffect_Shapeshift: hediffDef가 지정되지 않았습니다.");
                return;
            }
            ShapeshiftCoreUtility.GiveShiftHediff(target, Props.hediffDef);
        }
    }
}
