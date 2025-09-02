// Compat_HAR_BodyAddon_CanDrawNow.cs
// 목적: HAR BodyAddon의 CanDrawNow(...)에 Prefix를 걸어, 폼에서 showHarAddons=false면 렌더 차단
// 감지: packageId(erdelf.HumanoidAlienRaces) + 시그니처 동적 탐색
using HarmonyLib;
using ShapeshifterFramework.Utilities;
using System;
using System.Reflection;
using Verse;

namespace ShapeshifterFramework.Compat
{
    [HarmonyPatch]
    internal static class Compat_HAR_BodyAddon_CanDrawNow
    {
        static MethodBase _target;

        static bool Prepare()
        {
            if (!ShapeshiftCompat.HAR.IsActive) return false;

            var tWorker = ShapeshiftReflectionCache.TryType("AlienRace.AlienPawnRenderNodeWorker_BodyAddon");
            if (tWorker == null)
            {
                ShapeshiftCompat.HAR.Failed("CanDrawNow", "AlienPawnRenderNodeWorker_BodyAddon type not found");
                return false;
            }

            // 정확 시그니처 우선: bool CanDrawNow(PawnRenderNode, PawnDrawParms)
            _target = AccessTools.Method(tWorker, "CanDrawNow", new[] { typeof(PawnRenderNode), typeof(PawnDrawParms) });
            if (_target == null)
            {
                // 폴백: 이름/반환형만 맞는 메서드 검색
                foreach (var mi in AccessTools.GetDeclaredMethods(tWorker))
                {
                    if (mi?.Name == "CanDrawNow" && mi.ReturnType == typeof(bool))
                    {
                        var ps = mi.GetParameters();
                        if (ps.Length >= 2 && ps[1].ParameterType == typeof(PawnDrawParms))
                        {
                            _target = mi; break;
                        }
                    }
                }
            }

            if (_target == null)
            {
                ShapeshiftCompat.HAR.Failed("CanDrawNow", "CanDrawNow signature not found");
                return false;
            }

            ShapeshiftCompat.HAR.Patched("CanDrawNow");
            return true;
        }

        [HarmonyTargetMethod]
        static MethodBase TargetMethod() => _target;

        // Prefix: showHarAddons=false면 false 반환(원본 스킵)
        static bool Prefix(ref bool __result, object __0, object __1)
        {
            try
            {
                // __1: PawnDrawParms
                Pawn pawn = ShapeshiftReflectionCache.GetInstanceField<Pawn>(__1, "pawn");
                if (pawn == null) return true;

                var comp = ShapeshiftUtility.GetShapeShiftComp(pawn);
                if (comp?.currentForm != null && !comp.currentForm.showHarAddons)
                {
                    __result = false;
                    return false; // 원본 스킵
                }
            }
            catch (Exception e)
            {
                if (!ShapeshiftCompat.HAR.HasFailed("CanDrawNow:PrefixException"))
                    ShapeshiftCompat.HAR.Failed("CanDrawNow:PrefixException", e.Message);
            }
            return true; // 원본 실행
        }
    }
}
