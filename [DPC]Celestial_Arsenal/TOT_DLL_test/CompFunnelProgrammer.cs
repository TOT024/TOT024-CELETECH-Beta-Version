using RimWorld;
using System.Collections.Generic;
using System.Linq;
using TOT_DLL_test;
using Verse;
using Verse.AI;

namespace TOT_DLL_test
{
    public class CompProperties_FunnelProgrammer : CompProperties
    {
        public CompProperties_FunnelProgrammer()
        {
            compClass = typeof(CompFunnelProgrammer);
        }
    }
    public class CompFunnelProgrammer : ThingComp
    {
        private CompPowerTrader powerComp;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            powerComp = parent.GetComp<CompPowerTrader>();
        }
        public bool CanUseNow(Pawn selPawn, out string reason)
        {
            reason = null;
            if (!selPawn.CanReserve(parent, 1, -1, null, false))
            {
                reason = "CannotReserve".Translate();
                return false;
            }

            if (!selPawn.CanReach(parent, PathEndMode.InteractionCell, Danger.None, false, false, TraverseMode.ByPawn))
            {
                reason = "CannotReach".Translate();
                return false;
            }
            if (powerComp != null && !powerComp.PowerOn)
            {
                reason = "NoPower".Translate();
                return false;
            }
            Apparel backpack = selPawn.apparel?.WornApparel?.FirstOrDefault(a => a.GetComp<CompFunnelHauler>() != null);
            if (backpack == null)
            {
                reason = "NoFunnelBackpackEquipped".Translate();
                return false;
            }
            var haulerComp = backpack.GetComp<CompFunnelHauler>();
            if (haulerComp == null)
            {
                reason = "NoFunnelComp".Translate();
                return false;
            }
            return true;
        }
        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            bool canUse = CanUseNow(selPawn, out string cannotUseReason);
            if (canUse)
            {
                string label = "ProgramFunnels".Translate();
                yield return new FloatMenuOption(label, delegate ()
                {
                    Job job = JobMaker.MakeJob(CMC_Def.CMC_ChangeFunnelConfig, this.parent);
                    selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                });
                yield break;
            }
            else
            {
                string disabledLabel = "ProgramFunnels".Translate() + " (" + cannotUseReason + ")";
                yield return new FloatMenuOption(disabledLabel, null, MenuOptionPriority.DisabledOption, null, null, 0f, null, null, true, 0);
            }
        }
    }
}