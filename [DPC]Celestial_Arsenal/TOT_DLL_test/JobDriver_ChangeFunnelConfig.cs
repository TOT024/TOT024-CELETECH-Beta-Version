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
    internal class JobDriver_ChageFunnelConf : JobDriver
    {
        private ThingWithComps ConfigBuilding
        {
            get
            {
                return base.TargetThingA as ThingWithComps;
            }
        }
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            LocalTargetInfo target = ConfigBuilding;
            return pawn.Reserve(target, job, 1, -1, null, errorOnFailed);
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            CompFunnelProgrammer comp = ConfigBuilding.TryGetComp<CompFunnelProgrammer>();
            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOn(() => comp == null);
            this.FailOn(() => !comp.CanUseNow(pawn, out _));
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
            yield return Toils_General.Wait(60, TargetIndex.A).WithProgressBarToilDelay(TargetIndex.A);
            Toil openConfigToil = new Toil();
            openConfigToil.initAction = delegate ()
            {
                Pawn actor = openConfigToil.actor;
                Apparel backpack = actor.apparel?.WornApparel?.FirstOrDefault(a => a.GetComp<CompFunnelHauler>() != null);
                if (backpack != null)
                {
                    var haulerComp = backpack.TryGetComp<CompFunnelHauler>();
                    if (haulerComp != null)
                    {
                        Find.WindowStack.Add(new Dialog_ConfigureFunnels(haulerComp));
                    }
                    else
                    {
                        Log.Error("Failed to get CompFunnelHauler from backpack");
                    }
                }
                else
                {
                    Log.Error("Pawn does not have a funnel backpack equipped");
                }
            };
            openConfigToil.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return openConfigToil;
        }
    }
}