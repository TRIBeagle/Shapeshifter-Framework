// ShapeshifterFramework | Utilities | ShapeshiftSizeFactorResolver.cs
// 목적 : 폼에 지정된 단순 배율(bodyDrawScale 등)을 바닐라 렌더링의 4대 요소(bodyWidth, headSizeFactor, bodySizeFactor, attachPointScaleFactor)로 변환계산.
// 용도 : 복잡한 스케일 연산이 매 프레임마다 반복되는 것을 막기 위해, Time.frameCount를 기준으로 해당 프레임에 이미 계산이 끝난 폰의 결과값을 즉시 반환하는 고효율 캐싱(Frame Cache)을 적용함.

using ShapeshifterFramework.Comps;
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
                bodySizeFactor = baseBodyFac
            };

            var comp = pawn != null ? pawn.TryGetComp<CompShapeshifter>() : null;
            var form = comp != null ? comp.currentForm : null;
            if (comp == null || !comp.isTransformed || form == null) return false;

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

            return true;
        }

        // [수정] volatile로 선언하여 렌더링 멀티스레드 환경에서 _cacheFrame 읽기 가시성 보장
        private static volatile int _cacheFrame = -1;
        private static ConcurrentDictionary<Pawn, Factors> _frameCache = new ConcurrentDictionary<Pawn, Factors>();
        private static readonly object _frameLock = new object();

        internal static Factors Effective(Pawn pawn)
        {
            if (pawn == null) return default(Factors);

            int frame = Time.frameCount;
            // [수정] lock으로 프레임 전환 시 Clear()와 TryAdd()의 경합을 방지
            // 렌더링 스레드에서 한 스레드가 Clear() 중 다른 스레드가 TryAdd하면
            // 방금 추가한 데이터가 즉시 삭제되는 레이스 컨디션이 있었음
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
    }
}
