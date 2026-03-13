using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace TOT_DLL_test
{
    internal class JobDriver_TradewithCMCTS : JobDriver
    {
        private ThingWithComps Trader
        {
            get
            {
                return base.TargetThingA as ThingWithComps;
            }
        }
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return this.pawn.Reserve(this.Trader, this.job, 1, -1, null, errorOnFailed, false);
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            Comp_TraderShuttle comp = this.Trader.TryGetComp<Comp_TraderShuttle>();
            this.FailOnDespawnedOrNull(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell, false).FailOn(() => comp == null || !comp.tradeShip.CanTradeNow);
            Toil trade = new Toil();
            trade.initAction = delegate ()
            {
                Pawn actor = trade.actor;
                bool canTradeNow = comp.tradeShip.CanTradeNow;
                if (canTradeNow)
                {
                    Find.WindowStack.Add(new Dialog_Trade(actor, comp.tradeShip, false));
                }
            };
            yield return trade;
            yield break;
        }
    }
}
