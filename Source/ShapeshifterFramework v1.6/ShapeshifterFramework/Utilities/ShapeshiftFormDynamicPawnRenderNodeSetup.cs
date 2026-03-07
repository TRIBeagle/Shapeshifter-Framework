// ShapeshifterFramework | Utilities | ShapeshiftFormDynamicPawnRenderNodeSetup.cs
// 목적 : 활성화된 폼에 정의된 커스텀 렌더 노드(PawnRenderNodeProperties)를 바닐라 폰 렌더링 파이프라인(RenderTree)에 동적으로 주입.
// 용도 : 폼에 지정된 nodeClass의 타입 정보를 리플렉션으로 분석해 알맞은 생성자(Constructor)를 동적으로 호출하여 노드 인스턴스를 생성함.
// 주의 : 노드 생성 실패(예외) 시 게임이 터지는 것을 막기 위한 5단계 폴백(Fallback)이 적용되어 있으며, 디버그 로그는 120틱 쿨타임을 두어 스팸을 억제함.

using HarmonyLib;
using ShapeshifterFramework.Comps;
using System;
using System.Collections.Generic;
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
        const int LogDictCleanupThreshold = 256; // [수정] 딕셔너리 무한 성장 방지 임계값

        public override IEnumerable<(PawnRenderNode node, PawnRenderNode parent)> GetDynamicNodes(Pawn pawn, PawnRenderTree tree)
        {
            var outList = new List<ValueTuple<PawnRenderNode, PawnRenderNode>>();

            var comp = pawn?.TryGetComp<CompShapeshifter>();
            var form = (comp != null && comp.isTransformed) ? comp.currentForm : null;
            if (form == null)
                return outList;

            var extras = form.renderNodeProperties;
            if (extras == null || extras.Count == 0)
                return outList;

            for (int i = 0; i < extras.Count; i++)
            {
                var props = extras[i];
                if (props == null || props.nodeClass == null) continue;

                PawnRenderNode node = TryMakeNode(props, pawn, tree);
                if (node == null) continue;

                // parent=null로 넘기면 AddChild가 props.parentTagDef를 보고 라우팅
                outList.Add(new ValueTuple<PawnRenderNode, PawnRenderNode>(node, null));
            }

            // ── Dev 모드 로그
            if (ShapeshiftDiagnostics.DebugLog && outList.Count > 0)
            {
                int tick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
                int id = pawn.thingIDNumber;

                // [수정] 딕셔너리가 임계값을 넘으면 만료된 엔트리 일괄 정리
                if (lastLogTick.Count > LogDictCleanupThreshold)
                {
                    var staleKeys = new List<int>();
                    foreach (var kv in lastLogTick)
                        if (tick - kv.Value > LogCooldownTicks * 10) staleKeys.Add(kv.Key);
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

            return outList;
        }

        static PawnRenderNode TryMakeNode(PawnRenderNodeProperties props, Pawn pawn, PawnRenderTree tree)
        {
            try
            {
                // 1) (Pawn, PawnRenderNodeProperties, PawnRenderTree)
                var ctor = props.nodeClass.GetConstructor(new Type[] { typeof(Pawn), typeof(PawnRenderNodeProperties), typeof(PawnRenderTree) });
                if (ctor != null) return (PawnRenderNode)ctor.Invoke(new object[] { pawn, props, tree });

                // 2) (PawnRenderNodeProperties, PawnRenderTree)
                ctor = props.nodeClass.GetConstructor(new Type[] { typeof(PawnRenderNodeProperties), typeof(PawnRenderTree) });
                if (ctor != null) return (PawnRenderNode)ctor.Invoke(new object[] { props, tree });

                // 3) (PawnRenderNodeProperties)
                ctor = props.nodeClass.GetConstructor(new Type[] { typeof(PawnRenderNodeProperties) });
                if (ctor != null)
                {
                    var node = (PawnRenderNode)ctor.Invoke(new object[] { props });
                    // 그래프 필드 세팅(있으면)
                    var fGraph = AccessTools.Field(typeof(PawnRenderNode), "graph");
                    if (fGraph != null && node != null) fGraph.SetValue(node, tree);
                    return node;
                }

                // 4) (PawnRenderTree)
                ctor = props.nodeClass.GetConstructor(new Type[] { typeof(PawnRenderTree) });
                if (ctor != null)
                {
                    var node = (PawnRenderNode)ctor.Invoke(new object[] { tree });
                    var fProps = AccessTools.Field(typeof(PawnRenderNode), "props");
                    if (fProps != null && node != null) fProps.SetValue(node, props);
                    return node;
                }

                // 5) 기본 생성자
                ctor = props.nodeClass.GetConstructor(Type.EmptyTypes);
                if (ctor != null)
                {
                    var node = (PawnRenderNode)ctor.Invoke(null);
                    var fProps = AccessTools.Field(typeof(PawnRenderNode), "props");
                    var fGraph = AccessTools.Field(typeof(PawnRenderNode), "graph");
                    if (fProps != null && node != null) fProps.SetValue(node, props);
                    if (fGraph != null && node != null) fGraph.SetValue(node, tree);
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
