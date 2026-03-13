using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace TOT_DLL_test
{
    public class CompLegendaryWeapons : CompEquippable
    {
        public CompProperties_LegendaryWeapons Props
        {
            get
            {
                return this.props as CompProperties_LegendaryWeapons;
            }
        }
        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            if (Holder != null)
            {
                foreach (Ability i in AbilitysForReading)
                {
                    i.pawn = Holder;
                    i.verb.caster = Holder;
                }
            }
        }
        public override void Notify_Equipped(Pawn pawn)
        {
            foreach (Ability i in AbilitysForReading)
            {
                i.pawn = pawn;
                i.verb.caster = pawn;
                pawn.abilities.GainAbility(i.def);
            }
            this.CodeFor(pawn);
        }
        public override void Notify_Unequipped(Pawn pawn)
        {
            foreach (Ability i in AbilitysForReading)
            {
                pawn.abilities.RemoveAbility(i.def);
            }
        }
        public void CodeFor(Pawn pawn)
        {
            if (!this.Biocodable)
            {
                return;
            }
            this.biocoded = true;
            this.codedPawn = pawn;
            this.codedPawnLabel = pawn.Name.ToStringFull;
            this.OnCodedFor(pawn);
        }
        public override void Notify_KilledPawn(Pawn pawn)
        {
            base.Notify_KilledPawn(pawn);
            this.Killcount++;
            Pawn_PsychicEntropyTracker psychicEntropy = pawn.psychicEntropy;
            if (psychicEntropy == null || this.Props.GivePE == false)
            {
                return;
            }
            psychicEntropy.OffsetPsyfocusDirectly(Mathf.Max(0.5f, 0.07f * pawn.GetPsylinkLevel()));
        }
        public void UnCode()
        {
            this.biocoded = false;
            Pawn pawn = this.CodedPawn;
            this.codedPawn = null;
            this.codedPawnLabel = null;
            this.Killcount = 0;
        }
        public List<Ability> AbilitysForReading
        {
            get
            {
                List<Ability> abilitylist = new List<Ability>();
                foreach(AbilityDef i in this.Props.AbilitieDefs)
                {
                    abilitylist.Add(AbilityUtility.MakeAbility(i, Holder));
                }
                Log.Message("ability" + abilitylist);
                return abilitylist;
            }
        }
        public void ExposeData()
        {
            Scribe_Values.Look<int>(ref this.Killcount, "no. of kills", 0, false);
        }
        public virtual bool Biocodable
        {
            get
            {
                return true;
            }
        }
        public Pawn CodedPawn
        {
            get
            {
                return this.codedPawn;
            }
        }
        protected virtual void OnCodedFor(Pawn p)
        {
        }

        public int Killcount = 0;
        public int TickToCheck;
        protected bool biocoded; 
        protected Pawn codedPawn;
        protected string codedPawnLabel;
    }
}
