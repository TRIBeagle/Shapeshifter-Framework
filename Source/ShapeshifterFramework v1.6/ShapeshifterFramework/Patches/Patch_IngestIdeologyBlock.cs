// ShapeshifterFramework | Patches | Patch_IngestIdeologyBlock.cs
// 목적 : 이데올로기(Abhorrent) 금지 폰 또는 이미 다른 폼으로 변신 중인 폰이 변신 약물을 우클릭으로 섭취하지 못하도록 FloatMenu 옵션 비활성화.
// 용도 : FloatMenuMakerMap.GetOptions Postfix로 개입하여, IngestionOutcomeDoer_Shapeshift가 포함된 약물의 메뉴 항목을 비활성화(Disabled)하고 차단 사유를 표시.

using HarmonyLib;
using RimWorld;
using ShapeshifterFramework.Comps;
using ShapeshifterFramework.Utilities;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch(typeof(FloatMenuMakerMap), nameof(FloatMenuMakerMap.GetOptions))]
    [HarmonyPriority(Priority.Last - 1)]
    public static class Patch_IngestIdeologyBlock
    {
        static void Postfix(List<Pawn> selectedPawns, Vector3 clickPos, ref FloatMenuContext context, ref List<FloatMenuOption> __result)
        {
            if (__result == null || __result.Count == 0) return;

            var pawn = FloatMenuPatchHelper.GetHumanlikePawn(selectedPawns, context);
            if (pawn == null) return;

            bool ideologyForbidden = ShapeshiftEligibility.IsIdeologyForbidden(pawn);

            // 이데올로기 금지도 아니고, 변신 중도 아니면 스킵
            bool isTransformed = ShapeshiftEligibility.IsAlreadyTransformed(pawn);
            if (!ideologyForbidden && !isTransformed) return;

            for (int i = 0; i < __result.Count; i++)
            {
                var opt = __result[i];
                if (opt == null || opt.Disabled) continue;

                Thing target = FloatMenuPatchHelper.GetTargetThing(opt);
                if (target == null) continue;

                // 변신 약물 여부 확인: IngestionOutcomeDoer_Shapeshift가 포함된 약물인지 체크
                if (!HasShapeshiftOutcomeDoer(target.def, out HediffDef hediffDef)) continue;

                // 차단 사유 결정
                string blockReason = null;

                if (ideologyForbidden)
                {
                    blockReason = "IdeoligionForbids".Translate();
                }
                else if (isTransformed)
                {
                    blockReason = "SSF_Menu_Blocked".Translate();
                }

                if (blockReason == null) continue;

                __result[i] = FloatMenuPatchHelper.MakeDisabled(opt, " (" + blockReason + ")");
            }
        }

        /// <summary>ThingDef의 ingestible.outcomeDoers에 IngestionOutcomeDoer_Shapeshift가 포함되어 있는지 확인.</summary>
        private static bool HasShapeshiftOutcomeDoer(ThingDef def, out HediffDef hediffDef)
        {
            hediffDef = null;
            if (def?.ingestible?.outcomeDoers == null) return false;

            for (int i = 0; i < def.ingestible.outcomeDoers.Count; i++)
            {
                if (def.ingestible.outcomeDoers[i] is IngestionOutcomeDoer_Shapeshift ssDoer)
                {
                    hediffDef = ssDoer.hediffDef;
                    return true;
                }
            }
            return false;
        }
    }
}
