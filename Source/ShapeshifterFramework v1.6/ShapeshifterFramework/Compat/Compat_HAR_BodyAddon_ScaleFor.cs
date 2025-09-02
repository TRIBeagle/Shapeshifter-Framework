// Compat_HAR_BodyAddon_ScaleFor.cs
// 목적:
//  - HAR BodyAddon.ScaleFor(...) 결과에 "헤드 계열 애드온"일 때만 bodyDrawScale * headDrawScale 적용
//    (바디 애드온은 추가 배율 X → 제곱 방지)
//  - Portrait(정보창)에서는 옵션이 켜져 있으면 portraitDrawScale 추가 곱
// 감지: packageId(erdelf.HumanoidAlienRaces) + 시그니처 동적 탐색
using HarmonyLib;
using ShapeshifterFramework.Utilities;
using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using Verse;

namespace ShapeshifterFramework.Compat
{
    [HarmonyPatch]
    internal static class Compat_HAR_BodyAddon_ScaleFor
    {
        // ── HAR 리플렉션 캐시
        static readonly Type T_Worker = ShapeshiftReflectionCache.TryType("AlienRace.AlienPawnRenderNodeWorker_BodyAddon");
        static readonly Type T_Node = ShapeshiftReflectionCache.TryType("AlienRace.AlienPawnRenderNode_BodyAddon");
        static readonly Type T_Props = ShapeshiftReflectionCache.TryType("AlienRace.AlienPawnRenderNodeProperties_BodyAddon");
        static readonly Type T_Addon = ShapeshiftReflectionCache.TryType("AlienRace.AlienPartGenerator+BodyAddon");
        static readonly Type T_CondBP = ShapeshiftReflectionCache.TryType("AlienRace.ExtendedGraphics.ConditionBodyPart");

        static readonly FieldInfo FI_Node_props = T_Node != null ? AccessTools.Field(T_Node, "props") : null;
        static readonly FieldInfo FI_Props_addon = T_Props != null ? AccessTools.Field(T_Props, "addon") : null;
        static readonly FieldInfo FI_Props_parentTagDef = T_Props != null ? AccessTools.Field(T_Props, "parentTagDef") : null;
        static readonly FieldInfo FI_Addon_alignWithHead = T_Addon != null ? AccessTools.Field(T_Addon, "alignWithHead") : null;
        static readonly FieldInfo FI_Addon_conditions = T_Addon != null ? AccessTools.Field(T_Addon, "conditions") : null;

        static readonly FieldInfo FI_Cond_bodyPart = T_CondBP != null ? AccessTools.Field(T_CondBP, "bodyPart") : null; // BodyPartDef
        static readonly FieldInfo FI_Cond_bodyPartLabel = T_CondBP != null ? AccessTools.Field(T_CondBP, "bodyPartLabel") : null; // string

        static bool Prepare()
        {
            if (!ShapeshiftCompat.HAR.IsActive || T_Worker == null) return false;
            return true;
        }

        [HarmonyTargetMethod]
        static MethodBase TargetMethod()
        {
            // 정확: ScaleFor(PawnRenderNode, PawnDrawParms)
            var exact = AccessTools.Method(T_Worker, "ScaleFor", new[] { typeof(PawnRenderNode), typeof(PawnDrawParms) });
            if (exact != null)
            {
                ShapeshiftCompat.HAR.Patched("ScaleFor");
                return exact;
            }
            // 폴백
            foreach (var mi in AccessTools.GetDeclaredMethods(T_Worker))
            {
                if (mi?.Name == "ScaleFor" && mi.ReturnType == typeof(Vector3))
                {
                    var ps = mi.GetParameters();
                    if (ps.Length >= 2 && ps[0].ParameterType == typeof(PawnRenderNode) && ps[1].ParameterType == typeof(PawnDrawParms))
                    {
                        ShapeshiftCompat.HAR.Patched("ScaleFor");
                        return mi;
                    }
                }
            }
            ShapeshiftCompat.HAR.Failed("ScaleFor", "ScaleFor signature not found");
            return null;
        }

