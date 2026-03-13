using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TOT_DLL_test
{
    [StaticConstructorOnStartup]
    public class Gizmo_WeaponHolderUI : Gizmo
    {
        public CompApparelWeaponHolder comp;
        private static readonly Texture2D CustomBgTex = ContentFinder<Texture2D>.Get("UI/2X1BPBackground", false);
        private static readonly Texture2D DefaultWeaponSilhouetteTex = ContentFinder<Texture2D>.Get("UI/CMC_RL_UI", true);
        private Texture2D WeaponSilhouetteTex;
        private static readonly Texture2D AutoSwapOnTex = ContentFinder<Texture2D>.Get("UI/UI_ATKDraftOn_Funnel", true);
        private static readonly Texture2D AutoSwapOffTex = ContentFinder<Texture2D>.Get("UI/UI_ATKDraftOff_Funnel", true);
        private static readonly Texture2D EmptyBgTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.1f, 0.1f, 0.1f, 0.5f));
        private static readonly Texture2D BlueProgressTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.2f, 0.6f, 0.9f, 0.6f));
        private const float Width = 150f;

        public Gizmo_WeaponHolderUI(CompApparelWeaponHolder comp)
        {
            this.comp = comp;
            this.Order = -100f;
            WeaponSilhouetteTex = ContentFinder<Texture2D>.Get(comp.Props.TextWeaponSilhouetteTexPath, false);
            if (WeaponSilhouetteTex == null)
            {
                WeaponSilhouetteTex = DefaultWeaponSilhouetteTex;
            }
        }

        public override float GetWidth(float maxWidth)
        {
            return Width;
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            Rect fullRect = new Rect(topLeft.x, topLeft.y, Width, Height);
            if (CustomBgTex != null)
            {
                GUI.DrawTexture(fullRect, CustomBgTex);
            }
            else
            {
                Widgets.DrawWindowBackground(fullRect);
            }

            Rect leftPanel = new Rect(fullRect.x, fullRect.y, Width * 0.75f, Height); // 112.5 x 75
            Rect autoSwapRect = new Rect(fullRect.x + Width * 0.75f, fullRect.y, Width * 0.25f, Height / 2f); // 37.5 x 37.5
            Rect manualSwapRect = new Rect(fullRect.x + Width * 0.75f, fullRect.y + Height / 2f, Width * 0.25f, Height / 2f); // 37.5 x 37.5

            bool hasWeapon = comp.AnyWeaponInBelt;
            ThingWithComps equippedWeapon = comp.Wearer?.equipment?.Primary;

            if (hasWeapon)
            {
                ThingWithComps weapon = comp.GetWeaponContained();
                var ammoComp = weapon.TryGetComp<CompWeaponAmmo>();
                GUI.color = new Color(1f, 1f, 1f, 0.75f);
                GUI.DrawTexture(leftPanel, WeaponSilhouetteTex);
                GUI.color = Color.white;
                Rect weaponNameRect = new Rect(leftPanel.x + 5, leftPanel.y + 3, leftPanel.width - 10, 20);
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.UpperLeft;
                Text.WordWrap = false;
                string weaponLabel = weapon.LabelCap; 
                Widgets.Label(weaponNameRect, weaponLabel.Truncate(weaponNameRect.width));
                Text.WordWrap = true;
                TooltipHandler.TipRegion(weaponNameRect, weaponLabel); 
                // ----------------------------------------------------

                if (ammoComp != null)
                {
                    int backpackAmmo = ammoComp.ConnectedBackpack != null ? ammoComp.ConnectedBackpack.Charges : 0;
                    int reloadTicks = comp.ReloadTicksLeft;
                    string ammoText;
                    Rect bottomRect = new Rect(leftPanel.x + 5, leftPanel.y + leftPanel.height - 20, leftPanel.width - 10, 16);

                    if (reloadTicks > 0)
                    {
                        float seconds = reloadTicks / 60f;
                        ammoText = $"{ammoComp.currentMagAmmo} " + "CMC_Reloading".Translate(seconds.ToString("F1")) + $" / {backpackAmmo}";

                        float pct = 1f - ((float)reloadTicks / ammoComp.Props.reloadTicks);
                        Widgets.FillableBar(bottomRect, pct, BlueProgressTex);
                    }
                    else
                    {
                        ammoText = $"{ammoComp.currentMagAmmo} / {backpackAmmo}";
                    }
                    Text.Font = GameFont.Tiny;
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Widgets.Label(bottomRect, ammoText);
                }
            }
            else
            {
                if (equippedWeapon != null)
                {
                    GUI.color = new Color(1f, 1f, 1f, 0.25f);
                    GUI.DrawTexture(leftPanel, WeaponSilhouetteTex);
                    GUI.color = Color.white;
                    Rect equippedNameRect = new Rect(leftPanel.x + 5, leftPanel.y + 3, leftPanel.width - 10, 20);
                    Text.Font = GameFont.Tiny;
                    Text.Anchor = TextAnchor.UpperLeft;
                    Text.WordWrap = false;
                    string equippedLabel = equippedWeapon.LabelCap;
                    Widgets.Label(equippedNameRect, equippedLabel.Truncate(equippedNameRect.width));
                    Text.WordWrap = true;
                    TooltipHandler.TipRegion(equippedNameRect, equippedLabel);
                    Text.Font = GameFont.Small;
                    Text.Anchor = TextAnchor.MiddleCenter;
                    GUI.color = Color.gray;
                    Widgets.Label(leftPanel, "CMC_InHands".Translate());
                    GUI.color = Color.white;

                    var ammoComp = equippedWeapon.TryGetComp<CompWeaponAmmo>();
                    if (ammoComp != null)
                    {
                        int backpackAmmo = ammoComp.ConnectedBackpack != null ? ammoComp.ConnectedBackpack.Charges : 0;
                        int reloadTicks = comp.ReloadTicksLeft;
                        string ammoText;
                        Rect bottomRect = new Rect(leftPanel.x + 5, leftPanel.y + leftPanel.height - 20, leftPanel.width - 10, 16);

                        if (reloadTicks > 0)
                        {
                            float seconds = reloadTicks / 60f;
                            ammoText = $"{ammoComp.currentMagAmmo} " + "CMC_Reloading".Translate(seconds.ToString("F1")) + $" / {backpackAmmo}";

                            float pct = 1f - ((float)reloadTicks / ammoComp.Props.reloadTicks);
                            Widgets.FillableBar(bottomRect, pct, BlueProgressTex);
                        }
                        else
                        {
                            ammoText = $"{ammoComp.currentMagAmmo} / {backpackAmmo}";
                        }
                        Text.Font = GameFont.Tiny;
                        Text.Anchor = TextAnchor.MiddleCenter;
                        Widgets.Label(bottomRect, ammoText);
                    }
                }
                else
                {
                    Text.Font = GameFont.Small;
                    Text.Anchor = TextAnchor.MiddleCenter;
                    GUI.color = Color.gray;
                    Widgets.Label(leftPanel, "CMC_Empty".Translate());
                    GUI.color = Color.white;
                }
            }

            Text.Anchor = TextAnchor.UpperLeft;
            Texture2D autoTex = comp.AutoSwapEnabled ? AutoSwapOnTex : AutoSwapOffTex;

            if (Widgets.ButtonImage(autoSwapRect, autoTex))
            {
                comp.AutoSwapEnabled = !comp.AutoSwapEnabled;
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
            }
            TooltipHandler.TipRegion(autoSwapRect, "CMC_ToggleAutoSwapTip".Translate());

            Texture2D manualTex = hasWeapon ? CompApparelWeaponHolder.TakeOutCmdIcon : CompApparelWeaponHolder.PutInCmdIcon;
            if (Widgets.ButtonImage(manualSwapRect, manualTex))
            {
                if (hasWeapon)
                {
                    comp.TryTakeOutWeapon();
                    SoundDefOf.Tick_High.PlayOneShotOnCamera();
                }
                else
                {
                    if (equippedWeapon != null)
                    {
                        comp.TryPutInWeaponFromHands();
                        SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                    }
                    else
                    {
                        Messages.Message("CMC_NoWeapon".Translate(), MessageTypeDefOf.RejectInput, false);
                    }
                }
            }
            string manualTip = hasWeapon ? "CMC_TakeOutWeaponTip".Translate() : "CMC_PutInWeaponTip".Translate();
            TooltipHandler.TipRegion(manualSwapRect, manualTip);

            return new GizmoResult(GizmoState.Clear);
        }
    }
}