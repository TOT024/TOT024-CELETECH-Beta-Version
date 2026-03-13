using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace TOT_DLL_test
{
    public class Landed_CMCTS : TradeShip, ITrader
    {
        public Landed_CMCTS()
        {
        }
        public Landed_CMCTS(Map map, TraderKindDef def, Faction faction = null) : base(def, faction)
        {
            this.map = map;
            this.passingShipManager = map.passingShipManager;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<Map>(ref this.map, "map", false);
            Scribe_Values.Look<int>(ref this.iniSilver, "initsilver", 0, false);
        }
        public override void PassingShipTick()
        {
            base.PassingShipTick();
            bool flag = this.passingShipManager == null;
            if (flag)
            {
                this.passingShipManager = this.map.passingShipManager;
            }
        }
        public override void Depart()
        {
        }
        public new void GiveSoldThingToPlayer(Thing toGive, int countToGive, Pawn playerNegotiator)
        {
            Thing thing = toGive.SplitOff(countToGive);
            thing.PreTraded(TradeAction.PlayerBuys, playerNegotiator, this);
            bool flag = !GenPlace.TryPlaceThing(thing, playerNegotiator.Position, base.Map, ThingPlaceMode.Near, null, null, default(Rot4));
            if (flag)
            {
                Log.Error(string.Concat(new object[]
                {
                    "Could not place bought thing ",
                    thing,
                    " at ",
                    playerNegotiator.Position
                }));
                thing.Destroy(DestroyMode.Vanish);
            }
        }
        public bool ReachableForTrade(Pawn pawn, Thing thing)
        {
            return pawn.Map == thing.Map && pawn.Map.reachability.CanReach(pawn.Position, thing, PathEndMode.Touch, TraverseMode.PassDoors, Danger.Some);
        }
        public new IEnumerable<Thing> ColonyThingsWillingToBuy(Pawn playerNegotiator)
        {
            foreach (Thing thing in TradeUtility.AllLaunchableThingsForTrade(base.Map, this))
            {
                yield return thing;
            }
            foreach (Pawn pawn in TradeUtility.AllSellableColonyPawns(base.Map, false))
            {
                yield return pawn;
            }
            yield break;
        }
        private Map map;
        public int iniSilver = 0;
    }
}
