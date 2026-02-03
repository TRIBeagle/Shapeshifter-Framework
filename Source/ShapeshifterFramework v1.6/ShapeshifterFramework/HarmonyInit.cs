// HarmonyInit.cs
using HarmonyLib;
using ShapeshifterFramework.Compat;
using System.Reflection;
using Verse;

namespace ShapeshifterFramework
{
    [StaticConstructorOnStartup]
    public static class HarmonyInit
    {
        static HarmonyInit()
        {
            var harmony = new Harmony("TRIBeagle.shapeshifterframework");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            // 개별 패치들은 성공/실패만 집계함. 여기서 모드별 요약 1회 출력.
            CompatManager.ReportAllOnce();
        }
    }
}
