// ShapeshifterFramework | Patches | Patch_PawnWoundDrawer_WriteCache.cs
// 목적 : 상처(Wound) 오버레이를 그릴 때, 폰의 피/살점 타입(FleshType)을 바닐라 종족이 아닌 폼에 지정된 타입(기계, 곤충 등)으로 치환.
// 용도 : Transpiler로 get_FleshType 호출을 헬퍼로 교체. 파라미터 인덱스를 동적 탐색하여 시그니처 변경에 대비.

using HarmonyLib;
using RimWorld;
using ShapeshifterFramework.Utilities;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace ShapeshifterFramework.Patches
{
    /// <summary>WriteCache의 FleshType 접근을 폼 전용 헬퍼로 치환하는 Transpiler.</summary>
    [HarmonyPatch(typeof(PawnWoundDrawer), "WriteCache")]
    internal static class Patch_PawnWoundDrawer_WriteCache
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var list = new List<CodeInstruction>(instructions);

            var fleshGetter = AccessTools.PropertyGetter(typeof(RaceProperties), nameof(RaceProperties.FleshType));
            var helperMethod = AccessTools.Method(typeof(ShapeshiftOverlayUtility), nameof(ShapeshiftOverlayUtility.GetEffectiveFleshType));
            var parmsPawnFld = AccessTools.Field(typeof(PawnDrawParms), nameof(PawnDrawParms.pawn));

            // PawnDrawParms 파라미터 위치 동적 탐색 (+1: this)
            int parmsArgIndex = -1;
            var writeCache = AccessTools.Method(typeof(PawnWoundDrawer), "WriteCache");
            if (writeCache != null)
            {
                var parameters = writeCache.GetParameters();
                for (int p = 0; p < parameters.Length; p++)
                {
                    if (parameters[p].ParameterType == typeof(PawnDrawParms))
                    {
                        parmsArgIndex = p + 1; // +1: this
                        break;
                    }
                }
            }

            if (parmsArgIndex < 0)
            {
                Log.Warning("[SSF] PawnWoundDrawer transpiler: PawnDrawParms parameter not found. Falling back to vanilla wound overlays.");
                ShapeshiftPatchStatus.WoundDrawerTranspiled = false;
                return list;
            }

            // Ldarg 명령 생성
            CodeInstruction MakeLdarg(int index)
            {
                if (index == 0) return new CodeInstruction(OpCodes.Ldarg_0);
                if (index == 1) return new CodeInstruction(OpCodes.Ldarg_1);
                if (index == 2) return new CodeInstruction(OpCodes.Ldarg_2);
                if (index == 3) return new CodeInstruction(OpCodes.Ldarg_3);
                return new CodeInstruction(OpCodes.Ldarg_S, (byte)index);
            }

            int replaceCount = 0;

            for (int i = 0; i < list.Count; i++)
            {
                CodeInstruction ci = list[i];

                // get_FleshType 호출 지점
                if (ci.opcode == OpCodes.Callvirt && ci.operand is MethodInfo mi && mi == fleshGetter)
                {
                    // Pop RaceProperties → parms.pawn → helper(pawn)
                    list[i] = new CodeInstruction(OpCodes.Pop);
                    list.Insert(++i, MakeLdarg(parmsArgIndex));
                    list.Insert(++i, new CodeInstruction(OpCodes.Ldfld, parmsPawnFld));
                    list.Insert(++i, new CodeInstruction(OpCodes.Call, helperMethod));

                    replaceCount++;
                }
            }

            // 치환 실패 시 경고
            if (replaceCount == 0)
            {
                Log.Warning("[SSF] PawnWoundDrawer transpiler pattern not found. Falling back to vanilla wound overlays.");
                ShapeshiftPatchStatus.WoundDrawerTranspiled = false;
            }
            else
            {
                ShapeshiftDiagnostics.Info("PawnWoundDrawer transpiler applied. Replaced calls: " + replaceCount);
                ShapeshiftPatchStatus.WoundDrawerTranspiled = true;
            }

            return list;
        }
    }
}