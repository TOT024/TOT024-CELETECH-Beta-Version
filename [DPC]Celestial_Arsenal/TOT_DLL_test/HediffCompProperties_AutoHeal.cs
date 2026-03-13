using Verse;
using System;

namespace TOT_DLL_test
{
    public class HediffCompProperties_AutoHeal : HediffCompProperties
    {
        public HediffCompProperties_AutoHeal()
        {
            this.compClass = typeof(HediffComp_AutoHeal);
        }
    }
}
