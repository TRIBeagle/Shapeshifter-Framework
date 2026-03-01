// .NET Framework 4.8 / C# 7.3
using HarmonyLib;
using RimWorld;
using ShapeshifterFramework.Utilities;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace ShapeshifterFramework.Patches
{
    /// <summary>
    /// PawnWoundDrawer.WriteCache의 pawn.RaceProps.FleshType 접근을
    /// ShapeshiftOverlayHelper.GetEffectiveFleshType(pawn) 호출로 치환.
    /// 
    /// # 안전성
    /// - 스택을 정확히 맞추기 위해 get_FleshType 호출을 Pop하고,
    ///   인자(parms)에서 Pawn을 다시 로드하여 헬퍼를 호출한다.
    /// - 치환 실패 시 바닐라로 그대로 두고 경고 로그만 남긴다(크래시 방지).
    /// 
    /// # 성능
    /// - 치환 횟수 카운트/최소 로그만. LINQ 미사용.
    /// </summary>
    [HarmonyPatch(typeof(PawnWoundDrawer), "WriteCache")]
    internal static class Patch_PawnWoundDrawer_WriteCache
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var list = new List<CodeInstruction>(instructions);

            var fleshGetter = AccessTools.PropertyGetter(typeof(RaceProperties), nameof(RaceProperties.FleshType));
            var helperMethod = AccessTools.Method(typeof(ShapeshiftOverlayHelper), nameof(ShapeshiftOverlayHelper.GetEffectiveFleshType));
            var parmsPawnFld = AccessTools.Field(typeof(PawnDrawParms), nameof(PawnDrawParms.pawn));

            // PawnDrawParms 파라미터 위치를 동적으로 탐색 (시그니처 변경에 대비)
            // +1: this(인스턴스 메서드의 arg 0은 this)
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

            // parmsArgIndex에 맞는 Ldarg 명령 생성
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

                // RaceProperties.get_FleshType 호출 지점
                if (ci.opcode == OpCodes.Callvirt && ci.operand is MethodInfo mi && mi == fleshGetter)
                {
                    // 스택: ... [RaceProperties]
                    // 바꾸기:
                    //   Pop (RaceProperties 제거)
                    //   Ldarg (parms — 동적으로 탐색한 인덱스)
                    //   Ldfld PawnDrawParms.pawn
                    //   Call GetEffectiveFleshType(pawn)

                    list[i] = new CodeInstruction(OpCodes.Pop);                        // Pop RaceProperties
                    list.Insert(++i, MakeLdarg(parmsArgIndex));                        // parms
                    list.Insert(++i, new CodeInstruction(OpCodes.Ldfld, parmsPawnFld)); // parms.pawn
                    list.Insert(++i, new CodeInstruction(OpCodes.Call, helperMethod));  // helper(pawn)

                    replaceCount++;
                }
            }

            // 진단: 치환 실패 시 경고만 (호환성)
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