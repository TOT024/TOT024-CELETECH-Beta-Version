using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MYDE_CMC_Dll
{
    public class ThoughtWorker_CMC_DECO_WIFI : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            bool flag = !p.Spawned;
            ThoughtState result;
            if (flag)
            {
                result = false;
            }
            else
            {
                List<Thing> list = p.Map.listerThings.ThingsOfDef(MYDE_ThingDefOf.CMC_DECO_WIFI);
                for (int i = 0; i < list.Count; i++)
                {
                    CompPowerTrader compPowerTrader = list[i].TryGetComp<CompPowerTrader>();
                    bool flag2 = (compPowerTrader == null || compPowerTrader.PowerOn) && p.Position.InHorDistOf(list[i].Position, 10f);
                    if (flag2)
                    {
                        return true;
                    }
                }
                result = false;
            }
            return result;
        }
        private const float Radius = 10f;
    }
}
