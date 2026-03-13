using RimWorld;
using System.Collections.Generic;
using Verse;
using UnityEngine;

namespace TOT_DLL_test
{
    public class VerbProp_Anti_Inv : VerbProperties
    {
        public float MaxRange = 10f;
        public float MaxRange2 = 20f;
    }
    public class Verb_AntiInv : Verb
    {
        VerbProp_Anti_Inv Props => (VerbProp_Anti_Inv)verbProps;
        protected override bool TryCastShot()
        {
            IntVec3 intloc = IntVec3.FromVector3(this.caster.DrawPos);
            IEnumerable<IntVec3> celllist = GenRadial.RadialCellsAround(intloc, this.Props.MaxRange, true);
            foreach (IntVec3 cell in celllist)
            {
                Pawn pawn = cell.GetFirstPawn(this.caster.Map);
                if(pawn != null && pawn.Faction != null && pawn.Faction.HostileTo(this.caster.Faction))
                {
                    List<Hediff> hediffs = new List<Hediff>();
                    pawn.health.hediffSet.GetHediffs(ref hediffs, (Hediff x) => x.TryGetComp<HediffComp_Invisibility>() != null);
                    if(hediffs.Count > 0)
                    {
                        for(int i = 0; i <= hediffs.Count; i++)
                        {
                            pawn.health.RemoveHediff(hediffs[i]);
                        }
                        hediffs.Clear();
                    }
                }
            }
            CompApparelReloadable reloadableCompSource = base.ReloadableCompSource;
            if (reloadableCompSource != null)
            {
                reloadableCompSource.UsedOnce();
            }
            return true;
        }
    }
}
