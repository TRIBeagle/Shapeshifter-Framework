// ShapeshifterFramework | Patches | Patch_DynamicDrawManager_DrawDynamicThings.cs
// 목적 : DynamicDrawManager.DrawDynamicThings의 뷰렉트 확장(ExpandedBy(1))으로 커버되지 않는 대형 변신 폰을 보조 드로우.
// 용도 : bodyDrawScale이 큰 폼은 비주얼이 수 셀 밖으로 확장되나, 바닐라는 1셀만 확장하여 화면 가장자리에서 Draw()가 호출되지 않음.
//        Postfix에서 뷰렉트 밖이지만 확대된 범위 안에 있는 변신 폰만 추가 Draw().
// 주의 : 변신 중인 폰이 없으면 즉시 반환. 이미 뷰렉트 안에서 그려진 폰은 건너뜀(중복 방지).

using HarmonyLib;
using ShapeshifterFramework.Comps;
using ShapeshifterFramework.Utilities;
using UnityEngine;
using Verse;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch(typeof(DynamicDrawManager), nameof(DynamicDrawManager.DrawDynamicThings))]
    [HarmonyPriority(Priority.Last)]
    public static class Patch_DynamicDrawManager_DrawDynamicThings
    {
        [HarmonyPostfix]
        static void Postfix(Map ___map)
        {
            if (___map == null) return;

            // 활성 변신 폰이 없으면 즉시 반환 (제로 오버헤드)
            if (!ShapeshiftRegistry.HasAny()) return;

            CellRect viewRect = Find.CameraDriver.CurrentViewRect;
            viewRect.ClipInsideMap(___map);
            CellRect normalRect = viewRect.ExpandedBy(1);

            // 변신 폰 순회 — 보통 0~수 명이므로 성능 영향 무시 가능
            foreach (var entry in ShapeshiftRegistry.ActiveDict)
            {
                Pawn pawn = entry.Key;
                if (pawn == null || !pawn.Spawned || pawn.Map != ___map) continue;

                CompShapeshifter comp = entry.Value;
                ShapeshiftFormDef form = comp.currentForm;
                if (form == null) continue;

                float sBody = form.bodyDrawScale.HasValue ? Mathf.Max(0.01f, form.bodyDrawScale.Value) : 1f;
                if (sBody <= 1.5f) continue; // 1.5배 이하는 1셀 확장으로 충분

                IntVec3 pos = pawn.Position;

                // 이미 바닐라에서 그려졌으면 건너뜀
                if (normalRect.Contains(pos)) continue;

                // 확대된 범위 계산: bodyDrawScale에 비례 (최소 2, 최대 5)
                int extraMargin = Mathf.Clamp(Mathf.CeilToInt(sBody), 2, 5);
                CellRect extRect = viewRect.ExpandedBy(extraMargin);

                if (extRect.Contains(pos))
                {
                    // 안개(fog) 체크
                    if (___map.fogGrid != null && ___map.fogGrid.IsFogged(pos)) continue;

                    // Thing.DrawNowOrLater 대신 DynamicDraw 호출
                    ((Thing)pawn).DynamicDrawPhaseAt(DrawPhase.Draw, pawn.DrawPos, false);
                }
            }
        }
    }
}
