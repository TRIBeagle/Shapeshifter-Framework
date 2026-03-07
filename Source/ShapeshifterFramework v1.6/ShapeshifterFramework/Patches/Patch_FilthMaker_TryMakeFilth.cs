// ShapeshifterFramework | Patches | Patch_FilthMaker_TryMakeFilth.cs
// 목적 : 폰이 피를 흘릴 때, 바닐라의 붉은 피(Filth_Blood) 대신 폼에 설정된 전용 혈흔(예: 곤충 체액, 기계 윤활유 등)을 바닥에 생성.
// 용도 : TryMakeFilth에 Prefix로 개입하며, 시그니처에 Pawn 정보가 없으므로 ShapeshiftFilthScope.CurrentPawn(스레드 정적 변수)에서 출혈 중인 폰의 컨텍스트를 가져와 ThingDef를 바꿔치기함.

using HarmonyLib;
using RimWorld;
using ShapeshifterFramework.Utilities;
using System;
using System.Reflection;
using Verse;

namespace ShapeshifterFramework.Patches
{
    /// <summary>TryMakeFilth에서 혈흔/스미어를 변신 폼 값으로 교체.</summary>
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
