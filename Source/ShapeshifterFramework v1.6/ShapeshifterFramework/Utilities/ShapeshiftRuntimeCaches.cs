// ShapeshifterFramework | Utilities | ShapeshiftRuntimeCaches.cs
// 목적 : 프레임워크 전역에서 폰 별(Pawn) 전용 데이터(사운드, 혈흔, 살점 타입)를 O(1) 속도로 즉시 조회하기 위한 런타임 저장소.
// 용도 : 일반 Dictionary 대신 ConditionalWeakTable을 사용하여, 폰이 파괴되고 가비지 컬렉터(GC)에 의해 수거될 때 캐시 데이터도 자동으로 함께 소멸하게 만들어 메모리 누수(Memory Leak)를 원천 차단함.

using RimWorld;
using System.Runtime.CompilerServices;
using Verse;

namespace ShapeshifterFramework.Utilities
{
    internal static class ShapeshiftRuntimeCaches
    {
        // 보이스 캐시 (폼 전용)
        public static ConditionalWeakTable<Pawn, SoundDef> CallByPawn = new ConditionalWeakTable<Pawn, SoundDef>();
        public static ConditionalWeakTable<Pawn, SoundDef> WoundedByPawn = new ConditionalWeakTable<Pawn, SoundDef>();
        public static ConditionalWeakTable<Pawn, SoundDef> DeathByPawn = new ConditionalWeakTable<Pawn, SoundDef>();
        public static ConditionalWeakTable<Pawn, SoundDef> AngryByPawn = new ConditionalWeakTable<Pawn, SoundDef>();

        // 혈흔/스미어 캐시 (폼 전용)
        public static ConditionalWeakTable<Pawn, ThingDef> BloodByPawn = new ConditionalWeakTable<Pawn, ThingDef>();
        public static ConditionalWeakTable<Pawn, ThingDef> SmearByPawn = new ConditionalWeakTable<Pawn, ThingDef>();

        // FleshType 캐시 (폼 전용)
        public static ConditionalWeakTable<Pawn, FleshTypeDef> FleshTypeByPawn = new ConditionalWeakTable<Pawn, FleshTypeDef>();

        // 외부에서 안전하게 덮어쓰기 위한 헬퍼 (기존 Dictionary의 인덱서[pawn] = val 대체용)
        public static void SetCache<T>(ConditionalWeakTable<Pawn, T> table, Pawn pawn, T value) where T : class
        {
            if (pawn == null || value == null) return;
            table.Remove(pawn);
            table.Add(pawn, value);
        }

        /// <summary>해당 Pawn에 대한 모든 캐시 제거</summary>
        public static void ClearFor(Pawn pawn)
        {
            if (pawn == null) return;
            CallByPawn.Remove(pawn);
            WoundedByPawn.Remove(pawn);
            DeathByPawn.Remove(pawn);
            AngryByPawn.Remove(pawn);
            BloodByPawn.Remove(pawn);
            SmearByPawn.Remove(pawn);
            FleshTypeByPawn.Remove(pawn);
        }

        /// <summary>전체 리셋(맵 전환/디버그용)</summary>
        public static void ClearAll()
        {
            // ConditionalWeakTable은 Clear()가 없으므로 새 인스턴스로 교체
            CallByPawn = new ConditionalWeakTable<Pawn, SoundDef>();
            WoundedByPawn = new ConditionalWeakTable<Pawn, SoundDef>();
            DeathByPawn = new ConditionalWeakTable<Pawn, SoundDef>();
            AngryByPawn = new ConditionalWeakTable<Pawn, SoundDef>();
            BloodByPawn = new ConditionalWeakTable<Pawn, ThingDef>();
            SmearByPawn = new ConditionalWeakTable<Pawn, ThingDef>();
            FleshTypeByPawn = new ConditionalWeakTable<Pawn, FleshTypeDef>();
        }
    }
}