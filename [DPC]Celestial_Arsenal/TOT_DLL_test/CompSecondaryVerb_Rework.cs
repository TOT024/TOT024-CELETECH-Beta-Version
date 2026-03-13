using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;

namespace TOT_DLL_test
{
    public class CompSecondaryVerb_Rework : ThingComp
    {
        public CompProperties_SecondaryVerb_Rework Props
        {
            get
            {
                return (CompProperties_SecondaryVerb_Rework)this.props;
            }
        }
        public bool IsSecondaryVerbSelected
        {
            get
            {
                return this.isSecondaryVerbSelected;
            }
        }
        private CompEquippable EquipmentSource
        {
            get
            {
                bool flag = this.compEquippableInt != null;
                CompEquippable result;
                if (flag)
                {
                    result = this.compEquippableInt;
                }
                else
                {
                    this.compEquippableInt = this.parent.TryGetComp<CompEquippable>();
                    bool flag2 = this.compEquippableInt == null;
                    if (flag2)
                    {
                        Log.ErrorOnce(this.parent.LabelCap + " has CompSecondaryVerb but no CompEquippable", 50020);
                    }
                    result = this.compEquippableInt;
                }
                return result;
            }
        }
        public Pawn CasterPawn
        {
            get
            {
                return this.Verb.caster as Pawn;
            }
        }
        private Verb Verb
        {
            get
            {
                bool flag = this.verbInt == null;
                if (flag)
                {
                    this.verbInt = this.EquipmentSource.PrimaryVerb;
                }
                return this.verbInt;
            }
        }
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            bool flag = this.CasterPawn != null && !this.CasterPawn.Faction.Equals(Faction.OfPlayer);
            if (flag)
            {
                yield break;
            }
            string commandIcon = this.IsSecondaryVerbSelected ? this.Props.secondaryCommandIcon : this.Props.mainCommandIcon;
            bool flag2 = commandIcon == "";
            if (flag2)
            {
                commandIcon = "UI/Buttons/Reload";
            }
            Command_Action switchSecondaryLauncher = new Command_Action
            {
                action = new Action(this.SwitchVerb),
                defaultLabel = (this.IsSecondaryVerbSelected ? this.Props.secondaryWeaponLabel : this.Props.mainWeaponLabel),
                defaultDesc = this.Props.description,
                icon = ContentFinder<Texture2D>.Get(commandIcon, false)
            };
            Verb currentVerb = this.CasterPawn.CurrentEffectiveVerb;
            if (currentVerb != null && currentVerb.Bursting)
            {
                switchSecondaryLauncher.Disabled = true;
            }
            yield return switchSecondaryLauncher;
            yield break;
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<bool>(ref this.isSecondaryVerbSelected, "CMC_useSecondaryVerb", false, false);
            bool flag = Scribe.mode == LoadSaveMode.PostLoadInit;
            if (flag)
            {
                this.PostAmmoDataLoaded();
            }
        }
        private void SwitchVerb()
        {
            if (!this.IsSecondaryVerbSelected)
            {
                this.EquipmentSource.PrimaryVerb.verbProps = this.Props.verbProps;
                this.isSecondaryVerbSelected = true;
            }
            else
            {
                this.EquipmentSource.PrimaryVerb.verbProps = this.parent.def.Verbs[0];
                this.isSecondaryVerbSelected = false;
            }
            FieldInfo field1 = typeof(Verb).GetField("cachedTicksBetweenBurstShots", BindingFlags.Instance | BindingFlags.NonPublic);
            field1?.SetValue(Verb, null);
            FieldInfo field2 = typeof(Verb).GetField("cachedBurstShotCount", BindingFlags.Instance | BindingFlags.NonPublic);
            field2?.SetValue(Verb, null);
        }
        private void PostAmmoDataLoaded()
        {
            bool flag = this.isSecondaryVerbSelected;
            if (flag)
            {
                this.EquipmentSource.PrimaryVerb.verbProps = this.Props.verbProps;
            }
        }

        private Verb verbInt = null;
        private CompEquippable compEquippableInt;
        private bool isSecondaryVerbSelected;
    }
    public class CompProperties_SecondaryVerb_Rework : CompProperties
    {
        public CompProperties_SecondaryVerb_Rework()
        {
            this.compClass = typeof(CompSecondaryVerb_Rework);
        }
        public string mainCommandIcon = "";
        public string mainWeaponLabel = "";
        public string secondaryCommandIcon = "";
        public string secondaryWeaponLabel = "";
        public string description = "";
        public VerbProperties verbProps = new VerbProperties();
        public override IEnumerable<StatDrawEntry> SpecialDisplayStats(StatRequest req)
        {
            foreach (StatDrawEntry entry in base.SpecialDisplayStats(req))
            {
                yield return entry;
            }
            if (this.verbProps == null)
            {
                yield break;
            }
            StatCategoryDef category = StatCategoryDefOf.Weapon;
            if (this.verbProps.defaultProjectile != null)
            {
                ProjectileProperties projProps = this.verbProps.defaultProjectile.projectile;
                if (projProps != null)
                {
                    int damageAmount = projProps.GetDamageAmount(req.Thing, null);
                    yield return new StatDrawEntry(
                        category,
                        "TOT_AltFire_Damage".Translate(),
                        damageAmount.ToString(),
                        "TOT_AltFire_DamageDesc".Translate(),
                        5900
                    );
                    if (projProps.explosionRadius > 0f)
                    {
                        yield return new StatDrawEntry(
                            category,
                            "TOT_AltFire_ExplosionRadius".Translate(),
                            projProps.explosionRadius.ToString("F1"),
                            "TOT_AltFire_ExplosionRadiusDesc".Translate(),
                            5850
                        );
                    }
                    float ap = projProps.GetArmorPenetration(req.Thing, null);
                    yield return new StatDrawEntry(
                        category,
                        "TOT_AltFire_AP".Translate(),
                        ap.ToStringPercent(),
                        "TOT_AltFire_APDesc".Translate(),
                        5800
                    );
                }
            }
            yield return new StatDrawEntry(
                category,
                "TOT_AltFire_Range".Translate(),
                this.verbProps.range.ToString("F0"),
                "TOT_AltFire_RangeDesc".Translate(),
                5700
            );
            yield return new StatDrawEntry(
                category,
                "TOT_AltFire_Warmup".Translate(),
                this.verbProps.warmupTime.ToString("F2") + " s",
                "TOT_AltFire_WarmupDesc".Translate(),
                5600
            );
            if (this.verbProps.burstShotCount > 1)
            {
                yield return new StatDrawEntry(
                    category,
                    "TOT_AltFire_BurstCount".Translate(),
                    this.verbProps.burstShotCount.ToString(),
                    "TOT_AltFire_BurstCountDesc".Translate(),
                    5500
                );
            }
            if (this.verbProps.defaultCooldownTime > 0)
            {
                yield return new StatDrawEntry(
                category,
                "TOT_AltFire_Cooldown".Translate(),
                this.verbProps.defaultCooldownTime.ToString("F2") + " s",
                "TOT_AltFire_CooldownDesc".Translate(),
                5400
            );
            }
        }
    }
}