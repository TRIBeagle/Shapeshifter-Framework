// ShapeshifterFramework | Utilities | ShapeshiftTransformFxRunner.cs
// 목적 : 변신/해제 시 발생하는 시각/청각 이펙트(FX)의 딜레이, 쿨다운 스케줄링 및 실행을 관리하는 GameComponent.
// 용도 : 이펙트 다중 호출로 인한 소음 스팸과 과부하를 막기 위해 큐(Queue)와 쿨다운을 적용하며, 매 프레임 발생하는 가비지(GC) 할당을 막기 위해 List 기반의 재사용 버퍼(_removeBuffer)를 활용함.

using RimWorld;
using ShapeshifterFramework.Hediffs;
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

        private readonly List<ScheduledFx> _queue = new List<ScheduledFx>(16);

        // 쿨다운: key = (pawn, phase) 해시
        private readonly Dictionary<int, int> _cooldowns = new Dictionary<int, int>(64);

        // 매번 리스트를 새로 만들지 않기 위한 재활용 리스트 추가
        private readonly List<int> _removeBuffer = new List<int>(32);

        // ──────────────────────────────────────────────────────────────
        // 설정 상수
        private const int CooldownExpiryTicks = 60000; // 1게임일 후 자동 정리
        private const int MaxFleckCount = 50;          // Fleck 안전상한
        // ──────────────────────────────────────────────────────────────

        public ShapeshiftTransformFxRunner(Game game) { }

        /// <summary>게임 로드/시작 완료 시 전역 캐시 정리 — 이전 세션 잔여 데이터 누수 방지.</summary>
        /// <remarks>
        /// ClearAll()이 ShapeshiftRegistry를 비우므로, PostLoadInit/PostSpawnSetup에서 등록된 엔트리가 유실됨.
        /// 따라서 캐시 정리 후 모든 맵의 변신 중 폰을 재등록해야 함.
        /// </remarks>
        public override void FinalizeInit()
        {
            base.FinalizeInit();

            // ClearAll → 재등록 사이에 다른 GameComponent가 레지스트리를 조회해도
            // hediff 기반 폴백으로 정확한 결과를 반환하도록 가드
            ShapeshiftRegistry.BeginReInit();
            try
            {
                ShapeshiftCoreUtility.ClearEvents();
                ShapeshiftRuntimeCaches.ClearAll();
                Hediffs.HediffComp_Harvestable.ClearCache();

                // 캐시 클리어로 유실된 변신 폰 레지스트리 + 런타임 캐시 재등록
                if (Find.Maps != null)
                {
                    for (int m = 0; m < Find.Maps.Count; m++)
                    {
                        var pawns = Find.Maps[m]?.mapPawns?.AllPawnsSpawned;
                        if (pawns == null) continue;
                        for (int i = 0; i < pawns.Count; i++)
                        {
                            // HediffComp_ShapeshiftCore 기반 조회
                            if (ShapeshiftCoreUtility.TryGetCore(pawns[i], out var core))
                            {
                                if (core.isTransformed && core.currentForm != null)
                                {
                                    ShapeshiftRegistry.Register(pawns[i], core);
                                    HediffComp_ShapeshiftCore.ApplyRuntimeCaches(pawns[i], core.currentForm);
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                ShapeshiftRegistry.EndReInit();
            }
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

                // TryPlayNow 먼저 실행, RemoveAt 후처리 (예외 시 유실 방지)
                TryPlayNow(item.pawn, item.form, item.isEnter);
                _queue.RemoveAt(i);
            }

            // ── 오래된 쿨다운 엔트리 정리 ──
            if (_cooldowns.Count > 0 && (now % 250 == 0)) // 250틱(약4초)마다 점검
            {
                // 재활용 리스트로 GC 할당 방지
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
            var inst = Instance;
            if (inst == null) return;
            inst.EnqueueInternal(pawn, form, isEnter, delayTicks, cooldownTicks);
        }

        private void EnqueueInternal(Pawn pawn, ShapeshiftFormDef form, bool isEnter, int delayTicks, int cooldownTicks)
        {
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

        private void TryPlayNow(Pawn pawn, ShapeshiftFormDef form, bool isEnter)
        {
            try
            {
                if (pawn == null || pawn.Destroyed || form == null) return;

                // 맵/스폰 체크: 시각효과는 맵에서만, 사운드는 맵 사운드로 1회
                if (!pawn.Spawned || pawn.MapHeld == null)
                {
                    return; // 맵 외에서는 생략
                }

                // 지연 발사 시점의 상태 재검증 — delay 사이 해제/전환된 폰 위의 유령 FX 방지.
                // Enter: 여전히 '그 폼'으로 변신 중일 때만 / Exit: 그 폼이 더 이상 활성이 아닐 때만.
                bool formActive = ShapeshiftRegistry.TryGet(pawn, out _, out var curForm) && curForm == form;
                if (isEnter && !formActive) return;
                if (!isEnter && formActive) return;

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