using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace TOT_DLL_test
{
    public class CompAutoRepair : ThingComp
    {
        public CompProperties_AutoRepair Props
        {
            get
            {
                return (CompProperties_AutoRepair)this.props;
            }
        }
        public override void PostExposeData()
        {
            Scribe_Values.Look<int>(ref this.ticksPassedSinceLastHeal, "ticksPassedSinceLastHeal", 0, false);
        }
        public override void CompTick()
        {
            this.Tick(1);
        }
        public override void CompTickRare()
        {
            this.Tick(250);
        }
        public override void CompTickLong()
        {
            this.Tick(1200);
        }
        public void Tick(int ticks)
        {
            this.ticksPassedSinceLastHeal += ticks;
            if (this.ticksPassedSinceLastHeal >= this.Props.ticksPerHeal)
            {
                this.ticksPassedSinceLastHeal = 0;
                if(parent is Apparel)
                {
                    Apparel thing = (Apparel)this.parent;
                    Pawn pawn = thing.Wearer;
                    if(pawn != null)
                    {   
                        TryHealAllEquipment(pawn);
                    }
                }
            }
        }
        public void TryHealAllEquipment(Pawn pawn)
        {
            Pawn_ApparelTracker apparel = pawn.apparel;
            Pawn_EquipmentTracker equipment = pawn.equipment;
            ThingWithComps weapon = pawn.equipment.Primary;

            List<Apparel> list = (apparel != null) ? apparel.WornApparel : null;
            List<ThingWithComps> list2 = (equipment != null) ? equipment.AllEquipmentListForReading : null;

            if (!list.NullOrEmpty<Apparel>())
            {
                foreach (Apparel thing in list)
                {
                    this.TryRepair(thing);
                }
            }
            if (!list2.NullOrEmpty<ThingWithComps>())
            {
                foreach (ThingWithComps thing in list)
                {
                    this.TryRepair(thing);
                }
            }
            if(weapon != null)
            {
                this.TryRepair(weapon);
            }
        }
        public void TryRepair(Thing thing)
        {
            if (thing.def.useHitPoints)
            {
                if (thing.HitPoints < thing.MaxHitPoints)
                {
                    thing.HitPoints += Mathf.Max(5,Mathf.CeilToInt((float)(thing.MaxHitPoints) * 0.01f));
                    thing.HitPoints = Mathf.Min(thing.HitPoints, thing.MaxHitPoints);
                }
                else if (thing.HitPoints >= thing.MaxHitPoints)
                {
                    thing.HitPoints = thing.MaxHitPoints;
                }
            }
        }
        public int ticksPassedSinceLastHeal;
    }
}
