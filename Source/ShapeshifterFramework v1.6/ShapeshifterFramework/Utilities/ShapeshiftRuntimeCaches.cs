// .NET Framework 4.8 / C# 7.3
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace ShapeshifterFramework.Utilities
{
    internal static class ShapeshiftRuntimeCaches
    {
        // 보이스 캐시 (폼 전용)
        public static readonly Dictionary<Pawn, SoundDef> CallByPawn = new Dictionary<Pawn, SoundDef>(128);
        public static readonly Dictionary<Pawn, SoundDef> WoundedByPawn = new Dictionary<Pawn, SoundDef>(128);
        public static readonly Dictionary<Pawn, SoundDef> DeathByPawn = new Dictionary<Pawn, SoundDef>(128);

        // 혈흔/스미어 캐시 (폼 전용)
        public static readonly Dictionary<Pawn, ThingDef> BloodByPawn = new Dictionary<Pawn, ThingDef>(128);
        public static readonly Dictionary<Pawn, ThingDef> SmearByPawn = new Dictionary<Pawn, ThingDef>(128);

        // FleshType 캐시 (폼 전용)
        public static readonly Dictionary<Pawn, FleshTypeDef> FleshTypeByPawn = new Dictionary<Pawn, FleshTypeDef>(128);

        /// <summary>
        /// 해당 Pawn에 대한 모든 캐시 제거 (변신 OFF/Despawn/사망 등)
        /// </summary>
        public static void ClearFor(Pawn pawn)
        {
            if (pawn == null) return;

            // 보이스
            CallByPawn.Remove(pawn);
            WoundedByPawn.Remove(pawn);
            DeathByPawn.Remove(pawn);

            // 혈흔
            BloodByPawn.Remove(pawn);
            SmearByPawn.Remove(pawn);

            // FleshType
            FleshTypeByPawn.Remove(pawn);
        }

        /// <summary>
        /// 전체 리셋(맵 전환/디버그용)
        /// </summary>
        public static void ClearAll()
        {
            CallByPawn.Clear();
            WoundedByPawn.Clear();
            DeathByPawn.Clear();

            BloodByPawn.Clear();
            SmearByPawn.Clear();

            FleshTypeByPawn.Clear();
        }
    }
}
