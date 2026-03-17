// ShapeshifterFramework | Utilities | ShapeshiftCoreUtility.cs
// 목적 : HediffDef 기반 변신 진입점(ApplyShift) + 이벤트 콜백 + 조회 헬퍼.
// 용도 : 모든 변신 트리거는 이 유틸리티의 ApplyShift()를 호출하여 HediffDef를 부여.
//        바닐라 GiveHediff로도 변신 가능 (CompPostPostAdd → 자동 ApplyForm).
//        외부 모드 확장점: OnFormApplied / OnFormRemoved 이벤트.
// 주의 : ClearEvents()는 GameComponent.FinalizeInit에서 호출 (HarmonyInit은 모드 로드 시 1회만).

using ShapeshifterFramework.Hediffs;
using System;
using System.Collections.Generic;
using Verse;

namespace ShapeshifterFramework.Utilities
{
    /// <summary>HediffDef 기반 변신 진입점 + 이벤트 콜백 + 조회 헬퍼.</summary>
    public static class ShapeshiftCoreUtility
    {
        #region 이벤트 콜백

        /// <summary>변신 적용 후 발생. 외부 모드 확장점.</summary>
        public static event Action<Verse.Pawn, ShapeshiftFormDef> OnFormApplied;

        /// <summary>변신 해제 후 발생. 외부 모드 확장점.</summary>
        public static event Action<Verse.Pawn, ShapeshiftFormDef> OnFormRemoved;

        /// <summary>이벤트 핸들러 초기화. GameComponent.FinalizeInit에서 호출.</summary>
        public static void ClearEvents()
        {
            OnFormApplied = null;
            OnFormRemoved = null;
        }

        /// <summary>변신 적용 이벤트 발행 (HediffComp_ShapeshiftCore.ApplyForm에서 호출).</summary>
        internal static void FireFormApplied(Verse.Pawn pawn, ShapeshiftFormDef form)
        {
            if (pawn == null || form == null) return;
            try { OnFormApplied?.Invoke(pawn, form); }
            catch (Exception ex) { Log.Error($"[SSF] OnFormApplied event error: {ex}"); }
        }

        /// <summary>변신 해제 이벤트 발행 (HediffComp_ShapeshiftCore.RemoveForm에서 호출).</summary>
        internal static void FireFormRemoved(Verse.Pawn pawn, ShapeshiftFormDef form)
        {
            if (pawn == null || form == null) return;
            try { OnFormRemoved?.Invoke(pawn, form); }
            catch (Exception ex) { Log.Error($"[SSF] OnFormRemoved event error: {ex}"); }
        }

        #endregion

        #region 핵심 진입점

        /// <summary>
        /// HediffDef 기반 변신 진입 메서드.
        /// 기존 변신 hediff가 있으면 제거 후 새 hediff 부여.
        /// </summary>
        /// <param name="pawn">대상 Pawn.</param>
        /// <param name="hediffDef">부여할 HediffDef (HediffComp_ShapeshiftCore 포함 필수).</param>
        /// <param name="successChance">성공 확률 0~1. 기본 1.</param>
        /// <param name="sources">변신 유발 아이템. null이면 빈 리스트.</param>
        /// <param name="sourceItemRequireEquipped">true면 sourceItems가 장비 슬롯에 있어야 유지, false면 인벤토리 소지만으로 유지.</param>
        /// <returns>성공 시 true.</returns>
        public static bool ApplyShift(Verse.Pawn pawn, HediffDef hediffDef, float successChance = 1f, List<Thing> sources = null, bool sourceItemRequireEquipped = false)
        {
            if (pawn == null || pawn.Dead || hediffDef == null) return false;

            // 확률
            float p = UnityEngine.Mathf.Clamp01(successChance);
            if (p < 1f && Rand.Value > p)
            {
                RimWorld.Messages.Message("SSF_ShiftTarget_Resisted".Translate(pawn.LabelShortCap), RimWorld.MessageTypeDefOf.RejectInput, false);
                return false;
            }

            // 기존 변신 hediff 제거 (동시 적용 방지)
            if (pawn.health?.hediffSet != null)
            {
                var hediffs = pawn.health.hediffSet.hediffs;
                for (int i = hediffs.Count - 1; i >= 0; i--)
                {
                    var existingCore = (hediffs[i] as HediffWithComps)?.TryGetComp<HediffComp_ShapeshiftCore>();
                    if (existingCore != null)
                    {
                        if (existingCore.isTransformed)
                        {
                            try { existingCore.RemoveForm(); }
                            catch (Exception ex) { Log.Error($"[SSF] RemoveForm failed during re-transform for {pawn.Name}: {ex}"); }
                        }
                        pawn.health.RemoveHediff(hediffs[i]);
                        break; // 동시에 1개만
                    }
                }
            }

            // 새 hediff 생성 및 부여
            Hediff newHediff = HediffMaker.MakeHediff(hediffDef, pawn);
            if (newHediff == null)
            {
                Log.Error($"[SSF] HediffMaker.MakeHediff returned null for {hediffDef.defName}");
                return false;
            }

            // comp에 sources 사전 설정 (CompPostPostAdd 전에 설정 불가하므로, 부여 후 설정)
            pawn.health.AddHediff(newHediff);

            // 부여 후 comp 접근하여 sources 설정 + 확률 판정 중복 방지
            var core = (newHediff as HediffWithComps)?.TryGetComp<HediffComp_ShapeshiftCore>();
            if (core != null)
            {
                core.sourceItems = sources ?? new List<Thing>();
                core.sourceItemRequireEquipped = sourceItemRequireEquipped;
                core.successChanceAlreadyPassed = true; // needsInit에서 중복 확률 판정 방지
            }

            return true;
        }

        /// <summary>대상 Pawn의 변신 해제.</summary>
        public static void RemoveForm(Verse.Pawn pawn)
        {
            if (pawn == null) return;

            if (TryGetCore(pawn, out var core))
            {
                core.RemoveForm();
                // hediff 자체도 제거
                if (core.parent != null && pawn.health?.hediffSet != null
                    && pawn.health.hediffSet.hediffs.Contains(core.parent))
                {
                    pawn.health.RemoveHediff(core.parent);
                }
            }
        }

        #endregion

        #region 조회 헬퍼

        /// <summary>Pawn에서 HediffComp_ShapeshiftCore 조회 (레지스트리 우선, hediff 폴백).</summary>
        public static bool TryGetCore(Verse.Pawn pawn, out HediffComp_ShapeshiftCore core)
        {
            core = null;
            if (pawn == null) return false;

            // 레지스트리에서 O(1) 조회
            if (ShapeshiftRegistry.TryGet(pawn, out core, out _))
                return true;

            // 폴백: hediff 직접 탐색
            if (pawn.health?.hediffSet != null)
            {
                var hediffs = pawn.health.hediffSet.hediffs;
                for (int i = 0; i < hediffs.Count; i++)
                {
                    core = (hediffs[i] as HediffWithComps)?.TryGetComp<HediffComp_ShapeshiftCore>();
                    if (core != null) return true;
                }
            }

            return false;
        }

        /// <summary>Pawn에서 HediffComp_ShapeshiftCore + FormDef 동시 조회.</summary>
        public static bool TryGetCore(Verse.Pawn pawn, out HediffComp_ShapeshiftCore core, out ShapeshiftFormDef form)
        {
            form = null;
            if (TryGetCore(pawn, out core))
            {
                form = core.currentForm;
                return form != null;
            }
            return false;
        }

        #endregion
    }
}
