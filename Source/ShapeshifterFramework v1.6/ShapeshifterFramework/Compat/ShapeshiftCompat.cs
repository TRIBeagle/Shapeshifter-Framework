// ShapeshiftCompat.cs
// 정책 요약:
// - Failed(id, reason): ReportOnce() 이전엔 집계만, 이후(런타임)엔 즉시 1회 경고 출력
// - HasFailed(id): 동일 id 중복 경고 억제용
// - ReportAllOnce(): 준비(RegisterBeforeReport) → 모드별 요약/카운트/실패목록 출력

using System.Collections.Generic;
using Verse;

namespace ShapeshifterFramework.Compat
{
    // 모드별 집계기
    internal sealed class CompatMod
    {
        internal readonly string PackageId;
        internal readonly string LogPrefix;

        private readonly HashSet<string> ok = new HashSet<string>();
        private readonly Dictionary<string, string> fail = new Dictionary<string, string>();
        private readonly Dictionary<string, Dictionary<string, long>> metrics =
            new Dictionary<string, Dictionary<string, long>>();

        private bool reported;

        internal CompatMod(string packageId, string logPrefix)
        {
            PackageId = packageId;
            LogPrefix = logPrefix;
        }

        internal bool IsActive => ShapeshiftCompat.IsActive(PackageId);
        internal int OkCount => ok.Count;
        internal int FailCount => fail.Count;

        internal void Patched(string id) => ok.Add(id);

        internal void Failed(string id, string reason)
        {
            // 시작 단계에선 집계만, Report 이후(게임 도중)는 즉시 경고도 1회 출력
            fail[id] = reason;
            if (reported) Log.Warning($"{LogPrefix} {id} failed: {reason}");
        }

        // 중복 경고 억제용
        internal bool HasFailed(string id) => fail.ContainsKey(id);

        // 메트릭(예: AddComp 요약)
        internal void MetricSet(string scope, string key, long value)
        {
            if (!metrics.TryGetValue(scope, out var bag))
                metrics[scope] = bag = new Dictionary<string, long>();
            bag[key] = value;
        }

        internal void MetricAdd(string scope, string key, long delta = 1)
        {
            if (!metrics.TryGetValue(scope, out var bag))
                metrics[scope] = bag = new Dictionary<string, long>();
            bag.TryGetValue(key, out var cur);
            bag[key] = cur + delta;
        }

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

    internal static class ShapeshiftCompat
    {
        internal const string Pkg_HAR = "erdelf.HumanoidAlienRaces";
        internal const string Pkg_FA = "Nals.FacialAnimation";

        internal const string LOG_HAR = "[SSF/HAR]";
        internal const string LOG_FA = "[SSF/FA]";

        internal static bool IsActive(string packageId, bool ignorePostfix = false)
            => ModLister.GetActiveModWithIdentifier(packageId, ignorePostfix) != null;

        internal static readonly CompatMod HAR = new CompatMod(Pkg_HAR, LOG_HAR);
        internal static readonly CompatMod FA = new CompatMod(Pkg_FA, LOG_FA);

        // Report 전에 준비단계
        private static void RegisterBeforeReport()
        {
            // HAR: AddComp 실행 보장(요약 메트릭 준비)
            if (HAR.IsActive)
            {
                try { Compat_HAR_AddComp.EnsureInitialized(); } catch { }
            }
            // FA: 시작 시 폼 유효성 검증(잘못된 TypeDef 이름을 시작 로그에 누적)
            if (FA.IsActive)
            {
                try { FacialAnimationCompat.ValidateAllForms(); } catch { }
            }
        }

        internal static void ReportAllOnce()
        {
            RegisterBeforeReport();

            bool anyActive = false;
            bool allOk = true;

            if (HAR.IsActive) { anyActive = true; HAR.ReportOnce(); allOk &= (HAR.FailCount == 0); }
            if (FA.IsActive) { anyActive = true; FA.ReportOnce(); allOk &= (FA.FailCount == 0); }

            if (anyActive && allOk)
                Log.Message("[Shapeshift] all compatibility patches active.");
        }
    }
}
