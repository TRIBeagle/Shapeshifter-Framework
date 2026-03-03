// ShapeshiftSizeFactorResolver.cs
// 목적: 폼 스케일 입력을 바닐라 4값(bodyWidth/headSizeFactor/attachPoint/bodySizeFactor)로 변환.
// 용도: Effective/ TryGetOverrides로 계산해 각 렌더 패치에서 소비.
// 주의: 바닐라 기본값 폴백 보장. 0/음수 방지 처리 및 비율 1이면 무변경.

using ShapeshifterFramework.Comps;
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

            // 변신 폼이면 오버라이드했다고 간주(값이 1이어도 변신 중이면 true로 처리 무방)
            return true;
        }

        private static int _cacheFrame = -1;
        private static System.Collections.Generic.Dictionary<Pawn, Factors> _frameCache = new System.Collections.Generic.Dictionary<Pawn, Factors>();

        internal static Factors Effective(Pawn pawn)
        {
            if (pawn == null) return default(Factors);

            int frame = Time.frameCount;
            if (frame != _cacheFrame)
            {
                _cacheFrame = frame;
                _frameCache.Clear(); // 새 프레임이 시작되면 캐시 일괄 비우기
            }

            // 이번 프레임에 이미 계산이 끝난 폰이면 저장된 값을 즉시 반환
            if (_frameCache.TryGetValue(pawn, out Factors cached))
            {
                return cached;
            }

            TryGetOverrides(pawn, out Factors f);
            _frameCache[pawn] = f;
            return f;
        }
    }
}
