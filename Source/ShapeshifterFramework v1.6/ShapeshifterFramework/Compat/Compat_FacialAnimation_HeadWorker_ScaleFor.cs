// ShapeshifterFramework | Compat | Compat_FacialAnimation_HeadWorker_ScaleFor.cs
// 목적 : Facial Animation 모드 사용 시, 폰이 변신했을 때 FA의 '머리(Head)' 그래픽 크기가 변신 폼의 몸통 크기에 맞춰 정상적으로 확대/축소되도록 동기화.
// 용도 : Verse.PawnRenderNodeWorker.ScaleFor 메서드에 Harmony Postfix로 개입하여, 대상 노드의 실제 Worker가 FA의 HeadWorker(NLFacialAnimationHeadNodeWorker)인 경우에만 폼의 스케일 배율을 적용.
// 주의 : 리플렉션 비용 절감을 위해 대상 타입들을 ShapeshiftReflectionCache로 사전 캐싱함. 초상화나 월드맵 등 공통 렌더 경로를 보호하기 위해 대상 워커가 정확히 일치할 때만 작동함.

using HarmonyLib;
using ShapeshifterFramework.Utilities;
using System;
using System.Reflection;
using UnityEngine;
using Verse;

namespace ShapeshifterFramework.Compat
{
    /// <summary>
    /// Facial Animation(HAR-계열) 사용 시, Verse.PawnRenderNodeWorker.ScaleFor(Postfix)에 개입하여
    /// NLFacialAnimationHeadNodeWorker(헤드 워커)인 경우에만 변신 폼 스케일(body * head)을 적용한다.
    /// - 원본 메서드 : <c>Verse.PawnRenderNodeWorker.ScaleFor(PawnRenderNode, PawnDrawParms) : Vector3</c>
    /// - 패치 타입   : <b>Postfix</b>
    /// 전제:
    /// - Facial Animation 컨트롤러 컴프( FacicalAnimationControllerComp )가 있는 Pawn만 대상.
    /// - 변신 폼(SSOT)이 유효하고 isTransformed == true 이어야 적용.
    /// 부작용:
    /// - 없음(헤드 외 노드/워커에는 영향 없음).
    /// </summary>
    [HarmonyPatch]
    internal static class Compat_FacialAnimation_HeadWorker_ScaleFor
    {
        #region Cached Types & Accessors

        // [성능] 외부 모드 타입 캐시(리플렉션 비용 절감)
        private static readonly Type T_FAComp = ShapeshiftReflectionCache.TryType("FacialAnimation.FacialAnimationControllerComp");
        private static readonly Type T_BaseWorker = ShapeshiftReflectionCache.TryType("Verse.PawnRenderNodeWorker");
        private static readonly Type T_FAHeadWorker = ShapeshiftReflectionCache.TryType("FacialAnimation.NLFacialAnimationHeadNodeWorker");

        #endregion

        #region Harmony Bootstrapping

        /// <summary>
        /// Harmony 패치 여부를 사전 판정한다.
        /// Facial Animation이 비활성 상태이거나 필수 타입을 찾지 못하면 패치를 적용하지 않는다.
        /// </summary>
        /// <returns>패치 적용 가능 시 true, 아니면 false.</returns>
        static bool Prepare()
        {
            // [안전] 외부 의존성(Facial Animation) 없으면 패치 비적용
            if (!CompatManager.FA.IsActive || T_BaseWorker == null || T_FAHeadWorker == null)
            {
                CompatManager.FA.Failed("HeadScale", "not active or types missing");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 원본 타겟 메서드: <c>Verse.PawnRenderNodeWorker.ScaleFor(PawnRenderNode, PawnDrawParms)</c> 검색.
        /// 정확 시그니처 우선, 없으면 반환형 Vector3의 동명 메서드로 폴백.
        /// </summary>
        /// <returns>Harmony가 패치할 대상 메서드</returns>
        [HarmonyTargetMethod]
        static MethodBase TargetMethod()
        {
            var exact = AccessTools.Method(T_BaseWorker, "ScaleFor", new[] { typeof(PawnRenderNode), typeof(PawnDrawParms) });
            if (exact != null)
            {
                CompatManager.FA.Patched("HeadScale");
                return exact;
            }
            foreach (var mi in AccessTools.GetDeclaredMethods(T_BaseWorker))
            {
                if (mi?.Name == "ScaleFor" && mi.ReturnType == typeof(Vector3))
                {
                    CompatManager.FA.Patched("HeadScale");
                    return mi;
                }
            }
            CompatManager.FA.Failed("HeadScale", "Base Worker ScaleFor signature not found");
            return null;
        }

        #endregion

        #region Postfix

        /// <summary>
        /// <b>Postfix</b> — 헤드 워커(NLFacialAnimationHeadNodeWorker)인 경우에만
        /// 변신 폼의 bodyDrawScale * headDrawScale을 결과 스케일에 곱한다.
        /// </summary>
        /// <param name="__result">원본 ScaleFor 결과(Vector3)</param>
        /// <param name="__0">node (PawnRenderNode)</param>
        /// <param name="__1">parms (PawnDrawParms)</param>
        static void Postfix(ref Vector3 __result, PawnRenderNode __0, PawnDrawParms __1)
        {
            try
            {
                var node = __0;
                var parms = __1;
                if (node == null) return;

                Pawn pawn = parms.pawn;
                if (pawn == null) return;

                // [안전] Facial Animation 컨트롤러가 있는 Pawn만 진행
                if (!HasFAControllerComp(pawn)) return;

                // [안전] 실제 워커가 FA 헤드 워커인지 판정(눈/입/기타 노드는 제외)
                object worker = TryGetWorker(node);
                if (worker == null || !T_FAHeadWorker.IsAssignableFrom(worker.GetType()))
                    return;

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
                // [안전] 실패 시 경고만 출력하고 원본 흐름 유지
                Log.Warning($"{CompatManager.LOG_FA} Head scale postfix failed: {e}");
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Pawn이 FacialAnimation 컨트롤러 컴프를 보유하는지 검사한다.
        /// </summary>
        private static bool HasFAControllerComp(Pawn pawn)
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
            catch (Exception e)
            {
                if (!CompatManager.FA.HasFailed("HasFAComp:Exception"))
                    CompatManager.FA.Failed("HasFAComp:Exception", e.Message);
            }
            return false;
        }

        /// <summary>
        /// PawnRenderNode의 실제 worker 인스턴스를 안전하게 획득한다.
        /// 프로퍼티/필드 양 경로를 모두 시도한다.
        /// </summary>
        private static object TryGetWorker(PawnRenderNode node)
        {
            if (node == null) return null;

            // [성능] 캐시된 리플렉션 헬퍼 우선
            var v = ShapeshiftReflectionCache.GetInstanceProperty<object>(node, "Worker");
            if (v != null) return v;

            return ShapeshiftReflectionCache.GetInstanceField<object>(node, "worker");
        }

        #endregion
    }
}
