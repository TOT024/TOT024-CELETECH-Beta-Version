using RimWorld;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Noise;
namespace TOT_DLL_test
{
    [StaticConstructorOnStartup]
    public class SpawnTradeShip
    {
        public bool SpawnShip(string shipDefName)
        {
            Map map = Find.CurrentMap;
            Map anyPlayerHomeMap = Find.AnyPlayerHomeMap;
            Thing ship = MakeTraderShip(map, shipDefName);
            if(ship != null) 
            {
                return this.LandShip(map, ship);
            }
            return false;
        }
        private static Thing MakeTraderShip(Map map, string ShipName)
        {
            Thing thing = ThingMaker.MakeThing(CMC_Def.CMC_TraderShuttle, null);
            TradeShip tradeShip = new TradeShip(DefDatabase<TraderKindDef>.GetNamed(ShipName, true), null);
            tradeShip.name = "CMC_TradeShipName".Translate();
            if (tradeShip == null)
            {
                throw new InvalidOperationException();
            }
            Comp_TraderShuttle compShip = thing.TryGetComp<Comp_TraderShuttle>();
            compShip.GenerateInternalTradeShip(map, tradeShip.def);
            return thing;
        }
        private bool UsableLZ(Building buildingTT, out Thing blocker)
        {
            blocker = null;
            foreach (IntVec3 cell in buildingTT.OccupiedRect())
            {
                List<Thing> thingList = cell.GetThingList(Find.CurrentMap);
                foreach (Thing thing in thingList)
                {
                    if (thing is Skyfaller)
                    {
                        blocker = thing;
                        return false;
                    }
                    if (thing.def.IsBlueprint || thing.def.IsFrame)
                    {
                        blocker = thing;
                        return false;
                    }
                    if (thing is Pawn || thing.def.Fillage == FillCategory.None)
                    {
                        continue;
                    }
                    blocker = thing;
                    return false;
                }
            }
            return true; 
        }
        public virtual bool LandShip(Map map, Thing ship)
        {
            IntVec3 landingPos = IntVec3.Invalid;
            Comp_TraderShuttle compShip = ship.TryGetComp<Comp_TraderShuttle>();
            List<Thing> platforms = map.listerThings.ThingsOfDef(CMC_Def.CMC_LandPlatform);
            List<Building> validPlatforms = new List<Building>();

            if (platforms != null && platforms.Count > 0)
            {
                foreach (Thing t in platforms)
                {
                    Building building = t as Building;
                    CompPowerTrader compPower = t.TryGetComp<CompPowerTrader>();
                    if (compPower != null && compPower.PowerOn)
                    {
                        Thing dummyBlocker;
                        if (this.UsableLZ(building, out dummyBlocker))
                        {
                            validPlatforms.Add(building);
                        }
                    }
                }
            }
            if (validPlatforms.Count > 0)
            {
                Building selectedPlatform = validPlatforms.RandomElement();
                landingPos = selectedPlatform.Position;
                // building_LandingPlatform = selectedPlatform as Building_LandingPlatform;
            }
            if (landingPos.IsValid)
            {
                Messages.Message("Message_CMC_TraderLanded".Translate(), ship, MessageTypeDefOf.PositiveEvent);
                Skyfaller skyfaller = SkyfallerMaker.MakeSkyfaller(compShip.Props.landAnimation, ship);
                GenSpawn.Spawn(skyfaller, landingPos, map);
                return true;
            }
            else
            {
                Messages.Message("Message_CMC_TraderCantLanded".Translate(), ship, MessageTypeDefOf.NeutralEvent);
                return false;
            }
        }
        public static bool FindAnyLandingSpot(out IntVec3 spot, Faction faction, Map map, IntVec2? size)
        {
            bool flag = !DropCellFinder.FindSafeLandingSpot(out spot, faction, map, 0, 15, 25, size, null);
            if (flag)
            {
                IntVec3 intVec = DropCellFinder.RandomDropSpot(map, true);
                bool flag2 = !DropCellFinder.TryFindDropSpotNear(intVec, map, out spot, false, false, false, size, true);
                if (flag2)
                {
                    spot = intVec;
                }
            }
            return true;
        }
        public static void FindCloseLandingSpot(out IntVec3 spot, Faction faction, Map map, IntVec2? size)
        {
            IntVec3 intVec = default(IntVec3);
            int num = 0;
            foreach (Building building in from x in map.listerBuildings.allBuildingsColonist
                                          where x.def.size.x > 1 || x.def.size.z > 1
                                          select x)
            {
                intVec += building.Position;
                num++;
            }
            bool flag = num == 0;
            if (flag)
            {
                FindAnyLandingSpot(out spot, faction, map, size);
            }
            else
            {
                intVec.x /= num;
                intVec.z /= num;
                int num2 = 20;
                float num3 = 999999f;
                spot = default(IntVec3);
                for (int i = 0; i < num2; i++)
                {
                    IntVec3 intVec2;
                    FindAnyLandingSpot(out intVec2, faction, map, size);
                    bool flag2 = (float)(intVec2 - intVec).LengthManhattan < num3;
                    if (flag2)
                    {
                        num3 = (float)(intVec2 - intVec).LengthManhattan;
                        spot = intVec2;
                    }
                }
            }
        }
    }
}
