// ShapeshifterFramework | Compat | CompatManager.cs
// 목적 : 호환 패치 초기화·에러 집계·1회 보고 매니저
// 용도 : ModLister로 모드 활성 판별, 패치 성공/실패·메트릭을 캐싱 후 ReportAllOnce로 요약
// 주의 : Report 이후 런타임 에러는 동일 id당 1회만 경고

using System.Collections.Generic;
using Verse;

namespace ShapeshifterFramework.Compat
{
    /// <summary>모드별 패치 성공/실패 집계 및 1회 보고 담당.</summary>
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

        /// <summary>추가 패키지 ID (구버전 등). null이면 무시.</summary>
        internal string AltPackageId;

        internal bool IsActive => CompatManager.IsActive(PackageId)
            || (!string.IsNullOrEmpty(AltPackageId) && CompatManager.IsActive(AltPackageId));

        internal int OkCount => ok.Count;

        internal int FailCount => fail.Count;

        internal void Patched(string id) => ok.Add(id);

        /// <summary>실패 기록. Report 이후면 즉시 경고.</summary>
        internal void Failed(string id, string reason)
        {
            // 시작 단계: 집계만, Report 이후: 즉시 경고
            fail[id] = reason;
            if (reported) Log.Warning($"{LogPrefix} {id} failed: {reason}");
        }

        internal bool HasFailed(string id) => fail.ContainsKey(id);

        /// <summary>메트릭 값 설정.</summary>
        internal void MetricSet(string scope, string key, long value)
        {
            if (!metrics.TryGetValue(scope, out var bag))
                metrics[scope] = bag = new Dictionary<string, long>();
            bag[key] = value;
        }

        /// <summary>메트릭 값 누적.</summary>
        internal void MetricAdd(string scope, string key, long delta = 1)
        {
            if (!metrics.TryGetValue(scope, out var bag))
                metrics[scope] = bag = new Dictionary<string, long>();
            bag.TryGetValue(key, out var cur);
            bag[key] = cur + delta;
        }

        /// <summary>모드별 요약을 1회만 출력.</summary>
        internal void ReportOnce()
        {
            if (reported || !IsActive) return;
            reported = true;

            // AddComp 요약
            if (metrics.TryGetValue("AddComp", out var bag))
            {
                long added = 0, deduped = 0;
                bag.TryGetValue("added", out added);
                bag.TryGetValue("deduped", out deduped);
                Log.Message($"{LogPrefix} AddComp summary: added={added}, deduped={deduped}");
            }

            // 패치 카운트
            if (FailCount == 0)
                Log.Message($"{LogPrefix} {OkCount} compatibility patches active.");
            else
                Log.Message($"{LogPrefix} compatibility patches partial: ok={OkCount}, failed={FailCount}.");

            // 실패 목록
            if (FailCount > 0)
            {
                foreach (var kv in fail)
                    Log.Warning($"{LogPrefix} {kv.Key} failed: {kv.Value}");
            }
        }
    }

    /// <summary>호환 패치 전역 엔트리.</summary>
    internal static class CompatManager
    {
        internal const string Pkg_HAR = "erdelf.HumanoidAlienRaces";
        internal const string Pkg_FA = "Nals.FacialAnimation";
        internal const string Pkg_SS = "PeteTimesSix.SimpleSidearms";
        internal const string Pkg_Yayo = "Mlie.YayosCombat3";
        internal const string Pkg_Yayo_Legacy = "com.yayo.combat3";

        internal const string LOG_HAR = "[SSF/HAR]";
        internal const string LOG_FA = "[SSF/FA]";
        internal const string LOG_SS = "[SSF/SS]";
        internal const string LOG_Yayo = "[SSF/Yayo]";

        /// <summary>모드 활성 확인.</summary>
        internal static bool IsActive(string packageId, bool ignorePostfix = false)
            => ModLister.GetActiveModWithIdentifier(packageId, ignorePostfix) != null;

        internal static readonly CompatMod HAR = new CompatMod(Pkg_HAR, LOG_HAR);
        internal static readonly CompatMod FA = new CompatMod(Pkg_FA, LOG_FA);
        internal static readonly CompatMod SS = new CompatMod(Pkg_SS, LOG_SS);
        internal static readonly CompatMod Yayo = new CompatMod(Pkg_Yayo, LOG_Yayo) { AltPackageId = Pkg_Yayo_Legacy };

        /// <summary>Report 전 준비.</summary>
        private static void RegisterBeforeReport()
        {
            // HAR AddComp 보장
            if (HAR.IsActive)
            {
                try { Compat_HAR_AddComp.EnsureInitialized(); }
                catch (System.Exception e) { Log.Warning($"{HAR.LogPrefix} Compatibility failed to load: {e.Message}"); }
            }
            // FA 폼 검증
            if (FA.IsActive)
            {
                try { FacialAnimationCompat.ValidateAllForms(); }
                catch (System.Exception e) { Log.Warning($"{FA.LogPrefix} Compatibility failed to load: {e.Message}"); }
            }
            // Yayo's Combat 감지
            if (Yayo.IsActive)
            {
                try { Compat_YayoCombat.DetectAndLog(); }
                catch (System.Exception e) { Log.Warning($"{Yayo.LogPrefix} Compatibility failed to load: {e.Message}"); }
            }
        }

        /// <summary>모든 모드 보고(각 1회).</summary>
        internal static void ReportAllOnce()
        {
            RegisterBeforeReport();

            bool anyActive = false;
            bool allOk = true;

            if (HAR.IsActive) { anyActive = true; HAR.ReportOnce(); allOk &= (HAR.FailCount == 0); }
            if (FA.IsActive) { anyActive = true; FA.ReportOnce(); allOk &= (FA.FailCount == 0); }
            if (SS.IsActive) { anyActive = true; SS.ReportOnce(); allOk &= (SS.FailCount == 0); }
            if (Yayo.IsActive) { anyActive = true; Yayo.ReportOnce(); allOk &= (Yayo.FailCount == 0); }

            if (anyActive && allOk)
            {
                var mods = new System.Collections.Generic.List<string>();
                if (HAR.IsActive) mods.Add("HAR");
                if (FA.IsActive) mods.Add("FA");
                if (SS.IsActive) mods.Add("SS");
                if (Yayo.IsActive) mods.Add("Yayo");
                Log.Message($"[SSF] all compatibility patches active ({string.Join(", ", mods)}).");
            }
        }
    }
}
