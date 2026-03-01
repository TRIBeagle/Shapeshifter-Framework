// .NET Framework 4.8 / C# 7.3
// 목적: 변신 FX(Enter/Exit) 실행을 안전하게 스케줄(딜레이/쿨다운)하고, 맵 상태를 재검사한 뒤 원샷 실행.
// - GameComponent로 한 틱마다 큐를 확인. (세이브 불필요 — 런타임 전용)
// - Dictionary TryGetValue로 가볍게 쿨다운 관리.
// - 예외/NRE 방지 가드 다수.
// - 추가 최적화: 오래된 쿨다운 엔트리 정리, Fleck 생성 상한 적용.

using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ShapeshifterFramework.Utilities
{
    public class ShapeshiftTransformFxRunner : GameComponent
    {
        private struct ScheduledFx
        {
            public Pawn pawn;
            public ShapeshiftFormDef form;
            public bool isEnter;
            public int fireTick;
        }

        public static ShapeshiftTransformFxRunner Instance
        {
            get
            {
                return Current.Game?.GetComponent<ShapeshiftTransformFxRunner>();
            }
        }

        private static readonly List<ScheduledFx> _queue = new List<ScheduledFx>(16);

        // 쿨다운: key = (pawn, phase) 해시
        private static readonly Dictionary<int, int> _cooldowns = new Dictionary<int, int>(64);

        // 매번 리스트를 새로 만들지 않기 위한 재활용 리스트 추가
        private static readonly List<int> _removeBuffer = new List<int>(32);

        // ──────────────────────────────────────────────────────────────
        // Configurable values
        private const int CooldownExpiryTicks = 60000; // 1게임일 후 자동 정리
        private const int MaxFleckCount = 50;          // Fleck 안전상한
        // ──────────────────────────────────────────────────────────────

        public ShapeshiftTransformFxRunner(Game game)
        {
            // 게임을 새로 불러올 때마다 스태틱 리스트의 유령 데이터 청소
            _queue.Clear();
            _cooldowns.Clear();
            _removeBuffer.Clear();
        }

        public override void GameComponentTick()
        {
            if (_queue.Count == 0 && _cooldowns.Count == 0) return;

            int now = Find.TickManager.TicksGame;

            // ── 실행 큐 처리 ──
            for (int i = _queue.Count - 1; i >= 0; i--)
            {
                var item = _queue[i];
                if (item.fireTick > now) continue;

                _queue.RemoveAt(i);
                TryPlayNow(item.pawn, item.form, item.isEnter);
            }

            // ── 오래된 쿨다운 엔트리 정리 ──
            if (_cooldowns.Count > 0 && (now % 250 == 0)) // 250틱(약4초)마다 점검
            {
                // var toRemove = new List<int>() 대신 재활용 리스트 사용
                _removeBuffer.Clear();
                foreach (var kv in _cooldowns)
                {
                    if (now - kv.Value > CooldownExpiryTicks)
                        _removeBuffer.Add(kv.Key);
                }
                for (int i = 0; i < _removeBuffer.Count; i++)
                    _cooldowns.Remove(_removeBuffer[i]);
            }
        }

        public static void Enqueue(Pawn pawn, ShapeshiftFormDef form, bool isEnter, int delayTicks, int cooldownTicks)
        {
            if (pawn == null || form == null) return;

            int now = Find.TickManager.TicksGame;
            int key = MakeKey(pawn, isEnter);
            int last;
            if (_cooldowns.TryGetValue(key, out last))
            {
                if (now - last < Mathf.Max(0, cooldownTicks)) return; // 쿨다운 미충족 → 무시
            }

            var when = now + Mathf.Max(0, delayTicks);
            _queue.Add(new ScheduledFx { pawn = pawn, form = form, isEnter = isEnter, fireTick = when });
            // 스케줄 타임 기준으로 쿨다운 갱신(중복 스케줄 방지)
            _cooldowns[key] = when;
        }

        private static int MakeKey(Pawn pawn, bool isEnter)
        {
            unchecked
            {
                int h = pawn.thingIDNumber;
                h = (h * 397) ^ (isEnter ? 1 : 0);
                return h;
            }
        }

        private static void TryPlayNow(Pawn pawn, ShapeshiftFormDef form, bool isEnter)
        {
            try
            {
                if (pawn == null || form == null) return;

                // 맵/스폰 체크: 시각효과는 맵에서만, 사운드는 맵 사운드로 1회
                if (!pawn.Spawned || pawn.MapHeld == null)
                {
                    return; // 맵 외에서는 생략
                }

                var map = pawn.MapHeld;
                var pos = pawn.PositionHeld;
                var drawPos = pawn.DrawPos;

                // ── SFX
                var sfx = isEnter ? form.transformEnterSound : form.transformExitSound;
                if (sfx != null)
                    sfx.PlayOneShot(SoundInfo.InMap(new TargetInfo(pos, map), MaintenanceType.None));

                // ── Effecter (원샷 Trigger → Cleanup)
                var eff = isEnter ? form.transformEnterEffecter : form.transformExitEffecter;
                if (eff != null)
                {
                    var e = eff.Spawn();
                    var tgt = new TargetInfo(pos, map);
                    e.Trigger(tgt, tgt);
                    e.Cleanup();
                }

                // ── Fleck (상한 적용)
                var fleck = isEnter ? form.transformEnterFleck : form.transformExitFleck;
                int count = isEnter ? form.transformEnterFleckCount : form.transformExitFleckCount;
                float scale = isEnter ? Mathf.Max(0.01f, form.transformEnterFleckScale)
                                      : Mathf.Max(0.01f, form.transformExitFleckScale);

                if (fleck != null && count > 0)
                {
                    count = Mathf.Min(count, MaxFleckCount);
                    for (int i = 0; i < count; i++)
                    {
                        Vector3 p = drawPos;
                        p.x += Rand.Range(-0.2f, 0.2f);
                        p.z += Rand.Range(-0.2f, 0.2f);
                        FleckMaker.Static(p, map, fleck, scale);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Warning("[SSF] TransformFxRunner.TryPlayNow failed: " + e);
            }
        }
    }
}