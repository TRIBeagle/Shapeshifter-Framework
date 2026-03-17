// ShapeshifterFramework | Utilities | ShapeshiftCoreUtility.cs
// 목적 : 변신 이벤트 콜백 + 조회 헬퍼 + 저항 판정.
// 용도 : 변신은 바닐라 AddHediff로 진입 → CompPostPostAdd → 첫 Tick ApplyForm.
//        외부 모드 확장점: OnFormApplied / OnFormRemoved 이벤트.
//        저항 판정: 적대적 변신 시 대상의 스탯 기반 확률 체크 (능력/투사체 공용).
// 주의 : ClearEvents()는 GameComponent.FinalizeInit에서 호출 (HarmonyInit은 모드 로드 시 1회만).

using RimWorld;
using ShapeshifterFramework.Hediffs;
using System;
using System.Collections.Generic;
using Verse;

namespace ShapeshifterFramework.Utilities
{
    /// <summary>
    /// 저항 스탯의 방향성.
    /// Sensitivity: 높을수록 취약 (PsychicSensitivity 등). chance = base × stat.
    /// Resistance: 높을수록 면역 (ToxicResistance 등). chance = base × (1 - clamp01(stat)).
    /// </summary>
    public enum ResistMode
    {
        /// <summary>높을수록 취약. 예: PsychicSensitivity (0=면역, 2=매우 취약).</summary>
        Sensitivity,
        /// <summary>높을수록 면역. 예: ToxicResistance (0=무방비, 1=완전 면역).</summary>
        Resistance
    }

    /// <summary>변신 이벤트 콜백 + 조회 헬퍼 + 저항 판정.</summary>
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

        #region 저항 판정

        /// <summary>
        /// resistStat과 resistMode에 따라 최종 성공 확률을 계산한다.
        /// Sensitivity: chance = base × statValue (높을수록 취약).
        /// Resistance:  chance = base × (1 - clamp01(statValue)) (높을수록 면역).
        /// </summary>
        private static float CalcSuccessChance(Pawn target, float baseSuccessChance, StatDef resistStat, ResistMode mode)
        {
            float stat = target.GetStatValue(resistStat);
            if (mode == ResistMode.Resistance)
                return baseSuccessChance * (1f - UnityEngine.Mathf.Clamp01(stat));
            return baseSuccessChance * stat;
        }

        /// <summary>
        /// 능력(Ability) 기반 변신 저항 판정.
        /// 아군 대상은 바닐라 Psycast 패턴에 따라 저항 체크를 생략한다 (항상 성공).
        /// </summary>
        /// <param name="caster">시전자. null이면 저항 체크 생략.</param>
        /// <param name="target">대상 Pawn.</param>
        /// <param name="baseSuccessChance">기본 성공 확률 (0~1). 1 이상이면 항상 성공.</param>
        /// <param name="resistStat">저항에 사용할 StatDef. null이면 저항 체크 생략 (항상 성공).</param>
        /// <param name="mode">스탯 방향성. Sensitivity(높을수록 취약) / Resistance(높을수록 면역).</param>
        /// <returns>true면 저항 성공 (변신 안 됨).</returns>
        public static bool ResistAbility(Pawn caster, Pawn target, float baseSuccessChance, StatDef resistStat, ResistMode mode = ResistMode.Sensitivity)
        {
            if (resistStat == null) return false;
            if (baseSuccessChance >= 1f && mode == ResistMode.Sensitivity) return false;
            if (caster == null || caster == target) return false;
            if (!target.HostileTo(caster)) return false;

            float chance = CalcSuccessChance(target, baseSuccessChance, resistStat, mode);
            return !Rand.Chance(chance);
        }

        /// <summary>
        /// 투사체(Projectile) 기반 변신 저항 판정.
        /// 투사체는 부정적 효과이므로 아군 면제 없이 모든 대상에게 저항 체크를 수행한다.
        /// </summary>
        /// <param name="target">대상 Pawn.</param>
        /// <param name="baseSuccessChance">기본 성공 확률 (0~1). 1 이상이면 항상 성공.</param>
        /// <param name="resistStat">저항에 사용할 StatDef. null이면 저항 체크 생략 (항상 성공).</param>
        /// <param name="mode">스탯 방향성. Sensitivity(높을수록 취약) / Resistance(높을수록 면역).</param>
        /// <returns>true면 저항 성공 (변신 안 됨).</returns>
        public static bool ResistProjectile(Pawn target, float baseSuccessChance, StatDef resistStat, ResistMode mode = ResistMode.Sensitivity)
        {
            if (resistStat == null) return false;
            if (baseSuccessChance >= 1f && mode == ResistMode.Sensitivity) return false;

            float chance = CalcSuccessChance(target, baseSuccessChance, resistStat, mode);
            return !Rand.Chance(chance);
        }

        #endregion
    }
}
