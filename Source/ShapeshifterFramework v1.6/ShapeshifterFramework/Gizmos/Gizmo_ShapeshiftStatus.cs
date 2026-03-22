// ShapeshifterFramework | Gizmos | Gizmo_ShapeshiftStatus.cs
// 목적 : 변신 상태 프로그레스 바 기즈모 — 남은 시간/해제 버튼을 하단 기즈모 영역에 표시.
// 용도 : Gizmo_EnergyShieldStatus / Gizmo_Slider 스타일의 바 기즈모.
//        시간제 변신: 남은 시간 프로그레스 바 표시. 영구 변신: 바 꽉 참 + "무제한" 텍스트.
//        우측 상단에 변신 해제 버튼 배치.

using ShapeshifterFramework.Hediffs;
using ShapeshifterFramework.Utilities;
using UnityEngine;
using Verse;

namespace ShapeshifterFramework.Gizmos
{
    [StaticConstructorOnStartup]
    public class Gizmo_ShapeshiftStatus : Gizmo
    {
        /// <summary>대상 ShapeshiftCore 컴포넌트.</summary>
        public HediffComp_ShapeshiftCore core;

        // 바 텍스처 (static 캐시)
        private static readonly Texture2D BarFilledTex =
            SolidColorMaterials.NewSolidColorTexture(new Color(0.28f, 0.50f, 0.55f));

        private static readonly Texture2D BarFilledPermanentTex =
            SolidColorMaterials.NewSolidColorTexture(new Color(0.35f, 0.55f, 0.40f));

        private static readonly Texture2D BarEmptyTex =
            SolidColorMaterials.NewSolidColorTexture(new Color(0.03f, 0.035f, 0.05f));

        // 해제 아이콘 크기
        private const float RevertBtnSize = 24f;

        public Gizmo_ShapeshiftStatus()
        {
            Order = -99f;
        }

        public override float GetWidth(float maxWidth)
        {
            return 180f;
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            Rect outerRect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
            Rect innerRect = outerRect.ContractedBy(6f);
            Widgets.DrawWindowBackground(outerRect);

            if (core == null || core.currentForm == null)
                return new GizmoResult(GizmoState.Clear);

            var form = core.currentForm;

            // ── 상단: 폼 이름 + 해제 버튼 ──
            Rect headerRect = innerRect;
            headerRect.height = Text.LineHeightOf(GameFont.Small);

            // 해제 버튼 (우측)
            bool showRevert = core.ResolvedCanRevertVoluntarily;
            if (showRevert)
            {
                Rect revertBtnRect = new Rect(
                    headerRect.xMax - RevertBtnSize,
                    headerRect.y,
                    RevertBtnSize, RevertBtnSize);

                var revertIcon = ShapeshiftTextureUtility.GetRevertIcon(form);
                if (revertIcon != null)
                    GUI.DrawTexture(revertBtnRect, revertIcon);

                if (Widgets.ButtonInvisible(revertBtnRect))
                    core.RemoveForm();

                if (Mouse.IsOver(revertBtnRect))
                {
                    Widgets.DrawHighlight(revertBtnRect);
                    TooltipHandler.TipRegion(revertBtnRect, "SSF_Command_RevertDesc".Translate());
                }

                headerRect.xMax -= RevertBtnSize + 2f;
            }

            // 폼 이름
            Text.Font = GameFont.Small;
            string formLabel = form.LabelCap ?? form.defName;
            Widgets.Label(headerRect, formLabel.Truncate(headerRect.width));

            // ── 하단: 프로그레스 바 ──
            Rect barRect = innerRect;
            barRect.yMin = headerRect.yMax + 4f;

            var resolvedDuration = core.ResolvedDurationTicks;
            bool isPermanent = !resolvedDuration.HasValue || resolvedDuration.Value <= 0;

            float fillPct;
            string barLabel;

            if (isPermanent)
            {
                fillPct = 1f;
                barLabel = "SSF_Inspect_Permanent_Short".Translate();
            }
            else
            {
                int remain = core.RemainingShapeshiftTicks;
                int total = resolvedDuration.Value;
                fillPct = Mathf.Clamp01((float)remain / Mathf.Max(1f, total));
                barLabel = GenDate.ToStringTicksToPeriod(remain, allowSeconds: false, shortForm: true);
            }

            Texture2D fillTex = isPermanent ? BarFilledPermanentTex : BarFilledTex;
            Widgets.FillableBar(barRect, fillPct, fillTex, BarEmptyTex, doBorder: true);

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(barRect, barLabel);
            Text.Anchor = TextAnchor.UpperLeft;

            // ── 툴팁 ──
            if (Mouse.IsOver(outerRect))
            {
                Widgets.DrawHighlight(outerRect);
                string tip = GetTooltipText(form, isPermanent, resolvedDuration);
                TooltipHandler.TipRegion(outerRect, tip);
            }

            return new GizmoResult(GizmoState.Clear);
        }

        /// <summary>툴팁 텍스트 생성.</summary>
        private string GetTooltipText(ShapeshiftFormDef form, bool isPermanent, int? resolvedDuration)
        {
            string formName = form.LabelCap ?? form.defName;
            if (isPermanent)
                return "SSF_Gizmo_Tooltip_Permanent".Translate(formName);

            int remain = core.RemainingShapeshiftTicks;
            string remainStr = GenDate.ToStringTicksToPeriod(remain, allowSeconds: false, shortForm: false);
            string totalStr = GenDate.ToStringTicksToPeriod(resolvedDuration.Value, allowSeconds: false, shortForm: false);
            return "SSF_Gizmo_Tooltip_Timed".Translate(formName, remainStr, totalStr);
        }
    }
}
