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

        /// <summary>맵 전환/게임 로드 시 그림자 캐시 정리.</summary>
        public static void ClearCache() { FormShadowGraphicByKey.Clear(); }

        static bool Prefix(PawnRenderer __instance, Vector3 drawLoc)
        {
            Pawn pawn = ShapeshiftReflectionCache.GetPawn(__instance);
            if (pawn == null) return true;

            // 비행 시 바닐라 처리
            if (pawn.Flying) return true;

            // 수영 시 그림자 스킵
            if (pawn.Swimming || pawn.DrawNonHumanlikeSwimmingGraphic) return true;

            // 폼 수영 텍스처 시 그림자 스킵
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
                            // 수영 텍스처 충족 시 그림자 생략
                            return false;
                        }
                    }
                }
            }

            // 육지용 그림자 오버라이드
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

            // 원본 그림자 스킵
            return false;
        }
    }
}
