// ShapeshifterFramework | Utilities | ShapeshiftTransformFxUtility.cs
// 목적 : HediffComp_ShapeshiftCore 등에서 FX 재생을 요청할 때 사용하는 간편한 정적(Static) API.
// 용도 : 대상 폼의 설정(Delay, Cooldown)을 읽어와 ShapeshiftTransformFxRunner의 실행 큐(Enqueue)에 이펙트 발동을 스케줄링함.

using Verse;

namespace ShapeshifterFramework.Utilities
{
    public static class ShapeshiftTransformFxUtility
    {
        public static void PlayEnterFx(Pawn pawn, ShapeshiftFormDef form)
        {
            if (pawn == null || form == null) return;
            var runner = ShapeshiftTransformFxRunner.Instance;
            if (runner == null) return;

            ShapeshiftTransformFxRunner.Enqueue(
                pawn, form, isEnter: true,
                delayTicks: form.transformEnterFxDelayTicks,
                cooldownTicks: form.transformFxCooldownTicks);
        }

        public static void PlayExitFx(Pawn pawn, ShapeshiftFormDef form)
        {
            if (pawn == null || form == null) return;
            var runner = ShapeshiftTransformFxRunner.Instance;
            if (runner == null) return;

            ShapeshiftTransformFxRunner.Enqueue(
                pawn, form, isEnter: false,
                delayTicks: form.transformExitFxDelayTicks,
                cooldownTicks: form.transformFxCooldownTicks);
        }
    }
}
