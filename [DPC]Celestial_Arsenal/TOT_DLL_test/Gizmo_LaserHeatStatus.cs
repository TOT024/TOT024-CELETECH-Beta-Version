using RimWorld;
using UnityEngine;
using Verse;

namespace TOT_DLL_test
{
    [StaticConstructorOnStartup]
    public class Gizmo_LaserHeatStatus : Gizmo
    {
        public CompLaserHeat comp;
        public Thing weapon;
        private static readonly Texture2D WhiteTex = BaseContent.WhiteTex;
        private static readonly Texture2D SilhouetteMask =
            ContentFinder<Texture2D>.Get("UI/LaserHeat_Taotie", true);

        public Gizmo_LaserHeatStatus()
        {
            this.Order = -95f;
        }

        public override float GetWidth(float maxWidth)
        {
            return 140f;
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            Rect outRect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
            Rect inner = outRect.ContractedBy(6f);
            Widgets.DrawWindowBackground(outRect);
            float pct = 0f;
            if (comp != null)
            {
                pct = Mathf.Clamp01(comp.HeatPct);
            }
            Color heatColor = Color.Lerp(new Color(0.59f, 0.99f, 0.45f), new Color(0.9f, 0.15f, 0.1f), pct);
            Rect barZone = inner;
            barZone.xMin = inner.x + inner.width * 0.07f;
            barZone.xMax = inner.x + inner.width * 0.93f;
            barZone.yMin = inner.y + inner.height * 0.18f;
            barZone.yMax = inner.y + inner.height * 0.92f;

            Color oldColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.25f);
            GUI.DrawTexture(barZone, BaseContent.WhiteTex);
            Rect fillRect = barZone;
            fillRect.width = barZone.width * pct;
            GUI.color = heatColor;
            GUI.DrawTexture(fillRect, BaseContent.WhiteTex);
            GUI.color = Color.white;
            GUI.DrawTexture(outRect, SilhouetteMask, ScaleMode.StretchToFill, true);
            GUI.color = oldColor;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;

            return new GizmoResult(GizmoState.Clear);
        }
    }
}