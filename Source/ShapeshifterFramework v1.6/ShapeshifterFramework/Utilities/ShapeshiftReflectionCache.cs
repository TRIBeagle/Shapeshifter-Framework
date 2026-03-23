// ShapeshifterFramework | Utilities | ShapeshiftReflectionCache.cs
// 목적 : 바닐라 및 타 모드의 비공개 멤버(Field, Property, Method)에 접근하는 리플렉션 연산 비용과 충돌 위험을 최소화하는 중앙 집중식 캐시 매니저.
// 용도 : ConcurrentDictionary와 AccessTools를 활용해 타입/필드/속성/메서드를 캐싱하며, 외부 모드가 없거나 버전이 달라 멤버 탐색에 실패할 경우 예외(Exception)를 삼키고 null/false를 반환해 게임 크래시를 완벽히 방어함.

using HarmonyLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace ShapeshifterFramework.Utilities
{
    /// <summary>리플렉션 접근/캐시 유틸리티. FieldRef 우선, FieldInfo 폴백. 예외 삼킴.</summary>
    internal static class ShapeshiftReflectionCache
    {
        #region eq.ParentHolder 체인 → pawn

        private static readonly ConcurrentDictionary<Type, FieldInfo> HolderPawnField =
            new ConcurrentDictionary<Type, FieldInfo>();

        /// <summary>ParentHolder 체인을 따라가 Pawn 탐색 (최대 hop 제한).</summary>
        internal static Pawn TryGetHolderPawn(Thing eq, int maxHops = 8)
        {
            IThingHolder h = eq != null ? eq.ParentHolder : null;
            int guard = 0;
            while (h != null && guard++ < maxHops)
            {
                var t = h.GetType();
                FieldInfo fi;
                if (!HolderPawnField.TryGetValue(t, out fi) || fi == null)
                {
                    fi = t.GetField("pawn", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    HolderPawnField[t] = fi;
                }
                if (fi != null && fi.FieldType == typeof(Pawn))
                {
                    try { var p = (Pawn)fi.GetValue(h); if (p != null) return p; } catch { /* 리플렉션 폴백 — 타입 불일치 무시 */ }
                }
                IThingHolder next = null;
                try { next = h.ParentHolder; } catch { /* ParentHolder 접근 실패 무시 */ }
                h = next;
            }
            return null;
        }

        #endregion

        #region AttachPointTracker.parent

        // private 필드이므로 리플렉션 필요 (1.6 DLL 대조 감사 완료: RimWorld.AttachPointTracker.parent는 private)
        private static readonly Type AttachPointTrackerT = AccessTools.TypeByName("Verse.AttachPointTracker");
        private static readonly FieldInfo AptParentFI = AttachPointTrackerT != null
            ? AttachPointTrackerT.GetField("parent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            : null;

        /// <summary>AttachPointTracker의 parent Thing 반환.</summary>
        internal static Thing GetAttachParent(object attachPointTracker)
        {
            if (attachPointTracker == null || AptParentFI == null) return null;
            try { return (Thing)AptParentFI.GetValue(attachPointTracker); } catch { return null; /* 리플렉션 실패 방어 */ }
        }

        #endregion

        // PawnRenderNode.Props — public property이므로 node.Props로 직접 접근. 리플렉션 불필요.

        #region 범용 캐시: 필드/프로퍼티

        private static readonly ConcurrentDictionary<string, FieldInfo> FieldCache =
            new ConcurrentDictionary<string, FieldInfo>();
        // 필드 탐색 실패 기록
        private static readonly ConcurrentDictionary<string, bool> FieldNotFound =
            new ConcurrentDictionary<string, bool>();

        private static readonly ConcurrentDictionary<string, PropertyInfo> PropCache =
            new ConcurrentDictionary<string, PropertyInfo>();
        // 프로퍼티 탐색 실패 기록
        private static readonly ConcurrentDictionary<string, bool> PropNotFound =
            new ConcurrentDictionary<string, bool>();

        private static FieldInfo GetFieldCached(Type t, string name)
        {
            if (t == null || string.IsNullOrEmpty(name)) return null;
            string key = t.FullName + "::F::" + name;

            if (FieldNotFound.ContainsKey(key)) return null; // 이미 없다고 판명났으면 즉시 탈출

            if (!FieldCache.TryGetValue(key, out FieldInfo fi))
            {
                fi = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (fi != null)
                    FieldCache[key] = fi;
                else
                    FieldNotFound[key] = true; // 실패 기록
            }
            return fi;
        }

        private static PropertyInfo GetPropCached(Type t, string name)
        {
            if (t == null || string.IsNullOrEmpty(name)) return null;
            string key = t.FullName + "::P::" + name;

            if (PropNotFound.ContainsKey(key)) return null;

            if (!PropCache.TryGetValue(key, out PropertyInfo pi))
            {
                pi = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (pi != null)
                    PropCache[key] = pi;
                else
                    PropNotFound[key] = true;
            }
            return pi;
        }

        #endregion

        #region 필드/프로퍼티 Getter/Setter

        /// <summary>인스턴스 필드 값을 제네릭으로 읽는다(캐시 사용).</summary>
        internal static T GetInstanceField<T>(object obj, string name)
        {
            if (obj == null) return default(T);
            var fi = GetFieldCached(obj.GetType(), name);
            if (fi == null) return default(T);
            try { return (T)fi.GetValue(obj); } catch { return default(T); /* 필드 읽기 실패 방어 */ }
        }

        /// <summary>인스턴스 프로퍼티 값을 제네릭으로 읽는다(캐시 사용).</summary>
        internal static T GetInstanceProperty<T>(object obj, string name)
        {
            if (obj == null) return default(T);
            var pi = GetPropCached(obj.GetType(), name);
            if (pi == null || !pi.CanRead) return default(T);
            try { return (T)pi.GetValue(obj, null); } catch { return default(T); /* 프로퍼티 읽기 실패 방어 */ }
        }

        /// <summary>인스턴스 필드 값을 설정한다(캐시 사용).</summary>
        internal static bool TrySetInstanceField(object obj, string name, object value)
        {
            if (obj == null || string.IsNullOrEmpty(name)) return false;
            var fi = GetFieldCached(obj.GetType(), name);
            if (fi == null) return false;
            try { fi.SetValue(obj, value); return true; } catch { return false; /* 필드 쓰기 실패 방어 */ }
        }

        /// <summary>인스턴스 프로퍼티 값을 설정한다(캐시 사용).</summary>
        internal static bool TrySetInstanceProperty(object obj, string name, object value)
        {
            if (obj == null || string.IsNullOrEmpty(name)) return false;
            var pi = GetPropCached(obj.GetType(), name);
            if (pi == null || !pi.CanWrite) return false;
            try { pi.SetValue(obj, value, null); return true; } catch { return false; /* 프로퍼티 쓰기 실패 방어 */ }
        }

        #endregion

        #region 타입 스캔/패턴 헬퍼

        // 타입별 FieldInfo[]/PropertyInfo[] 캐시 — 렌더 핫패스에서 매 프레임 GetFields() 호출 방지
        private static readonly ConcurrentDictionary<Type, FieldInfo[]> FieldArrayCache =
            new ConcurrentDictionary<Type, FieldInfo[]>();

        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertyArrayCache =
            new ConcurrentDictionary<Type, PropertyInfo[]>();

        private static FieldInfo[] GetFieldsCached(Type t)
        {
            FieldInfo[] fs;
            if (!FieldArrayCache.TryGetValue(t, out fs))
                FieldArrayCache[t] = fs = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return fs;
        }

        private static PropertyInfo[] GetPropertiesCached(Type t)
        {
            PropertyInfo[] ps;
            if (!PropertyArrayCache.TryGetValue(t, out ps))
                PropertyArrayCache[t] = ps = t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return ps;
        }

        /// <summary>객체 a/b의 필드에서 특정 타입 값을 탐색 (첫 일치 반환).</summary>
        internal static T TryScanFieldsForType<T>(object a, object b) where T : class
        {
            var target = typeof(T);
            T r = TryScanFieldsOne<T>(a, target);
            if (r != null) return r;
            return TryScanFieldsOne<T>(b, target);
        }

        private static T TryScanFieldsOne<T>(object obj, Type target) where T : class
        {
            if (obj == null) return null;
            var fs = GetFieldsCached(obj.GetType());
            for (int i = 0; i < fs.Length; i++)
            {
                var ft = fs[i].FieldType;
                if (target.IsAssignableFrom(ft))
                {
                    try
                    {
                        var v = fs[i].GetValue(obj) as T;
                        if (v != null) return v;
                    }
                    catch { /* 필드 스캔 캐스트 실패 무시 */ }
                }
            }
            return null;
        }

        /// <summary>exclusionTags(List(string))를 필드/프로퍼티에서 탐색 반환.</summary>
        internal static List<string> TryGetExclusionTags(object obj)
        {
            if (obj == null) return null;

            // 필드 우선
            var fs = GetFieldsCached(obj.GetType());
            for (int i = 0; i < fs.Length; i++)
            {
                if (string.Equals(fs[i].Name, "exclusionTags", StringComparison.OrdinalIgnoreCase)
                    && typeof(List<string>).IsAssignableFrom(fs[i].FieldType))
                {
                    try { return (List<string>)fs[i].GetValue(obj); } catch { /* 필드 읽기 실패 무시 */ }
                }
            }

            // 프로퍼티 폴백
            var ps = GetPropertiesCached(obj.GetType());
            for (int i = 0; i < ps.Length; i++)
            {
                if (ps[i].CanRead
                    && string.Equals(ps[i].Name, "exclusionTags", StringComparison.OrdinalIgnoreCase)
                    && typeof(List<string>).IsAssignableFrom(ps[i].PropertyType))
                {
                    try { return (List<string>)ps[i].GetValue(obj, null); } catch { /* 프로퍼티 읽기 실패 무시 */ }
                }
            }
            return null;
        }

        /// <summary>풀네임으로 타입을 찾는다(AccessTools.TypeByName 포장).</summary>
        internal static Type TryType(string fullName) => AccessTools.TypeByName(fullName);

        #endregion
    }
}