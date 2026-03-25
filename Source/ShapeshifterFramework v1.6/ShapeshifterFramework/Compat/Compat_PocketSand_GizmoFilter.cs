// ShapeshifterFramework | Compat | Compat_PocketSand_GizmoFilter.cs
// 목적 : Pocket Sand 모드의 무기 선택 기즈모를 장비 잠금 상태에서 숨김.
// 용도 : 무기 교체가 불가능한 변신 폼에서 Pocket Sand 기즈모를 필터링하여,
//        Job 생성 후 실패하는 나쁜 UX를 방지. 무기 교체 허용 폼에서는 정상 표시.

using System;
using System.Collections.Generic;
using HarmonyLib;
using ShapeshifterFramework.Hediffs;
using ShapeshifterFramework.Utilities;
using Verse;

namespace ShapeshifterFramework.Compat
{
    /// <summary>Pocket Sand 기즈모 필터링 — 무기 잠금 시 Gizmo_WeaponSelector 숨김.</summary>
    [HarmonyPatch]
    internal static class Compat_PocketSand_GizmoFilter
    {
        private static Type _gizmoType;
        private static bool _gizmoTypeResolved;

        /// <summary>Pocket Sand Gizmo_WeaponSelector 타입 (리플렉션, 1회 캐싱).</summary>
        private static Type GizmoType
        {
            get
            {
                if (!_gizmoTypeResolved)
                {
                    _gizmoTypeResolved = true;
                    _gizmoType = ShapeshiftReflectionCache.TryType("PocketSand.Gizmo_WeaponSelector");
                }
                return _gizmoType;
            }
        }

        /// <summary>Pocket Sand 비활성이면 패치 건너뜀.</summary>
        static bool Prepare()
        {
            if (!CompatManager.PS.IsActive) return false;
            if (GizmoType == null)
            {
                CompatManager.PS.Failed("GizmoFilter:Prepare", "Gizmo_WeaponSelector type not found");
                return false;
            }
            CompatManager.PS.Patched("GizmoFilter");
            return true;
        }

        /// <summary>Pawn.GetGizmos 결과에서 무기 잠금 시 Pocket Sand 기즈모 제거.</summary>
        [HarmonyPatch(typeof(Pawn), "GetGizmos")]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Low)]
        static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Pawn __instance)
        {
            // 변신 중이 아니거나 무기 잠금이 아니면 필터링 불필요
            bool shouldFilter = false;
            try
            {
                if (ShapeshiftRegistry.TryGet(__instance, out var comp, out _)
                    && ShapeshiftEquipRules.LockWeapons(comp))
                {
                    shouldFilter = true;
                }
            }
            catch { }

            if (!shouldFilter)
            {
                foreach (var g in __result) yield return g;
                yield break;
            }

            // 무기 잠금 상태: Pocket Sand 기즈모 필터링
            var gType = GizmoType;
            foreach (var g in __result)
            {
                if (gType != null && gType.IsInstanceOfType(g))
                    continue;
                yield return g;
            }
        }
    }
}
