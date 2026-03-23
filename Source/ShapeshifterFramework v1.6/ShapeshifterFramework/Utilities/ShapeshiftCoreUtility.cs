// ShapeshifterFramework | Utilities | ShapeshiftCoreUtility.cs
// 목적 : 변신 이벤트 콜백 + 조회 헬퍼.
// 용도 : 변신은 바닐라 AddHediff로 진입 → CompPostPostAdd → 첫 Tick ApplyForm.
//        외부 모드 확장점: OnFormApplied / OnFormRemoved 이벤트.
// 주의 : ClearEvents()는 GameComponent.FinalizeInit에서 호출 (HarmonyInit은 모드 로드 시 1회만).

using ShapeshifterFramework.Hediffs;
using System;
using System.Collections.Generic;
using Verse;

namespace ShapeshifterFramework.Utilities
{
    /// <summary>변신 이벤트 콜백 + 조회 헬퍼.</summary>
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

        #region 변신 부여/해제

        /// <summary>
        /// HediffDef를 부여하여 변신을 시작합니다.
        /// CompPostPostAdd → 첫 Tick에서 자동으로 ApplyForm이 실행됩니다.
        /// </summary>
        /// <param name="pawn">대상 Pawn.</param>
        /// <param name="hediffDef">부여할 HediffDef (HediffComp_ShapeshiftCore 포함 필수).</param>
        /// <param name="sources">변신 유발 아이템. null이면 빈 리스트. 장착 해제 시 변신 해제.</param>
        public static void GiveShiftHediff(Verse.Pawn pawn, HediffDef hediffDef, List<Thing> sources = null)
        {
            if (pawn == null || pawn.Dead || hediffDef == null) return;

            // 동일 HediffDef가 이미 존재하면 중복 적용 불가 — 메시지만 표시
            if (pawn.health?.hediffSet?.HasHediff(hediffDef) == true)
            {
                Messages.Message("SSF_Message_SameFormActive".Translate(), pawn, MessageTypeDefOf.RejectInput, false);
                return;
            }

            Hediff newHediff = HediffMaker.MakeHediff(hediffDef, pawn);
            if (newHediff == null)
            {
                Log.Error($"[SSF] HediffMaker.MakeHediff returned null for {hediffDef.defName}");
                return;
            }

            pawn.health.AddHediff(newHediff);

            // 부여 후 comp에 sourceItems 설정
            if (sources != null && sources.Count > 0)
            {
                var core = (newHediff as HediffWithComps)?.TryGetComp<HediffComp_ShapeshiftCore>();
                if (core != null)
                    core.sourceItems = sources;
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

        #endregion
    }
}
