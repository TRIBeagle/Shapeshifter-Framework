// ShapeshifterFramework | Patches | Patch_Pawn_Destroy.cs
// 목적 : Kill을 거치지 않고 파괴되는 폰(데브 삭제, 외부 모드 제거 등)의 레지스트리 엔트리 정리.
// 용도 : ShapeshiftRegistry는 강한 참조 Dictionary라 파괴된 폰 엔트리가 세이브 재로드까지 잔류함.
//        Pawn.Destroy Postfix에서 Unregister하여 누수 차단. (정상 사망은 Patch_Pawn_Kill이 처리.)

using HarmonyLib;
using ShapeshifterFramework.Utilities;
using Verse;

namespace ShapeshifterFramework.Patches
{
    /// <summary>Pawn.Destroy Postfix — 레지스트리 엔트리 정리 (Kill 미경유 파괴 대비).</summary>
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Destroy))]
    internal static class Patch_Pawn_Destroy
    {
        static void Postfix(Pawn __instance)
        {
            if (__instance != null)
                ShapeshiftRegistry.Unregister(__instance);
        }
    }
}
