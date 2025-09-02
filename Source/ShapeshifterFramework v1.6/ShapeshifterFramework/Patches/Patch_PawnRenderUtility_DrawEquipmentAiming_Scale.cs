// Patch_PawnRenderUtility_DrawEquipmentAiming_Scale.cs  (C# 7.3)
using HarmonyLib;
using ShapeshifterFramework.Utilities;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch(typeof(PawnRenderUtility), "DrawEquipmentAiming")]
    public static class Patch_PawnRenderUtility_DrawEquipmentAiming_Scale
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instr, ILGenerator gen)
        {
            var code = new List<CodeInstruction>(instr);
            var mTRS = AccessTools.Method(typeof(Matrix4x4), "TRS", new System.Type[] { typeof(Vector3), typeof(Quaternion), typeof(Vector3) });
            var mAdj = AccessTools.Method(typeof(Patch_PawnRenderUtility_DrawEquipmentAiming_Scale), nameof(AdjustWeaponScale));

            for (int i = 0; i < code.Count; i++)
            {
                var ins = code[i];
                if (ins.opcode == OpCodes.Call && ins.operand is MethodInfo && (MethodInfo)ins.operand == mTRS)
                {
                    // 스택: … pos, rot, scale
                    // eq(arg0) 로 스케일 조정: AdjustWeaponScale(scale, eq)
                    yield return new CodeInstruction(OpCodes.Ldarg_0);        // … pos, rot, scale, eq
                    yield return new CodeInstruction(OpCodes.Call, mAdj);     // … pos, rot, scale'
                    // 원래 TRS 호출
                    yield return ins;
                    continue;
                }
                yield return ins;
            }
        }

        // scale 보정: 바디 폭(bodyWidth) 비율을 등방으로 곱함
        public static Vector3 AdjustWeaponScale(Vector3 originalScale, Thing eq)
        {
            // 옵션 OFF면 그대로
            if (ShapeshifterFrameworkMod.Settings == null || !ShapeshifterFrameworkMod.Settings.scaleHeldWeapons)
                return originalScale;

            Pawn pawn = ShapeshiftReflectionCache.TryGetHolderPawn(eq);
            if (pawn == null) return originalScale;

            // 바닐라 기준과 "효과값" 비교
            var ls = pawn.ageTracker != null ? pawn.ageTracker.CurLifeStage : null;
            float vanillaBody = (ls != null && ls.bodyWidth.HasValue) ? ls.bodyWidth.Value : 1.5f;
            float targetBody = ShapeshiftSizeFactorResolver.Effective(pawn).bodyWidth;

            if (Mathf.Approximately(vanillaBody, 0f)) return originalScale;
            float s = targetBody / vanillaBody;
            if (Mathf.Approximately(s, 1f)) return originalScale;

            // 무기는 등방 스케일이 자연스러움 (x/z 동일 배율)
            return new Vector3(originalScale.x * s, originalScale.y, originalScale.z * s);
        }
    }
}
