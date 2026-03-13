using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace TOT_DLL_test
{
    public class ITab_TurretAccessories : ITab
    {
        private static readonly Vector2 WinSize = new Vector2(400f, 500f);
        private Vector2 scrollPosition;

        public ITab_TurretAccessories()
        {
            this.size = WinSize;
            this.labelKey = "TabAccessories";
        }
        private Building_CMCTurretGun SelTurret => base.SelThing as Building_CMCTurretGun;
        private CompAccessoryHolder HolderComp => SelTurret?.GetComp<CompAccessoryHolder>();

        public override bool IsVisible
        {
            get
            {
                return HolderComp != null;
            }
        }

        protected override void FillTab()
        {
            Building_CMCTurretGun turret = SelTurret;
            CompAccessoryHolder holder = HolderComp;
            Thing accessoryToRemove = null;

            if (turret == null || holder == null)
            {
                return;
            }
            Rect rootRect = new Rect(0f, 0f, WinSize.x, WinSize.y).ContractedBy(10f);
            Rect contentRect = new Rect(rootRect.x, rootRect.y + 10f, rootRect.width, rootRect.height - 10f);

            GUI.BeginGroup(contentRect);

            Listing_Standard listing = new Listing_Standard();
            listing.Begin(new Rect(0, 0, contentRect.width, contentRect.height));
            Text.Font = GameFont.Medium;
            listing.Label("Installed Accessories");
            Text.Font = GameFont.Small;
            string capacityLabel = $"Slots: {holder.InstalledAccessories.Count} / {holder.Props.maxAccessories}";
            listing.Label(capacityLabel);
            listing.GapLine();
            if (holder.InstalledAccessories.Any())
            {
                float rowHeight = 32f;
                for (int i = 0; i < holder.InstalledAccessories.Count; i++)
                {
                    Thing accessory = holder.InstalledAccessories[i];
                    Rect rowRect = listing.GetRect(rowHeight);

                    if (DrawAccessoryRow(rowRect, accessory, i))
                    {
                        accessoryToRemove = accessory;
                    }
                }
            }
            else
            {
                listing.Label("No accessories installed.", -1f);
            }

            listing.End();
            GUI.EndGroup();
            if (accessoryToRemove != null)
            {
                holder.UninstallAccessory(accessoryToRemove);
            }
        }
        private bool DrawAccessoryRow(Rect rect, Thing accessory, int index)
        {
            bool uninstallClicked = false;
            if (Mouse.IsOver(rect))
            {
                Widgets.DrawHighlight(rect);
                TooltipHandler.TipRegion(rect, accessory.DescriptionDetailed);
            }
            else if (index % 2 == 1)
            {
                Widgets.DrawLightHighlight(rect);
            }
            Rect infoRect = new Rect(rect.x, rect.y + (rect.height - 24f) / 2, 24f, 24f);
            Widgets.InfoCardButton(infoRect.x, infoRect.y, accessory);
            Rect iconRect = new Rect(infoRect.xMax + 4f, rect.y + 2f, 28f, 28f);
            Widgets.ThingIcon(iconRect, accessory);
            float btnWidth = 80f;
            Rect btnRect = new Rect(rect.width - btnWidth, rect.y + 1f, btnWidth, rect.height - 2f);
            if (Widgets.ButtonText(btnRect, "Uninstall"))
            {
                uninstallClicked = true;
            }
            Rect labelRect = new Rect(
                iconRect.xMax + 8f,
                rect.y,
                btnRect.x - (iconRect.xMax + 12f),
                rect.height
            );
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(labelRect, accessory.LabelCap);
            Text.Anchor = TextAnchor.UpperLeft;

            return uninstallClicked;
        }
    }
}