// ShapeshifterFramework | Patches | Patch_Pawn_Kill.cs
// 목적 : 폰이 사망할 때 변신 상태를 완전히 해제하고 메모리에 남은 찌꺼기를 청소.
// 용도 : Pawn.Kill 메서드가 끝나는 시점(Postfix)에 HediffComp_ShapeshiftCore.Notify_Killed를 호출하여 런타임 캐시를 지우고 장비 드랍/복구 로직을 안전하게 마무리함.

using HarmonyLib;
using ShapeshifterFramework.Utilities;
using Verse;

namespace ShapeshifterFramework.Patches
{
    /// <summary>Pawn.Kill Postfix에서 Notify_Killed 호출, 변신 해제 및 캐시 정리.</summary>
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
    internal static class Patch_Pawn_Kill
    {
        static void Postfix(Pawn __instance, DamageInfo? dinfo, Hediff exactCulprit)
        {
            if (__instance == null) return;

            // HediffComp_ShapeshiftCore 기반 조회
            if (ShapeshiftCoreUtility.TryGetCore(__instance, out var core))
            {
                core.Notify_Killed(dinfo, exactCulprit);
            }
        }
    }
}
