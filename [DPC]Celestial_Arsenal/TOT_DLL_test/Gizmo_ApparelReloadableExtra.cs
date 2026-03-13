using RimWorld;
using UnityEngine;
using Verse;

namespace TOT_DLL_test
{
    [StaticConstructorOnStartup]
    public class Gizmo_ApparelReloadableExtra : Gizmo
    {
        public Gizmo_ApparelReloadableExtra(CompApparelReloadable carrier)
        {
            ApparelHediffAdder = carrier;
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            Rect rect = new Rect(topLeft.x, topLeft.y, this.GetWidth(maxWidth), 75f);
            Rect rect2 = rect.ContractedBy(6f);
            Widgets.DrawWindowBackground(rect);
            Rect rect3 = rect2;
            rect3.height = rect.height / 2f;
            Text.Font = GameFont.Tiny;
            Widgets.Label(rect3, ApparelHediffAdder.parent.def.label.Translate().Resolve());
            Rect rect4 = rect2;
            rect4.yMin = rect2.y + rect2.height / 2f;
            float fillPercent = (float)(ApparelHediffAdder.RemainingCharges) / (float)(ApparelHediffAdder.MaxCharges);
            {
                Widgets.FillableBar(rect4, fillPercent, FullBatteryBarTex, EmptyBatteryBarTex, true);
            }
            int maxCharges = ApparelHediffAdder.MaxCharges;
            if (maxCharges > 1)
            {
                Color originalColor = GUI.color;
                GUI.color = new Color(0.1f, 0.1f, 0.1f);

                for (int i = 1; i < maxCharges; i++)
                {
                    float xPos = rect4.x + (rect4.width / (float)maxCharges) * i;
                    Widgets.DrawLineVertical(xPos, rect4.y, rect4.height);
                }
                GUI.color = originalColor;
            }
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect4, ApparelHediffAdder.RemainingCharges.ToString() + "/" + ApparelHediffAdder.MaxCharges.ToString());
            return new GizmoResult(GizmoState.Clear);
        }

        public override float GetWidth(float maxWidth)
        {
            return 140f;
        }

        public CompApparelReloadable ApparelHediffAdder;
        private static readonly Texture2D FullBatteryBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.35f, 0.35f, 0.35f));
        private static readonly Texture2D EmptyBatteryBarTex = SolidColorMaterials.NewSolidColorTexture(Color.clear);
    }
}