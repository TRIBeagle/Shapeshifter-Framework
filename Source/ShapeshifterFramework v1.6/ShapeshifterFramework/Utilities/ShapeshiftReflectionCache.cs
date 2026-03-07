// ShapeshifterFramework | Utilities | ShapeshiftReflectionCache.cs
// 목적 : 바닐라 및 타 모드의 비공개 멤버(Field, Property, Method)에 접근하는 리플렉션 연산 비용과 충돌 위험을 최소화하는 중앙 집중식 캐시 매니저.
// 용도 : ConcurrentDictionary와 AccessTools.FieldRef를 혼용해 읽기/쓰기 속도를 극대화하며, 외부 모드가 없거나 버전이 달라 멤버 탐색에 실패할 경우 예외(Exception)를 삼키고 null/false를 반환해 게임 크래시를 완벽히 방어함.

using HarmonyLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using Verse.AI;

namespace ShapeshifterFramework.Utilities
{
    /// <summary>
    /// 리플렉션 접근/캐시 유틸리티.
    /// - PawnRenderer.pawn 등 빈번 접근 경로는 FieldRef 우선, 실패 시 FieldInfo 폴백.
    /// - 타입별/이름별 멤버를 키로 캐싱하여 반복 리플렉션 비용을 상쇄.
    /// - 메서드 호출은 인자 수 기반 캐시 키를 사용(정확 시그니처가 불명확한 경우의 보편적 선택).
    /// - Setter 헬퍼(TrySetInstanceField/Property) 포함.
    /// 부작용:
    /// - 내부 예외를 삼켜 호출부 흐름을 유지(호출자는 성공/실패만 확인).
    /// </summary>
    internal static class ShapeshiftReflectionCache
    {
        #region PawnRenderer.pawn

        private static readonly AccessTools.FieldRef<PawnRenderer, Pawn> RendererPawnRef =
            AccessTools.FieldRefAccess<PawnRenderer, Pawn>("pawn");
        private static readonly FieldInfo RendererPawnFI =
            AccessTools.Field(typeof(PawnRenderer), "pawn");

        /// <summary>
        /// PawnRenderer가 소유한 Pawn을 반환한다.
        /// FieldRef 우선, 실패 시 FieldInfo 폴백.
        /// </summary>
        internal static Pawn GetPawn(PawnRenderer renderer)
        {
            if (renderer == null) return null;
            try { return RendererPawnRef(renderer); } catch { }
            return RendererPawnFI != null ? (Pawn)RendererPawnFI.GetValue(renderer) : null;
        }

        #endregion

        #region Pawn_PathFollower.pawn

        private static readonly AccessTools.FieldRef<Pawn_PathFollower, Pawn> PathFollowerPawnRef =
            AccessTools.FieldRefAccess<Pawn_PathFollower, Pawn>("pawn");
        private static readonly FieldInfo PathFollowerPawnFI =
            AccessTools.Field(typeof(Pawn_PathFollower), "pawn");

        /// <summary>
        /// Pawn_PathFollower가 소유한 Pawn을 반환한다.
        /// FieldRef 우선, 실패 시 FieldInfo 폴백.
        /// </summary>
        internal static Pawn GetPawn(Pawn_PathFollower pf)
        {
            if (pf == null) return null;
            try { return PathFollowerPawnRef(pf); } catch { }
            return PathFollowerPawnFI != null ? (Pawn)PathFollowerPawnFI.GetValue(pf) : null;
        }

        #endregion

        #region PawnRenderer.results → PreRenderResults.parms

        // [주의] results는 struct(PreRenderResults)라서 boxing/unboxing 시 SetValue로 다시 넣어줘야 반영됨.
        private static readonly FieldInfo RendererResultsFI =
            AccessTools.Field(typeof(PawnRenderer), "results");

        private static readonly ConcurrentDictionary<Type, FieldInfo> PreRenderParmsFieldByResultsType =
            new ConcurrentDictionary<Type, FieldInfo>();

