// ShapeshifterFramework | Compat | Compat_FacialAnimation_HeadWorker_ScaleFor.cs
// 목적 : Facial Animation 머리 스케일 동기화 — 변신 시 FA 머리 스케일을 폼에 맞춰 적용.
// 용도 : FA HeadWorker ScaleFor Postfix로, 대상 워커가 NLFacialAnimationHeadNodeWorker일 때만 배율 적용. 타입은 ReflectionCache로 캐싱.

using HarmonyLib;
using ShapeshifterFramework.Utilities;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
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
            // FA 비활성이면 패치 비적용 — 실패 기록 불필요
            if (!CompatManager.FA.IsActive) return false;
            if (T_BaseWorker == null || T_FAHeadWorker == null)
            {
                CompatManager.FA.Failed("HeadScale", "types missing");
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
        static void Postfix(PawnRenderNodeWorker __instance, ref Vector3 __result, PawnRenderNode __0, PawnDrawParms __1)
        {
            try
            {
                // __instance가 곧 워커 — 리플렉션 없이 타입 체크만으로 FA 헤드 워커 판별
                if (__instance == null || !T_FAHeadWorker.IsInstanceOfType(__instance))
                    return;

                Pawn pawn = __1.pawn;
                if (pawn == null) return;

                // 비변신 폰 즉시 스킵
                if (!ShapeshiftRegistry.TryGet(pawn, out var comp, out var form)) return;

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
                // 렌더 핫패스 — 매 프레임 로그 스팸 방지를 위해 1회만 기록, 원본 유지.
                if (!CompatManager.FA.HasFailed("HeadScale:PostfixException"))
                    CompatManager.FA.Failed("HeadScale:PostfixException", e.Message);
            }
        }

        #endregion

        #region Helpers

        // FA 컨트롤러 보유 여부 캐시 — AllComps 순회 방지 (게임 중 FA 컴프는 변하지 않음)
        // ConditionalWeakTable: Pawn GC 시 캐시 엔트리 자동 제거 → stale 데이터 방지
        private static ConditionalWeakTable<Pawn, BoolBox> _faCompCache
            = new ConditionalWeakTable<Pawn, BoolBox>();

        /// <summary>ConditionalWeakTable 값 래퍼 (value type을 참조로 보관).</summary>
        private sealed class BoolBox { public bool value; public bool resolved; }

        /// <summary>게임 로드/전환 시 FA 컴프 캐시 정리.</summary>
        internal static void ClearFACompCache() { _faCompCache = new ConditionalWeakTable<Pawn, BoolBox>(); }

        /// <summary>Pawn이 FA 컨트롤러 컴프를 보유하는지 캐시 조회. 최초 1회만 AllComps 순회.</summary>
        private static bool HasFAControllerCompCached(Pawn pawn)
        {
            var box = _faCompCache.GetOrCreateValue(pawn);
            if (box.resolved) return box.value;

            box.value = HasFAControllerCompScan(pawn);
            box.resolved = true;
            return box.value;
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

        #endregion
    }
}
