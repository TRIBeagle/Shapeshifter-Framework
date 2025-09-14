// .NET Framework 4.8 / C# 7.3
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;
using RimWorld;
using ShapeshifterFramework.Utilities;

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

            // args: this(0), key(1), parms(2), writeTarget(3)
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
                    //   Ldarg_2 (parms)
                    //   Ldfld PawnDrawParms.pawn
                    //   Call GetEffectiveFleshType(pawn)

                    list[i] = new CodeInstruction(OpCodes.Pop);              // Pop RaceProperties
                    list.Insert(++i, new CodeInstruction(OpCodes.Ldarg_2));  // parms
                    list.Insert(++i, new CodeInstruction(OpCodes.Ldfld, parmsPawnFld)); // parms.pawn
                    list.Insert(++i, new CodeInstruction(OpCodes.Call, helperMethod));   // helper(pawn)

                    replaceCount++;
                }
            }

            // 진단: 치환 실패 시 경고만 (호환성)
            if (replaceCount == 0)
            {
                ShapeshiftDiagnostics.Warn("PawnWoundDrawer transpiler pattern not found. Falling back to vanilla wound overlays.");
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