        /// <summary>
        /// PawnRenderer.results(PreRenderResults)의 parms(PawnDrawParms)를 안전하게 얻는다.
        /// boxedResults/parmsFi를 같이 돌려줘서, 수정 후 TrySetPreRenderParms로 다시 반영 가능.
        /// </summary>
        internal static bool TryGetPreRenderParms(PawnRenderer renderer, out object boxedResults, out FieldInfo parmsFi, out PawnDrawParms parms)
        {
            boxedResults = null;
            parmsFi = null;
            parms = default(PawnDrawParms);

            if (renderer == null || RendererResultsFI == null) return false;

            try { boxedResults = RendererResultsFI.GetValue(renderer); } catch { boxedResults = null; }
            if (boxedResults == null) return false;

            var t = boxedResults.GetType(); // PawnRenderer+PreRenderResults
            if (t == null) return false;

            if (!PreRenderParmsFieldByResultsType.TryGetValue(t, out parmsFi) || parmsFi == null)
            {
                parmsFi = t.GetField("parms", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                PreRenderParmsFieldByResultsType[t] = parmsFi;
            }
            if (parmsFi == null) return false;

            try
            {
                parms = (PawnDrawParms)parmsFi.GetValue(boxedResults);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// TryGetPreRenderParms로 얻은 boxedResults/parmsFi 조합을 이용해 parms 변경을 renderer.results에 반영한다.
        /// </summary>
        internal static bool TrySetPreRenderParms(PawnRenderer renderer, object boxedResults, FieldInfo parmsFi, PawnDrawParms parms)
        {
            if (renderer == null || RendererResultsFI == null) return false;
            if (boxedResults == null || parmsFi == null) return false;

            try
            {
                parmsFi.SetValue(boxedResults, parms);
                RendererResultsFI.SetValue(renderer, boxedResults);
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region eq.ParentHolder 체인 → pawn

        private static readonly ConcurrentDictionary<Type, FieldInfo> HolderPawnField =
            new ConcurrentDictionary<Type, FieldInfo>();

        /// <summary>
        /// 장비/아이템의 ParentHolder 체인을 따라가 Pawn을 탐색한다(최대 hop 제한).
        /// </summary>
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

        #endregion

        #region AttachPointTracker.parent

        private static readonly Type AttachPointTrackerT = AccessTools.TypeByName("Verse.AttachPointTracker");
        private static readonly FieldInfo AptParentFI = AttachPointTrackerT != null
            ? AttachPointTrackerT.GetField("parent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            : null;

        /// <summary>
        /// AttachPointTracker의 parent Thing을 반환한다(없으면 null).
        /// </summary>
        internal static Thing GetAttachParent(object attachPointTracker)
        {
            if (attachPointTracker == null || AptParentFI == null) return null;
            try { return (Thing)AptParentFI.GetValue(attachPointTracker); } catch { return null; }
        }

        #endregion

        #region PawnRenderNode: Owner/Props

        private static readonly PropertyInfo PI_Node_Owner = AccessTools.Property(typeof(PawnRenderNode), "Owner");
        private static readonly FieldInfo FI_Node_owner = AccessTools.Field(typeof(PawnRenderNode), "owner");
        private static readonly PropertyInfo PI_Node_Props = AccessTools.Property(typeof(PawnRenderNode), "Props");
        private static readonly FieldInfo FI_Node_props = AccessTools.Field(typeof(PawnRenderNode), "props");

        /// <summary>PawnRenderNode.Owner를 안전하게 가져온다(Prop→Field 폴백).</summary>
        internal static object TryGetOwnerFromNode(PawnRenderNode node)
        {
            if (node == null) return null;
            object v = null;
            if (PI_Node_Owner != null) { try { v = PI_Node_Owner.GetValue(node, null); } catch { } }
            if (v != null) return v;
            if (FI_Node_owner != null) { try { v = FI_Node_owner.GetValue(node); } catch { } }
            return v;
        }

        /// <summary>PawnRenderNode.Props를 안전하게 가져온다(Prop→Field 폴백).</summary>
        internal static object TryGetPropsFromNode(PawnRenderNode node)
        {
            if (node == null) return null;
            object v = null;
            if (PI_Node_Props != null) { try { v = PI_Node_Props.GetValue(node, null); } catch { } }
            if (v != null) return v;
            if (FI_Node_props != null) { try { v = FI_Node_props.GetValue(node); } catch { } }
            return v;
        }

        /// <summary>Props(out) 비제네릭 버전.</summary>
        internal static bool TryGetPropsFromNode(PawnRenderNode node, out PawnRenderNodeProperties props)
        {
            props = TryGetPropsFromNode(node) as PawnRenderNodeProperties;
            return props != null;
        }

        /// <summary>Props(out) 제네릭 버전.</summary>
        internal static bool TryGetPropsFromNode<T>(PawnRenderNode node, out T props) where T : class
        {
            props = TryGetPropsFromNode(node) as T;
            return props != null;
        }

        #endregion

        #region PawnRenderNodeWorker.owner (타입별 캐시)

        private static readonly ConcurrentDictionary<Type, FieldInfo> OwnerFieldByWorker =
            new ConcurrentDictionary<Type, FieldInfo>();

        /// <summary>
        /// 워커 인스턴스가 가진 owner 필드를 타입별로 캐싱하여 반환한다.
        /// </summary>
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

        #endregion

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

        #region 메서드 캐시/호출

        private static readonly ConcurrentDictionary<string, MethodInfo> MethodCache =
            new ConcurrentDictionary<string, MethodInfo>();
        // 메서드 탐색 실패 기록
        private static readonly ConcurrentDictionary<string, bool> MethodNotFound =
            new ConcurrentDictionary<string, bool>();

        private static MethodInfo GetMethodCached(Type t, string name, Type[] paramTypes, bool isStatic)
        {
            if (t == null || string.IsNullOrEmpty(name)) return null;

            int argc = paramTypes?.Length ?? 0;
            // [수정] LINQ(Select)를 제거하고 수동 StringBuilder 루프로 대체
            string typeStr;
            if (paramTypes == null || paramTypes.Length == 0)
            {
                typeStr = "0";
            }
            else
            {
                var sb = new System.Text.StringBuilder(paramTypes.Length * 16);
                for (int i = 0; i < paramTypes.Length; i++)
                {
                    if (i > 0) sb.Append('_');
                    sb.Append(paramTypes[i] != null ? paramTypes[i].Name : "any");
                }
                typeStr = sb.ToString();
            }
            string key = string.Concat(t.FullName, "::M::", isStatic ? "S" : "I", "::", name, "#", typeStr);

            if (MethodNotFound.ContainsKey(key)) return null; // 실패 기록 확인

            if (!MethodCache.TryGetValue(key, out MethodInfo mi))
            {
                var flags = (isStatic ? BindingFlags.Static : BindingFlags.Instance)
                            | BindingFlags.Public | BindingFlags.NonPublic;

                var methods = t.GetMethods(flags);
                MethodInfo candidate = null;
                for (int i = 0; i < methods.Length; i++)
                {
                    var m = methods[i];
                    if (!string.Equals(m.Name, name, StringComparison.Ordinal)) continue;

                    var ps = m.GetParameters();
                    if (ps == null || ps.Length != argc) continue;

                    bool match = true;
                    if (paramTypes != null)
                    {
                        for (int j = 0; j < argc; j++)
                        {
                            if (paramTypes[j] != null && !ps[j].ParameterType.IsAssignableFrom(paramTypes[j]))
                            {
                                match = false;
                                break;
                            }
                        }
                    }

                    if (match)
                    {
                        candidate = m;
                        break;
                    }
                }

                if (candidate != null)
                {
                    MethodCache[key] = candidate;
                    mi = candidate;
                }
                else
                {
                    MethodNotFound[key] = true; // 실패 기록
                }
            }
            return mi;
        }

        /// <summary>인스턴스 메서드 호출(결과 무시).</summary>
        internal static bool TryCallInstanceMethod(object obj, string name)
        {
            object _; return TryCallInstanceMethod(obj, name, null, out _);
        }

        /// <summary>인스턴스 메서드 호출(결과 무시, 인자 포함).</summary>
        internal static bool TryCallInstanceMethod(object obj, string name, object[] args)
        {
            object _; return TryCallInstanceMethod(obj, name, args, out _);
        }

        /// <summary>인스턴스 메서드 호출(결과 out).</summary>
        internal static bool TryCallInstanceMethod(object obj, string name, object[] args, out object result)
        {
            result = null;
            if (obj == null || string.IsNullOrEmpty(name)) return false;

            // 넘겨받은 args 배열에서 실제 타입들을 추출
            Type[] paramTypes = null;
            if (args != null)
            {
                paramTypes = new Type[args.Length];
                for (int i = 0; i < args.Length; i++)
                {
                    paramTypes[i] = args[i]?.GetType(); // null인 경우 null 저장 (GetMethodCached에서 처리)
                }
            }

            var mi = GetMethodCached(obj.GetType(), name, paramTypes, false);
            if (mi == null) return false;

            try { result = mi.Invoke(obj, args); return true; } catch { return false; }
        }

        /// <summary>정적 메서드 호출(옵션 인자/결과 out).</summary>
        internal static bool TryCallStaticMethod(Type t, string name)
        {
            object _; return TryCallStaticMethod(t, name, null, out _);
        }

        internal static bool TryCallStaticMethod(Type t, string name, object[] args, out object result)
        {
            result = null;
            if (t == null || string.IsNullOrEmpty(name)) return false;

            // 넘겨받은 args 배열에서 실제 타입들을 추출
            Type[] paramTypes = null;
            if (args != null)
            {
                paramTypes = new Type[args.Length];
                for (int i = 0; i < args.Length; i++)
                {
                    paramTypes[i] = args[i]?.GetType();
                }
            }

            var mi = GetMethodCached(t, name, paramTypes, true);
            if (mi == null) return false;

            try { result = mi.Invoke(null, args); return true; } catch { return false; }
        }

        #endregion

        #region 필드/프로퍼티 Getter/Setter

        /// <summary>인스턴스 필드 값을 제네릭으로 읽는다(캐시 사용).</summary>
        internal static T GetInstanceField<T>(object obj, string name)
        {
            if (obj == null) return default(T);
            var fi = GetFieldCached(obj.GetType(), name);
            if (fi == null) return default(T);
            try { return (T)fi.GetValue(obj); } catch { return default(T); }
        }

        /// <summary>인스턴스 프로퍼티 값을 제네릭으로 읽는다(캐시 사용).</summary>
        internal static T GetInstanceProperty<T>(object obj, string name)
        {
            if (obj == null) return default(T);
            var pi = GetPropCached(obj.GetType(), name);
            if (pi == null || !pi.CanRead) return default(T);
            try { return (T)pi.GetValue(obj, null); } catch { return default(T); }
        }

        /// <summary>인스턴스 필드 값을 설정한다(캐시 사용).</summary>
        internal static bool TrySetInstanceField(object obj, string name, object value)
        {
            if (obj == null || string.IsNullOrEmpty(name)) return false;
            var fi = GetFieldCached(obj.GetType(), name);
            if (fi == null) return false;
            try { fi.SetValue(obj, value); return true; } catch { return false; }
        }

        /// <summary>인스턴스 프로퍼티 값을 설정한다(캐시 사용).</summary>
        internal static bool TrySetInstanceProperty(object obj, string name, object value)
        {
            if (obj == null || string.IsNullOrEmpty(name)) return false;
            var pi = GetPropCached(obj.GetType(), name);
            if (pi == null || !pi.CanWrite) return false;
            try { pi.SetValue(obj, value, null); return true; } catch { return false; }
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

        /// <summary>
        /// 객체 a/b의 인스턴스 필드에서 특정 타입의 값을 탐색하여 반환한다(첫 일치).
        /// </summary>
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
                    catch { }
                }
            }
            return null;
        }

        /// <summary>
        /// 공통 패턴: exclusionTags(List&lt;string&gt;)를 필드/프로퍼티에서 찾아 반환한다.
        /// </summary>
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
                    try { return (List<string>)fs[i].GetValue(obj); } catch { }
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
                    try { return (List<string>)ps[i].GetValue(obj, null); } catch { }
                }
            }
            return null;
        }

        /// <summary>풀네임으로 타입을 찾는다(AccessTools.TypeByName 포장).</summary>
        internal static Type TryType(string fullName) => AccessTools.TypeByName(fullName);

        /// <summary>런타임 캐시 초기화(테스트/핫리로드 시 유용).</summary>
        internal static void ClearCaches()
        {
            FieldCache.Clear();
            FieldNotFound.Clear();
            PropCache.Clear();
            PropNotFound.Clear();
            MethodCache.Clear();
            MethodNotFound.Clear();
            OwnerFieldByWorker.Clear();
            HolderPawnField.Clear();
            PreRenderParmsFieldByResultsType.Clear();
        }

        #endregion
    }
}