// ShapeshifterFramework | Compat | CompatManager.cs
// 목적   : 외부 모드(HAR/FA)별 호환 패치 상태를 집계·보고하는 경량 매니저(구 ShapeshiftCompat).
// 용도   : 패치 성공/실패/메트릭을 누적하고, ReportAllOnce에서 1회 요약을 출력한다.
// 변경   : 2025-09-22 v1.1 — ShapeshiftCompat → CompatManager로 리네이밍(주석 규칙 적용, 로직 변경 없음).
// 주의   : Report 이전에는 Failed 집계만 수행하고, Report 이후 런타임에는 동일 id에 대해 1회 경고를 즉시 출력.

using System.Collections.Generic;
using Verse;

namespace ShapeshifterFramework.Compat
{
    /// <summary>
    /// 모드별 집계기. 패치 성공/실패, 메트릭(added/deduped 등), 1회 보고(ReportOnce)를 담당한다.
    /// 정책:
    /// - <see cref="Failed(string, string)"/>: Report 이전엔 집계만, Report 이후엔 동일 id 경고를 즉시 1회 출력.
    /// - <see cref="HasFailed(string)"/>: 동일 id 중복 경고 억제용.
    /// - <see cref="ReportOnce()"/>: 모드별 요약/카운트/실패 목록을 한 번만 출력.
    /// </summary>
    internal sealed class CompatMod
    {
        internal readonly string PackageId;
        internal readonly string LogPrefix;

        private readonly HashSet<string> ok = new HashSet<string>();
        private readonly Dictionary<string, string> fail = new Dictionary<string, string>();
        private readonly Dictionary<string, Dictionary<string, long>> metrics =
            new Dictionary<string, Dictionary<string, long>>();

        private bool reported;

        /// <summary>패키지 식별자/로그 프리픽스를 바인딩한다.</summary>
        internal CompatMod(string packageId, string logPrefix)
        {
            PackageId = packageId;
            LogPrefix = logPrefix;
        }

        /// <summary>대상 모드가 현재 활성인지.</summary>
        internal bool IsActive => CompatManager.IsActive(PackageId);

        /// <summary>성공(패치 적용) 카운트.</summary>
        internal int OkCount => ok.Count;

        /// <summary>실패(패치 미적용/오류) 카운트.</summary>
        internal int FailCount => fail.Count;

        /// <summary>패치 적용 성공 id 등록.</summary>
        internal void Patched(string id) => ok.Add(id);

        /// <summary>
        /// 실패 id와 사유를 기록. ReportOnce 이후라면 동일 id 경고를 즉시 1회 출력한다.
        /// </summary>
        internal void Failed(string id, string reason)
        {
            // 시작 단계에선 집계만, Report 이후(게임 도중)는 즉시 경고도 1회 출력
            fail[id] = reason;
            if (reported) Log.Warning($"{LogPrefix} {id} failed: {reason}");
        }

        /// <summary>동일 실패 id가 이미 보고/집계되었는지.</summary>
        internal bool HasFailed(string id) => fail.ContainsKey(id);

        /// <summary>메트릭 값을 설정(예: AddComp 요약).</summary>
        internal void MetricSet(string scope, string key, long value)
        {
            if (!metrics.TryGetValue(scope, out var bag))
                metrics[scope] = bag = new Dictionary<string, long>();
            bag[key] = value;
        }

        /// <summary>메트릭 값을 누적.</summary>
        internal void MetricAdd(string scope, string key, long delta = 1)
        {
            if (!metrics.TryGetValue(scope, out var bag))
                metrics[scope] = bag = new Dictionary<string, long>();
            bag.TryGetValue(key, out var cur);
            bag[key] = cur + delta;
        }

