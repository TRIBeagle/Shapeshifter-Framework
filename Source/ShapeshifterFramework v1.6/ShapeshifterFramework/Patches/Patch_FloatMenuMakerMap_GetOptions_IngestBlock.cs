// ShapeshifterFramework | Patches | Patch_FloatMenuMakerMap_GetOptions_IngestBlock.cs
// 목적 : 변신/연장 약물의 FloatMenu 옵션을 조건부 비활성화. 이데올로기 금지, 폼 불일치(allowedFromForms), 비변신 상태 등을 종합 판단.
// 용도 : FloatMenuMakerMap.GetOptions Postfix로 개입하여, 변신/연장 약물의 메뉴 항목을 비활성화(Disabled)하고 차단 사유를 표시.

using HarmonyLib;
using RimWorld;
using ShapeshifterFramework.Utilities;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch(typeof(FloatMenuMakerMap), nameof(FloatMenuMakerMap.GetOptions))]
    [HarmonyPriority(Priority.Last - 1)]
    internal static class Patch_FloatMenuMakerMap_GetOptions_IngestBlock
    {
        static void Postfix(List<Pawn> selectedPawns, Vector3 clickPos, ref FloatMenuContext context, ref List<FloatMenuOption> __result)
        {
            if (__result == null || __result.Count == 0) return;

            var pawn = FloatMenuPatchHelper.GetHumanlikePawn(selectedPawns, context);
            if (pawn == null) return;

            bool ideologyForbidden = ShapeshiftEligibility.IsIdeologyForbidden(pawn);
            bool isTransformed = ShapeshiftEligibility.IsAlreadyTransformed(pawn);

            for (int i = 0; i < __result.Count; i++)
            {
                var opt = __result[i];
                if (opt == null || opt.Disabled) continue;

                Thing target = FloatMenuPatchHelper.GetTargetThing(opt);
                if (target == null) continue;

                string blockReason = null;

                // 1) 변신 약물 (IngestionOutcomeDoer_Shapeshift)
                if (ShapeshiftEligibility.HasShapeshiftOutcomeDoer(target.def))
                {
                    if (ideologyForbidden)
                    {
                        blockReason = "SSF_GizmoDisabled_IdeologyForbidden".Translate();
                    }
                    else if (isTransformed)
                    {
                        // 약물의 allowedFromForms에 현재 폼이 포함되어 있으면 허용
                        var drugAllowed = ShapeshiftEligibility.GetDrugAllowedFromForms(target.def);
                        if (!ShapeshiftEligibility.IsFormTransitionAllowed(pawn, drugAllowed))
                            blockReason = "SSF_Menu_DrugBlocked".Translate();
                    }
                }
                // 2) 연장 약물 (IngestionOutcomeDoer_ExtendShapeshift) — 비변신/폼 불일치 시 차단
                else
                {
                    blockReason = ShapeshiftEligibility.GetExtendDrugBlockReason(pawn, target.def);
                }

                if (blockReason == null) continue;

                __result[i] = FloatMenuPatchHelper.MakeDisabled(opt, " (" + blockReason + ")");
            }
        }
    }
}
