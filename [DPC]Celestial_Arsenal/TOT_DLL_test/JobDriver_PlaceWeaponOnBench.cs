using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace TOT_DLL_test
{
    public class JobDriver_PlaceCarriedWeaponOnBench : JobDriver
    {
        private ThingWithComps WeaponToPlace => job.GetTarget(TargetIndex.A).Thing as ThingWithComps;
        private Building Bench => job.GetTarget(TargetIndex.B).Thing as Building;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Bench, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.B);
            this.FailOn(() => !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation));
            yield return Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.InteractionCell);
            var placeWeapon = new Toil();
            placeWeapon.initAction = () =>
            {
                var holderComp = Bench.TryGetComp<CompWeaponHolder>();
                if (holderComp == null || holderComp.IsOccupied)
                {
                    pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                    return;
                }

                if (pawn.equipment.Contains(WeaponToPlace))
                {
                    pawn.equipment.TryDropEquipment(WeaponToPlace, out ThingWithComps droppedWeapon, pawn.Position, false);
                    holderComp.InstallWeapon(droppedWeapon);
                }
                else if (pawn.inventory.Contains(WeaponToPlace))
                {
                    pawn.inventory.innerContainer.TryDrop(WeaponToPlace, pawn.Position, pawn.Map, ThingPlaceMode.Direct, out Thing droppedWeapon);
                    holderComp.InstallWeapon(droppedWeapon);
                }
                else
                {
                    pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                }
            };
            placeWeapon.defaultCompleteMode = ToilCompleteMode.Instant;

            yield return placeWeapon;
        }
    }
}