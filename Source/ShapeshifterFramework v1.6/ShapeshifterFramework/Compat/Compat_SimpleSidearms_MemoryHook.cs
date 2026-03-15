// ShapeshifterFramework | Compat | Compat_SimpleSidearms_MemoryHook.cs
// 목적 : Simple Sidearms 모드의 CompSidearmMemory와 변신 시스템 간 무기 메모리 충돌 방지.
// 용도 : 변신 시 SS 메모리(rememberedWeapons + 선호 무기 필드) 백업/클리어, 해제 시 원복.
//        폼 전용 무기가 SS 메모리에 잔류하지 않도록 관리.
// 주의 : SS 어셈블리에 하드 참조 없이 리플렉션으로 접근. 메인 스레드 단일 실행 전제.

using HarmonyLib;
using ShapeshifterFramework.Comps;
using ShapeshifterFramework.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace ShapeshifterFramework.Compat
{
    #region Simple Sidearms 메모리 백업/복원 유틸리티

    /// <summary>Simple Sidearms CompSidearmMemory 조작 유틸리티.</summary>
    internal static class SimpleSidearmsCompat
    {
        /// <summary>SS 활성 여부.</summary>
        internal static bool Active => CompatManager.SS.IsActive;

        // ── 리플렉션 캐시 ──

        private static Type _compMemoryType;
        private static bool _compMemoryTypeResolved;

        private static FieldInfo _fRememberedWeapons;
        private static bool _fRememberedWeaponsResolved;

        /// <summary>CompSidearmMemory 타입 조회 (1회 캐싱).</summary>
        private static Type CompMemoryType
        {
            get
            {
                if (!_compMemoryTypeResolved)
                {
                    _compMemoryTypeResolved = true;
                    _compMemoryType = ShapeshiftReflectionCache.TryType(
                        "SimpleSidearms.rimworld.CompSidearmMemory");
                    // 구버전 경로 폴백
                    if (_compMemoryType == null)
                        _compMemoryType = ShapeshiftReflectionCache.TryType(
                            "PeteTimesSix.SimpleSidearms.CompSidearmMemory");
                    if (_compMemoryType == null)
                        _compMemoryType = ShapeshiftReflectionCache.TryType(
                            "SimpleSidearms.CompSidearmMemory");
                }
                return _compMemoryType;
            }
        }

        /// <summary>기억된 무기 목록 필드 조회 (1회 캐싱).</summary>
        private static FieldInfo RememberedWeaponsField
        {
            get
            {
                if (!_fRememberedWeaponsResolved)
                {
                    _fRememberedWeaponsResolved = true;
                    var t = CompMemoryType;
                    if (t != null)
                    {
                        // 필드명 후보 탐색
                        _fRememberedWeapons = t.GetField("rememberedWeapons",
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (_fRememberedWeapons == null)
                            _fRememberedWeapons = t.GetField("RememberedWeapons",
                                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    }
                }
                return _fRememberedWeapons;
            }
        }

        // ── 선호 무기 필드 리플렉션 캐시 ──
        // 변신 시 이 필드들을 클리어하지 않으면 SS가 더 이상 존재하지 않는 무기를 강제 장착 시도할 수 있음.
        private static readonly string[] _prefFieldNames = new[]
        {
            "forcedWeaponEx",              // ThingDefStuffDefPair? — 강제 장착 무기
            "forcedWeaponWhileDraftedEx",   // ThingDefStuffDefPair? — 징집 시 강제 장착 무기
            "defaultRangedWeaponEx",        // ThingDefStuffDefPair? — 기본 원거리 무기
            "preferredMeleeWeaponEx",       // ThingDefStuffDefPair? — 선호 근접 무기
        };

        private static FieldInfo[] _prefFields;
        private static bool _prefFieldsResolved;

        /// <summary>선호 무기 필드 일괄 조회 (1회 캐싱). null 요소는 해당 필드 미발견.</summary>
        private static FieldInfo[] PrefFields
        {
            get
            {
                if (!_prefFieldsResolved)
                {
                    _prefFieldsResolved = true;
                    var t = CompMemoryType;
                    if (t != null)
                    {
                        _prefFields = new FieldInfo[_prefFieldNames.Length];
                        for (int i = 0; i < _prefFieldNames.Length; i++)
                        {
                            _prefFields[i] = t.GetField(_prefFieldNames[i],
                                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        }
                    }
                }
                return _prefFields;
            }
        }

        /// <summary>Pawn에서 CompSidearmMemory 찾기.</summary>
        internal static ThingComp GetMemoryComp(Pawn pawn)
        {
            if (pawn == null || CompMemoryType == null) return null;

            var comps = (pawn as ThingWithComps)?.AllComps;
            if (comps == null) return null;

            for (int i = 0; i < comps.Count; i++)
            {
                var c = comps[i];
                if (c != null && CompMemoryType.IsInstanceOfType(c))
                    return c;
            }
            return null;
        }

        /// <summary>SS 무기 메모리를 IList로 가져오기.</summary>
        private static IList GetRememberedWeapons(ThingComp memComp)
        {
            if (memComp == null || RememberedWeaponsField == null) return null;
            return RememberedWeaponsField.GetValue(memComp) as IList;
        }

        // ── 백업 구조 ──

        /// <summary>SS 무기 메모리 백업 데이터.</summary>
        internal sealed class SSBackup
        {
            // 원본 IList의 요소를 object로 보관 (ThingDefStuffDefPair 등 value type)
            internal readonly List<object> entries = new List<object>();

            // 선호 무기 필드 백업 (인덱스는 _prefFieldNames와 동일)
            internal object[] prefValues;

            internal bool IsEmpty => entries.Count == 0 && !HasPrefValues;

            internal bool HasPrefValues
            {
                get
                {
                    if (prefValues == null) return false;
                    for (int i = 0; i < prefValues.Length; i++)
                        if (prefValues[i] != null) return true;
                    return false;
                }
            }

            internal void Clear()
            {
                entries.Clear();
                prefValues = null;
            }
        }

        /// <summary>현재 SS 메모리를 백업 (rememberedWeapons + 선호 무기 필드).</summary>
        internal static SSBackup BackupMemory(Pawn pawn)
        {
            var backup = new SSBackup();
            if (!Active) return backup;

            var comp = GetMemoryComp(pawn);
            if (comp == null) return backup;

            // rememberedWeapons 백업
            var list = GetRememberedWeapons(comp);
            if (list != null)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    var item = list[i];
                    if (item != null)
                        backup.entries.Add(item);
                }
            }

            // 선호 무기 필드 백업
            var fields = PrefFields;
            if (fields != null)
            {
                backup.prefValues = new object[fields.Length];
                for (int i = 0; i < fields.Length; i++)
                {
                    if (fields[i] != null)
                    {
                        try { backup.prefValues[i] = fields[i].GetValue(comp); }
                        catch (Exception) { /* 접근 실패 — 무시 */ }
                    }
                }
            }

            return backup;
        }

        /// <summary>SS 메모리 클리어 (rememberedWeapons + 선호 무기 필드).</summary>
        internal static void ClearMemory(Pawn pawn)
        {
            if (!Active) return;

            var comp = GetMemoryComp(pawn);
            if (comp == null) return;

            // rememberedWeapons 클리어
            var list = GetRememberedWeapons(comp);
            list?.Clear();

            // 선호 무기 필드 클리어 — Nullable<T> 필드는 null 설정으로 비움
            var fields = PrefFields;
            if (fields != null)
            {
                for (int i = 0; i < fields.Length; i++)
                {
                    if (fields[i] == null) continue;
                    try
                    {
                        // Nullable<T> 필드 → null 설정, 비-Nullable → 기본값
                        var ft = fields[i].FieldType;
                        if (Nullable.GetUnderlyingType(ft) != null)
                            fields[i].SetValue(comp, null);
                        else if (ft.IsValueType)
                            fields[i].SetValue(comp, Activator.CreateInstance(ft));
                        else
                            fields[i].SetValue(comp, null);
                    }
                    catch (Exception) { /* 접근 실패 — 무시 */ }
                }
            }
        }

        /// <summary>백업에서 SS 메모리 복원 (rememberedWeapons + 선호 무기 필드).</summary>
        internal static void RestoreMemory(Pawn pawn, SSBackup backup)
        {
            if (!Active || backup == null || backup.IsEmpty) return;

            var comp = GetMemoryComp(pawn);
            if (comp == null) return;

            // rememberedWeapons 복원
            if (backup.entries.Count > 0)
            {
                var list = GetRememberedWeapons(comp);
                if (list != null)
                {
                    list.Clear();
                    for (int i = 0; i < backup.entries.Count; i++)
                    {
                        try { list.Add(backup.entries[i]); }
                        catch (Exception)
                        {
                            // 타입 불일치 등 — 무시하고 계속
                        }
                    }
                }
            }

            // 선호 무기 필드 복원
            var fields = PrefFields;
            if (fields != null && backup.prefValues != null)
            {
                int len = Math.Min(fields.Length, backup.prefValues.Length);
                for (int i = 0; i < len; i++)
                {
                    if (fields[i] == null) continue;
                    try { fields[i].SetValue(comp, backup.prefValues[i]); }
                    catch (Exception) { /* 타입 불일치 — 무시 */ }
                }
            }
        }

    }

    #endregion

    #region Save store: Pawn → SSBackup (GameComponent)

    /// <summary>Pawn별 SS 메모리 백업 저장소. 딥세이브/로드 지원.</summary>
    internal sealed class SSMemoryStore : GameComponent
    {
        public SSMemoryStore(Game game) { Inst = this; }

        public static SSMemoryStore Inst { get; private set; }

        // SS 백업은 object 리스트(value type)라 IExposable 직렬화가 어려우므로
        // 런타임 전용. 세이브/로드 시에는 SS가 자체적으로 메모리를 관리함.
        // 변신 중 저장 → 로드 시에는 SS 메모리가 이미 클리어 상태이므로,
        // PostLoadInit에서 변신 해제 시 SS가 AddEquipment 훅으로 자연 복구됨.
        private readonly Dictionary<Pawn, SimpleSidearmsCompat.SSBackup> map =
            new Dictionary<Pawn, SimpleSidearmsCompat.SSBackup>();

        private readonly List<Pawn> _removeBuffer = new List<Pawn>();

        public override void GameComponentTick()
        {
            base.GameComponentTick();
            // 60,000틱(1일)마다 죽은 폰 청소
            if (Find.TickManager.TicksGame % 60000 == 0)
                Cleanup();
        }

        private void Cleanup()
        {
            _removeBuffer.Clear();
            foreach (var kv in map)
            {
                if (kv.Key == null || kv.Key.Destroyed)
                    _removeBuffer.Add(kv.Key);
            }
            for (int i = 0; i < _removeBuffer.Count; i++)
            {
                if (_removeBuffer[i] != null) map.Remove(_removeBuffer[i]);
            }
            _removeBuffer.Clear();
        }

        public void Store(Pawn p, SimpleSidearmsCompat.SSBackup backup)
        {
            if (p != null && backup != null && !backup.IsEmpty)
                map[p] = backup;
        }

        public bool TryGet(Pawn p, out SimpleSidearmsCompat.SSBackup backup)
        {
            if (p != null && map.TryGetValue(p, out backup) && backup != null)
                return true;
            backup = null;
            return false;
        }

        public void Remove(Pawn p)
        {
            if (p != null) map.Remove(p);
        }
    }

    #endregion

    #region Harmony hooks: CompShapeshifter ↔ SimpleSidearmsCompat

    /// <summary>CompShapeshifter 라이프사이클에 SS 메모리 백업/클리어/원복 훅.</summary>
    [HarmonyPatch]
    internal static class Compat_SimpleSidearms_MemoryHook
    {
        private static bool counted;

        /// <summary>ApplyForm Prefix: SS 메모리 백업 후 클리어.</summary>
        [HarmonyPatch(typeof(CompShapeshifter), "ApplyForm",
            new Type[] { typeof(ShapeshiftFormDef), typeof(string),
                typeof(List<Thing>) })]
        static class Patch_ApplyForm
        {
            static bool Prepare()
            {
                if (!CompatManager.SS.IsActive) return false;
                if (!counted) { counted = true; CompatManager.SS.Patched("MemoryHook"); }
                return true;
            }

            /// <summary>변신 직전: SS 메모리 백업 → 클리어 (무기 제거 시 SS 간섭 방지).</summary>
            static void Prefix(CompShapeshifter __instance)
            {
                try
                {
                    var pawn = __instance?.parent as Pawn;
                    if (pawn == null) return;

                    var backup = SimpleSidearmsCompat.BackupMemory(pawn);

                    var store = Current.Game?.GetComponent<SSMemoryStore>();
                    store?.Store(pawn, backup);

                    // 클리어: 이후 HandleGearOnTransform에서 무기 드랍/이동 시 SS가 반응하지 않도록
                    SimpleSidearmsCompat.ClearMemory(pawn);
                }
                catch (Exception e)
                {
                    if (!CompatManager.SS.HasFailed("MemoryHook:ApplyForm:Exception"))
                        CompatManager.SS.Failed("MemoryHook:ApplyForm:Exception", e.Message);
                }
            }
        }

        /// <summary>RemoveForm Postfix: SS 메모리 원복.</summary>
        [HarmonyPatch(typeof(CompShapeshifter), "RemoveForm")]
        static class Patch_RemoveForm
        {
            static bool Prepare() => CompatManager.SS.IsActive;

            /// <summary>변신 해제 후: 원래 SS 메모리 복원.</summary>
            static void Postfix(CompShapeshifter __instance)
            {
                try
                {
                    var pawn = __instance?.parent as Pawn;
                    if (pawn == null) return;

                    var store = Current.Game?.GetComponent<SSMemoryStore>();
                    if (store != null && store.TryGet(pawn, out var backup))
                    {
                        SimpleSidearmsCompat.RestoreMemory(pawn, backup);
                        store.Remove(pawn);
                    }
                }
                catch (Exception e)
                {
                    if (!CompatManager.SS.HasFailed("MemoryHook:RemoveForm:Exception"))
                        CompatManager.SS.Failed("MemoryHook:RemoveForm:Exception", e.Message);
                }
            }
        }
    }

    #endregion
}
