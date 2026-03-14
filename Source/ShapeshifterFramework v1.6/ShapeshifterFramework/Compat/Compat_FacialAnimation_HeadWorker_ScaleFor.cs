// ShapeshifterFramework | Compat | Compat_FacialAnimation_HeadWorker_ScaleFor.cs
// FA HeadWorker ScaleFor Postfix: 변신 시 FA 머리 스케일을 폼에 맞춰 동기화.
// 대상 워커가 NLFacialAnimationHeadNodeWorker일 때만 배율 적용. 타입은 ReflectionCache로 캐싱.

using HarmonyLib;
using ShapeshifterFramework.Utilities;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;

namespace ShapeshifterFramework.Compat
{
    /// <summary>FA HeadWorker ScaleFor Postfix. 헤드 워커에만 변신 폼 스케일(body * head) 적용.</summary>
    [HarmonyPatch]
    internal static class Compat_FacialAnimation_HeadWorker_ScaleFor
    {
        #region Cached Types & Accessors

        // 외부 모드 타입 캐시
        private static readonly Type T_FAComp = ShapeshiftReflectionCache.TryType("FacialAnimation.FacialAnimationControllerComp");
        private static readonly Type T_BaseWorker = ShapeshiftReflectionCache.TryType("Verse.PawnRenderNodeWorker");
        private static readonly Type T_FAHeadWorker = ShapeshiftReflectionCache.TryType("FacialAnimation.NLFacialAnimationHeadNodeWorker");

        #endregion

        #region Harmony Bootstrapping

        /// <summary>패치 적용 가능 여부 판정.</summary>
        static bool Prepare()
        {
            // FA 비활성이면 패치 비적용
            if (!CompatManager.FA.IsActive || T_BaseWorker == null || T_FAHeadWorker == null)
            {
                CompatManager.FA.Failed("HeadScale", "not active or types missing");
                return false;
            }
            return true;
        }

        /// <summary>대상 메서드 탐색. 정확 시그니처 우선, 폴백.</summary>
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

        /// <summary>Postfix: 헤드 워커면 폼의 body*head 스케일 적용.</summary>
        static void Postfix(ref Vector3 __result, PawnRenderNode __0, PawnDrawParms __1)
        {
            try
            {
                var node = __0;
                var parms = __1;
                if (node == null) return;

                Pawn pawn = parms.pawn;
                if (pawn == null) return;

                // 비변신 폰 즉시 스킵 — 렌더 핫패스에서 AllComps 순회/리플렉션 방지
                if (!ShapeshiftRegistry.TryGet(pawn, out var comp, out var form)) return;

                // FA 헤드 워커인지 먼저 판정 — 폰당 ~40개 노드 중 1개만 해당하므로 95%+ 탈락
                object worker = TryGetWorker(node);
                if (worker == null || !T_FAHeadWorker.IsAssignableFrom(worker.GetType()))
                    return;

                // FA 컨트롤러 없으면 스킵 (캐시로 AllComps 순회 방지)
                if (!HasFAControllerCompCached(pawn)) return;

                float factor = 1f;

                // body * head 적용
                float bodyS = form.bodyDrawScale ?? 1f;
                float headS = form.headDrawScale ?? 1f;
                if (!Mathf.Approximately(bodyS, 1f)) factor *= bodyS;
                if (!Mathf.Approximately(headS, 1f)) factor *= headS;

                if (!Mathf.Approximately(factor, 1f))
                    __result = new Vector3(__result.x * factor, __result.y, __result.z * factor);
            }
            catch (Exception e)
            {
                // 실패 시 경고, 원본 유지
                Log.Warning($"{CompatManager.LOG_FA} Head scale postfix failed: {e}");
            }
        }

        #endregion

        #region Helpers

        // FA 컨트롤러 보유 여부 캐시 — AllComps 순회 방지 (게임 중 FA 컴프는 변하지 않음)
        private static readonly Dictionary<int, bool> _faCompCache = new Dictionary<int, bool>(256);

        /// <summary>게임 로드/전환 시 FA 컴프 캐시 정리.</summary>
        internal static void ClearFACompCache() { _faCompCache.Clear(); }

        /// <summary>Pawn이 FA 컨트롤러 컴프를 보유하는지 캐시 조회. 최초 1회만 AllComps 순회.</summary>
        private static bool HasFAControllerCompCached(Pawn pawn)
        {
            int id = pawn.thingIDNumber;
            if (_faCompCache.TryGetValue(id, out bool has))
                return has;

            has = HasFAControllerCompScan(pawn);
            _faCompCache[id] = has;
            return has;
        }

        /// <summary>Pawn이 FA 컨트롤러 컴프를 보유하는지 실제 검사.</summary>
        private static bool HasFAControllerCompScan(Pawn pawn)
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

        /// <summary>PawnRenderNode에서 worker 인스턴스 획득(프로퍼티/필드 폴백).</summary>
        private static object TryGetWorker(PawnRenderNode node)
        {
            if (node == null) return null;

            // 캐시된 리플렉션 헬퍼 우선
            var v = ShapeshiftReflectionCache.GetInstanceProperty<object>(node, "Worker");
            if (v != null) return v;

            return ShapeshiftReflectionCache.GetInstanceField<object>(node, "worker");
        }

        #endregion
    }
}
