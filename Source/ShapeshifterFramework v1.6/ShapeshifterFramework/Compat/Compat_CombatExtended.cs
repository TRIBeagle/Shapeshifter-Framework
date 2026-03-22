// ShapeshifterFramework | Compat | Compat_CombatExtended.cs
// 목적 : Combat Extended 모드 감지 및 호환성 훅 포인트 제공.
// 용도 : 변신 폼의 NativeVerb가 CE 탄약/탄도 시스템과 충돌하지 않는지 확인하기 위한 감지 전용 스텁.
//        현재는 감지 로그만 출력하며, 실제 충돌 확인 시 Harmony prefix를 추가할 수 있는 구조.
// 주의 : CE 어셈블리에 하드 참조 없음. 패키지 ID는 CETeam.CombatExtended로 감지.
//        폼 verb는 NativeVerb(EquipmentSource == null)이므로 CE 탄약 시스템 자연 면제.
//        CE 전용 melee/ranged 값(ToolCE, VerbPropertiesCE)은 모더가 MayRequire XML 패치로 적용.

using Verse;

namespace ShapeshifterFramework.Compat
{
    /// <summary>Combat Extended 감지 및 호환성 훅 포인트.</summary>
    internal static class Compat_CombatExtended
    {
        private static bool detected;

        /// <summary>Combat Extended 활성 여부를 감지하고 로그 기록.</summary>
        internal static void DetectAndLog()
        {
            if (detected) return;
            detected = true;

            // 폼 verb는 NativeVerb 소유(EquipmentSource == null)이므로
            // CE 탄약 시스템(CompAmmoUser)이 자연 면제됨.
            // CE 전용 melee/ranged 값은 모더가 MayRequire XML 패치로 ToolCE/VerbPropertiesCE 적용 가능.
            CompatManager.CE.Patched("Detect");
            Log.Message($"{CompatManager.LOG_CE} Detected. Form verbs use NativeVerb (EquipmentSource=null), exempt from CE ammo system. " +
                        "Modders can apply ToolCE/VerbPropertiesCE via MayRequire XML patches for CE-specific values.");
        }
    }
}
