// ShapeshifterFramework | Compat | Compat_YayoCombat.cs
// 목적 : Yayo's Combat 모드 감지 및 호환성 훅 포인트 제공.
// 용도 : 변신 폼의 NativeVerb가 Yayo 탄약 시스템과 충돌하지 않는지 확인하기 위한 감지 전용 스텁.
//        현재는 감지 로그만 출력하며, 실제 충돌 확인 시 Harmony prefix를 추가할 수 있는 구조.
// 주의 : Yayo 어셈블리에 하드 참조 없음. 충돌 미확인 상태이므로 패치 없이 감지만 수행.

using Verse;

namespace ShapeshifterFramework.Compat
{
    /// <summary>Yayo's Combat 감지 및 호환성 훅 포인트.</summary>
    internal static class Compat_YayoCombat
    {
        private static bool detected;

        /// <summary>Yayo's Combat 활성 여부를 감지하고 로그 기록.</summary>
        internal static void DetectAndLog()
        {
            if (detected) return;
            detected = true;

            // 폼 verb는 NativeVerb 소유(EquipmentSource == null)이므로
            // 바닐라 동물 공격과 동일 패턴 — Yayo 탄약 시스템 자연 면제 예상.
            CompatManager.Yayo.Patched("Detect");
            Log.Message($"{CompatManager.LOG_Yayo} 감지 완료. 폼 verb는 NativeVerb로 탄약 시스템 면제 예상.");
        }
    }
}
