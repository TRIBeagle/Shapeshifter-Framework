// ShapeshifterFramework | Compat | Compat_HAR_BodyAddon_ScaleFor.cs
// 목적   : HAR BodyAddon.ScaleFor(...) 결과에서, "헤드 계열 애드온"에만 변신 폼 스케일(body * head)을 곱한다.
// 용도   : AlienRace.AlienPawnRenderNodeWorker_BodyAddon.ScaleFor(Postfix)로 개입하여
//          본체 스케일과의 일관성을 유지하되, 바디 애드온에 중복 배율이 적용되는 것을 방지한다.
// 변경   : 2025-09-22 v1.0 — 프로젝트 주석 규칙 적용 및 리플렉션 접근을 ShapeshiftReflectionCache로 통일(기능 동일).
// 주의   : 외부 모드/시그니처 변경 대비 — 정확 시그니처 우선, 반환형/파라미터 기반 폴백을 포함.
// 성능   : FieldInfo/PropertyInfo 직접 보관 제거 → 캐시 헬퍼 사용으로 리플렉션 비용 및 중복 코드 축소.

using HarmonyLib;
using ShapeshifterFramework.Utilities;
using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using Verse;

namespace ShapeshifterFramework.Compat
{
    /// <summary>
    /// HAR BodyAddon의 스케일 보정(Postfix).
    /// - 원본: <c>AlienRace.AlienPawnRenderNodeWorker_BodyAddon.ScaleFor(PawnRenderNode, PawnDrawParms) : Vector3</c>
    /// - 동작: "헤드 계열 애드온"으로 판정되면 <c>form.bodyDrawScale * form.headDrawScale</c>를 적용.
    /// - 바디 애드온에는 추가 배율을 적용하지 않아 제곱 누적을 방지.
    /// 전제:
    /// - HAR 모드 활성( <c>CompatManager.HAR.IsActive</c> ) 및 변신 폼 유효.
    /// 부작용:
    /// - 없음(헤드 외에는 영향 없음).
    /// </summary>
    [HarmonyPatch]
    internal static class Compat_HAR_BodyAddon_ScaleFor
    {
        // ── HAR 타입 핸들(존재 여부만 확인; 실제 멤버 접근은 캐시 헬퍼 사용)
        private static readonly Type T_Worker = ShapeshiftReflectionCache.TryType("AlienRace.AlienPawnRenderNodeWorker_BodyAddon");

        /// <summary>패치 적용 가능 여부 점검.</summary>
        static bool Prepare()
        {
            if (!CompatManager.HAR.IsActive || T_Worker == null) return false;
            return true;
        }

        /// <summary>
        /// 대상 메서드: 정확 시그니처 우선, 불발 시 반환형/파라미터 기반 폴백.
        /// </summary>
        [HarmonyTargetMethod]
        static MethodBase TargetMethod()
        {
            // 정확: ScaleFor(PawnRenderNode, PawnDrawParms)
            var exact = AccessTools.Method(T_Worker, "ScaleFor", new[] { typeof(PawnRenderNode), typeof(PawnDrawParms) });
            if (exact != null)
            {
                CompatManager.HAR.Patched("ScaleFor");
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
                        CompatManager.HAR.Patched("ScaleFor");
                        return mi;
                    }
                }
            }

