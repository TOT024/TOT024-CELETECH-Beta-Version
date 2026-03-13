using System;
using UnityEngine;
using Verse;
using RimWorld;

namespace TOT_DLL_test
{
    [StaticConstructorOnStartup]
    public class Command_FunnelPanel : Command
    {
        public Texture2D customBackground;
        public string funnelName = "FunnelSyetem";
        public int containedCount;
        public int maxCount;

        public Action actionRelease;
        public string disableReasonRelease;
        public Texture2D iconRelease;

        public Action actionRecall;
        public string disableReasonRecall;
        public Texture2D iconRecall;

        public Action actionToggleMode;
        public string disableReasonToggleMode;
        public Texture2D iconMode;
        public bool isAssaultMode;

        public Action actionTarget;
        public string disableReasonTarget;
        public Texture2D iconTarget;

        public Action actionCancelTarget;
        public string disableReasonCancelTarget;
        public Texture2D iconCancelTarget;

        public Action actionAutoDraft;
        public bool isAutoDraftOn;
        public Texture2D iconAutoDraft;
        public Texture2D iconAutoDraftOff;
        private static readonly Texture2D FullBatteryBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.35f, 0.35f, 0.35f));
        private static readonly Texture2D EmptyBatteryBarTex = SolidColorMaterials.NewSolidColorTexture(Color.clear);

        public override float GetWidth(float maxWidth) => 375f;

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
            if (customBackground != null)
            {
                GUI.DrawTexture(rect, customBackground);
            }
            else
            {
                Widgets.DrawWindowBackground(rect);
            }
            Rect rectInfo = new Rect(rect.x, rect.y, 150f, 75f);
            Rect rectDeploy = new Rect(rect.x + 150f, rect.y, 75f, 75f);
            Rect rectTactics = new Rect(rect.x + 225f, rect.y, 75f, 75f);
            Rect rectDecor = new Rect(rect.x + 300f, rect.y, 75f, 75f);

            bool interacted = false;
            Rect rectInfoInner = rectInfo;
            Rect nameRect = new Rect(rectInfoInner.x, rectInfoInner.y, rectInfoInner.width, 40f);
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperCenter;
            Widgets.Label(nameRect, funnelName);
            Text.Anchor = TextAnchor.UpperLeft;
            Rect barRect = new Rect(rectInfo.x + 15f, rectInfo.y + 40f, rectInfo.width - 30f, 20f);
            float fillPercent = maxCount > 0 ? (float)containedCount / maxCount : 0f;
            barRect.yMin = rectInfoInner.y + rectInfoInner.height / 2f;
            Widgets.FillableBar(barRect, fillPercent, FullBatteryBarTex, EmptyBatteryBarTex, true);
            if (maxCount > 1)
            {
                Color originalColor = GUI.color;
                GUI.color = new Color(0.1f, 0.1f, 0.1f);
                for (int i = 1; i < maxCount; i++)
                {
                    float xPos = barRect.x + (barRect.width / (float)maxCount) * i;
                    Widgets.DrawLineVertical(xPos, barRect.y, barRect.height);
                }
                GUI.color = originalColor;
            }
            Widgets.FillableBar(barRect, fillPercent, SolidColorMaterials.NewSolidColorTexture(new Color(0.2f, 0.6f, 1f)), BaseContent.BlackTex, false);
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(barRect, $"{containedCount}/{maxCount}");
            Rect rectReleaseTop = new Rect(rectDeploy.x, rectDeploy.y, 75f, 37.5f);
            Rect rectRecallBot = new Rect(rectDeploy.x, rectDeploy.y + 37.5f, 75f, 37.5f);
            interacted |= DrawFullImageButton(rectReleaseTop, iconRelease, actionRelease, disableReasonRelease, "TOT_CommandReleaseAll".Translate());
            interacted |= DrawFullImageButton(rectRecallBot, iconRecall, actionRecall, disableReasonRecall, "TOT_CommandRecallAll".Translate());
            Rect rectGridTopLeft = new Rect(rectTactics.x, rectTactics.y, 37.5f, 37.5f);
            Rect rectGridTopRight = new Rect(rectTactics.x + 37.5f, rectTactics.y, 37.5f, 37.5f);
            Rect rectGridBotLeft = new Rect(rectTactics.x, rectTactics.y + 37.5f, 37.5f, 37.5f);
            Rect rectGridBotRight = new Rect(rectTactics.x + 37.5f, rectTactics.y + 37.5f, 37.5f, 37.5f);

            string modeLabel = isAssaultMode ? "TOT_ModeAttack".Translate() : "TOT_ModeGuard".Translate();
            string modeTooltip = "TOT_CommandToggleModeDesc".Translate() + $"\n当前: {modeLabel}";

            Texture2D currentAutoDraftIcon = isAutoDraftOn ? iconAutoDraft : iconAutoDraftOff;
            interacted |= DrawIconButton(rectGridTopLeft, currentAutoDraftIcon, actionAutoDraft, null, "TOT_CommandAutoDraftDesc".Translate());
            interacted |= DrawIconButton(rectGridTopRight, iconMode, actionToggleMode, disableReasonToggleMode, modeTooltip);
            interacted |= DrawIconButton(rectGridBotLeft, iconTarget, actionTarget, disableReasonTarget, "TOT_CommandTargetAttackDesc".Translate());
            interacted |= DrawIconButton(rectGridBotRight, iconCancelTarget, actionCancelTarget, disableReasonCancelTarget, "TOT_CommandCancelAttackDesc".Translate());

            Text.Anchor = TextAnchor.UpperLeft;

            if (interacted) return new GizmoResult(GizmoState.Interacted, Event.current);
            if (Mouse.IsOver(rect)) return new GizmoResult(GizmoState.Mouseover);
            return new GizmoResult(GizmoState.Clear);
        }
        private bool DrawFullImageButton(Rect rect, Texture2D icon, Action action, string disableReason, string tooltip)
        {
            bool disabled = !string.IsNullOrEmpty(disableReason);
            if (disabled) GUI.color = Color.gray;

            Widgets.DrawHighlightIfMouseover(rect);

            if (icon != null)
            {
                GUI.DrawTexture(rect.ContractedBy(2f), icon);
            }

            GUI.color = Color.white;

            TooltipHandler.TipRegion(rect, disabled ? disableReason : tooltip);
            if (!disabled && Widgets.ButtonInvisible(rect))
            {
                action?.Invoke();
                return true;
            }
            return false;
        }

        private bool DrawIconButton(Rect rect, Texture2D icon, Action action, string disableReason, string tooltip)
        {
            bool disabled = !string.IsNullOrEmpty(disableReason);
            if (disabled) GUI.color = Color.gray;

            Widgets.DrawHighlightIfMouseover(rect);
            if (icon != null) GUI.DrawTexture(rect, icon);

            GUI.color = Color.white;

            TooltipHandler.TipRegion(rect, disabled ? disableReason : tooltip);
            if (!disabled && Widgets.ButtonInvisible(rect))
            {
                action?.Invoke();
                return true;
            }
            return false;
        }
    }
}