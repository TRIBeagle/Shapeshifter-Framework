using RimWorld;
using ShapeshifterFramework.Utilities;
using Verse;

namespace ShapeshifterFramework.Comps
{
    public class CompUseEffect_ShiftTarget : CompUseEffect
    {
        public CompProperties_UseEffect_ShiftTarget Props => (CompProperties_UseEffect_ShiftTarget)props;

        // 사용 대상은 RimWorld 표준: 사용된 Thing의 위치/사용자·대상 등 컨텍스트로 판단.
        public override void DoEffect(Pawn user)
        {
            base.DoEffect(user);

            // 예시 1) 자기 앞 셀의 Pawn 한 명을 변신
            var map = user.Map;
            var c = user.Position; // 필요에 맞게 선택(사용 UI에서 직접 pawn 지정이 가능하면 그 pawn 사용)
            var pawn = (c.GetFirstPawn(map) ?? user); // 예: 없으면 자기 자신(데모용)

            ShapeshiftTargetUtility.TryShiftPawn(pawn, Props.formDefName, Props.successChance);
        }
    }
}
