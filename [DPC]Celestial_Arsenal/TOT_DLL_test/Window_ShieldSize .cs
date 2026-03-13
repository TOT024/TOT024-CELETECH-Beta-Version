using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace TOT_DLL_test
{
    public class Window_ShieldSize : Window
    {
        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(this.windowWidth, this.windowHeight);
            }
        }
        public Window_ShieldSize(string title, string description, Building_FRShield building) : base(null)
        {
            this.title = title;
            this.description = description;
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
            this.closeOnAccept = false;
            this.closeOnCancel = false;
            this.soundAppear = SoundDefOf.CommsWindow_Open;
            this.soundClose = SoundDefOf.CommsWindow_Close;
            this.windowWidth = 320f;
            this.windowHeight = 190f;
            this.building_FRShield = building;
        }
        public override void DoWindowContents(Rect inRect)
        {
            Rect rect = inRect.ContractedBy(10f);
            Listing_Standard listing_Standard = new Listing_Standard();
            listing_Standard.Begin(rect);
            Text.Font = GameFont.Medium;
            listing_Standard.Label((TaggedString)this.title, -1f, null);
            Text.Font = GameFont.Small;
            listing_Standard.Label("CMC.ChangeSizeDescription".Translate(this.building_FRShield.compFullProjectileInterceptor.radius.ToString("F1")), -1f, null);
            this.building_FRShield.compFullProjectileInterceptor.radius = (float)((int)listing_Standard.Slider(this.building_FRShield.compFullProjectileInterceptor.radius, 20f, 60f));
            listing_Standard.End();
            for (int i = 0; i < this.options.Length; i++)
            {
                Rect rect2 = new Rect(rect.x, inRect.height - 25f - (float)(this.options.Length + 1) * Text.LineHeight + (float)(i + 2) * Text.LineHeight, rect.width, Text.LineHeight);
                Widgets.DrawHighlightIfMouseover(rect2);
                Widgets.Label(rect2, this.options[i]);
                bool flag = Widgets.ButtonInvisible(rect2, true);
                bool flag2 = flag;
                if (flag2)
                {
                    this.selectedOption = i;
                    bool flag3 = this.selectedOption == 0;
                    bool flag4 = flag3;
                    if (flag4)
                    {
                        this.Close(true);
                    }
                }
            }
        }
        private float windowWidth = 0f;
        private float windowHeight = 0f;
        private string[] options = new string[]
        {
            "CloseButton".Translate()
        };
        private int selectedOption = -1;
        private readonly string title;
        private readonly string description;
        private Building_FRShield building_FRShield;
        private Vector2 scrollPosition = Vector2.zero;
    }
}