            CompatManager.HAR.Failed("ScaleFor", "ScaleFor signature not found");
            return null;
        }

        /// <summary>
        /// <b>Postfix</b> — 헤드 계열 애드온이면 (body * head) 배율을 적용.
        /// </summary>
        /// <param name="__result">원본 결과 스케일</param>
        /// <param name="__0">node</param>
        /// <param name="__1">parms</param>
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
                if (!CompatManager.HAR.HasFailed("ScaleFor:PostfixException"))
                    CompatManager.HAR.Failed("ScaleFor:PostfixException", e.Message);
            }
        }

        #region Head-addon 판정

        /// <summary>
        /// 헤드 계열 판정:
        /// 1) props.addon.alignWithHead == true
        /// 2) props.parentTagDef.defName == "Head"
        /// 3) props.addon.conditions 안의 BodyPart/label이 Head 하위 트리에 속함
        /// </summary>
        private static bool IsHeadAddon(PawnRenderNode node, Pawn pawn)
        {
            if (node == null) return false;

            // Verse.PawnRenderNode → Props(AlienPawnRenderNodeProperties_BodyAddon)
            object props = ShapeshiftReflectionCache.TryGetPropsFromNode(node);
            // props.addon (AlienPartGenerator.BodyAddon)
            object addon = props != null
                ? (ShapeshiftReflectionCache.GetInstanceField<object>(props, "addon")
                   ?? ShapeshiftReflectionCache.GetInstanceProperty<object>(props, "addon"))
                : null;

            // 1) alignWithHead
            if (addon != null)
            {
                object v = ShapeshiftReflectionCache.GetInstanceField<object>(addon, "alignWithHead")
                           ?? ShapeshiftReflectionCache.GetInstanceProperty<object>(addon, "alignWithHead");
                if (v != null)
                {
                    try { if (Convert.ToBoolean(v)) return true; } catch { }
                }
            }

            // 2) parentTagDef == "Head"
            if (props != null)
            {
                object parentTagDef = ShapeshiftReflectionCache.GetInstanceField<object>(props, "parentTagDef")
                                     ?? ShapeshiftReflectionCache.GetInstanceProperty<object>(props, "parentTagDef");
                var dn = GetDefName(parentTagDef);
                if (!string.IsNullOrEmpty(dn) && string.Equals(dn, "Head", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            // 3) conditions → BodyPart/label 검사
            if (addon != null && pawn != null)
            {
                var list = (ShapeshiftReflectionCache.GetInstanceField<object>(addon, "conditions")
                            ?? ShapeshiftReflectionCache.GetInstanceProperty<object>(addon, "conditions")) as IEnumerable;

                if (list != null)
                {
                    foreach (var cond in list)
                    {
                        if (cond == null) continue;

                        // bodyPart (BodyPartDef)
                        var bpObj = ShapeshiftReflectionCache.GetInstanceField<object>(cond, "bodyPart")
                                   ?? ShapeshiftReflectionCache.GetInstanceProperty<object>(cond, "bodyPart");
                        if (bpObj is BodyPartDef bpDef && IsUnderHead(pawn, bpDef)) return true;

                        // bodyPartLabel (string)
                        var label = ShapeshiftReflectionCache.GetInstanceField<string>(cond, "bodyPartLabel");
                        if (string.IsNullOrEmpty(label))
                            label = ShapeshiftReflectionCache.GetInstanceProperty<string>(cond, "bodyPartLabel");
                        if (!string.IsNullOrEmpty(label) && IsUnderHead(pawn, label)) return true;
                    }
                }
            }

            return false;
        }

        /// <summary>Def 또는 Def 유사 객체에서 defName을 얻는다(필드→프로퍼티 폴백).</summary>
        private static string GetDefName(object defObj)
        {
            if (defObj == null) return null;
            var dn = ShapeshiftReflectionCache.GetInstanceField<string>(defObj, "defName");
            if (!string.IsNullOrEmpty(dn)) return dn;
            return ShapeshiftReflectionCache.GetInstanceProperty<string>(defObj, "defName");
        }

        /// <summary>지정 BodyPartDef가 Head 트리 아래에 속하는지 검사.</summary>
        private static bool IsUnderHead(Pawn pawn, BodyPartDef def)
        {
            if (pawn?.RaceProps?.body?.AllParts == null || def == null) return false;
            for (int i = 0; i < pawn.RaceProps.body.AllParts.Count; i++)
            {
                var rec = pawn.RaceProps.body.AllParts[i];
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

        /// <summary>라벨 일치 파트가 Head 트리 아래에 속하는지 검사.</summary>
        private static bool IsUnderHead(Pawn pawn, string partLabel)
        {
            if (pawn?.RaceProps?.body?.AllParts == null || string.IsNullOrEmpty(partLabel)) return false;
            for (int i = 0; i < pawn.RaceProps.body.AllParts.Count; i++)
            {
                var rec = pawn.RaceProps.body.AllParts[i];
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

        #endregion
    }
}
