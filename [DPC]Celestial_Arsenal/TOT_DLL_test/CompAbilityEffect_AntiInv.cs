using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TOT_DLL_test
{
    public class CompAbilityEffect_AntiInv : CompAbilityEffect
    {
        private new CompProperties_AntiInv Props
        {
            get
            {
                return (CompProperties_AntiInv)this.props;
            }
        }
        private IntVec3 Centre
        {
            get
            {
                return IntVec3.FromVector3(this.parent.pawn.DrawPos);
            }
        }
        public override void OnGizmoUpdate()
        {
            GenDraw.DrawRadiusRing(Centre, this.Props.SpotRange, new Color(51f/255f, 153f/255f, 255f/255f, 1f));
            GenDraw.DrawRadiusRing(Centre, this.Props.DetectRange, new Color(51f/255f, 153f/255f, 255f/255f, 0.5f));
        }
        public override IEnumerable<PreCastAction> GetPreCastActions()
        {
            yield return new PreCastAction
            {
                action = delegate (LocalTargetInfo t, LocalTargetInfo d)
                {
                    Pawn pawn = base.parent.pawn;
                },
                ticksAwayFromCast = 5
            };
            yield break;
        }
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            target = this.parent.pawn;
            Spot();
            base.Apply(target, dest);
        }
        private void Spot()
        {
            IntVec3 intloc = Centre;
            IEnumerable<IntVec3> celllist = GenRadial.RadialCellsAround(intloc, this.Props.SpotRange, true);
            foreach (IntVec3 cell in celllist)
            {
                Pawn pawn = cell.GetFirstPawn(this.parent.pawn.Map);
                if (pawn != null && pawn.Faction != null && pawn.Faction.HostileTo(this.parent.pawn.Faction))
                {
                    List<Hediff> hediffs = new List<Hediff>();
                    pawn.health.hediffSet.GetHediffs(ref hediffs, (Hediff x) => x.TryGetComp<HediffComp_Invisibility>() != null);
                    if (hediffs.Count > 0)
                    {
                        for (int i = 0; i <= hediffs.Count; i++)
                        {
                            pawn.health.RemoveHediff(hediffs[i]);
                        }
                    }
                }
            }
            return;
        }
    }
}
