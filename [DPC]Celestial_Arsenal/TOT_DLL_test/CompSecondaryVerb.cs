using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TOT_DLL_test
{
    internal class CompSecondaryVerb : ThingComp
    {
        public CompProperties_SecondaryVerb Props
        {
            get
            {
                return (CompProperties_SecondaryVerb)this.props;
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
            yield return switchSecondaryLauncher;
            yield break;
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<bool>(ref this.isSecondaryVerbSelected, "CMC_useSecondaryVerb", false, false);
            Scribe_Values.Look<int>(ref this.BurstCountPrimarySaved, "BurstCountPrimarySaved", -1, false);
            bool flag = Scribe.mode == LoadSaveMode.PostLoadInit;
            if (flag)
            {
                this.PostAmmoDataLoaded();
            }
        }
        private void SwitchVerb()
        {
            bool flag = !this.IsSecondaryVerbSelected;
            if (flag)
            {
                this.EquipmentSource.PrimaryVerb.verbProps = this.Props.verbProps;
                this.isSecondaryVerbSelected = true;
            }
            else
            {
                this.EquipmentSource.PrimaryVerb.verbProps = this.parent.def.Verbs[0];
                this.isSecondaryVerbSelected = false;
            }
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
        public int BurstCountPrimarySaved = -1;
    }
    internal class CompProperties_SecondaryVerb : CompProperties
    {
        public CompProperties_SecondaryVerb()
        {
            this.compClass = typeof(CompSecondaryVerb);
        }
        public VerbProperties verbProps = new VerbProperties();
        public Verb SecondaryVerb;
        public string mainCommandIcon = "";
        public string mainWeaponLabel = "";
        public string secondaryCommandIcon = "";
        public string secondaryWeaponLabel = "";
        public string description = "";
    }
}