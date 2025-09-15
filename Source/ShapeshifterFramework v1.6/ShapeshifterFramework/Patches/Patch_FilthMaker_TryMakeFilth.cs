using HarmonyLib;
using RimWorld;
using ShapeshifterFramework.Utilities;
using System;
using System.Reflection;
using Verse;

namespace ShapeshifterFramework.Patches
{
    /// <summary>
    /// FilthMaker.TryMakeFilth에서 바닥 혈흔/스미어를 변신 폼 값으로 교체.
    /// - Pawn 컨텍스트는 ShapeshiftFilthScope.CurrentPawn에서 가져옴.
    /// - 변신 OFF 상태면 바닐라 그대로.
    /// - 변신 ON + 캐시 값 존재 시 bloodDef / bloodSmearDef로 대체.
    /// </summary>
    [HarmonyPatch]
    internal static class Patch_FilthMaker_TryMakeFilth
    {
        static MethodBase TargetMethod()
        {
            // private static bool TryMakeFilth(IntVec3 c, Map map, ThingDef filthDef, IEnumerable<string> sources, bool shouldPropagate, out Filth outFilth, FilthSourceFlags additionalFlags = FilthSourceFlags.None)
            return AccessTools.Method(typeof(FilthMaker), "TryMakeFilth", new Type[]
            {
                typeof(IntVec3),
                typeof(Map),
                typeof(ThingDef),
                typeof(System.Collections.Generic.IEnumerable<string>),
                typeof(bool),
                typeof(Filth).MakeByRefType(),
                typeof(FilthSourceFlags)
            });
        }

        static void Prefix(ref ThingDef filthDef)
        {
            Pawn pawn = ShapeshiftFilthScope.CurrentPawn;
            if (pawn == null) return;

            if (filthDef == ThingDefOf.Filth_Blood)
            {
                if (ShapeshiftRuntimeCaches.BloodByPawn.TryGetValue(pawn, out var customBlood) && customBlood != null)
                {
                    filthDef = customBlood;
                }
            }
            else if (filthDef == ThingDefOf.Filth_BloodSmear)
            {
                if (ShapeshiftRuntimeCaches.SmearByPawn.TryGetValue(pawn, out var customSmear) && customSmear != null)
                {
                    filthDef = customSmear;
                }
            }
        }
    }
}
