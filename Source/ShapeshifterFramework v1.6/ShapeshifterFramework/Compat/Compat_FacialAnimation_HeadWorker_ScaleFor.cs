// Compat_FacialAnimation_HeadWorker_ScaleFor.cs
// 목적: Facial Animation 사용 시, "FA 헤드 워커"가 그리는 헤드에만 form.bodyDrawScale * form.headDrawScale 적용
//      (포트레잇이면 portraitDrawScale 옵션 추가)
// 방식: 베이스 Worker(Verse.PawnRenderNodeWorker).ScaleFor(...)에 Postfix -> node의 실제 worker 타입이
//      FacialAnimation.NLFacialAnimationHeadNodeWorker일 때만 배율을 곱한다.
using HarmonyLib;
using ShapeshifterFramework.Utilities;
using System;
using System.Reflection;
using UnityEngine;
using Verse;

namespace ShapeshifterFramework.Compat
{
    [HarmonyPatch]
    internal static class Compat_FacialAnimation_HeadWorker_ScaleFor
    {
        // 모드 타입 캐시
        static readonly Type T_FAComp = ShapeshiftReflectionCache.TryType("FacialAnimation.FacialAnimationControllerComp");
        static readonly Type T_BaseWorker = ShapeshiftReflectionCache.TryType("Verse.PawnRenderNodeWorker");
        static readonly Type T_FAHeadWorker = ShapeshiftReflectionCache.TryType("FacialAnimation.NLFacialAnimationHeadNodeWorker");

        // node에서 worker를 꺼내는 경로 (필드/프로퍼티 여러 이름 대비)
        static readonly FieldInfo FI_Node_worker = AccessTools.Field(typeof(PawnRenderNode), "worker");
        static readonly PropertyInfo PI_Node_Worker = AccessTools.Property(typeof(PawnRenderNode), "Worker");

        static bool Prepare()
        {
            if (!ShapeshiftCompat.FA.IsActive || T_BaseWorker == null || T_FAHeadWorker == null)
            {
                ShapeshiftCompat.FA.Failed("HeadScale", "not active or types missing");
                return false;
            }
            return true;
        }

        // Target: Verse.PawnRenderNodeWorker.ScaleFor(PawnRenderNode, PawnDrawParms) : Vector3
        [HarmonyTargetMethod]
        static MethodBase TargetMethod()
        {
            var exact = AccessTools.Method(T_BaseWorker, "ScaleFor", new[] { typeof(PawnRenderNode), typeof(PawnDrawParms) });
            if (exact != null)
            {
                ShapeshiftCompat.FA.Patched("HeadScale");
                return exact;
            }
            foreach (var mi in AccessTools.GetDeclaredMethods(T_BaseWorker))
            {
                if (mi?.Name == "ScaleFor" && mi.ReturnType == typeof(Vector3))
                {
                    ShapeshiftCompat.FA.Patched("HeadScale");
                    return mi;
                }
            }
            ShapeshiftCompat.FA.Failed("HeadScale", "Base Worker ScaleFor signature not found");
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

                // Facial Animation 컨트롤러가 있는 폰만 대상으로
                if (!HasFAControllerComp(pawn)) return;

                // 이 노드의 실제 워커가 FA의 "Head" 워커인지 확인
                object worker = TryGetWorker(node);
                if (worker == null || !T_FAHeadWorker.IsAssignableFrom(worker.GetType()))
                    return; // 헤드가 아니면 스킵(눈/입 파츠, 기타 노드 등)

                var comp = ShapeshiftUtility.GetShapeShiftComp(pawn);
                var form = comp?.currentForm;
                if (comp == null || !comp.isTransformed || form == null) return;

                float factor = 1f;

                // 헤드는 본체와 일치해야 하므로 body * head 모두 적용
                float bodyS = form.bodyDrawScale ?? 1f;
                float headS = form.headDrawScale ?? 1f;
                if (!Mathf.Approximately(bodyS, 1f)) factor *= bodyS;
                if (!Mathf.Approximately(headS, 1f)) factor *= headS;

                if (!Mathf.Approximately(factor, 1f))
                    __result = new Vector3(__result.x * factor, __result.y, __result.z * factor);
            }
            catch (Exception e)
            {
                Log.Warning($"{ShapeshiftCompat.LOG_FA} Head scale postfix failed: {e}");
            }
        }

        // ── helpers ───────────────────────────────────────────────────────────────

        static bool HasFAControllerComp(Pawn pawn)
        {
            if (T_FAComp == null || pawn == null) return false;
            try
            {
                var comps = (pawn as ThingWithComps)?.AllComps;
                if (comps == null) return false;
                for (int i = 0; i < comps.Count; i++)
                {
                    var c = comps[i];
                    if (c != null && T_FAComp.IsAssignableFrom(c.GetType()))
                        return true;
                }
            }
            catch { }
            return false;
        }

        static object TryGetWorker(PawnRenderNode node)
        {
            if (node == null) return null;
            var v = ShapeshiftReflectionCache.GetInstanceProperty<object>(node, "Worker");
            if (v != null) return v;
            return ShapeshiftReflectionCache.GetInstanceField<object>(node, "worker");
        }
    }
}
