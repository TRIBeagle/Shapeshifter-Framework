// ShapeshifterFramework | Utilities | ShapeshiftRuntimeCaches.cs
// 목적 : 프레임워크 전역에서 폰 별(Pawn) 전용 데이터(사운드, 혈흔, 살점 타입)를 O(1) 속도로 즉시 조회하기 위한 런타임 저장소.
// 용도 : 일반 Dictionary 대신 ConditionalWeakTable을 사용하여, 폰이 파괴되고 가비지 컬렉터(GC)에 의해 수거될 때 캐시 데이터도 자동으로 함께 소멸하게 만들어 메모리 누수(Memory Leak)를 원천 차단함.

using RimWorld;
using System.Collections.Generic;
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

        // ── HediffDef → MutantDef 역인덱스 (Anomaly DLC) ──
        // 뮤턴트 필터에서 Pawn의 hediff로부터 MutantDef를 찾을 때 O(1) 조회 제공.
        // 최초 조회 시 1회 빌드, ClearAll()에서 null로 리셋하여 다음 조회 시 재빌드.
        private static Dictionary<HediffDef, MutantDef> _hediffToMutant;

        /// <summary>HediffDef에 대응하는 MutantDef를 O(1)로 조회. 최초 호출 시 역인덱스 자동 빌드.</summary>
        internal static bool TryGetMutantDef(HediffDef hediffDef, out MutantDef mutant)
        {
            if (_hediffToMutant == null)
                BuildMutantIndex();
            return _hediffToMutant.TryGetValue(hediffDef, out mutant);
        }

        private static void BuildMutantIndex()
        {
            var all = DefDatabase<MutantDef>.AllDefsListForReading;
            _hediffToMutant = new Dictionary<HediffDef, MutantDef>(all.Count);
            for (int i = 0; i < all.Count; i++)
            {
                var m = all[i];
                if (m?.hediff != null)
                    _hediffToMutant[m.hediff] = m;
            }
        }

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
            // 변신 레지스트리 초기화
            ShapeshiftRegistry.Clear();

            // ConditionalWeakTable은 Clear()가 없으므로 새 인스턴스로 교체
            CallByPawn = new ConditionalWeakTable<Pawn, SoundDef>();
            WoundedByPawn = new ConditionalWeakTable<Pawn, SoundDef>();
            DeathByPawn = new ConditionalWeakTable<Pawn, SoundDef>();
            AngryByPawn = new ConditionalWeakTable<Pawn, SoundDef>();
            BloodByPawn = new ConditionalWeakTable<Pawn, ThingDef>();
            SmearByPawn = new ConditionalWeakTable<Pawn, ThingDef>();
            FleshTypeByPawn = new ConditionalWeakTable<Pawn, FleshTypeDef>();

            // MutantDef 역인덱스 리셋 (다음 조회 시 재빌드)
            _hediffToMutant = null;

            // 그림자 그래픽 캐시 정리
            try { ShapeshifterFramework.Patches.Patch_PawnRenderer_DrawShadowInternal.ClearCache(); }
            catch (System.Exception ex) { Log.Warning($"[SSF] ClearAll: DrawShadow cache clear failed: {ex.Message}"); }

            // FA 컨트롤러 보유 캐시 정리
            try { ShapeshifterFramework.Compat.Compat_FacialAnimation_HeadWorker_ScaleFor.ClearFACompCache(); }
            catch (System.Exception ex) { Log.Warning($"[SSF] ClearAll: FA comp cache clear failed: {ex.Message}"); }

            // HAR 헤드 애드온 판정 캐시 정리
            try { ShapeshifterFramework.Compat.Compat_HAR_BodyAddon_ScaleFor.ClearHeadAddonCache(); }
            catch (System.Exception ex) { Log.Warning($"[SSF] ClearAll: HAR addon cache clear failed: {ex.Message}"); }
        }
    }
}