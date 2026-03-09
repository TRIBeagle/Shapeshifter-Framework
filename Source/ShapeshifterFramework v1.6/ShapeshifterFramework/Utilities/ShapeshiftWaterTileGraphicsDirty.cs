// ShapeshifterFramework | Utilities | ShapeshiftWaterTileGraphicsDirty.cs
// 목적 : 변신 중인 폰이 물(Water) 타일로 들어가거나 나올 때 렌더링을 갱신(Dirty)하여 수영 전용 텍스처가 적용되도록 유도하는 보조 맵 컴포넌트(MapComponent).
// 용도 : 바닐라 이동 훅이 잡히지 않는 엣지 케이스(예: 순간이동 등)를 대비해 60틱(약 1초)마다 폰의 바닥 타일 속성을 검사하여 상태가 변경되었을 때만 그래픽 갱신 루프를 호출함.

using System.Collections.Generic;
using Verse;

namespace ShapeshifterFramework.Utilities
{
    internal sealed class ShapeshiftWaterTileGraphicsDirty : MapComponent
    {
        private readonly Dictionary<int, bool> lastOnWaterTile = new Dictionary<int, bool>(128);
        private const int CheckIntervalTicks = 60;
        private const int PurgeIntervalTicks = 2500; // ~1일마다 죽은 폰 정리

        public ShapeshiftWaterTileGraphicsDirty(Map map) : base(map) { }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            if (map == null) return;

            var pawns = map.mapPawns?.AllPawnsSpawned;
            if (pawns == null || pawns.Count == 0)
            {
                if (lastOnWaterTile.Count > 0) lastOnWaterTile.Clear();
                return;
            }

            // 주기적으로 맵에서 사라진 폰 엔트리 정리
            if (Find.TickManager.TicksGame % PurgeIntervalTicks == 0 && lastOnWaterTile.Count > 0)
            {
                var spawnedIds = new HashSet<int>(pawns.Count);
                for (int i = 0; i < pawns.Count; i++)
                    if (pawns[i] != null) spawnedIds.Add(pawns[i].thingIDNumber);

                var staleIds = new List<int>();
                foreach (var kv in lastOnWaterTile)
                    if (!spawnedIds.Contains(kv.Key)) staleIds.Add(kv.Key);
                for (int i = 0; i < staleIds.Count; i++) lastOnWaterTile.Remove(staleIds[i]);
            }

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn == null || pawn.DestroyedOrNull() || !pawn.Spawned) continue;
                if (!pawn.IsHashIntervalTick(CheckIntervalTicks)) continue;

                int id = pawn.thingIDNumber;

                bool last;
                bool had = lastOnWaterTile.TryGetValue(id, out last);

                bool onWater = IsOnWaterTile(pawn);

                // 기록도 없고 지금도 물이 아니면 관심 없음
                if (!onWater && !had) continue;

                // 추적 대상이 아니면 기록 제거(있던 경우만)
                if (!IsTrackedPawn(pawn))
                {
                    if (had) lastOnWaterTile.Remove(id);
                    continue;
                }

                if (had)
                {
                    if (last != onWater)
                    {
                        lastOnWaterTile[id] = onWater;
                        pawn.Drawer?.renderer?.SetAllGraphicsDirty();
                    }
                }
                else
                {
                    lastOnWaterTile[id] = onWater;
                }
            }
        }

        private static bool IsTrackedPawn(Pawn pawn)
        {
            ShapeshiftFormDef form;
            if (!ShapeshiftPartControlUtility.ShouldRun(pawn, out form) || form == null) return false;

            string swimPath;
            return ShapeshiftPartControlUtility.TryGetBodySwimmingReplacementPath(pawn, form, out swimPath);
        }

        private static bool IsOnWaterTile(Pawn pawn)
        {
            try
            {
                var terr = pawn.Position.GetTerrain(pawn.Map);
                return terr != null && terr.IsWater;
            }
            catch
            {
                return false;
            }
        }
    }
}
