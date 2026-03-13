using System;
using Verse;

namespace TOT_DLL_test
{
    public class CompProperties_TraderShuttle : CompProperties
    {
        public CompProperties_TraderShuttle()
        {
            this.compClass = typeof(Comp_TraderShuttle);
        }
        public SoundDef soundThud;
        public ThingDef landAnimation;
        public ThingDef takeoffAnimation;
    }
}
