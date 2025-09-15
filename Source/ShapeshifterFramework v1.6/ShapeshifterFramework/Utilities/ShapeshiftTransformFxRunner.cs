// .NET Framework 4.8 / C# 7.3
// 목적: 변신 FX(Enter/Exit) 실행을 안전하게 스케줄(딜레이/쿨다운)하고, 맵 상태를 재검사한 뒤 원샷 실행.
// - GameComponent로 한 틱마다 큐를 확인. (세이브 불필요 — 런타임 전용)
// - Dictionary TryGetValue로 가볍게 쿨다운 관리.
// - 예외/NRE 방지 가드 다수.

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

        private static ShapeshiftTransformFxRunner _inst;
        private static readonly List<ScheduledFx> _queue = new List<ScheduledFx>(16);

        // 쿨다운: key = (pawn, phase) 해시
        private static readonly Dictionary<int, int> _cooldowns = new Dictionary<int, int>(64);

        public ShapeshiftTransformFxRunner(Game game) { _inst = this; }

        public static ShapeshiftTransformFxRunner Instance
        {
            get
            {
                if (_inst == null) _inst = Current.Game.GetComponent<ShapeshiftTransformFxRunner>();
                return _inst;
            }
        }

        public override void GameComponentTick()
        {
            if (_queue.Count == 0) return;

            int now = Find.TickManager.TicksGame;
            // in-place remove
            for (int i = _queue.Count - 1; i >= 0; i--)
            {
                var item = _queue[i];
                if (item.fireTick > now) continue;

                _queue.RemoveAt(i);
                TryPlayNow(item.pawn, item.form, item.isEnter);
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
                    // 맵 외에서는 시각효과 생략, 사운드도 생략(일관성)
                    return;
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

                // ── Fleck (count>0일 때만)
                var fleck = isEnter ? form.transformEnterFleck : form.transformExitFleck;
                int count = isEnter ? form.transformEnterFleckCount : form.transformExitFleckCount;
                float scale = isEnter ? Mathf.Max(0.01f, form.transformEnterFleckScale)
                                      : Mathf.Max(0.01f, form.transformExitFleckScale);

                if (fleck != null && count > 0)
                {
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
