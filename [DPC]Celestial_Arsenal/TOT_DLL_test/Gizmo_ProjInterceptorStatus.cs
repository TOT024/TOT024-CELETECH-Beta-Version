using System;
using UnityEngine;
using Verse;
using RimWorld;

namespace TOT_DLL_test
{
    [StaticConstructorOnStartup]
    public class Gizmo_PIStatus : Gizmo
    {
        public Gizmo_PIStatus()
        {
            this.Order = -100f;
        }
        public override float GetWidth(float maxWidth)
        {
            return 140f;
        }
        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms p)
        {
            Rect rect = new Rect(topLeft.x, topLeft.y, this.GetWidth(maxWidth), 75f);
            Rect rect2 = rect.ContractedBy(6f);
            Widgets.DrawWindowBackground(rect);
            Rect rect3 = rect2;
            rect3.height = rect.height / 2f;
            Text.Font = GameFont.Tiny;
            Widgets.Label(rect3, this.shield.parent.LabelCap);
            Rect rect4 = rect2;
            rect4.yMin = rect2.y + rect2.height / 2f;
            float fillPercent = this.shield.energy / Mathf.Max(1f, this.shield.EnergyMax);
            Widgets.FillableBar(rect4, fillPercent, Gizmo_PIStatus.FullShieldBarTex, Gizmo_PIStatus.EmptyShieldBarTex, false);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect4, (this.shield.energy).ToString("F0") + " / " + (this.shield.EnergyMax).ToString("F0"));
            Text.Anchor = TextAnchor.UpperLeft;
            return new GizmoResult(GizmoState.Clear);
        }

        public CompFullProjectileInterceptor shield;
        private static readonly Texture2D FullShieldBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.2f, 0.2f, 0.24f));
        private static readonly Texture2D EmptyShieldBarTex = SolidColorMaterials.NewSolidColorTexture(Color.clear);
    }
}
