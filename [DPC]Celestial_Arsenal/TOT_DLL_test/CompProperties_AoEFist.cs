using RimWorld;
using Verse;

namespace TOT_DLL_test
{
    public class CompProperties_AoEFist : CompProperties_AbilityEffect
    {
        public CompProperties_AoEFist()
        {
            this.compClass = typeof(CompProperties_AoEFist);
        }

        public float range;
        public float lineWidthEnd = 13;
        public FleckDef SpawnFleck;
        public int Fleck_Num = 11;
    }
}
