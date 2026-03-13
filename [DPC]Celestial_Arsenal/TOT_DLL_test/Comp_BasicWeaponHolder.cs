using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace TOT_DLL_test
{
    public class CompProperties_BasicWeaponHolder : CompProperties
    {
        public float maxMass = 5f;

        public CompProperties_BasicWeaponHolder()
        {
            base.compClass = typeof(CompBasicWeaponHolder);
        }
    }
    [StaticConstructorOnStartup]
    public class CompBasicWeaponHolder : ThingComp, IThingHolder
    {
        public static readonly Texture2D TakeOutCmdIcon = ContentFinder<Texture2D>.Get("UI/CompApparelWeaponHolder_TakeOutCmd", true);
        public static readonly Texture2D PutInCmdIcon = ContentFinder<Texture2D>.Get("UI/CompApparelWeaponHolder_PutInCmd", true);
        private ThingOwner<ThingWithComps> weaponContainer;
        private CompProperties_BasicWeaponHolder Props => (CompProperties_BasicWeaponHolder)base.props;
        public bool IsApparel => base.parent is Apparel;
        public Pawn Wearer => ((Apparel)base.parent).Wearer;
        public bool AnyWeaponInBelt => weaponContainer.Any;

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            weaponContainer = new ThingOwner<ThingWithComps>(this);
        }
        public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
        {
            if (!IsApparel) yield break;
            foreach (Gizmo gizmo in GetGizmos())
            {
                yield return gizmo;
            }
        }
        private IEnumerable<Gizmo> GetGizmos()
        {
            if (Wearer == null || (!Wearer.IsPlayerControlled && !DebugSettings.ShowDevGizmos))
            {
                yield break;
            }
            ThingWithComps weaponEquipping = Wearer?.equipment?.Primary;
            if (AnyWeaponInBelt)
            {
                Command_Action takeOutCmd = new Command_Action
                {
                    groupKey = 456123 + this.parent.thingIDNumber,
                    action = delegate
                    {
                        if (!AnyWeaponInBelt) return;

                        if (weaponEquipping != null)
                        {
                            Wearer.inventory.innerContainer.TryAddOrTransfer(weaponEquipping, true);
                        }

                        ThingWithComps weaponContained = weaponContainer.InnerListForReading.First();
                        Wearer.equipment.GetDirectlyHeldThings().TryAddOrTransfer(weaponContained, true);

                        if (Wearer.Drawer?.renderer != null)
                        {
                            Wearer.Drawer.renderer.SetAllGraphicsDirty();
                        }
                    },
                    defaultDesc = "CMC_TakeWeaponFromBelt".Translate(weaponContainer.InnerListForReading.First().LabelShort),
                    activateSound = SoundDefOf.Click,
                    icon = TakeOutCmdIcon
                };

                if (Wearer.WorkTagIsDisabled(WorkTags.Violent) || Wearer.health.capacities.GetLevel(PawnCapacityDefOf.Manipulation) == 0f)
                {
                    string reason = Wearer.WorkTagIsDisabled(WorkTags.Violent)
                        ? "IsIncapableOfViolence".Translate(Wearer.LabelShort, Wearer)
                        : "MessageIncapableOfManipulation".Translate(Wearer.LabelShort);
                    takeOutCmd.Disable("CMC_CannotTakeOutWeapon".Translate(reason));
                }

                yield return takeOutCmd;
                yield break;
            }
            Command_Action putInCmd = new Command_Action
            {
                groupKey = 789456 + this.parent.thingIDNumber,
                action = delegate
                {
                    if (weaponEquipping == null || Wearer?.equipment?.Primary != weaponEquipping) return;

                    weaponContainer.TryAddOrTransfer(weaponEquipping, true);

                    if (Wearer.Drawer?.renderer != null)
                    {
                        Wearer.Drawer.renderer.SetAllGraphicsDirty();
                    }
                },
                defaultDesc = "CMC_PutWeaponInBelt".Translate(weaponEquipping != null ? weaponEquipping.LabelShort : "NULL"),
                activateSound = SoundDefOf.Click,
                icon = PutInCmdIcon
            };
            if (weaponEquipping == null)
            {
                putInCmd.Disable("CMC_NoWeapon".Translate());
            }
            else
            {
                Pawn_ApparelTracker apparel = Wearer.apparel;
                if (!apparel.IsLocked(base.parent as Apparel))
                {
                    CompBiocodable biocodableComp = weaponEquipping.TryGetComp<CompBiocodable>();
                    if (biocodableComp != null && biocodableComp.Biocoded)
                    {
                        putInCmd.Disable("CMC_WeaponWasBiocoded".Translate(weaponEquipping.LabelShort));
                    }
                    else
                    {
                        CompBladelinkWeapon bladelinkComp = weaponEquipping.TryGetComp<CompBladelinkWeapon>();
                        if (bladelinkComp != null && bladelinkComp.Biocoded)
                        {
                            putInCmd.Disable("CMC_WeaponWasLinked".Translate(weaponEquipping.LabelShort));
                        }
                    }
                }
                if (!putInCmd.Disabled)
                {
                    if (weaponEquipping.def.BaseMass > Props.maxMass)
                    {
                        putInCmd.Disable("CMC_WeaponTooHeavy".Translate(weaponEquipping.LabelShort));
                    }
                }
            }

            yield return putInCmd;
        }
        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }
        public ThingOwner GetDirectlyHeldThings()
        {
            return weaponContainer;
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Deep.Look(ref weaponContainer, "weaponContainer", this);
        }
    }
}