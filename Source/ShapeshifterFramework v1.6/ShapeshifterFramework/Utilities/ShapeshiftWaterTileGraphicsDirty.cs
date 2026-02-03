// Utilities/ShapeshiftWaterTileGraphicsDirty.cs
// 목적: (보험) 이동 훅이 안 잡히는 케이스에서 물 타일(IsWater) 진입/이탈 감지 → SetAllGraphicsDirty()
// 주기: 60틱(약 1초)
// 핵심: SelectByGender 제거. 유틸의 merge 기반 TryGetBodySwimmingReplacementPath 사용.
// 최적화: TryGetValue 1회 조회.

using System.Collections.Generic;
using Verse;

namespace ShapeshifterFramework.Utilities
{
    internal sealed class ShapeshiftWaterTileGraphicsDirty : MapComponent
    {
        private readonly Dictionary<int, bool> lastOnWaterTile = new Dictionary<int, bool>(128);
        private const int CheckIntervalTicks = 60;

        public ShapeshiftWaterTileGraphicsDirty(Map map) : base(map) { }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            if (map == null) return;

            var pawns = map.mapPawns?.AllPawnsSpawned;
            if (pawns == null || pawns.Count == 0) return;

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
