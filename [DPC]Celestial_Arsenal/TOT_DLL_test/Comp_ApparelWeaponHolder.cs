using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace TOT_DLL_test
{
    [StaticConstructorOnStartup]
    public class CompProperties_ApparelWeaponHolder : CompProperties
    {
        public bool useDefNameCheck = false;
        public float maxMass = 999f;
        public bool autoGenerateWeapon = false;
        public string autoGenerateWeaponDefName = "";
        public List<string> allowedDefNames = new List<string>();
        private const int displayPriority = 300;
        private static StatCategoryDef statCategoryDef;
        public string TextWeaponSilhouetteTexPath = string.Empty;
        public CompProperties_ApparelWeaponHolder()
        {
            base.compClass = typeof(CompApparelWeaponHolder);
        }
        public override void ResolveReferences(ThingDef parentDef)
        {
            statCategoryDef = StatCategoryDefOf.Apparel;
        }
    }
    [StaticConstructorOnStartup]
    public class CompApparelWeaponHolder : ThingComp, IThingHolder
    {
        public int ReloadTicksLeft => reloadTicksLeft;
        public bool AutoSwapEnabled = true;
        public static readonly Texture2D TakeOutCmdIcon = ContentFinder<Texture2D>.Get("UI/CompApparelWeaponHolder_TakeOutCmd", true);
        public static readonly Texture2D PutInCmdIcon = ContentFinder<Texture2D>.Get("UI/CompApparelWeaponHolder_PutInCmd", true);
        private ThingOwner<ThingWithComps> weaponContainer;
        private ThingWithComps weaponContained;
        public CompProperties_ApparelWeaponHolder Props => (CompProperties_ApparelWeaponHolder)base.props;
        public bool IsApparel => base.parent is Apparel;
        public Pawn Wearer => ((Apparel)base.parent).Wearer;
        public bool AnyWeaponInBelt => weaponContainer.Any;
        private int reloadTicksLeft = -1;
        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            weaponContainer = new ThingOwner<ThingWithComps>(this);
        }
        private bool initialWeaponGenerated = false;
        private void TryGenerateInitialWeapon()
        {
            if (initialWeaponGenerated || !Props.useDefNameCheck || !Props.autoGenerateWeapon) return;
            initialWeaponGenerated = true;
            string defNameToGenerate = Props.autoGenerateWeaponDefName;
            if (string.IsNullOrEmpty(defNameToGenerate) && Props.allowedDefNames != null && Props.allowedDefNames.Count > 0)
            {
                defNameToGenerate = Props.allowedDefNames[0];
            }
            if (string.IsNullOrEmpty(defNameToGenerate)) return;
            ThingDef weaponDef = DefDatabase<ThingDef>.GetNamedSilentFail(defNameToGenerate);
            if (weaponDef == null)
            {
                Log.Error($"[TOT_DLL_test] Cannot find weapon def: {defNameToGenerate} to auto-generate.");
                return;
            }
            ThingWithComps newWeapon = (ThingWithComps)ThingMaker.MakeThing(weaponDef);
            CompQuality parentQuality = this.parent.TryGetComp<CompQuality>();
            CompQuality weaponQuality = newWeapon.TryGetComp<CompQuality>();
            if (parentQuality != null && weaponQuality != null)
            {
                weaponQuality.SetQuality(parentQuality.Quality, ArtGenerationContext.Colony);
            }
            var ammoComp = newWeapon.GetComp<CompWeaponAmmo>();
            if (ammoComp != null)
            {
                ammoComp.currentMagAmmo = ammoComp.Props.maxMagAmmo;
            }
            this.weaponContainer.TryAddOrTransfer(newWeapon, true);
        }
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (!respawningAfterLoad)
            {
                TryGenerateInitialWeapon();
            }
        }
        public override void Notify_Equipped(Pawn pawn)
        {
            base.Notify_Equipped(pawn);
            TryGenerateInitialWeapon();
        }
        public ThingWithComps GetWeaponContained()
        {
            if (!AnyWeaponInBelt) return null;
            return weaponContainer.InnerListForReading.First() as ThingWithComps;
        }
        public void TryTakeOutWeapon()
        {
            if (!AnyWeaponInBelt) return;
            ThingWithComps weaponEquipping = Wearer?.equipment?.Primary;

            if (weaponEquipping != null)
            {
                Wearer.inventory.innerContainer.TryAddOrTransfer(weaponEquipping, true);
            }

            ThingWithComps weaponContained = GetWeaponContained();
            Wearer.equipment.GetDirectlyHeldThings().TryAddOrTransfer(weaponContained, true);

            if (Wearer.Drawer?.renderer != null)
            {
                Wearer.Drawer.renderer.SetAllGraphicsDirty();
            }
        }
        public void TryPutInWeaponFromHands()
        {
            ThingWithComps weaponEquipping = Wearer?.equipment?.Primary;
            if (weaponEquipping == null)
            {
                Messages.Message("CMC_NoWeapon".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }
            Pawn_ApparelTracker apparel = Wearer.apparel;
            if (apparel.IsLocked(base.parent as Apparel))
            {
                Messages.Message("Cannot drop locked apparel.", MessageTypeDefOf.RejectInput, false);
                return;
            }
            CompBiocodable biocodableComp = weaponEquipping.TryGetComp<CompBiocodable>();
            if (biocodableComp != null && biocodableComp.Biocoded)
            {
                Messages.Message("CMC_WeaponWasBiocoded".Translate(weaponEquipping.LabelShort), MessageTypeDefOf.RejectInput, false);
                return;
            }
            CompBladelinkWeapon bladelinkComp = weaponEquipping.TryGetComp<CompBladelinkWeapon>();
            if (bladelinkComp != null && bladelinkComp.Biocoded)
            {
                Messages.Message("CMC_WeaponWasLinked".Translate(weaponEquipping.LabelShort), MessageTypeDefOf.RejectInput, false);
                return;
            }
            if (Props.useDefNameCheck)
            {
                if (Props.allowedDefNames != null && Props.allowedDefNames.Count > 0 && !Props.allowedDefNames.Contains(weaponEquipping.def.defName))
                {
                    Messages.Message("CMC_WeaponTypeNotAllowed".Translate(weaponEquipping.LabelShort), MessageTypeDefOf.RejectInput, false);
                    return;
                }
            }
            else
            {
                if (weaponEquipping.def.BaseMass > Props.maxMass)
                {
                    Messages.Message("CMC_WeaponTooHeavy".Translate(weaponEquipping.LabelShort), MessageTypeDefOf.RejectInput, false);
                    return;
                }
            }
            bool success = weaponContainer.TryAddOrTransfer(weaponEquipping, true);
            if (success)
            {
                var ammoComp = weaponEquipping.GetComp<CompWeaponAmmo>();
                if (ammoComp != null && ammoComp.currentMagAmmo < ammoComp.Props.maxMagAmmo)
                {
                    reloadTicksLeft = ammoComp.Props.reloadTicks;
                }
                else
                {
                    reloadTicksLeft = -1;
                }
                ThingWithComps val = (ThingWithComps)Wearer.inventory.innerContainer.FirstOrDefault(item => item.TryGetComp<CompEquippable>() != null);
                if (val != null)
                {
                    Wearer.equipment.GetDirectlyHeldThings().TryAddOrTransfer(val, true);
                }
                if (Wearer.Drawer?.renderer != null)
                {
                    Wearer.Drawer.renderer.SetAllGraphicsDirty();
                }
            }
        }
        public override void CompTick()
        {
            base.CompTick();
            if (pendingWeaponToHolster != null)
            {
                if (pendingWeaponToHolster.Destroyed || Wearer?.equipment?.Primary != pendingWeaponToHolster)
                {
                    pendingWeaponToHolster = null;
                }
                else if (IsSafeToHolster(Wearer))
                {
                    if (TryPutInWeapon(pendingWeaponToHolster))
                    {
                        EquipNextWeaponFromInventory();
                    }
                    pendingWeaponToHolster = null;
                }
            }
            if (reloadTicksLeft > 0)
            {
                reloadTicksLeft--;

                if (reloadTicksLeft == 0)
                {
                    FinishReloading();
                }
            }
            if (pendingAutoSwapBack)
            {
                if (!AnyWeaponInBelt)
                {
                    pendingAutoSwapBack = false;
                }
                else if (IsSafeToSwapBack(Wearer))
                {
                    ThingWithComps reloadedWeapon = weaponContainer.InnerListForReading.First() as ThingWithComps;
                    AutoSwapOut(reloadedWeapon);
                    pendingAutoSwapBack = false;
                }
            }
        }
        private bool IsSafeToHolster(Pawn pawn)
        {
            if (pawn == null || pawn.stances == null) return true;
            return pawn.stances.curStance is Stance_Mobile;
        }
        private bool IsSafeToSwapBack(Pawn pawn)
        {
            if (pawn == null || pawn.stances == null) return true;
            if (pawn.jobs != null && pawn.jobs.curJob != null)
            {
                if (pawn.jobs.curJob.playerForced && pawn.jobs.curJob.def == JobDefOf.AttackStatic)
                {
                    return false;
                }
            }
            if (pawn.stances.curStance is Stance_Mobile) return true;
            if (pawn.stances.curStance is Stance_Warmup) return true;
            return false;
        }
        private void FinishReloading()
        {
            if (!AnyWeaponInBelt) return;

            ThingWithComps weapon = weaponContainer.InnerListForReading.First() as ThingWithComps;
            var ammoComp = weapon?.GetComp<CompWeaponAmmo>();

            if (ammoComp != null)
            {
                ammoComp.TryReloadFromBackpack();
                if (this.AutoSwapEnabled)
                {
                    pendingAutoSwapBack = true;
                }
            }
            reloadTicksLeft = -1;
        }
        private void AutoSwapOut(ThingWithComps weaponToEject)
        {
            if (Wearer == null || Wearer.Dead || Wearer.Downed) return;
            CompWeaponAmmo ammo = weaponToEject?.TryGetComp<CompWeaponAmmo>();
            if (ammo != null && ammo.currentMagAmmo <= 0) return;
            ThingWithComps weaponEquipping = Wearer.equipment?.Primary;
            if (weaponEquipping != null)
            {
                Wearer.inventory.innerContainer.TryAddOrTransfer(weaponEquipping, true);
            }
            Wearer.equipment.GetDirectlyHeldThings().TryAddOrTransfer(weaponToEject, true);
            if (Wearer.Drawer?.renderer != null)
            {
                Wearer.Drawer.renderer.SetAllGraphicsDirty();
            }
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
            yield return new Gizmo_WeaponHolderUI(this);
        }
        public void EquipNextWeaponFromInventory()
        {
            if (Wearer == null || Wearer.equipment == null || Wearer.inventory == null) return;
            ThingWithComps nextWeapon = Wearer.inventory.innerContainer
                .OfType<ThingWithComps>()
                .FirstOrDefault(t =>
                {
                    if (!t.def.IsWeapon || t.def.equipmentType != EquipmentType.Primary) return false;
                    var ammoComp = t.GetComp<CompWeaponAmmo>();
                    if (ammoComp != null)
                    {
                        if (ammoComp.currentMagAmmo <= 0) return false;
                    }

                    return true;
                });
            if (nextWeapon != null)
            {
                if (Wearer.equipment.Primary != null)
                {
                    Wearer.inventory.innerContainer.TryAddOrTransfer(Wearer.equipment.Primary, true);
                }

                Wearer.inventory.innerContainer.TryTransferToContainer(nextWeapon, Wearer.equipment.GetDirectlyHeldThings(), 1);

                if (Wearer.Drawer?.renderer != null)
                {
                    Wearer.Drawer.renderer.SetAllGraphicsDirty();
                }
            }
        }
        public bool TryPutInWeapon(ThingWithComps eq)
        {
            if (AnyWeaponInBelt)
            {
                Log.ErrorOnce("Try put weapon into CompApparelWeaponHolder where already has a weapon:" + base.parent.ToString(), 19472935);
                return false;
            }
            bool success = weaponContainer.TryAddOrTransfer(eq, true);
            if (success)
            {
                var ammoComp = eq.GetComp<CompWeaponAmmo>();
                if (ammoComp != null && ammoComp.currentMagAmmo < ammoComp.Props.maxMagAmmo)
                {
                    reloadTicksLeft = ammoComp.Props.reloadTicks;
                    //MoteMaker.ThrowText(Wearer.DrawPos, Wearer.Map, $"Reloading {eq.LabelShort}...", ammoComp.Props.reloadTicks / 60);
                }
                else
                {
                    reloadTicksLeft = -1;
                }
            }
            return success;
        }
        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }
        public ThingOwner GetDirectlyHeldThings()
        {
            return weaponContainer;
        }
        public override string CompInspectStringExtra()
        {
            string extra = base.CompInspectStringExtra();
            if (reloadTicksLeft > 0 && AnyWeaponInBelt)
            {
                ThingWithComps weapon = weaponContainer.InnerListForReading.First() as ThingWithComps;
                var ammoComp = weapon?.GetComp<CompWeaponAmmo>();
                if (ammoComp != null && ammoComp.Props.reloadTicks > 0)
                {
                    float progress = 1f - ((float)reloadTicksLeft / ammoComp.Props.reloadTicks);
                    string reloadStr = "Reloading: " + progress.ToStringPercent();
                    return string.IsNullOrEmpty(extra) ? reloadStr : extra + "\n" + reloadStr;
                }
                else
                {
                    reloadTicksLeft = -1;
                }
            }
            return extra;
        }
        private ThingWithComps pendingWeaponToHolster = null;
        private bool pendingAutoSwapBack = false;
        public void RequestHolster(ThingWithComps weapon)
        {
            pendingWeaponToHolster = weapon;
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Deep.Look(ref weaponContainer, "weaponContainer", this);
            Scribe_Values.Look(ref reloadTicksLeft, "reloadTicksLeft", -1);
            Scribe_Values.Look(ref initialWeaponGenerated, "initialWeaponGenerated", false);
            Scribe_References.Look(ref pendingWeaponToHolster, "pendingWeaponToHolster");
            Scribe_Values.Look(ref pendingAutoSwapBack, "pendingAutoSwapBack", false);
            Scribe_Values.Look(ref AutoSwapEnabled, "AutoSwapEnabled", true);
        }
    }
}