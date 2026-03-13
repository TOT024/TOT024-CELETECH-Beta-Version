using RimWorld;
using Verse;
using Verse.AI;

namespace TOT_DLL_test
{
    public class HediffComp_AutoStopMentalState : HediffComp
    {
        public HediffCompProperties_AutoStopMentalState Props
        {
            get
            {
                return (HediffCompProperties_AutoStopMentalState)this.props;
            }
        }
        public override void CompPostTick(ref float severityAdjustment)
        {
            tickcount++;
            Pawn pawn = base.Pawn;
            if(pawn != null)
            {
                if (tickcount % 120 == 0)
                {
                    Hediff firstHediffOfDef = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.CatatonicBreakdown, false);
                    if (firstHediffOfDef != null)
                    {
                        pawn.health.RemoveHediff(firstHediffOfDef);
                    }
                    if (pawn != null)
                    {
                        MentalState mentalState = pawn.MentalState;
                        if (mentalState != null)
                        {
                            mentalState.RecoverFromState();
                            HediffDef named = DefDatabase<HediffDef>.GetNamed("CMC_HealingSE", true);
                            if(named != null)
                            {
                                Hediff hedifftogive = HediffMaker.MakeHediff(named, base.Pawn, null);
                                hedifftogive.TryGetComp<HediffComp_Disappears>().ticksToDisappear = 300;
                                pawn.health.AddHediff(hedifftogive, null, null, null);
                            }
                        }
                    }
                }
            }
        }
        public int tickcount;
    }
}
