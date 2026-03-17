// ShapeshifterFramework | Comps | CompUseEffect_Shapeshift.cs
// 목적 : 물약, 아티팩트 등 아이템(Use/Ingest) 사용 시 대상 폰을 변신시키는 실행 로직.
// 용도 : 같은 ThingDef에 CompTargetable(예: CompTargetable_SinglePawn)이 함께 정의되어 있으면
//        플레이어가 클릭으로 선택한 대상 Pawn을 변신시키고, CompTargetable이 없으면 사용자 자신을 변신시킴.

using RimWorld;
using ShapeshifterFramework.Hediffs;
using ShapeshifterFramework.Utilities;
using Verse;

namespace ShapeshifterFramework.Comps
{
    /// <summary>아이템 사용 시 대상 Pawn을 폼으로 변신시키는 효과.</summary>
    public class CompUseEffect_Shapeshift : CompUseEffect
    {
        public CompProperties_UseEffect_Shapeshift Props => (CompProperties_UseEffect_Shapeshift)props;

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

            // hediffDef가 지정되면 HediffDef 경로 우선 사용
            if (Props.hediffDef != null)
            {
                ShapeshiftCoreUtility.ApplyShift(target, Props.hediffDef, Props.successChance);
            }
            else
            {
                // formDefName 폴백
                ShapeshiftTargetUtility.TryShiftPawn(target, Props.formDefName, Props.successChance);
            }
        }
    }
}
