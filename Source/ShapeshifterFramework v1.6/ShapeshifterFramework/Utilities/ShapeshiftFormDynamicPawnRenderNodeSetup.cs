// ShapeshifterFramework | Utilities | ShapeshiftFormDynamicPawnRenderNodeSetup.cs
// 목적 : 활성화된 폼에 정의된 커스텀 렌더 노드(PawnRenderNodeProperties)를 바닐라 폰 렌더링 파이프라인(RenderTree)에 동적으로 주입.
// 용도 : 폼에 지정된 nodeClass의 타입 정보를 리플렉션으로 분석해 알맞은 생성자(Constructor)를 동적으로 호출하여 노드 인스턴스를 생성함.
// 주의 : 노드 생성 실패(예외) 시 게임이 터지는 것을 막기 위한 5단계 폴백(Fallback)이 적용되어 있으며, 디버그 로그는 120틱 쿨타임을 두어 스팸을 억제함.

using HarmonyLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace ShapeshifterFramework.Utilities
{
    // 바닐라 파이프라인에 정석으로 합류하는 다이나믹 노드 세트
    public class ShapeshiftFormDynamicPawnRenderNodeSetup : DynamicPawnRenderNodeSetup
    {
        public override bool HumanlikeOnly => false;

        // Dev 로그 스로틀링
        static readonly Dictionary<int, int> lastLogTick = new Dictionary<int, int>();
        const int LogCooldownTicks = 120; // 2초 정도
        const int LogDictCleanupThreshold = 128; // 딕셔너리 무한 성장 방지 임계값
        static int lastCleanupTick;

        // 비변신/노드없음 케이스용 무할당 빈 리스트 (읽기 전용으로만 반환 — 절대 변경하지 않음)
        static readonly List<ValueTuple<PawnRenderNode, PawnRenderNode>> _emptyDynamicNodes
            = new List<ValueTuple<PawnRenderNode, PawnRenderNode>>();

        public override IEnumerable<(PawnRenderNode node, PawnRenderNode parent)> GetDynamicNodes(Pawn pawn, PawnRenderTree tree)
        {
            // 비변신 폰(흔한 경우)은 리스트 할당 없이 공유 빈 리스트 반환
            if (!ShapeshiftRegistry.TryGet(pawn, out var comp, out var form))
                return _emptyDynamicNodes;

            var extras = form.renderNodeProperties;
            if (extras == null || extras.Count == 0)
                return _emptyDynamicNodes;

            var outList = new List<ValueTuple<PawnRenderNode, PawnRenderNode>>();

            for (int i = 0; i < extras.Count; i++)
            {
                var props = extras[i];
                if (props == null || props.nodeClass == null) continue;

                PawnRenderNode node = TryMakeNode(props, pawn, tree);
                if (node == null) continue;

                // parent=null로 넘기면 AddChild가 props.parentTagDef를 보고 라우팅
                outList.Add(new ValueTuple<PawnRenderNode, PawnRenderNode>(node, null));
            }

            LogDynamicNodes(pawn, outList, extras);
            return outList;
        }

        // ── 리플렉션 캐시 ──
        // PawnRenderNode의 private 필드 — 항상 동일 타입이므로 정적 캐시
        private static readonly FieldInfo _fProps = AccessTools.Field(typeof(PawnRenderNode), "props");
        private static readonly FieldInfo _fGraph = AccessTools.Field(typeof(PawnRenderNode), "graph");

        /// <summary>Dev 모드 로그 출력 (쿨타임 적용, 스팸 방지).</summary>
        static void LogDynamicNodes(Pawn pawn, List<ValueTuple<PawnRenderNode, PawnRenderNode>> outList, List<PawnRenderNodeProperties> extras)
        {
            if (!ShapeshiftDiagnostics.DebugLog || outList.Count == 0) return;

            int tick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            int id = pawn.thingIDNumber;

            // 딕셔너리가 임계값 초과 시 또는 일정 주기로 만료 엔트리 정리
            if (lastLogTick.Count > LogDictCleanupThreshold || tick - lastCleanupTick > LogCooldownTicks * 50)
            {
                lastCleanupTick = tick;
                var staleKeys = new List<int>();
                foreach (var kv in lastLogTick)
                    if (tick - kv.Value > LogCooldownTicks * 5) staleKeys.Add(kv.Key);
                for (int k = 0; k < staleKeys.Count; k++)
                    lastLogTick.Remove(staleKeys[k]);
            }

            int last;
            if (!lastLogTick.TryGetValue(id, out last) || tick - last >= LogCooldownTicks)
            {
                lastLogTick[id] = tick;
                try
                {
                    ShapeshiftDiagnostics.Info($"DynamicSetup_ShapeshifterForm: +{outList.Count} node(s) for {pawn.LabelShortCap} ({pawn.thingIDNumber})");
                    for (int i = 0; i < extras.Count; i++)
                    {
                        var p = extras[i];
                        if (p == null) continue;
                        var parentTag = p.parentTagDef != null ? p.parentTagDef.defName : "null";
                        var tag = p.tagDef != null ? p.tagDef.defName : "null";
                        ShapeshiftDiagnostics.Info($"  - {p.nodeClass?.Name}  parentTag={parentTag}  tag={tag}  tex={p.texPath ?? "(none)"}");
                    }
                }
                catch { /* 로그 실패 무시 */ }
            }
        }

        // 생성자 시그니처 후보 (우선순위순)
        private static readonly Type[][] CtorSignatures =
        {
            new[] { typeof(Pawn), typeof(PawnRenderNodeProperties), typeof(PawnRenderTree) },
            new[] { typeof(PawnRenderNodeProperties), typeof(PawnRenderTree) },
            new[] { typeof(PawnRenderNodeProperties) },
            new[] { typeof(PawnRenderTree) },
            Type.EmptyTypes,
        };

        // nodeClass별 생성자 캐시 — 동적 생성자 호출이지만 같은 타입은 한 번만 탐색
        private static readonly ConcurrentDictionary<Type, (ConstructorInfo ctor, int sigIndex)> _ctorCache =
            new ConcurrentDictionary<Type, (ConstructorInfo, int)>();

        // vanilla-private: PawnRenderNode.props, PawnRenderNode.graph (RimWorld 1.6) — 바닐라 버전 변경 시 확인 필요
        static PawnRenderNode TryMakeNode(PawnRenderNodeProperties props, Pawn pawn, PawnRenderTree tree)
        {
            try
            {
                var nodeClass = props.nodeClass;
                if (!_ctorCache.TryGetValue(nodeClass, out var cached))
                {
                    cached = (null, -1);
                    for (int i = 0; i < CtorSignatures.Length; i++)
                    {
                        var ctor = nodeClass.GetConstructor(CtorSignatures[i]);
                        if (ctor != null) { cached = (ctor, i); break; }
                    }
                    _ctorCache[nodeClass] = cached;
                }

                if (cached.ctor == null) return null;

                PawnRenderNode node;
                switch (cached.sigIndex)
                {
                    case 0: // (Pawn, Props, Tree)
                        // 지역 배열 사용 — 병렬 렌더 스레드 간 공유 버퍼 레이스 방지 (노드 생성은 빈번하지 않아 GC 무시 가능).
                        return (PawnRenderNode)cached.ctor.Invoke(new object[] { pawn, props, tree });

                    case 1: // (Props, Tree)
                        return (PawnRenderNode)cached.ctor.Invoke(new object[] { props, tree });

                    case 2: // (Props)
                        node = (PawnRenderNode)cached.ctor.Invoke(new object[] { props });
                        if (_fGraph != null && node != null) _fGraph.SetValue(node, tree);
                        return node;

                    case 3: // (Tree)
                        node = (PawnRenderNode)cached.ctor.Invoke(new object[] { tree });
                        if (_fProps != null && node != null) _fProps.SetValue(node, props);
                        return node;

                    case 4: // ()
                        node = (PawnRenderNode)cached.ctor.Invoke(null);
                        if (_fProps != null && node != null) _fProps.SetValue(node, props);
                        if (_fGraph != null && node != null) _fGraph.SetValue(node, tree);
                        return node;
                }
            }
            catch (Exception e)
            {
                Log.Warning("[SSF] MakeNode failed: " + e);
            }
            return null;
        }
    }
}
