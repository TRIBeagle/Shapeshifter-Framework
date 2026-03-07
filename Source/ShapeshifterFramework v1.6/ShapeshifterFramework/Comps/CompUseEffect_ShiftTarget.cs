// ShapeshifterFramework | Comps | CompUseEffect_ShiftTarget.cs
// 목적 : 물약, 아티팩트 등 아이템(Use/Ingest) 사용 시 대상 폰을 변신시키는 실행 로직.
// 용도 : 아이템 사용(DoEffect) 시점의 위치와 방향(Rotation)을 계산하여, '사용자가 바라보는 바로 앞 칸(FacingCell)'에 다른 폰이 있다면 해당 대상을, 없다면 사용자 자신을 타겟으로 지정하여 TryShiftPawn을 호출함.
// 주의 : 단순 자가 버프가 아니라 방향 기반의 타겟팅 로직이 포함되어 있으므로, 아이템 사용 시 폰의 위치와 바라보는 방향이 중요하게 작용함.

using RimWorld;
using ShapeshifterFramework.Utilities;
using Verse;

namespace ShapeshifterFramework.Comps
{
    /// <summary>
    /// 아이템 사용 효과: 대상 Pawn을 지정된 폼으로 변신시킨다.
    /// - Props로 지정된 <see cref="CompProperties_UseEffect_ShiftTarget.formDefName"/> 과
    ///   <see cref="CompProperties_UseEffect_ShiftTarget.successChance"/> 를 참조한다.
    /// - 기본 구현에서는 자기 위치/앞 칸 Pawn을 찾거나, 없으면 자기 자신을 대상으로 한다.
    /// </summary>
    public class CompUseEffect_ShiftTarget : CompUseEffect
    {
        /// <summary>
        /// 속성 캐스팅 접근자.
        /// </summary>
        public CompProperties_UseEffect_ShiftTarget Props => (CompProperties_UseEffect_ShiftTarget)props;

        /// <summary>
        /// 아이템 사용 시 효과를 실행한다.  
        /// RimWorld 표준 컨텍스트(사용자 Pawn, 위치, Map 등)에 따라 Pawn을 선택하여 변신 처리한다.
        /// </summary>
        /// <param name="user">아이템을 사용한 Pawn</param>
        public override void DoEffect(Pawn user)
        {
            base.DoEffect(user);

            // [수정] 카라반/수송 포드 등 오프맵 상태에서 Map이 null일 수 있으므로 방어 처리
            var map = user.Map;
            Pawn pawn = null;

            if (map != null)
            {
                // user.Position이 아닌 '사용자가 바라보는 앞 칸(FacingCell)'을 확인
                IntVec3 targetCell = user.Position + user.Rotation.FacingCell;
                // 앞 칸에 폰이 있으면 그 폰을, 없으면 자기 자신을 대상으로 지정
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
