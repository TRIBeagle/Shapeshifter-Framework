// ShapeshifterFramework | Patches | Patch_FloatMenuMakerMap_GetOptions.cs
// 목적 : 변신 중 장비 잠금(EquipLock) 설정이 켜져 있을 때, 유저가 우클릭으로 아이템을 착용/해제하려는 시도를 방지.
// 용도 : 플로트 메뉴 생성(GetOptions) 직후 Postfix로 개입하여, 대상이 의복이나 무기일 경우 해당 메뉴 항목을 비활성화(Disabled)하고 라벨 끝에 '장착 불가' 안내 문구를 덧붙임.

using HarmonyLib;
using RimWorld;
using ShapeshifterFramework.Hediffs;
using ShapeshifterFramework.Utilities;
using System.Collections.Generic;
using UnityEngine;                        // Vector3
using Verse;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch(typeof(FloatMenuMakerMap), nameof(FloatMenuMakerMap.GetOptions))]
    [HarmonyPriority(Priority.Last)]
    public static class Patch_FloatMenuMakerMap_GetOptions
    {
        // GetOptions Postfix
        static void Postfix(List<Pawn> selectedPawns, Vector3 clickPos, ref FloatMenuContext context, ref List<FloatMenuOption> __result)
        {
            if (__result == null || __result.Count == 0) return;

            var pawn = FloatMenuPatchHelper.GetHumanlikePawn(selectedPawns, context);
            if (pawn == null) return;

            // HediffComp_ShapeshiftCore 기반 조회
            if (!ShapeshiftRegistry.TryGet(pawn, out var core, out var form)) return;

            bool lockApparel = ShapeshiftEquipRules.LockApparel(core);
            bool lockWeapon = ShapeshiftEquipRules.LockWeapons(core);
            if (!lockApparel && !lockWeapon) return;

            string blockedSuffix = " (" + "SSF_Menu_Blocked".Translate() + ")";

            for (int i = 0; i < __result.Count; i++)
            {
                var opt = __result[i];
                if (opt == null) continue;
                if (opt.Disabled) continue;

                Thing target = FloatMenuPatchHelper.GetTargetThing(opt);
                if (target == null) continue;

                bool isApparel = target is Apparel;
                // def.IsWeapon 우선 체크 — true이면 TryGetComp 호출 불필요 (short-circuit)
                bool isWeaponThing = target is ThingWithComps twc
                                     && twc.def != null
                                     && twc.def.IsWeapon;

                if ((isApparel && lockApparel) || (isWeaponThing && lockWeapon))
                {
                    __result[i] = FloatMenuPatchHelper.MakeDisabled(opt, blockedSuffix);
                }
            }
        }
    }
}
