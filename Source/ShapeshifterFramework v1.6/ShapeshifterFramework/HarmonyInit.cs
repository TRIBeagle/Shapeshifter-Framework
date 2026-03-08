// ShapeshifterFramework | Root | HarmonyInit.cs
// 목적 : 바닐라 림월드 로직에 개입하기 위한 Harmony 패치 초기화.
// 용도 : 모드가 로드될 때 단 1회 실행되며, [HarmonyPatch] 어트리뷰트가 붙은 모든 클래스를 자동 스캔 및 적용함.
// 주의 : 타 모드(HAR, FA 등) 의존성이 있는 패치는 여기서 자동 적용하지 않고 CompatManager를 통해 수동 지연 로딩함.

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

            // 폼별 동적 스탯 헤디프 — 누락분 보험 (ResolveReferences에서 대부분 생성됨)
            ShapeshifterFramework.Utilities.ShapeshiftStatHediffGenerator.GenerateAll();

            // 폼별 동적 AbilityDef — 누락분 보험 (ResolveReferences에서 대부분 생성됨)
            ShapeshifterFramework.Utilities.ShapeshiftAbilityGenerator.GenerateAll();

            // 개별 패치들은 성공/실패만 집계함. 여기서 모드별 요약 1회 출력.
            CompatManager.ReportAllOnce();
        }
    }
}
