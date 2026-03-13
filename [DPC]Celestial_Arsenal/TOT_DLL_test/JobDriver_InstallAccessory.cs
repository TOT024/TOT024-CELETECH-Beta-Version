using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using RimWorld;

namespace TOT_DLL_test
{
    public class JobDriver_InstallAccessory : JobDriver
    {
        private const TargetIndex AccessoryInd = TargetIndex.A;
        private const TargetIndex TurretInd = TargetIndex.B;
        private const int InstallDurationTicks = 600;
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            Pawn pawn = this.pawn;
            LocalTargetInfo targetA = this.job.GetTarget(AccessoryInd);
            LocalTargetInfo targetB = this.job.GetTarget(TurretInd);
            return pawn.Reserve(targetA, this.job, 1, this.job.count, null, errorOnFailed) &&
                   pawn.Reserve(targetB, this.job, 1, -1, null, errorOnFailed);
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(AccessoryInd);
            this.FailOnDestroyedOrNull(TurretInd);
            yield return Toils_Goto.GotoThing(AccessoryInd, PathEndMode.ClosestTouch)
                .FailOnForbidden(AccessoryInd);

            yield return Toils_Haul.StartCarryThing(AccessoryInd);
            yield return Toils_Goto.GotoThing(TurretInd, PathEndMode.Touch)
                .FailOnForbidden(TurretInd);
            Toil install = Toils_General.Wait(InstallDurationTicks)
                .WithProgressBarToilDelay(TurretInd)
                .FailOnDestroyedOrNull(TurretInd)
                .FailOnCannotTouch(TurretInd, PathEndMode.Touch)
                .PlaySustainerOrSound(SoundDefOf.Roof_Start);

            install.initAction = delegate
            {
                pawn.Rotation = Rot4.South;
            };

            install.tickAction = delegate
            {
                if (pawn.carryTracker.CarriedThing == null || pawn.carryTracker.CarriedThing != job.targetA.Thing)
                {
                    this.EndJobWith(JobCondition.Incompletable);
                }
            };
            yield return install;
            yield return new Toil
            {
                initAction = delegate
                {
                    CompAccessoryHolder compHolder = job.targetB.Thing.TryGetComp<CompAccessoryHolder>();
                    Thing accessory = job.targetA.Thing;
                    if (compHolder == null)
                    {
                        this.EndJobWith(JobCondition.Errored);
                        return;
                    }
                    bool success = compHolder.TryInstallAccessory(accessory);
                    if (!success)
                    {
                        pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out Thing droppedThing);
                        this.EndJobWith(JobCondition.Incompletable);
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }
    }
}