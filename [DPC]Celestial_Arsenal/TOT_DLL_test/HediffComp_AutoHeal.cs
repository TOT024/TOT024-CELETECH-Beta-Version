using System.Collections.Generic;
using Verse;

namespace TOT_DLL_test
{
    public class HediffComp_AutoHeal : HediffComp
    {
        public HediffCompProperties_AutoHeal Props
        {
            get
            {
                return (HediffCompProperties_AutoHeal)this.props;
            }
        }
        private bool CanHeal
        {
            get
            {
                return base.Pawn.health.hediffSet.hediffs.Find(delegate (Hediff x)
                {
                    Hediff_Injury hediff_Injury = x as Hediff_Injury;
                    return hediff_Injury != null && (hediff_Injury.CanHealNaturally() || hediff_Injury.CanHealFromTending());
                }) != null;
            }
        }
        public override void CompPostTick(ref float severityAdjustment)
        {
            tickcount++;
            Pawn pawn = base.Pawn;
            if (tickcount % 360 == 0)
            {
                if (pawn != null && !pawn.Dead)
                {
                    if(CanHeal)
                    {
                        this.TryHealInjury(pawn);
                    }
                }
            }
        }
        public void TryHealInjury(Pawn pawn)
        {
            flag = false;
            List<Hediff_Injury> wounds = new List<Hediff_Injury>();
            pawn.health.hediffSet.GetHediffs(ref wounds, (Hediff_Injury x) => x.CanHealNaturally() || x.CanHealFromTending());
            Hediff_Injury hediff_Injury;
            if (wounds.TryRandomElement(out hediff_Injury))
            {
                hediff_Injury.Heal(20.0f);
                Log.Message("Healed");
                flag = true;
            }
            if(flag == true)
            {
                HediffDef named = DefDatabase<HediffDef>.GetNamed("CMC_HealingSE", true);
                Hediff hedifftogive = HediffMaker.MakeHediff(named, base.Pawn, null);
                hedifftogive.TryGetComp<HediffComp_Disappears>().ticksToDisappear = 300;
                pawn.health.AddHediff(hedifftogive, null, null, null);
            }
        }
        public int tickcount;
        public bool flag = false;
    }
}
