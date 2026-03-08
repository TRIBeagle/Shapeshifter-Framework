// ShapeshifterFramework | Comps | CompUseEffect_ShiftTarget.cs
// 목적 : 물약, 아티팩트 등 아이템(Use/Ingest) 사용 시 대상 폰을 변신시키는 실행 로직.
// 용도 : 아이템 사용(DoEffect) 시점의 위치와 방향(Rotation)을 계산하여, '사용자가 바라보는 바로 앞 칸(FacingCell)'에 다른 폰이 있다면 해당 대상을, 없다면 사용자 자신을 타겟으로 지정하여 TryShiftPawn을 호출함.
// 주의 : 단순 자가 버프가 아니라 방향 기반의 타겟팅 로직이 포함되어 있으므로, 아이템 사용 시 폰의 위치와 바라보는 방향이 중요하게 작용함.

using RimWorld;
using ShapeshifterFramework.Utilities;
using Verse;

namespace ShapeshifterFramework.Comps
{
    /// <summary>아이템 사용 시 대상 Pawn을 폼으로 변신시키는 효과.</summary>
    public class CompUseEffect_ShiftTarget : CompUseEffect
    {
        public CompProperties_UseEffect_ShiftTarget Props => (CompProperties_UseEffect_ShiftTarget)props;

        /// <summary>아이템 사용 시 변신 효과 실행.</summary>
        /// <param name="user">사용 Pawn</param>
        public override void DoEffect(Pawn user)
        {
            base.DoEffect(user);

            // 오프맵 pawn Map null 방어
            var map = user.Map;
            Pawn pawn = null;

            if (map != null)
            {
                // 앞 칸(FacingCell) Pawn 우선, 없으면 자신
                IntVec3 targetCell = user.Position + user.Rotation.FacingCell;
                pawn = targetCell.InBounds(map) ? targetCell.GetFirstPawn(map) : null;
            }

            if (pawn == null)
            {
                pawn = user;
            }

            ShapeshiftTargetUtility.TryShiftPawn(pawn, Props.formDefName, Props.successChance);
        }
    }
}
