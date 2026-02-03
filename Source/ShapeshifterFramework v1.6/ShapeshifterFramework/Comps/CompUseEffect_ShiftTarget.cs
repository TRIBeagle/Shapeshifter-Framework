// ShapeshifterFramework | Comps | CompUseEffect_ShiftTarget.cs
// 목적   : 아이템 사용 시 Pawn을 변신시키는 실행 컴포넌트.
// 용도   : <see cref="CompProperties_UseEffect_ShiftTarget"/> 설정을 참조하여 대상 Pawn을 변신 처리한다.
// 변경   : 2025-09-22 v1.0 — 프로젝트 주석 규칙 적용(주석만 정리, 로직 변경 없음).

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

            // 예시 구현:
            // - 사용자 앞 셀에 Pawn이 있으면 그 Pawn을 대상
            // - 없으면 자기 자신을 대상
            var map = user.Map;
            var c = user.Position;
            var pawn = (c.GetFirstPawn(map) ?? user);

            ShapeshiftTargetUtility.TryShiftPawn(pawn, Props.formDefName, Props.successChance);
        }
    }
}
