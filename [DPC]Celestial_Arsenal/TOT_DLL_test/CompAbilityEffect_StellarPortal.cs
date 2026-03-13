using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using System.Linq;
namespace TOT_DLL_test
{
    public class CompAbilityEffect_StellarPortal : CompAbilityEffect
    {
        public new CompProperties_AbilityStellarPortal Props
        {
            get
            {
                return (CompProperties_AbilityStellarPortal)this.props;
            }
        }
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Map map = this.parent.pawn.Map;
            named = DefDatabase<ThingDef>.GetNamed("CMC_StellarPortal", true);
            
            List<Thing> list = new List<Thing>();
            list.AddRange(this.AffectedCells(target, map).SelectMany((IntVec3 c) => from t in c.GetThingList(map)
                                                                                          where t.def.category == ThingCategory.Item
                                                                                          select t));
            foreach (Thing thing in list)
            {
                thing.DeSpawn(DestroyMode.Vanish);
            }
            
            foreach (IntVec3 loc in this.AffectedCells(target, map))
            {
                if(named != null)
                {
                    Thing thingtomake = ThingMaker.MakeThing(named, null);
                    thingtomake.SetFaction(this.parent.pawn.Faction, null);
                    GenSpawn.Spawn(thingtomake, loc, map, WipeMode.Vanish); 
                    FleckMaker.ThrowDustPuffThick(loc.ToVector3Shifted(), map, Rand.Range(1.5f, 3f), CompAbilityEffect_StellarPortal.DustColor);
                }
            }
            foreach (Thing thing2 in list)
            {
                IntVec3 intVec = IntVec3.Invalid;
                for (int j = 0; j < 9; j++)
                {
                    IntVec3 intVec2 = thing2.Position + GenRadial.RadialPattern[j];
                    bool flag = intVec2.InBounds(map) && intVec2.Walkable(map) && map.thingGrid.ThingsListAtFast(intVec2).Count <= 0;
                    if (flag)
                    {
                        intVec = intVec2;
                        break;
                    }
                }
                bool flag2 = intVec != IntVec3.Invalid;
                if (flag2)
                {
                    GenSpawn.Spawn(thing2, intVec, map, WipeMode.Vanish);
                }
                else
                {
                    GenPlace.TryPlaceThing(thing2, thing2.Position, map, ThingPlaceMode.Near, null, null, default(Rot4));
                }
            }
        }

        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            return this.Valid(target, true);
        }

        public override void DrawEffectPreview(LocalTargetInfo target)
        {
            GenDraw.DrawFieldEdges(this.AffectedCells(target, this.parent.pawn.Map).ToList<IntVec3>(), this.Valid(target, false) ? Color.white : Color.red, null);
        }

        private IEnumerable<IntVec3> AffectedCells(LocalTargetInfo target, Map map)
        {
            foreach (IntVec2 intVec in this.Props.pattern)
            {
                IntVec3 intVec2 = target.Cell + new IntVec3(intVec.x, 0, intVec.z);
                if (intVec2.InBounds(map))
                {
                    yield return intVec2;
                }
            }
            yield break;
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (this.AffectedCells(target, this.parent.pawn.Map).Any((IntVec3 c) => c.Filled(this.parent.pawn.Map)))
            {
                if (throwMessages)
                {
                    Messages.Message("CannotUseAbility".Translate(this.parent.def.label) + ": " + "AbilityOccupiedCells".Translate(), target.ToTargetInfo(this.parent.pawn.Map), MessageTypeDefOf.RejectInput, false);
                }
                return false;
            }
            if (this.AffectedCells(target, this.parent.pawn.Map).Any((IntVec3 c) => !c.Standable(this.parent.pawn.Map)))
            {
                if (throwMessages)
                {
                    Messages.Message("CannotUseAbility".Translate(this.parent.def.label) + ": " + "AbilityUnwalkable".Translate(), target.ToTargetInfo(this.parent.pawn.Map), MessageTypeDefOf.RejectInput, false);
                }
                return false;
            }
            return true;
        }

        public static Color DustColor = new Color(0.55f, 0.55f, 0.55f, 4f);
        public static ThingDef named;
    }
}
