using Verse;
using RimWorld;
using System.Collections.Generic;
using Verse.AI;
using System.Linq;

namespace TOT_DLL_test
{
    public class CompProperties_UsableByTurret : CompProperties
    {
        public CompProperties_UsableByTurret()
        {
            this.compClass = typeof(CompUsableByTurret);
        }
    }
    public class CompUsableByTurret : ThingComp
    {
        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            if (!this.parent.Spawned)
            {
                yield break;
            }
            foreach (Thing turret in this.parent.Map.listerBuildings.AllBuildingsColonistOfClass<Building_CMCTurretGun>())
            {
                CompAccessoryHolder compHolder = turret.TryGetComp<CompAccessoryHolder>();
                if (compHolder != null && turret.Faction == Faction.OfPlayer)
                {
                    if (!selPawn.CanReach(turret, PathEndMode.ClosestTouch, Danger.Deadly))
                    {
                        yield return new FloatMenuOption($"Install {this.parent.LabelCap} on {turret.LabelCap} (Cannot reach turret)", null);
                        continue;
                    }
                    if (!compHolder.Props.allowedAccessoryDefs.Contains(this.parent.def.defName))
                    {
                        yield return new FloatMenuOption($"Install {this.parent.LabelCap} on {turret.LabelCap} (Incompatible accessory)", null);
                        continue;
                    }
                    if (compHolder.GetInstalledAccessoriesDefs().Count >= compHolder.Props.maxAccessories)
                    {
                        yield return new FloatMenuOption($"Install {this.parent.LabelCap} on {turret.LabelCap} (Turret is full)", null);
                        continue;
                    }
                    if (!selPawn.CanReserveAndReach(this.parent, PathEndMode.OnCell, Danger.Deadly))
                    {
                        yield return new FloatMenuOption($"Install {this.parent.LabelCap} on {turret.LabelCap} (Cannot reserve accessory)", null);
                        continue;
                    }
                    string label = $"Install {this.parent.LabelCap} on {turret.LabelCap}";
                    yield return new FloatMenuOption(label, delegate
                    {
                        Job job = JobMaker.MakeJob(CMC_Def.InstallTurretAccessory, this.parent, turret);
                        job.count = 1;
                        selPawn.jobs.TryTakeOrderedJob(job, new JobTag?(JobTag.Misc), false);
                    });
                }
            }
        }
    }
}