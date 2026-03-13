using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace TOT_DLL_test
{
    public class Building_AccessoryCabinet : Building_Storage, IThingHolder
    {
        protected ThingOwner innerContainer;
        private const int MaxCapacity = 4;
        public const float EffectRadius = 12.9f;

        public Building_AccessoryCabinet()
        {
            this.innerContainer = new ThingOwner<Thing>(this, false, LookMode.Deep);
        }
        public IList<Thing> StoredAccessories => innerContainer;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            UpdateStorageState();
        }

        public override void Notify_ReceivedThing(Thing newItem)
        {
            base.Notify_ReceivedThing(newItem);
            if (newItem == null) return;
            if (newItem.TryGetComp<CompAccessoryStats>() == null) return;
            if (innerContainer.Count < MaxCapacity || CanStackWithExisting(newItem))
            {
                bool added = innerContainer.TryAdd(newItem.SplitOff(newItem.stackCount), canMergeWithExistingStacks: true);

                if (added)
                {
                    UpdateStorageState();
                }
            }
        }
        private bool CanStackWithExisting(Thing item)
        {
            for (int i = 0; i < innerContainer.Count; i++)
            {
                if (innerContainer[i].CanStackWith(item) && innerContainer[i].stackCount < innerContainer[i].def.stackLimit)
                {
                    return true;
                }
            }
            return false;
        }
        public override void DrawExtraSelectionOverlays()
        {
            base.DrawExtraSelectionOverlays();
            GenDraw.DrawRadiusRing(this.Position, EffectRadius);
        }

        private void UpdateStorageState()
        {
            if (this.Map == null) return;
            bool isFull = innerContainer.Count >= MaxCapacity;
            StoragePriority desiredPriority = isFull ? StoragePriority.Unstored : StoragePriority.Important;

            if (this.settings.Priority != desiredPriority)
            {
                this.settings.Priority = desiredPriority;
                this.Map.listerHaulables.Notify_SlotGroupChanged(this.slotGroup);
            }
        }
        public override void TickRare()
        {
            base.TickRare();
            UpdateStorageState();
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if (innerContainer != null && innerContainer.Count > 0)
            {
                innerContainer.TryDropAll(this.Position, this.Map, ThingPlaceMode.Near);
            }
            base.Destroy(mode);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
        }
        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, this.GetDirectlyHeldThings());
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return innerContainer;
        }
        public void EjectAccessory(Thing t)
        {
            if (innerContainer.Contains(t))
            {
                if (innerContainer.TryDrop(t, this.Position, this.Map, ThingPlaceMode.Near, out Thing droppedThing))
                {
                    UpdateStorageState();
                }
            }
        }

        public override string GetInspectString()
        {
            string s = base.GetInspectString();
            if (innerContainer.Count > 0)
            {
                if (!s.NullOrEmpty()) s += "\n";
                s += "Stored: " + innerContainer.Count + "/" + MaxCapacity;
            }
            return s;
        }
    }
    public class ITab_AccessoryContainer : ITab_ContentsBase
    {
        private static readonly CachedTexture DropTex = new CachedTexture("UI/Buttons/Drop");
        private Building_AccessoryCabinet SelectedCabinet
        {
            get => base.SelThing as Building_AccessoryCabinet;
        }

        public override IList<Thing> container
        {
            get
            {
                if (SelectedCabinet != null)
                {
                    return SelectedCabinet.GetDirectlyHeldThings().ToList();
                }
                return new List<Thing>();
            }
        }

        public override bool IsVisible
        {
            get
            {
                return SelectedCabinet != null;
            }
        }

        public ITab_AccessoryContainer()
        {
            this.labelKey = "CMC_TabAccessoryContents";
            this.containedItemsKey = "CMC_ContainedAccessories";
        }

        protected override void DoItemsLists(Rect inRect, ref float curY)
        {
            this.ListContainedAccessories(inRect, this.container, ref curY);
        }

        private void ListContainedAccessories(Rect inRect, IList<Thing> items, ref float curY)
        {
            GUI.BeginGroup(inRect);
            float num = curY;
            Widgets.ListSeparator(ref curY, inRect.width, this.containedItemsKey.Translate());
            Rect rect = new Rect(0f, num, inRect.width, curY - num - 3f);

            if (Mouse.IsOver(rect))
            {
                Widgets.DrawHighlight(rect);
            }

            bool flag = false;
            for (int i = 0; i < items.Count; i++)
            {
                Thing t = items[i];
                if (t != null)
                {
                    flag = true;
                    this.DoRow(t, inRect.width, i, ref curY);
                }
            }

            if (!flag)
            {
                Widgets.NoneLabel(ref curY, inRect.width, null);
            }
            GUI.EndGroup();
        }

        private void DoRow(Thing thing, float width, int i, ref float curY)
        {
            Rect rect = new Rect(0f, curY, width, 28f);
            Widgets.InfoCardButton(0f, curY, thing);

            if (Mouse.IsOver(rect))
            {
                Widgets.DrawHighlightSelected(rect);
            }
            else if (i % 2 == 1)
            {
                Widgets.DrawLightHighlight(rect);
            }
            Rect rect2 = new Rect(rect.width - 24f, curY, 24f, 24f);

            if (Widgets.ButtonImage(rect2, DropTex.Texture, true, null))
            {
                SelectedCabinet?.EjectAccessory(thing);
            }
            else if (Widgets.ButtonInvisible(rect, true))
            {
                Find.Selector.ClearSelection();
                Find.Selector.Select(thing, true, true);
            }

            TooltipHandler.TipRegionByKey(rect2, "CMC_EjectAccessoryTooltip");
            Widgets.ThingIcon(new Rect(24f, curY, 28f, 28f), thing, 1f, null, false);
            Rect rect3 = new Rect(60f, curY, rect.width - 36f, rect.height);
            rect3.xMax = rect2.xMin;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(rect3, thing.LabelCap.Truncate(rect3.width, null));
            Text.Anchor = TextAnchor.UpperLeft;

            if (Mouse.IsOver(rect))
            {
                TargetHighlighter.Highlight(thing, true, false, false);
                TooltipHandler.TipRegion(rect, thing.DescriptionDetailed);
            }
            curY += 28f;
        }
    }
}