// ShapeshifterFramework | Compat | Compat_HAR_BodyAddon_CanDrawNow.cs
// 목적 : 변신 폼 설정(showHarAddons = false)에 따라, HAR 종족 고유의 신체 부착물(뿔, 꼬리 등 BodyAddon) 렌더링을 차단함.
// 용도 : HAR의 AlienPawnRenderNodeWorker_BodyAddon.CanDrawNow 메서드에 Harmony Prefix로 개입하여, 렌더링 조건이 불만족일 경우 원본 메서드 실행을 강제로 스킵(__result = false) 처리함.
// 주의 : 외부 모드의 시그니처 변경에 대비해 타겟 메서드 탐색 시 정확한 시그니처를 우선 검사하고, 실패 시 반환형과 파라미터 기반의 동적 폴백(Fallback) 탐색을 수행하도록 설계됨.

using HarmonyLib;
using ShapeshifterFramework.Utilities;
using System;
using System.Reflection;
using Verse;

namespace ShapeshifterFramework.Compat
{
    /// <summary>HAR BodyAddon 렌더 차단 Prefix. showHarAddons=false면 원본 스킵.</summary>
    [HarmonyPatch]
    internal static class Compat_HAR_BodyAddon_CanDrawNow
    {
        private static MethodBase _target;

        /// <summary>패치 가능 여부 점검 및 대상 메서드 탐색(정확 시그니처 우선, 폴백).</summary>
        static bool Prepare()
        {
            if (!CompatManager.HAR.IsActive) return false;

            var tWorker = ShapeshiftReflectionCache.TryType("AlienRace.AlienPawnRenderNodeWorker_BodyAddon");
            if (tWorker == null)
            {
                CompatManager.HAR.Failed("CanDrawNow", "AlienPawnRenderNodeWorker_BodyAddon type not found");
                return false;
            }

            // 정확 시그니처 우선
            _target = AccessTools.Method(tWorker, "CanDrawNow", new[] { typeof(PawnRenderNode), typeof(PawnDrawParms) });
            if (_target == null)
            {
                // 폴백: 이름/반환형 + PawnDrawParms 파라미터
                foreach (var mi in AccessTools.GetDeclaredMethods(tWorker))
                {
                    if (mi?.Name == "CanDrawNow" && mi.ReturnType == typeof(bool))
                    {
                        var ps = mi.GetParameters();
                        if (ps.Length >= 2 && ps[1].ParameterType == typeof(PawnDrawParms))
                        {
                            _target = mi;
                            break;
                        }
                    }
                }
            }

            if (_target == null)
            {
                CompatManager.HAR.Failed("CanDrawNow", "CanDrawNow signature not found");
                return false;
            }

            CompatManager.HAR.Patched("CanDrawNow");
            return true;
        }

        /// <summary>Harmony 대상 메서드 반환.</summary>
        [HarmonyTargetMethod]
        static MethodBase TargetMethod() => _target;

        /// <summary>Prefix: 폼 설정에 따라 BodyAddon 렌더 차단.</summary>
        /// <param name="__result">원본 반환값</param>
        /// <param name="__0">PawnRenderNode</param>
        /// <param name="__1">PawnDrawParms</param>
        /// <returns>원본 실행 여부</returns>
        static bool Prefix(ref bool __result, PawnRenderNode __0, PawnDrawParms __1)
        {
            try
            {
                Pawn pawn = __1.pawn;
                if (pawn == null) return true;

                if (ShapeshiftRegistry.TryGet(pawn, out var comp, out var form))
                {
                    // HARFormExtension 미지정 시 기본적으로 HAR 애드온을 숨김 (변신 폼의 외형 일관성 보장).
                    // 모더가 <showHarAddons>true</showHarAddons>를 명시해야 HAR 애드온이 표시됨.
                    var harExt = form.GetModExtension<HARFormExtension>();
                    bool show = harExt != null && harExt.showHarAddons;
                    if (!show)
                    {
                        __result = false;
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                if (!CompatManager.HAR.HasFailed("CanDrawNow:PrefixException"))
                    CompatManager.HAR.Failed("CanDrawNow:PrefixException", e.Message);
            }
            return true;
        }
    }
}
