// ShapeshiftReflectionCache.cs
// 목적: 리플렉션/필드·프로퍼티 접근을 일원화(성능/정리).
// 메모: C# 7.3 호환. Setter 헬퍼(TrySetInstanceField/Property) 포함.

using HarmonyLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace ShapeshifterFramework.Utilities
{
    internal static class ShapeshiftReflectionCache
    {
        // ── PawnRenderer.pawn ──
        private static readonly AccessTools.FieldRef<PawnRenderer, Pawn> RendererPawnRef =
            AccessTools.FieldRefAccess<PawnRenderer, Pawn>("pawn");
        private static readonly FieldInfo RendererPawnFI =
            AccessTools.Field(typeof(PawnRenderer), "pawn");

        internal static Pawn GetPawn(PawnRenderer renderer)
        {
            if (renderer == null) return null;
            try { return RendererPawnRef(renderer); } catch { }
            return RendererPawnFI != null ? (Pawn)RendererPawnFI.GetValue(renderer) : null;
        }

        // ── eq.ParentHolder 체인 → pawn ──
        private static readonly ConcurrentDictionary<Type, FieldInfo> HolderPawnField =
            new ConcurrentDictionary<Type, FieldInfo>();

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
                    try { var p = (Pawn)fi.GetValue(h); if (p != null) return p; } catch { }
                }
                IThingHolder next = null;
                try { next = h.ParentHolder; } catch { }
                h = next;
            }
            return null;
        }

        // ── AttachPointTracker.parent ──
        private static readonly Type AttachPointTrackerT = AccessTools.TypeByName("Verse.AttachPointTracker");
        private static readonly FieldInfo AptParentFI = AttachPointTrackerT != null
            ? AttachPointTrackerT.GetField("parent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            : null;

        internal static Thing GetAttachParent(object attachPointTracker)
        {
            if (attachPointTracker == null || AptParentFI == null) return null;
            try { return (Thing)AptParentFI.GetValue(attachPointTracker); } catch { return null; }
        }

        // ── PawnRenderNode: Owner/Props ──
        private static readonly PropertyInfo PI_Node_Owner = AccessTools.Property(typeof(PawnRenderNode), "Owner");
        private static readonly FieldInfo FI_Node_owner = AccessTools.Field(typeof(PawnRenderNode), "owner");
        private static readonly PropertyInfo PI_Node_Props = AccessTools.Property(typeof(PawnRenderNode), "Props");
        private static readonly FieldInfo FI_Node_props = AccessTools.Field(typeof(PawnRenderNode), "props");

        internal static object TryGetOwnerFromNode(PawnRenderNode node)
        {
            if (node == null) return null;
            object v = null;
            if (PI_Node_Owner != null) { try { v = PI_Node_Owner.GetValue(node, null); } catch { } }
            if (v != null) return v;
            if (FI_Node_owner != null) { try { v = FI_Node_owner.GetValue(node); } catch { } }
            return v;
        }

        internal static object TryGetPropsFromNode(PawnRenderNode node)
        {
            if (node == null) return null;
            object v = null;
            if (PI_Node_Props != null) { try { v = PI_Node_Props.GetValue(node, null); } catch { } }
            if (v != null) return v;
            if (FI_Node_props != null) { try { v = FI_Node_props.GetValue(node); } catch { } }
            return v;
        }

        // out 버전(비제네릭)
        internal static bool TryGetPropsFromNode(PawnRenderNode node, out PawnRenderNodeProperties props)
        {
            props = TryGetPropsFromNode(node) as PawnRenderNodeProperties;
            return props != null;
        }

        // 제네릭 out 버전
        internal static bool TryGetPropsFromNode<T>(PawnRenderNode node, out T props) where T : class
        {
            props = TryGetPropsFromNode(node) as T;
            return props != null;
        }

        // ── PawnRenderNodeWorker.owner (타입별 캐시) ──
        private static readonly ConcurrentDictionary<Type, FieldInfo> OwnerFieldByWorker =
            new ConcurrentDictionary<Type, FieldInfo>();

        internal static object TryGetOwnerFromWorker(object worker)
        {
            if (worker == null) return null;
            var t = worker.GetType();
            FieldInfo fi;
            if (!OwnerFieldByWorker.TryGetValue(t, out fi) || fi == null)
            {
                fi = t.GetField("owner", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                OwnerFieldByWorker[t] = fi;
            }
            return fi != null ? fi.GetValue(worker) : null;
        }

        // ── 범용 캐시: 필드/프로퍼티 ──
        private static readonly ConcurrentDictionary<string, FieldInfo> FieldCache =
            new ConcurrentDictionary<string, FieldInfo>();
        private static readonly ConcurrentDictionary<string, PropertyInfo> PropCache =
            new ConcurrentDictionary<string, PropertyInfo>();

        private static FieldInfo GetFieldCached(Type t, string name)
        {
            if (t == null || string.IsNullOrEmpty(name)) return null;
            string key = t.FullName + "::F::" + name;
            FieldInfo fi;
            if (!FieldCache.TryGetValue(key, out fi) || fi == null)
            {
                fi = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                FieldCache[key] = fi;
            }
            return fi;
        }

        private static PropertyInfo GetPropCached(Type t, string name)
        {
            if (t == null || string.IsNullOrEmpty(name)) return null;
            string key = t.FullName + "::P::" + name;
            PropertyInfo pi;
            if (!PropCache.TryGetValue(key, out pi) || pi == null)
            {
                pi = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                PropCache[key] = pi;
            }
            return pi;
        }

        // ── 메서드 캐시 ──
        private static readonly ConcurrentDictionary<string, MethodInfo> MethodCache =
            new ConcurrentDictionary<string, MethodInfo>();

        private static MethodInfo GetMethodCached(Type t, string name, int argc, bool isStatic)
        {
            if (t == null || string.IsNullOrEmpty(name)) return null;
            string key = t.FullName + "::M::" + (isStatic ? "S" : "I") + "::" + name + "#" + argc;

            MethodInfo mi;
            if (!MethodCache.TryGetValue(key, out mi) || mi == null)
            {
                var flags = (isStatic ? BindingFlags.Static : BindingFlags.Instance)
                            | BindingFlags.Public | BindingFlags.NonPublic;

                var methods = t.GetMethods(flags);
                // 1) 우선 인자 수로 1차 필터
                MethodInfo candidate = null;
                for (int i = 0; i < methods.Length; i++)
                {
                    var m = methods[i];
                    if (!string.Equals(m.Name, name, StringComparison.Ordinal)) continue;
                    var ps = m.GetParameters();
                    if (ps == null || ps.Length != argc) continue;

                    // 2) 타입 매칭 시도: 캐시 키가 argc만 써서, 여기선 가장 보편적인 시그니처를 고름
                    //    (전역 호출 헬퍼에서 args 타입을 모르면 어쩔 수 없음)
                    candidate = m;
                    break;
                }
                MethodCache[key] = candidate;
                mi = candidate;
            }
            return mi;
        }

        // ── 인스턴스 메서드 호출(결과 무시) ──
        internal static bool TryCallInstanceMethod(object obj, string name)
        {
            object _; return TryCallInstanceMethod(obj, name, null, out _);
        }

        // ── 인스턴스 메서드 호출(결과 무시, 인자 포함) ──
        internal static bool TryCallInstanceMethod(object obj, string name, object[] args)
        {
            object _; return TryCallInstanceMethod(obj, name, args, out _);
        }

        // ── 인스턴스 메서드 호출(결과 out) ──
        internal static bool TryCallInstanceMethod(object obj, string name, object[] args, out object result)
        {
            result = null;
            if (obj == null || string.IsNullOrEmpty(name)) return false;

            int argc = (args == null) ? 0 : args.Length;
            var mi = GetMethodCached(obj.GetType(), name, argc, false);
            if (mi == null) return false;

            try { result = mi.Invoke(obj, args); return true; } catch { return false; }
        }

        // ── 정적 메서드 호출(옵션) ──
        internal static bool TryCallStaticMethod(Type t, string name)
        {
            object _; return TryCallStaticMethod(t, name, null, out _);
        }

        internal static bool TryCallStaticMethod(Type t, string name, object[] args, out object result)
        {
            result = null;
            if (t == null || string.IsNullOrEmpty(name)) return false;

            int argc = (args == null) ? 0 : args.Length;
            var mi = GetMethodCached(t, name, argc, true);
            if (mi == null) return false;

            try { result = mi.Invoke(null, args); return true; } catch { return false; }
        }

        internal static T GetInstanceField<T>(object obj, string name)
        {
            if (obj == null) return default(T);
            var fi = GetFieldCached(obj.GetType(), name);
            if (fi == null) return default(T);
            try { return (T)fi.GetValue(obj); } catch { return default(T); }
        }

        internal static T GetInstanceProperty<T>(object obj, string name)
        {
            if (obj == null) return default(T);
            var pi = GetPropCached(obj.GetType(), name);
            if (pi == null || !pi.CanRead) return default(T);
            try { return (T)pi.GetValue(obj, null); } catch { return default(T); }
        }

        // ✅ Setter 헬퍼 추가
        internal static bool TrySetInstanceField(object obj, string name, object value)
        {
            if (obj == null || string.IsNullOrEmpty(name)) return false;
            var fi = GetFieldCached(obj.GetType(), name);
            if (fi == null) return false;
            try { fi.SetValue(obj, value); return true; } catch { return false; }
        }

        internal static bool TrySetInstanceProperty(object obj, string name, object value)
        {
            if (obj == null || string.IsNullOrEmpty(name)) return false;
            var pi = GetPropCached(obj.GetType(), name);
            if (pi == null || !pi.CanWrite) return false;
            try { pi.SetValue(obj, value, null); return true; } catch { return false; }
        }

        // ── 타입 스캔: 객체 a/b의 인스턴스 필드 중 특정 타입 찾기 ──
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
            var fs = obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
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
                    catch { }
                }
            }
            return null;
        }

        // ── 공통 패턴: exclusionTags(List<string>) 추출 ──
        internal static List<string> TryGetExclusionTags(object obj)
        {
            if (obj == null) return null;

            // 필드
            var fs = obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < fs.Length; i++)
            {
                if (string.Equals(fs[i].Name, "exclusionTags", StringComparison.OrdinalIgnoreCase)
                    && typeof(List<string>).IsAssignableFrom(fs[i].FieldType))
                {
                    try { return (List<string>)fs[i].GetValue(obj); } catch { }
                }
            }

            // 프로퍼티
            var ps = obj.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < ps.Length; i++)
            {
                if (ps[i].CanRead
                    && string.Equals(ps[i].Name, "exclusionTags", StringComparison.OrdinalIgnoreCase)
                    && typeof(List<string>).IsAssignableFrom(ps[i].PropertyType))
                {
                    try { return (List<string>)ps[i].GetValue(obj, null); } catch { }
                }
            }
            return null;
        }
        internal static Type TryType(string fullName) => AccessTools.TypeByName(fullName);
        internal static void ClearCaches()
        {
            FieldCache.Clear();
            PropCache.Clear();
            MethodCache.Clear();
            OwnerFieldByWorker.Clear();
            HolderPawnField.Clear();
        }
    }
}
