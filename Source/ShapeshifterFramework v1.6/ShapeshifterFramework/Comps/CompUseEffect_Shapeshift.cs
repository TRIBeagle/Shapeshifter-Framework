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

        // CompTargetable 1회 캐시 — CanBeUsedBy(FloatMenu/hover 빈번 호출)·DoEffect의 반복 선형 탐색 방지
        private CompTargetable _targetable;
        private bool _targetableResolved;
        private CompTargetable Targetable
        {
            get
            {
                if (!_targetableResolved) { _targetable = parent.GetComp<CompTargetable>(); _targetableResolved = true; }
                return _targetable;
            }
        }

        /// <summary>사용 가능 여부 판정. 이데올로기 금지, 사용자/대상 변신 중이면 차단.
        /// CompTargetable이 있는 타인변신 아이템은 사용자 변신 상태와 무관하게 사용 가능.</summary>
        public override AcceptanceReport CanBeUsedBy(Pawn pawn)
        {
            if (ShapeshiftEligibility.IsIdeologyForbidden(pawn))
                return "SSF_GizmoDisabled_IdeologyForbidden".Translate();

            var targetable = Targetable;

            // 자기변신 아이템(CompTargetable 없음): 사용자 본인이 변신 중이면 차단 (allowedFromForms 예외)
            if (targetable == null && ShapeshiftEligibility.IsAlreadyTransformed(pawn))
            {
                if (!ShapeshiftEligibility.IsFormTransitionAllowed(pawn, Props.allowedFromForms))
                    return ShapeshiftEligibility.AlreadyTransformedReason(pawn);
            }

            // 타인변신 아이템: 대상이 이미 변신 중이면 차단 (Job 실행 시점에서만 유효)
            if (targetable != null)
            {
                var jobTarget = pawn.CurJob?.targetB.Thing as Pawn;
                if (jobTarget != null && ShapeshiftEligibility.IsAlreadyTransformed(jobTarget))
                {
                    // 판정 메서드이므로 Messages.Message 부수효과 없이 거부 사유만 반환 (UI가 reason을 표출)
                    return ShapeshiftEligibility.AlreadyTransformedReason(jobTarget);
                }
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
            var targetable = Targetable;
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

            // 대상이 이미 변신 중이면 차단 (CanBeUsedBy 우회 시 방어, allowedFromForms 예외)
            if (ShapeshiftEligibility.IsAlreadyTransformed(target)
                && !ShapeshiftEligibility.IsFormTransitionAllowed(target, Props.allowedFromForms))
            {
                Messages.Message(ShapeshiftEligibility.AlreadyTransformedReason(target), target, MessageTypeDefOf.RejectInput, false);
                return;
            }

            // HediffDef 기반 변신 진입
            if (Props.hediffDef == null)
            {
                Log.Error("[SSF] CompUseEffect_Shapeshift: hediffDef is not specified.");
                return;
            }
            ShapeshiftCoreUtility.GiveShiftHediff(target, Props.hediffDef);
        }
    }
}
