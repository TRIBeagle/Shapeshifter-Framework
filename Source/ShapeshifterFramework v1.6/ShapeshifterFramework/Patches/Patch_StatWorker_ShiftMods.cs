// Patch_StatWorker_ShiftMods.cs
// 목적: 스탯 실값 계산에 폼 statFactors/Offsets를 반영.
// 용도: Postfix에서 현재 StatDef에 한해 __result에 가산/배수 적용.
// 최적화: StatWorker.stat 접근을 FieldInfo.GetValue(리플렉션) → FieldRef(델리게이트)로 변경 (핫패스)

using HarmonyLib;
using RimWorld;
using ShapeshifterFramework.Comps;
using Verse;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch(typeof(StatWorker), nameof(StatWorker.GetValueUnfinalized),
        new[] { typeof(StatRequest), typeof(bool) })]
    public static class Patch_StatWorker_ShiftMods
    {
        // protected StatDef stat;  (StatWorker)
        private static readonly AccessTools.FieldRef<StatWorker, StatDef> StatRef =
            AccessTools.FieldRefAccess<StatWorker, StatDef>("stat");

        static void Postfix(StatWorker __instance, StatRequest req, bool applyPostProcess, ref float __result)
        {
            // 1) Pawn 확보
            Pawn pawn = req.HasThing ? req.Thing as Pawn : req.Pawn;
            if (pawn == null) return;

            // 2) 변신 폼 확인
            var comp = pawn.TryGetComp<CompShapeshifter>();
            var form = comp?.currentForm;
            if (comp == null || !comp.isTransformed || form == null) return;

            // 3) 현재 계산 중인 StatDef (리플렉션 대신 FieldRef)
            StatDef stat = null;
            try { stat = StatRef(__instance); } catch { stat = null; }
            if (stat == null) return;

            // 4) 폼 보정치 합산(해당 stat만)
            float factor = 1f, offset = 0f;

            if (form.statFactors != null)
                for (int i = 0; i < form.statFactors.Count; i++)
                {
                    var f = form.statFactors[i];
                    if (f?.stat == stat) factor *= f.value;
                }

            if (form.statOffsets != null)
                for (int i = 0; i < form.statOffsets.Count; i++)
                {
                    var o = form.statOffsets[i];
                    if (o?.stat == stat) offset += o.value;
                }

            // 5) 변화 있을 때만 적용 (오프셋 → 배수)
            if (factor != 1f || offset != 0f)
                __result = (__result + offset) * factor;
        }
    }
}
