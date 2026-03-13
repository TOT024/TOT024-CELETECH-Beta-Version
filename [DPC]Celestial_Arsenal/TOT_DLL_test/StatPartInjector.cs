using System.Collections.Generic;
using Verse;
using RimWorld;

namespace TOT_DLL_test
{
    [StaticConstructorOnStartup]
    public static class StatPartInjector
    {
        static StatPartInjector()
        {
            InjectStatParts();
        }
        private static void InjectStatParts()
        {
            HashSet<StatDef> relevantStats = new HashSet<StatDef>();
            foreach (var thingDef in DefDatabase<ThingDef>.AllDefs)
            {
                var compProps = thingDef.GetCompProperties<CompProperties_AccessoryStats>();
                if (compProps?.stats != null)
                {
                    foreach (var modifier in compProps.stats)
                    {
                        if (modifier.stat != null)
                        {
                            relevantStats.Add(modifier.stat);
                        }
                    }
                }
            }
            foreach (var stat in relevantStats)
            {
                if (stat.parts == null) stat.parts = new List<StatPart>();
                bool alreadyHas = false;
                foreach (var part in stat.parts)
                {
                    if (part is StatPart_AccessoryModifiers)
                    {
                        alreadyHas = true;
                        break;
                    }
                }

                if (!alreadyHas)
                {
                    var part = new StatPart_AccessoryModifiers();
                    part.parentStat = stat;
                    stat.parts.Add(part);
                }
            }

            Log.Message($"[TOT] Successfully injected accessory modifiers into {relevantStats.Count} stats.");
        }
    }
}