// .NET Framework 4.8 / C# 7.3
using Verse;
using Verse.Sound;

namespace ShapeshifterFramework.Utilities
{
    /// <summary>
    /// 보이스 재생 시 폼/캐시를 우선적으로 선택하는 헬퍼.
    /// - 변신 중일 때만 폼 값을 반환.
    /// - 변신 해제/Despawn/사망 시 캐시는 Comp에서 정리(별도 구현).
    /// </summary>
    internal static class ShapeshiftVoiceHelper
    {
        public static bool TryGetCall(Pawn pawn, out SoundDef def)
        {
            def = null;
            if (pawn == null) return false;
            return ShapeshiftRuntimeCaches.CallByPawn.TryGetValue(pawn, out def) && def != null;
        }

        public static bool TryGetWounded(Pawn pawn, out SoundDef def)
        {
            def = null;
            if (pawn == null) return false;
            return ShapeshiftRuntimeCaches.WoundedByPawn.TryGetValue(pawn, out def) && def != null;
        }

        public static bool TryGetDeath(Pawn pawn, out SoundDef def)
        {
            def = null;
            if (pawn == null) return false;
            return ShapeshiftRuntimeCaches.DeathByPawn.TryGetValue(pawn, out def) && def != null;
        }
        public static bool TryGetAngry(Pawn pawn, out SoundDef def)
        {
            def = null;
            if (pawn == null) return false;
            return ShapeshiftRuntimeCaches.AngryByPawn.TryGetValue(pawn, out def) && def != null;
        }

        /// <summary>
        /// 바닐라가 유전자/뮤턴트 오버라이드 시 pitch/volume을 CurLifeStage 기준으로 리셋하므로
        /// 폼 사운드도 동일 규칙을 적용: pitch/volume = CurLifeStage 값 × 외부 volumeFactor.
        /// </summary>
        public static void PlayOneShotAt(Pawn pawn, SoundDef def, float volumeFactor)
        {
            if (pawn == null || def == null) return;
            var info = SoundInfo.InMap(new TargetInfo(pawn.PositionHeld, pawn.MapHeld));
            info.pitchFactor = pawn.ageTracker.CurLifeStage.voxPitch;
            info.volumeFactor = pawn.ageTracker.CurLifeStage.voxVolume * volumeFactor;
            def.PlayOneShot(info);
        }
    }
}
