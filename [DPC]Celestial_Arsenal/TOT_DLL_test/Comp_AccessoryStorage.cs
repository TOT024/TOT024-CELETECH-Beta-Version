using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace TOT_DLL_test
{
    public class CompProperties_AccessoryContainer : CompProperties
    {
        public int maxCapacity = 8;

        public CompProperties_AccessoryContainer()
        {
            this.compClass = typeof(CompAccessoryContainer);
        }
    }

    public class CompAccessoryContainer : ThingComp, IThingHolder, ISearchableContents
    {
        public ThingOwner innerContainer;
        public List<Thing> leftToLoad = new List<Thing>();
        public bool autoLoad = true;
        private List<Thing> tmpAccessories = new List<Thing>();
        private static readonly CachedTexture EjectTex = new CachedTexture("UI/Gizmos/EjectAll");

        public CompProperties_AccessoryContainer Props
        {
            get
            {
                return (CompProperties_AccessoryContainer)this.props;
            }
        }

        public bool PowerOn
        {
            get
            {
                var power = this.parent.TryGetComp<CompPowerTrader>();
                return power == null || power.PowerOn;
            }
        }
        public bool Full
        {
            get
            {
                return this.innerContainer.Count >= this.Props.maxCapacity;
            }
        }
        public ThingOwner SearchableContents
        {
            get
            {
                return this.innerContainer;
            }
        }
        public List<Thing> ContainedAccessories
        {
            get
            {
                this.tmpAccessories.Clear();
                this.tmpAccessories.AddRange(this.innerContainer);
                return this.tmpAccessories;
            }
        }

        public override void PostPostMake()
        {
            base.PostPostMake();
            this.innerContainer = new ThingOwner<Thing>(this);
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, this.GetDirectlyHeldThings());
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return this.innerContainer;
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            this.innerContainer.ClearAndDestroyContents(DestroyMode.Vanish);
            base.PostDestroy(mode, previousMap);
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            if (mode != DestroyMode.WillReplace)
            {
                this.EjectContents(map);
            }
            this.leftToLoad.Clear();
        }

        public void EjectContents(Map destMap = null)
        {
            if (destMap == null)
            {
                destMap = this.parent.Map;
            }
            IntVec3 dropLoc = this.parent.def.hasInteractionCell ? this.parent.InteractionCell : this.parent.Position;
            this.innerContainer.TryDropAll(dropLoc, destMap, ThingPlaceMode.Near, null, null, true);
        }

        public void EjectAccessory(Thing accessory, Map destMap = null)
        {
            if (destMap == null)
            {
                destMap = this.parent.Map;
            }
            if (this.innerContainer.Contains(accessory))
            {
                IntVec3 dropLoc = this.parent.def.hasInteractionCell ? this.parent.InteractionCell : this.parent.Position;
                Thing result;
                this.innerContainer.TryDrop(accessory, dropLoc, destMap, ThingPlaceMode.Near, 1, out result);
            }
        }

        public bool Accepts(Thing thing)
        {
            return !this.Full && thing.TryGetComp<CompAccessoryStats>() != null;
        }

        public bool TryAcceptAccessory(Thing thing)
        {
            if (!Accepts(thing)) return false;

            if (thing.stackCount > 1)
            {
                Thing toAdd = thing.SplitOff(1);
                return this.innerContainer.TryAdd(toAdd, true);
            }
            else
            {
                return this.innerContainer.TryAdd(thing, true);
            }
        }

        public override void CompTickRare()
        {
            if (this.innerContainer != null)
            {
                for (int i = 0; i < this.innerContainer.Count; i++)
                {
                    this.innerContainer[i].TickRare();
                }
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (this.parent.Faction == Faction.OfPlayer && this.innerContainer.Any)
            {
                yield return new Command_Action
                {
                    defaultLabel = "EjectAll".Translate(),
                    defaultDesc = "EjectAllDesc".Translate(),
                    icon = EjectTex.Texture,
                    action = delegate ()
                    {
                        this.EjectContents(this.parent.Map);
                    }
                };
            }

            if (DebugSettings.ShowDevGizmos)
            {
                yield return new Command_Action
                {
                    defaultLabel = "DEV: Fill with test items",
                    action = delegate ()
                    {
                        this.innerContainer.ClearAndDestroyContents(DestroyMode.Vanish);
                    }
                };
            }
        }

        public override string CompInspectStringExtra()
        {
            return "Stored Accessories: " + string.Format("{0} / {1}", this.innerContainer.Count, this.Props.maxCapacity);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Deep.Look<ThingOwner>(ref this.innerContainer, "innerContainer", new object[]
            {
                this
            });
            Scribe_Collections.Look<Thing>(ref this.leftToLoad, "leftToLoad", LookMode.Reference, Array.Empty<object>());
            Scribe_Values.Look<bool>(ref this.autoLoad, "autoLoad", true, false);
        }
    }
}