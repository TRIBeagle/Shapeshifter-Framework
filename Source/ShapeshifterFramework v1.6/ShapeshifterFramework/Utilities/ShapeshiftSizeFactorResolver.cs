// ShapeshifterFramework | Utilities | ShapeshiftSizeFactorResolver.cs
// 목적 : 폼에 지정된 단순 배율(bodyDrawScale 등)을 바닐라 렌더링의 4대 요소(bodyWidth, headSizeFactor, bodySizeFactor, attachPointScaleFactor)로 변환계산.
// 용도 : 복잡한 스케일 연산이 매 프레임마다 반복되는 것을 막기 위해, Time.frameCount를 기준으로 해당 프레임에 이미 계산이 끝난 폰의 결과값을 즉시 반환하는 고효율 캐싱(Frame Cache)을 적용함.

using System.Collections.Concurrent;
using UnityEngine;
using Verse;

namespace ShapeshifterFramework.Utilities
{
    internal static class ShapeshiftSizeFactorResolver
    {
        internal struct Factors
        {
            public float bodySizeFactor;         // 바닐라: sqrt(...)로 머리 기본오프셋에 영향
            public float bodyWidth;              // 바닐라: 몸/의복 메쉬 크기
            public float headSizeFactor;         // 바닐라: 헤드/헤어/모자 메쉬 크기
            public float attachPointScaleFactor; // 바닐라: 부착점(무기/등짐) 거리
            public float bodyWidthRatio;         // GetShapeScale 전용: eff.bodyWidth / 바닐라 baseBodyWidth (= sBody)
            public float headSizeRatio;          // GetShapeScale 전용: eff.headSizeFactor / 바닐라 baseHead (= sBody*sHead)
        }

        internal static bool TryGetOverrides(Pawn pawn, out Factors f)
        {
            var ls = pawn != null ? pawn.ageTracker != null ? pawn.ageTracker.CurLifeStage : null : null;

            // 바닐라 기본값
            float baseBodyWidth = (ls != null && ls.bodyWidth.HasValue) ? ls.bodyWidth.Value : 1.5f;
            float baseHead = (ls != null && ls.headSizeFactor.HasValue) ? ls.headSizeFactor.Value : 1f;
            float baseAttach = (ls != null) ? ls.attachPointScaleFactor : 1f;
            float baseBodyFac = (ls != null) ? ls.bodySizeFactor : 1f;

            f = new Factors
            {
                bodyWidth = baseBodyWidth,
                headSizeFactor = baseHead,
                attachPointScaleFactor = baseAttach,
                bodySizeFactor = baseBodyFac,
                bodyWidthRatio = 1f,   // 비변신 기본: 배수 없음
                headSizeRatio = 1f
            };

            if (!ShapeshiftRegistry.TryGet(pawn, out var comp, out var form)) return false;

            // 입력 배수(비우면 1)
            float sBody = form.bodyDrawScale.HasValue ? Mathf.Max(0.01f, form.bodyDrawScale.Value) : 1f;
            float sHead = form.headDrawScale.HasValue ? Mathf.Max(0.01f, form.headDrawScale.Value) : 1f;

            // 기본 연동 규칙:
            //  - bodyWidth           = baseBodyWidth  * sBody
            //  - headSizeFactor      = baseHead       * sBody * sHead  (헤드는 바디 추가 배수)
            //  - attachPointScale... = baseAttach     * sBody
            //  - bodySizeFactor      = baseBodyFac    * sBody^2  (√를 거치면 sBody가 되도록)
            f.bodyWidth = baseBodyWidth * sBody;
            f.headSizeFactor = baseHead * sBody * sHead;
            f.attachPointScaleFactor = baseAttach * sBody;
            f.bodySizeFactor = baseBodyFac * sBody * sBody;

            // GetShapeScale 비율 미리 계산 (base 약분 → 사실상 변신 배수). base=0 가드 보존.
            f.bodyWidthRatio = Mathf.Approximately(baseBodyWidth, 0f) ? 1f : (f.bodyWidth / baseBodyWidth);
            f.headSizeRatio = Mathf.Approximately(baseHead, 0f) ? 1f : (f.headSizeFactor / baseHead);

            return true;
        }

        // volatile: 멀티스레드 환경에서 읽기 가시성 보장
        private static volatile int _cacheFrame = -1;
        private static ConcurrentDictionary<Pawn, Factors> _frameCache = new ConcurrentDictionary<Pawn, Factors>();
        private static readonly object _frameLock = new object();

        internal static Factors Effective(Pawn pawn)
        {
            if (pawn == null) return default(Factors);

            int frame = Time.frameCount;
            // lock으로 프레임 전환 시 Clear/TryAdd 경합 방지
            if (frame != _cacheFrame)
            {
                lock (_frameLock)
                {
                    // double-check: lock 획득 사이에 다른 스레드가 이미 처리했을 수 있음
                    if (frame != _cacheFrame)
                    {
                        _frameCache.Clear();
                        _cacheFrame = frame;
                    }
                }
            }

            // ConcurrentDictionary.TryGetValue는 스레드 세이프
            Factors cached;
            if (_frameCache.TryGetValue(pawn, out cached))
            {
                return cached;
            }

            TryGetOverrides(pawn, out Factors f);
            _frameCache.TryAdd(pawn, f); // TryAdd로 경합 안전 — 중복 삽입 시 무시
            return f;
        }

        /// <summary>GetShapeScale 전용: 프레임 캐시에서 변신 스케일 비율을 즉시 반환.
        /// base 재조회/재나눗셈 없이 TryGetOverrides 단계에서 계산해 둔 비율을 그대로 사용.</summary>
        internal static float GetScaleRatio(Pawn pawn, bool useHeadScale)
        {
            if (pawn == null) return 1f;
            var f = Effective(pawn);
            return useHeadScale ? f.headSizeRatio : f.bodyWidthRatio;
        }
    }
}
