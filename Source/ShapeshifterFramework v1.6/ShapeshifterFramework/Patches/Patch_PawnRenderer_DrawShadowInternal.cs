// ShapeshifterFramework | Patches | Patch_PawnRenderer_DrawShadowInternal.cs
// 목적 : 폰의 바닥 그림자(Shadow) 크기와 오프셋을 변신 폼의 거대한 덩치에 맞게 보정.
// 용도 : Graphic_Shadow를 커스텀 데이터로 생성해 캐싱하여 렌더링함. 특히, 육지 타일이지만 물속 전용 텍스처(SwimmingReplacement)가 적용 중일 때는 바닐라 수영처럼 그림자 자체를 스킵(return false)하는 디테일이 포함됨.

using HarmonyLib;
using ShapeshifterFramework.Utilities;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ShapeshifterFramework.Patches
{
    [HarmonyPatch(typeof(PawnRenderer), "DrawShadowInternal")]
    internal static class Patch_PawnRenderer_DrawShadowInternal
    {
        private struct ShadowKey
        {
            public Vector3 v;
            public Vector3 o;

            public override int GetHashCode()
            {
                unchecked
                {
                    int h = 17;
                    h = h * 31 + v.GetHashCode();
                    h = h * 31 + o.GetHashCode();
                    return h;
                }
            }

            public override bool Equals(object obj)
            {
                if (!(obj is ShadowKey)) return false;
                ShadowKey other = (ShadowKey)obj;
                return v == other.v && o == other.o;
            }
        }

        private static readonly Dictionary<ShadowKey, Graphic_Shadow> FormShadowGraphicByKey =
            new Dictionary<ShadowKey, Graphic_Shadow>(64);

        private static readonly Dictionary<ShadowData, Graphic_Shadow> SpecialShadowGraphicByRef =
            new Dictionary<ShadowData, Graphic_Shadow>(16);

        static bool Prefix(PawnRenderer __instance, Vector3 drawLoc)
        {
            Pawn pawn = ShapeshiftReflectionCache.GetPawn(__instance);
            if (pawn == null) return true;

            // 비행은 바닐라 전용 처리(FlightShadowMaterial 등) 쓰므로 건드리지 않음
            if (pawn.Flying) return true;

            // 바닐라: 수영 관련 상태면 그림자 자체를 안 그림
            if (pawn.Swimming || pawn.DrawNonHumanlikeSwimmingGraphic) return true;

            // [추가] 변신 폼에서 "수영 텍스처"를 쓰는 상태면 그림자도 스킵(바닐라 동물 수영 룩과 일치)
            // - pawn.Swimming 플래그가 false여도, 우리 로직(물 타일 기반 텍스처 교체)로 수영 룩이 될 수 있기 때문
            ShapeshiftFormDef form;
            if (ShapeshiftPartControlUtility.ShouldRun(pawn, out form) && form != null)
            {
                if (pawn.Spawned && pawn.Map != null)
                {
                    TerrainDef terr = pawn.Position.GetTerrain(pawn.Map);
                    if (terr != null && terr.IsWater)
                    {
                        string swimPath;
                        if (ShapeshiftPartControlUtility.TryGetBodySwimmingReplacementPath(pawn, form, out swimPath))
                        {
                            // 수영 텍스처 조건을 충족하면: 원본도, 오버라이드도 그리지 않음
                            return false;
                        }
                    }
                }
            }

            // 변신 아니거나, 수영 텍스처를 쓰는 상태가 아니면: 기존 오버라이드(육지용)만 적용
            if (form == null)
                return true;

            Vector3 vol, off;
            if (!ShapeshiftPartControlUtility.TryGetBodyShadowOverride(pawn, form, out vol, out off))
                return true;

            ShadowKey key = new ShadowKey { v = vol, o = off };
            Graphic_Shadow formShadow;
            if (!FormShadowGraphicByKey.TryGetValue(key, out formShadow) || formShadow == null)
            {
                ShadowData sd = new ShadowData { volume = vol, offset = off };
                formShadow = new Graphic_Shadow(sd);
                FormShadowGraphicByKey[key] = formShadow;
            }

            formShadow.Draw(drawLoc, Rot4.North, pawn);

            // 원본이 그리던 BodyGraphic.ShadowGraphic은 스킵(중복 방지)
            return false;
        }
    }
}