        // Postfix: (PawnRenderNode node, PawnDrawParms parms)
        static void Postfix(ref Vector3 __result, PawnRenderNode __0, PawnDrawParms __1)
        {
            try
            {
                var node = __0;
                var parms = __1;
                if (node == null) return;

                Pawn pawn = parms.pawn;
                if (pawn == null) return;

                var comp = ShapeshiftUtility.GetShapeShiftComp(pawn);
                var form = comp?.currentForm;
                if (comp == null || !comp.isTransformed || form == null) return;

                float factor = 1f;

                // 헤드 계열이면 body*head
                if (IsHeadAddon(node, pawn))
                {
                    float bodyS = form.bodyDrawScale ?? 1f;
                    float headS = form.headDrawScale ?? 1f;
                    if (!Mathf.Approximately(bodyS, 1f)) factor *= bodyS;
                    if (!Mathf.Approximately(headS, 1f)) factor *= headS;
                }

                if (!Mathf.Approximately(factor, 1f))
                    __result = new Vector3(__result.x * factor, __result.y, __result.z * factor);
            }
            catch (Exception e)
            {
                if (!ShapeshiftCompat.HAR.HasFailed("ScaleFor:PostfixException"))
                    ShapeshiftCompat.HAR.Failed("ScaleFor:PostfixException", e.Message);
            }
        }

        // 헤드 계열 판정: alignWithHead || parentTagDef==Head || conditions(BodyPart/label)이 Head 하위
        static bool IsHeadAddon(PawnRenderNode node, Pawn pawn)
        {
            if (node == null) return false;

            object props = FI_Node_props?.GetValue(node);
            object addon = (props != null && FI_Props_addon != null) ? FI_Props_addon.GetValue(props) : null;

            // 1) alignWithHead
            if (addon != null && FI_Addon_alignWithHead != null)
            {
                try { if (Convert.ToBoolean(FI_Addon_alignWithHead.GetValue(addon))) return true; } catch { }
            }

            // 2) parentTagDef == Head
            if (props != null && FI_Props_parentTagDef != null)
            {
                try
                {
                    var tag = FI_Props_parentTagDef.GetValue(props);
                    var dn = GetDefName(tag);
                    if (!string.IsNullOrEmpty(dn) && string.Equals(dn, "Head", StringComparison.OrdinalIgnoreCase))
                        return true;
                }
                catch { }
            }

            // 3) conditions → BodyPart/label이 Head 하위인지
            if (addon != null && FI_Addon_conditions != null && pawn != null)
            {
                try
                {
                    var list = FI_Addon_conditions.GetValue(addon) as IEnumerable;
                    if (list != null)
                    {
                        foreach (var cond in list)
                        {
                            if (cond == null || T_CondBP == null || !T_CondBP.IsAssignableFrom(cond.GetType())) continue;

                            var bpObj = FI_Cond_bodyPart?.GetValue(cond);
                            if (bpObj is BodyPartDef bpDef && IsUnderHead(pawn, bpDef)) return true;

                            var label = FI_Cond_bodyPartLabel?.GetValue(cond) as string;
                            if (!string.IsNullOrEmpty(label) && IsUnderHead(pawn, label)) return true;
                        }
                    }
                }
                catch { }
            }
            return false;
        }

        static string GetDefName(object defObj)
        {
            if (defObj == null) return null;
            var dn = ShapeshiftReflectionCache.GetInstanceField<string>(defObj, "defName");
            if (!string.IsNullOrEmpty(dn)) return dn;
            return ShapeshiftReflectionCache.GetInstanceProperty<string>(defObj, "defName");
        }

        static bool IsUnderHead(Pawn pawn, BodyPartDef def)
        {
            if (pawn?.RaceProps?.body?.AllParts == null || def == null) return false;
            foreach (var rec in pawn.RaceProps.body.AllParts)
            {
                if (rec.def != def) continue;
                var p = rec;
                while (p != null)
                {
                    var dn = p.def?.defName;
                    if (!string.IsNullOrEmpty(dn) && string.Equals(dn, "Head", StringComparison.OrdinalIgnoreCase))
                        return true;
                    p = p.parent;
                }
            }
            return false;
        }

        static bool IsUnderHead(Pawn pawn, string partLabel)
        {
            if (pawn?.RaceProps?.body?.AllParts == null || string.IsNullOrEmpty(partLabel)) return false;
            foreach (var rec in pawn.RaceProps.body.AllParts)
            {
                var lbl = rec.def?.label;
                if (string.IsNullOrEmpty(lbl) || !string.Equals(lbl, partLabel, StringComparison.OrdinalIgnoreCase)) continue;

                var p = rec;
                while (p != null)
                {
                    var dn = p.def?.defName;
                    if (!string.IsNullOrEmpty(dn) && string.Equals(dn, "Head", StringComparison.OrdinalIgnoreCase))
                        return true;
                    p = p.parent;
                }
            }
            return false;
        }
    }
}
