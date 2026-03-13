using RimWorld;
using System.Collections.Generic;
using Verse;


namespace TOT_DLL_test
{
    public class CompProperties_LegendaryWeapons : CompProperties
    {
        public CompProperties_LegendaryWeapons()
        {
            this.compClass = typeof(CompLegendaryWeapons);
        }
        public bool biocodeOnEquip = true;
        public List<AbilityDef> AbilitieDefs;
        public bool GivePE = true;
    }
}
