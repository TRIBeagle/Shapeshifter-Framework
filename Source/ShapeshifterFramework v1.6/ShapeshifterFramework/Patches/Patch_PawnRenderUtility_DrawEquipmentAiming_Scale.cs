// ShapeshifterFramework | Patches | Patch_PawnRenderUtility_DrawEquipmentAiming_Scale.cs
// 목적 : 거대해지거나 작아진 폰의 덩치에 맞춰, 손에 들고 조준 중인 무기의 렌더링 크기(Scale)도 함께 연동.
// 용도 : Harmony Transpiler를 사용하여 Matrix4x4.TRS 호출 직전의 IL(스택) 명령에 개입, 회전이나 위치는 유지한 채 무기의 스케일 벡터만 바디 배율에 맞춰 팽창/축소시킴.

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

        // 바디 폭 비율로 스케일 보정
        public static Vector3 AdjustWeaponScale(Vector3 originalScale, Thing eq)
        {
            // 옵션 OFF 시 원본 유지
            if (ShapeshifterFrameworkMod.Settings == null || !ShapeshifterFrameworkMod.Settings.scaleHeldWeapons)
                return originalScale;

            Pawn pawn = ShapeshiftReflectionCache.TryGetHolderPawn(eq);
            if (pawn == null) return originalScale;

            float s = ShapeshiftRenderUtility.GetShapeScale(pawn, useHeadScale: false);

            if (Mathf.Approximately(s, 1f)) return originalScale;

            // 등방 스케일 적용
            return new Vector3(originalScale.x * s, originalScale.y, originalScale.z * s);
        }
    }
}
