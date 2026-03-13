using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace TOT_DLL_test
{
    public class CompProperties_PawnEquipmentGizmo : CompProperties
    {
        public CompProperties_PawnEquipmentGizmo()
        {
            this.compClass = typeof(CompPawnEquipmentGizmo);
        }
    }
    public class CompPawnEquipmentGizmo : CompEquippable
    {
        public CompSecondaryVerb_Rework compSecondaryVerb_ReworkInt;
        public CompSecondaryVerb_Rework CompSecondaryVerb_Rework
        {
            get
            {
                if(compSecondaryVerb_ReworkInt == null)
                {
                    compSecondaryVerb_ReworkInt = this.parent.TryGetComp<CompSecondaryVerb_Rework>();
                }
                return compSecondaryVerb_ReworkInt;
            }
        }
        public override IEnumerable<Gizmo> CompGetEquippedGizmosExtra()
        {
            Pawn holder = this.Holder;
            if (holder == null || holder.Faction != Faction.OfPlayer) yield break;
            if (Find.Selector.SingleSelectedThing != holder) yield break;
            if (CompSecondaryVerb_Rework != null)
            {
                foreach (Gizmo g in CompSecondaryVerb_Rework.CompGetGizmosExtra())
                    yield return g;
            }
            CompLaserHeat heat = this.parent.TryGetComp<CompLaserHeat>();
            if (heat != null)
            {
                yield return new Gizmo_LaserHeatStatus { comp = heat };
            }
        }
    }
}