        /// <summary>
        /// 모드별 보고를 정확히 한 번만 수행한다.
        /// - AddComp 요약(added/deduped)
        /// - 패치 카운트(성공/실패)
        /// - 실패 목록(경고)
        /// </summary>
        internal void ReportOnce()
        {
            if (reported || !IsActive) return;
            reported = true;

            // 1) AddComp 요약
            if (metrics.TryGetValue("AddComp", out var bag))
            {
                long added = 0, deduped = 0;
                bag.TryGetValue("added", out added);
                bag.TryGetValue("deduped", out deduped);
                Log.Message($"{LogPrefix} AddComp summary: added={added}, deduped={deduped}");
            }

            // 2) 패치 카운트
            if (FailCount == 0)
                Log.Message($"{LogPrefix} {OkCount} compatibility patches active.");
            else
                Log.Message($"{LogPrefix} compatibility patches partial: ok={OkCount}, failed={FailCount}.");

            // 3) 실패 목록
            if (FailCount > 0)
            {
                foreach (var kv in fail)
                    Log.Warning($"{LogPrefix} {kv.Key} failed: {kv.Value}");
            }
        }
    }

    /// <summary>
    /// 호환 패치 전역 엔트리. 모드 활성 감지, CompatMod 인스턴스 보관,
    /// Report 시점 준비(RegisterBeforeReport) 및 모드별 ReportOnce 집행을 담당한다.
    /// </summary>
    internal static class CompatManager
    {
        // ── 외부 패키지 ID ──
        internal const string Pkg_HAR = "erdelf.HumanoidAlienRaces";
        internal const string Pkg_FA = "Nals.FacialAnimation";

        // ── 로그 프리픽스 ──
        internal const string LOG_HAR = "[SSF/HAR]";
        internal const string LOG_FA = "[SSF/FA]";

        /// <summary>
        /// 모드 활성 확인. <paramref name="ignorePostfix"/>는 RimWorld의 ModLister 규약을 따른다.
        /// </summary>
        internal static bool IsActive(string packageId, bool ignorePostfix = false)
            => ModLister.GetActiveModWithIdentifier(packageId, ignorePostfix) != null;

        /// <summary>HAR/FA 집계기.</summary>
        internal static readonly CompatMod HAR = new CompatMod(Pkg_HAR, LOG_HAR);
        internal static readonly CompatMod FA = new CompatMod(Pkg_FA, LOG_FA);

        /// <summary>
        /// Report 이전 준비 단계:
        /// - HAR: AddComp 실행 보장(요약 메트릭 준비)
        /// - FA : 시작 시 폼 유효성 검증(잘못된 TypeDef defName 선보고)
        /// </summary>
        private static void RegisterBeforeReport()
        {
            // HAR: AddComp 실행 보장(요약 메트릭 준비)
            if (HAR.IsActive)
            {
                try { Compat_HAR_AddComp.EnsureInitialized(); }
                catch (System.Exception e) { Log.Warning($"{HAR.LogPrefix} Compatibility failed to load: {e.Message}"); }
            }
            // FA: 시작 시 폼 유효성 검증(잘못된 TypeDef 이름을 시작 로그에 누적)
            if (FA.IsActive)
            {
                try { FacialAnimationCompat.ValidateAllForms(); }
                catch (System.Exception e) { Log.Warning($"{FA.LogPrefix} Compatibility failed to load: {e.Message}"); }
            }
        }

        /// <summary>
        /// 모든 모드의 보고를 수행한다(각 1회).
        /// 모두 성공이면 최종 OK 메시지를 출력한다.
        /// </summary>
        internal static void ReportAllOnce()
        {
            RegisterBeforeReport();

            bool anyActive = false;
            bool allOk = true;

            if (HAR.IsActive) { anyActive = true; HAR.ReportOnce(); allOk &= (HAR.FailCount == 0); }
            if (FA.IsActive) { anyActive = true; FA.ReportOnce(); allOk &= (FA.FailCount == 0); }

            if (anyActive && allOk)
            {
                var mods = new System.Collections.Generic.List<string>();
                if (HAR.IsActive) mods.Add("HAR");
                if (FA.IsActive) mods.Add("FA");
                Log.Message($"[SSF] all compatibility patches active ({string.Join(", ", mods)}).");
            }
        }
    }
}
