using System.Collections.Generic;
using Verse;

namespace TOT_DLL_test
{
    public class CompProperties_AutoRepair : CompProperties
    {
        public CompProperties_AutoRepair()
        {
            this.compClass = typeof(CompAutoRepair);
        }
        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            foreach (string text in base.ConfigErrors(parentDef))
            {
                yield return text;
            }
            if (parentDef.tickerType == TickerType.Rare && this.ticksPerHeal % 250 != 0)
            {
                yield return "TickerType is set to Rare, but ticksPerHeal value is not multiple of " + 250;
            }
            if (parentDef.tickerType == TickerType.Long && this.ticksPerHeal % 1200 != 0)
            {
                yield return "TickerType is set to Long, but ticksPerHeal value is not multiple of " + 2000;
            }
            if (parentDef.tickerType == TickerType.Never)
            {
                yield return "has CompSelfhealHitpoints, but its TickerType is set to Never";
            }
            yield break;
        }
        public int ticksPerHeal;
    }
}
