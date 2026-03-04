// ShapeshifterFramework | Utilities | ShapeshiftPartControlUtility.cs
// 목적 : 변신 폼의 각 렌더 노드 파츠(머리, 몸통, 수염 등)에 대한 렌더링 제어(Default, Hidden, Replace)를 수행하는 헬퍼.
// 용도 : 폰의 성별(Gender) 설정과 폼의 기본 설정을 병합(Merge)하여 최우선 규칙을 판별하며, 수영 타일(Water) 진입 시 전용 텍스처와 셰이더(Shader)로 교체하는 로직을 담당함.
// 주의 : 렌더 핫패스(Hot-path)에서 실행되므로 LINQ를 배제하고, 동적 셰이더 탐색 결과를 Dictionary에 캐싱하여 성능 저하를 막음.

using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ShapeshifterFramework.Utilities
{
    internal static class ShapeshiftPartControlUtility
    {
        // shaderTypeDefName -> Shader 캐시(없으면 null 캐시)
        private static readonly Dictionary<string, Shader> ShaderByTypeDefName = new Dictionary<string, Shader>(64);

        internal static bool ShouldRun(Pawn pawn, out ShapeshiftFormDef form)
        {
            form = null;
            if (pawn == null) return false;

            var comp = ShapeshiftUtility.GetShapeShiftComp(pawn);
            if (comp == null || !comp.isTransformed || comp.currentForm == null) return false;

            form = comp.currentForm;
            return true;
        }

        private static PartOverrideOption GenderOpt(PartOverrideOption baseOpt, Gender g)
        {
            if (baseOpt == null) return null;
            if (g == Gender.Female) return baseOpt.female;
            if (g == Gender.Male) return baseOpt.male;
            return null;
        }

        private static Shader ResolveShaderFromTypeDefName(string shaderTypeDefName)
        {
            if (string.IsNullOrEmpty(shaderTypeDefName)) return null;

            Shader sh;
            if (ShaderByTypeDefName.TryGetValue(shaderTypeDefName, out sh))
                return sh;

            // ShaderTypeDef는 Core Def(ShaderTypeDefs)에서 로드됨
            var def = DefDatabase<ShaderTypeDef>.GetNamedSilentFail(shaderTypeDefName);
            sh = def != null ? def.Shader : null;

            ShaderByTypeDefName[shaderTypeDefName] = sh; // null도 캐시(재조회 방지)
            return sh;
        }

        /// <summary>
        /// 성별 옵션과 base를 merge해서 유효 값을 뽑는다.
        /// - mode: 성별 옵션이 있으면 그 mode 우선
        /// - 그 외: 성별 옵션 필드가 null/empty면 base 값을 사용
        /// </summary>
        private static void ResolveEffective(
            PartOverrideOption baseOpt, PartOverrideOption gOpt,
            out PartControlMode mode,
            out string replacementTexPath,
            out string swimmingReplacementTexPath,
            out Color? color,
            out Color? swimmingColor,
            out string shaderTypeDefName,
            out string swimmingShaderTypeDefName)
        {
            mode = (gOpt != null) ? gOpt.mode : baseOpt.mode;

            replacementTexPath =
                (gOpt != null && !string.IsNullOrEmpty(gOpt.replacementTexPath)) ? gOpt.replacementTexPath : baseOpt.replacementTexPath;

            swimmingReplacementTexPath =
                (gOpt != null && !string.IsNullOrEmpty(gOpt.swimmingReplacementTexPath)) ? gOpt.swimmingReplacementTexPath : baseOpt.swimmingReplacementTexPath;

            color =
                (gOpt != null && gOpt.color.HasValue) ? gOpt.color : baseOpt.color;

            swimmingColor =
                (gOpt != null && gOpt.swimmingColor.HasValue) ? gOpt.swimmingColor : baseOpt.swimmingColor;

            shaderTypeDefName =
                (gOpt != null && !string.IsNullOrEmpty(gOpt.shaderTypeDefName)) ? gOpt.shaderTypeDefName : baseOpt.shaderTypeDefName;

            swimmingShaderTypeDefName =
                (gOpt != null && !string.IsNullOrEmpty(gOpt.swimmingShaderTypeDefName)) ? gOpt.swimmingShaderTypeDefName : baseOpt.swimmingShaderTypeDefName;
        }

        internal static bool IsHeadHiddenForGender(Pawn pawn, ShapeshiftFormDef form)
        {
            if (pawn == null || form == null || form.head == null) return false;

            PartOverrideOption baseOpt = form.head;
            PartOverrideOption gOpt = GenderOpt(baseOpt, pawn.gender);

            PartControlMode mode;
            string rep, swim, shBase, shSwim;
            Color? col;
            Color? swimCol;
            ResolveEffective(baseOpt, gOpt, out mode, out rep, out swim, out col, out swimCol, out shBase, out shSwim);

            return mode == PartControlMode.Hidden;
        }

        /// <summary>
        /// 바디 수영 replacement 존재 여부(성별 우선 + base 폴백, merge 방식).
        /// - mode가 Replace가 아니면 false
        /// - swimmingReplacementTexPath가 비어있으면 false
        /// </summary>
        internal static bool TryGetBodySwimmingReplacementPath(Pawn pawn, ShapeshiftFormDef form, out string swimmingPath)
        {
            swimmingPath = null;
            if (pawn == null || form == null || form.body == null) return false;

            PartOverrideOption baseOpt = form.body;
            PartOverrideOption gOpt = GenderOpt(baseOpt, pawn.gender);

            PartControlMode mode;
            string rep, swim, shBase, shSwim;
            Color? col;
            Color? swimCol;
            ResolveEffective(baseOpt, gOpt, out mode, out rep, out swim, out col, out swimCol, out shBase, out shSwim);

            if (mode != PartControlMode.Replace) return false;
            if (string.IsNullOrEmpty(swim)) return false;

            swimmingPath = swim;
            return true;
        }

        internal static bool TryApplyPartControl(object node, Pawn pawn, ShapeshiftFormDef form, ref Graphic result)
        {
            PartOverrideOption baseOpt = null;
            Shader nodeShader = ShaderDatabase.Cutout;
            bool isBody = false;

            if (node is PawnRenderNode_Head) { baseOpt = form.head; nodeShader = ShaderDatabase.Cutout; }
            else if (node is PawnRenderNode_Hair) { baseOpt = form.hair; nodeShader = ShaderDatabase.CutoutHair; }
            else if (node is PawnRenderNode_Beard) { baseOpt = form.beard; nodeShader = ShaderDatabase.CutoutSkinOverlay; }
            else if (node is PawnRenderNode_Tattoo_Head) { baseOpt = form.tattooHead; nodeShader = ShaderDatabase.CutoutSkinOverlay; }
            else if (node is PawnRenderNode_Tattoo_Body) { baseOpt = form.tattooBody; nodeShader = ShaderDatabase.CutoutSkinOverlay; }
            else if (node is PawnRenderNode_Body) { baseOpt = form.body; nodeShader = ShaderDatabase.Cutout; isBody = true; }

            if (baseOpt == null) return false;

            PartOverrideOption gOpt = (pawn != null) ? GenderOpt(baseOpt, pawn.gender) : null;

            PartControlMode mode;
            string repPath, swimPath, shaderBaseName, shaderSwimName;
            Color? col;
            Color? swimCol;
            ResolveEffective(baseOpt, gOpt, out mode, out repPath, out swimPath, out col, out swimCol, out shaderBaseName, out shaderSwimName);

            if (mode == PartControlMode.Default)
                return false;

            if (mode == PartControlMode.Hidden)
            {
                result = null;
                return true;
            }

            if (mode == PartControlMode.Replace)
            {
                string path = repPath;
                bool usedSwimming = false;

                // 바디 + 물 타일 + swimmingReplacementTexPath 있으면 그걸 우선
                if (isBody && pawn != null && pawn.Spawned && pawn.Map != null && !string.IsNullOrEmpty(swimPath))
                {
                    var terr = pawn.Position.GetTerrain(pawn.Map);
                    if (terr != null && terr.IsWater)
                    {
                        path = swimPath;
                        usedSwimming = true;
                    }
                }

                if (string.IsNullOrEmpty(path)) return false;

                Color c1;
                if (usedSwimming && swimCol.HasValue)
                    c1 = swimCol.Value;
                else
                    c1 = col.HasValue ? col.Value : Color.white;

                // 셰이더 결정 규칙
                // - 육지: shaderTypeDefName(있으면) -> 노드 기본
                // - 수영(바디): swimmingShaderTypeDefName -> shaderTypeDefName -> Transparent(안전 폴백) -> 노드 기본
                Shader useShader;

                if (usedSwimming && isBody)
                {
                    useShader =
                        ResolveShaderFromTypeDefName(shaderSwimName) ??
                        ResolveShaderFromTypeDefName(shaderBaseName) ??
                        ShaderDatabase.Transparent;
                }
                else
                {
                    useShader =
                        ResolveShaderFromTypeDefName(shaderBaseName) ??
                        nodeShader;
                }

                Graphic newG = GraphicDatabase.Get<Graphic_Multi>(path, useShader, Vector2.one, c1);
                if (newG != null) result = newG;

                return true;
            }

            return false;
        }

        /// <summary>
        /// 바디 shadow 오버라이드 값 추출(입력하면 대체, 미입력이면 바닐라 유지)
        /// - 성별 옵션이 있으면: 해당 필드가 null이면 base 값을 유지(merge)
        /// - volume/offset 중 하나라도 지정되면 true
        /// </summary>
        internal static bool TryGetBodyShadowOverride(Pawn pawn, ShapeshiftFormDef form, out Vector3 volume, out Vector3 offset)
        {
            volume = Vector3.one;
            offset = Vector3.zero;

            if (pawn == null || form == null || form.body == null) return false;

            PartOverrideOption baseOpt = form.body;
            PartOverrideOption gOpt = GenderOpt(baseOpt, pawn.gender);

            // merge 규칙: 성별값이 있으면 그걸, 없으면 base
            Vector3? v = (gOpt != null && gOpt.shadowVolume.HasValue) ? gOpt.shadowVolume : baseOpt.shadowVolume;
            Vector3? o = (gOpt != null && gOpt.shadowOffset.HasValue) ? gOpt.shadowOffset : baseOpt.shadowOffset;

            if (!v.HasValue && !o.HasValue) return false;

            volume = v ?? baseOpt.shadowVolume ?? Vector3.one;
            offset = o ?? baseOpt.shadowOffset ?? Vector3.zero;
            return true;
        }
    }
}
