// ShapeshifterFramework | Patches | Patch_FloatMenuMakerMap_GetOptions.cs
// 목적 : 변신 중 장비 잠금(EquipLock) 설정이 켜져 있을 때, 유저가 우클릭으로 아이템을 착용/해제하려는 시도를 방지.
// 용도 : 플로트 메뉴 생성(GetOptions) 직후 Postfix로 개입하여, 대상이 의복이나 무기일 경우 해당 메뉴 항목을 비활성화(Disabled)하고 라벨 끝에 '장착 불가' 안내 문구를 덧붙임.

using HarmonyLib;
using RimWorld;
using ShapeshifterFramework.Comps;        // CompShapeshifter
using ShapeshifterFramework.Utilities;    // ShapeshiftEquipRules
using System.Collections.Generic;
using UnityEngine;                        // Vector3
using Verse;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch(typeof(FloatMenuMakerMap), nameof(FloatMenuMakerMap.GetOptions))]
    [HarmonyPriority(Priority.Last)]
    public static class Patch_FloatMenuMakerMap_GetOptions
    {
        // 원형: public static List<FloatMenuOption> GetOptions(List<Pawn> selectedPawns, Vector3 clickPos, out FloatMenuContext context)
        static void Postfix(List<Pawn> selectedPawns, Vector3 clickPos, ref FloatMenuContext context, ref List<FloatMenuOption> __result)
        {
            if (__result == null || __result.Count == 0) return;

            // 기준 Pawn
            Pawn pawn = null;
            if (context != null && context.FirstSelectedPawn != null) pawn = context.FirstSelectedPawn;
            else if (selectedPawns != null && selectedPawns.Count > 0) pawn = selectedPawns[0];
            if (pawn == null) return;
            if (!pawn.RaceProps.Humanlike) return;

            var comp = pawn.TryGetComp<CompShapeshifter>();
            if (comp == null || !comp.isTransformed) return;

            bool lockApparel = ShapeshiftEquipRules.LockApparel(comp);
            bool lockWeapon = ShapeshiftEquipRules.LockWeapons(comp);
            if (!lockApparel && !lockWeapon) return;

            string blockedSuffix = " (" + "SSF_Menu_Blocked".Translate() + ")";

            for (int i = 0; i < __result.Count; i++)
            {
                var opt = __result[i];
                if (opt == null) continue;
                if (opt.Disabled) continue;

                // 후보 대상 Thing만 안전하게 추출(월드타겟은 무시)
                Thing target = opt.iconThing;
                if (target == null)
                {
                    var ct = opt.revalidateClickTarget as Thing;
                    if (ct != null) target = ct;
                    // opt.revalidateWorldClickTarget는 WorldObject일 수 있으므로 캐스팅하지 않음
                }

                if (target == null) continue;

                bool isApparel = target is Apparel;
                bool isWeaponThing = target is ThingWithComps twc
                                     && twc.def != null
                                     && (twc.def.IsWeapon || twc.TryGetComp<CompEquippable>() != null);

                if ((isApparel && lockApparel) || (isWeaponThing && lockWeapon))
                {
                    // 간단 생성자만 사용(1.6 호환)
                    var disabled = new FloatMenuOption(opt.Label + blockedSuffix, null)
                    {
                        Disabled = true,
                        iconThing = opt.iconThing,
                        tutorTag = opt.tutorTag,
                        autoTakeable = false
                    };
                    __result[i] = disabled;
                }
            }
        }
    }
}
