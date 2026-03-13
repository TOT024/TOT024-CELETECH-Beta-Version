using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace TOT_DLL_test
{
    public class CompProperties_AccessoryHolder : CompProperties
    {
        public int maxAccessories = 2;
        public List<string> allowedAccessoryDefs = new List<string>();

        [Unsaved]
        private List<ThingDef> cachedAllowedDefs;

        public CompProperties_AccessoryHolder()
        {
            compClass = typeof(CompAccessoryHolder);
        }

        public override void ResolveReferences(ThingDef parentDef)
        {
            base.ResolveReferences(parentDef);

            if (cachedAllowedDefs == null) cachedAllowedDefs = new List<ThingDef>();
            cachedAllowedDefs.Clear();

            if (allowedAccessoryDefs != null)
            {
                foreach (string defName in allowedAccessoryDefs)
                {
                    ThingDef def = DefDatabase<ThingDef>.GetNamed(defName, errorOnFail: false);
                    if (def != null)
                    {
                        cachedAllowedDefs.Add(def);
                    }
                }
            }
            cachedAllowedDefs.SortBy(x => x.label);
        }

        public override IEnumerable<StatDrawEntry> SpecialDisplayStats(StatRequest req)
        {
            foreach (var entry in base.SpecialDisplayStats(req))
            {
                yield return entry;
            }

            if (cachedAllowedDefs != null && cachedAllowedDefs.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                List<Dialog_InfoCard.Hyperlink> hyperlinks = new List<Dialog_InfoCard.Hyperlink>();

                sb.AppendLine("CMC_CompatibleDescription".Translate());

                foreach (ThingDef def in cachedAllowedDefs)
                {
                    sb.AppendLine($"- {def.LabelCap}");
                    hyperlinks.Add(new Dialog_InfoCard.Hyperlink(def));
                }

                yield return new StatDrawEntry(
                    StatCategoryDefOf.Weapon,
                    "CMC_CompatibleAccesory".Translate(),
                    $"{cachedAllowedDefs.Count}",
                    sb.ToString(),
                    50,
                    null,
                    hyperlinks,
                    false
                );

                yield return new StatDrawEntry(
                    StatCategoryDefOf.Weapon,
                    "CMC_MaxSlots".Translate(),
                    maxAccessories.ToString(),
                    "CMC_MaxSlotsDesc".Translate(),
                    60,
                    null,
                    null,
                    false
                );
            }
        }
    }

    public class CompAccessoryHolder : ThingComp
    {
        private List<Thing> installedAccessories = new List<Thing>();
        private List<CompAccessoryEffect> cachedEffects = new List<CompAccessoryEffect>();
        private Dictionary<StatDef, (float add, float mult)> statCache = new Dictionary<StatDef, (float, float)>();

        private bool cacheDirty = true;
        private CompEquippable compEquippableInt;

        public CompProperties_AccessoryHolder Props => (CompProperties_AccessoryHolder)props;
        public List<Thing> InstalledAccessories => installedAccessories;
        public bool IsFull => installedAccessories.Count >= Props.maxAccessories;
        public bool IsTurret => parent is Building_CMCTurretGun;

        public CompEquippable CompEquippable
        {
            get
            {
                if (compEquippableInt == null)
                {
                    compEquippableInt = parent.TryGetComp<CompEquippable>();
                }
                return compEquippableInt;
            }
        }

        protected Pawn Holder
        {
            get
            {
                ThingWithComps parent = this.parent;
                if (!((parent?.ParentHolder) is Pawn_EquipmentTracker pawn_EquipmentTracker))
                {
                    return null;
                }
                return pawn_EquipmentTracker.pawn;
            }
        }
        private void RebuildAllCaches()
        {
            cachedEffects.Clear();
            statCache.Clear();

            for (int i = 0; i < installedAccessories.Count; i++)
            {
                Thing accessory = installedAccessories[i];
                if (accessory == null || accessory.Destroyed) continue;
                CompAccessoryEffect effectComp = accessory.TryGetComp<CompAccessoryEffect>();
                if (effectComp != null)
                {
                    cachedEffects.Add(effectComp);
                }
                CompAccessoryStats statsComp = accessory.TryGetComp<CompAccessoryStats>();
                if (statsComp?.Props?.stats != null)
                {
                    foreach (var modifier in statsComp.Props.stats)
                    {
                        if (modifier == null) continue;
                        if (!statCache.TryGetValue(modifier.stat, out var currentVal))
                        {
                            currentVal = (0f, 1f);
                        }

                        if (modifier.modifier == ModifierType.Add)
                            currentVal.add += modifier.value;
                        else if (modifier.modifier == ModifierType.Multiply)
                            currentVal.mult *= modifier.value;

                        statCache[modifier.stat] = currentVal;
                    }
                }
            }

            cacheDirty = false;
        }
        public bool TryGetStatOffset(StatDef stat, out float add, out float mult)
        {
            if (cacheDirty) RebuildAllCaches();

            if (statCache.TryGetValue(stat, out var val))
            {
                add = val.add;
                mult = val.mult;
                return true;
            }

            add = 0f;
            mult = 1f;
            return false;
        }
        private void ClearAllStatCachesFor(Thing thing)
        {
            if (thing == null) return;
            thing.Notify_ColorChanged(); 
            var stats = DefDatabase<StatDef>.AllDefsListForReading;
            for (int i = 0; i < stats.Count; i++)
            {
                stats[i].Worker?.ClearCacheForThing(thing);
            }
        }

        private bool AddAccessoryInternal(Thing accessoryInstance)
        {
            if (IsFull || accessoryInstance == null || installedAccessories.Contains(accessoryInstance))
                return false;

            installedAccessories.Add(accessoryInstance);
            cacheDirty = true;
            ClearAllStatCachesFor(parent);

            return true;
        }

        public bool TryInstallAccessory(Thing accessory)
        {
            if (AddAccessoryInternal(accessory))
            {
                if (accessory != null && !accessory.Destroyed && IsTurret)
                {
                    accessory.Destroy(DestroyMode.Vanish);
                }
                return true;
            }
            return false;
        }

        public void UninstallAccessory(Thing accessory)
        {
            if (installedAccessories.Contains(accessory))
            {
                installedAccessories.Remove(accessory);
                cacheDirty = true;

                if (IsTurret)
                {
                    ClearAllStatCachesFor(parent);
                }
                ClearAllStatCachesFor(parent);
            }
        }


        private void TryAddAbilityFromAccessory(Thing accessory)
        {
            var abilityComp = accessory.TryGetComp<CompAccessoryStats>();
            if (abilityComp?.Props?.abilities == null) return;

            if (this.Holder == null) return;

            foreach (AbilityDef ability in abilityComp.Props.abilities)
            {
                Holder.abilities.GainAbility(ability);
            }
        }

        private void TryRemoveAbilityFromAccessory(Thing accessory, Pawn pawnToClean = null)
        {
            var abilityComp = accessory.TryGetComp<CompAccessoryStats>();
            if (abilityComp?.Props?.abilities == null) return;

            Pawn targetPawn = pawnToClean ?? this.Holder;
            if (targetPawn == null) return;

            foreach (AbilityDef ability in abilityComp.Props.abilities)
            {
                targetPawn.abilities.RemoveAbility(ability);
            }
            targetPawn.abilities.Notify_TemporaryAbilitiesChanged();
        }

        public override void Notify_Equipped(Pawn pawn)
        {
            base.Notify_Equipped(pawn);
            foreach (Thing thing in installedAccessories)
            {
                TryAddAbilityFromAccessory(thing);
            }
            Holder?.abilities.Notify_TemporaryAbilitiesChanged();
        }

        public override void Notify_Unequipped(Pawn pawn)
        {
            base.Notify_Unequipped(pawn);
            foreach (Thing thing in installedAccessories)
            {
                TryRemoveAbilityFromAccessory(thing, pawn);
            }
            pawn?.abilities.Notify_TemporaryAbilitiesChanged();
        }

        public override void Notify_UsedWeapon(Pawn pawn)
        {
            base.Notify_UsedWeapon(pawn);
            if (cacheDirty) RebuildAllCaches(); 

            for (int i = 0; i < cachedEffects.Count; i++)
            {
                cachedEffects[i].Notify_WeaponFired(pawn, this.parent);
            }
        }

        public override void Notify_KilledPawn(Pawn pawn)
        {
            base.Notify_KilledPawn(pawn);
            if (cacheDirty) RebuildAllCaches();

            for (int i = 0; i < cachedEffects.Count; i++)
            {
                cachedEffects[i].Notify_WeaponKilled(pawn, this.parent);
            }
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            foreach (Thing thing in installedAccessories)
            {
                TryRemoveAbilityFromAccessory(thing);
            }
            Holder?.abilities.Notify_TemporaryAbilitiesChanged();
            base.PostDestroy(mode, previousMap);
        }


        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Collections.Look(ref installedAccessories, "installedAccessories", LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (installedAccessories == null)
                {
                    installedAccessories = new List<Thing>();
                }
                cacheDirty = true;
            }
        }

        public List<ThingDef> GetInstalledAccessoriesDefs()
        {
            return installedAccessories.Select(acc => acc.def).ToList();
        }

        public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
        {
            if (installedAccessories == null) yield break;
            var validInstalled = installedAccessories.Where(t => t != null && !t.Destroyed).ToList();

            StringBuilder installedDesc = new StringBuilder();
            List<Dialog_InfoCard.Hyperlink> installedLinks = new List<Dialog_InfoCard.Hyperlink>();
            string valueString;

            if (validInstalled.Count > 0)
            {
                valueString = $"{validInstalled.Count} / {Props.maxAccessories}";
                installedDesc.AppendLine("CMC_InstalledListHeader".Translate());

                foreach (Thing thing in validInstalled)
                {
                    installedDesc.AppendLine($"- {thing.LabelCap}");
                    installedLinks.Add(new Dialog_InfoCard.Hyperlink(thing));
                }
            }
            else
            {
                valueString = $"0 / {Props.maxAccessories}";
                installedDesc.AppendLine("CMC_NoneInstalled".Translate());
            }

            yield return new StatDrawEntry(
                CMC_Def.CMC_WeaponAccessory,
                "CMC_InstalledAccesory".Translate(),
                valueString,
                installedDesc.ToString(),
                100,
                null,
                installedLinks,
                false
            );
        }
    }
}