// ShapeshifterFramework | Utilities | ShapeshiftVoiceUtility.cs
// 목적 : 변신 중인 폰이 내는 소리(콜, 피격, 사망, 분노음)를 가로채어 폼 전용 사운드로 재생하기 위한 유틸리티.
// 용도 : 런타임 캐시에서 사운드 Def를 즉각적으로 가져와 바닐라의 연령대(CurLifeStage) 피치/볼륨 계수를 보정하여 출력하며, 월드폰이나 드랍팟 탑승 등 폰의 좌표(Position)가 유효하지 않은 예외 상황의 에러를 방어함.

using Verse;
using Verse.Sound;

namespace ShapeshifterFramework.Utilities
{
    /// <summary>
    /// 보이스 재생 시 폼/캐시를 우선적으로 선택하는 헬퍼.
    /// - 변신 중일 때만 폼 값을 반환.
    /// - 변신 해제/Despawn/사망 시 캐시는 Comp에서 정리(별도 구현).
    /// </summary>
    internal static class ShapeshiftVoiceUtility
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

            // 맵/좌표 방어: 월드폰/드랍팟/디스폰 타이밍 NRE 방지
            var map = pawn.MapHeld;
            if (map == null) return;
            var pos = pawn.PositionHeld;
            if (!pos.IsValid) return;

            var info = SoundInfo.InMap(new TargetInfo(pos, map));

            // CurLifeStage가 null일 수 있는 엣지 케이스 방어 (이전 최적화 유지)
            if (pawn.ageTracker != null)
            {
                var ls = pawn.ageTracker.CurLifeStage;
                info.pitchFactor = ls != null ? ls.voxPitch : 1f;
                info.volumeFactor = (ls != null ? ls.voxVolume : 1f) * volumeFactor;
            }
            else
            {
                info.pitchFactor = 1f;
                info.volumeFactor = volumeFactor;
            }

            def.PlayOneShot(info);
        }
    }
}
