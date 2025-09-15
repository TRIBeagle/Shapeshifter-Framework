// .NET 4.8 / C# 7.3
// 목적: 변신 중 착용/장착 관련 플로트메뉴 항목을 "보이되 비활성(Disabled)" 처리
// 대상: FloatMenuMakerMap.GetOptions(...) Postfix에서 __result 후처리
// 주의: WorldObject는 Thing이 아니므로 캐스팅하지 않음(월드 타겟 무시)

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

            string blockedSuffix = " (" + "Shapeshift_Menu_Blocked".Translate() + ")";

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
