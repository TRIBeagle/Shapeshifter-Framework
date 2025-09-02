// DynamicPawnRenderNodeSetup_ShapeshifterForm.cs
// 목적: 활성 폼의 renderNodeProperties를 바닐라 렌더 트리에 동적으로 주입.
// 용도: 폼별 추가 파츠를 Pawn 렌더 파이프라인에 합류시킴.
// 주의: 폼 미활성/목록 비면 무시. 노드 생성 시 예외 방지(널가드) 필수.

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

            // ── Dev 모드 로그 (과다 출력 방지)
            if (Prefs.DevMode && outList.Count > 0)
            {
                int tick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
                int id = pawn.thingIDNumber;

                int last;
                if (!lastLogTick.TryGetValue(id, out last) || tick - last >= LogCooldownTicks)
                {
                    lastLogTick[id] = tick;
                    try
                    {
                        Log.Message($"[SSF] DynamicSetup_ShapeshifterForm: +{outList.Count} node(s) for {pawn.LabelShortCap} ({pawn.thingIDNumber})");
                        for (int i = 0; i < extras.Count; i++)
                        {
                            var p = extras[i];
                            if (p == null) continue;
                            var parentTag = p.parentTagDef != null ? p.parentTagDef.defName : "null";
                            var tag = p.tagDef != null ? p.tagDef.defName : "null";
                            Log.Message($"[SSF]   - {p.nodeClass?.Name}  parentTag={parentTag}  tag={tag}  tex={p.texPath ?? "(none)"}");
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
