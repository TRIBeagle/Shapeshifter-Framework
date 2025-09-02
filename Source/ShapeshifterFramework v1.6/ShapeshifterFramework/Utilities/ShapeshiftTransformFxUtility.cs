// .NET Framework 4.8 / C# 7.3
// 목적: 외부(CompShapeshifter 등)에서 간단히 호출하는 프런트 API.
// - 폼 설정의 딜레이/쿨다운을 읽어 TransformFxRunner에 enqueue.
// - 기존 PlayEnterFx/PlayExitFx 시그니처 그대로 유지(내부가 큐로 변경).

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
