using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace TOT_DLL_test
{
    public class StockGenerator_CMCClones : StockGenerator
    {
        public override bool HandlesThingDef(ThingDef thingDef)
        {
            return thingDef.category == ThingCategory.Pawn && thingDef.race.Humanlike && thingDef.tradeability > Tradeability.None;
        }
        public override IEnumerable<Thing> GenerateThings(PlanetTile forTile, Faction faction = null)
        {
            if (this.respectPopulationIntent && Rand.Value > StorytellerUtilityPopulation.PopulationIntent)
            {
                yield break;
            }
            if (faction != null && faction.ideos != null)
            {
                bool flag = true;
                foreach (Ideo ideo in faction.ideos.AllIdeos)
                {
                    if (!ideo.IdeoApprovesOfSlavery())
                    {
                        flag = false;
                        break;
                    }
                }
                if (!flag)
                {
                    yield break;
                }
            }
            int count = this.countRange.RandomInRange;
            for (int i = 0; i < count; i++)
            {
                Faction faction2;
                if (!(from fac in Find.FactionManager.AllFactionsVisible
                      where fac != Faction.OfPlayer && fac.def.humanlikeFaction && !fac.temporary
                      select fac).TryRandomElement(out faction2))
                {
                    yield break;
                }
                PawnKindDef kindToGenerate = PawnKindDefOf.Colonist;
                PawnGenerationRequest request = new PawnGenerationRequest(
                    kind: kindToGenerate,
                    faction: faction2,
                    context: PawnGenerationContext.NonPlayer,
                    tile: forTile,
                    forceGenerateNewPawn: true,
                    allowDead: false,
                    allowDowned: false,
                    canGeneratePawnRelations: false,
                    mustBeCapableOfViolence: true,
                    colonistRelationChanceFactor: 0f,
                    forceAddFreeWarmLayerIfNeeded: !this.trader.orbital,
                    allowGay: true,
                    allowAddictions: false,
                    allowPregnant: false,
                    fixedChronologicalAge: Rand.Range(114f, 514f), 
                    fixedBiologicalAge: Rand.Range(18f, 25f),    
                    forceNoGear: true,
                    forceNoIdeo: true,
                    forceBaselinerChance: 1f,
                    forceRecruitable: true
                );
                Pawn pawn = PawnGenerator.GeneratePawn(request);
                if((pawn.story.bodyType == BodyTypeDefOf.Hulk || pawn.story.bodyType == BodyTypeDefOf.Fat)&& pawn.gender == Gender.Female)
                {
                    pawn.story.bodyType = BodyTypeDefOf.Female;
                }
                if ((pawn.story.bodyType == BodyTypeDefOf.Hulk || pawn.story.bodyType == BodyTypeDefOf.Fat) && pawn.gender == Gender.Male)
                {
                    pawn.story.bodyType = BodyTypeDefOf.Male;
                }
                int chance = Rand.RangeInclusive(0,100);
                if(chance > 98)
                {
                    if(ModLister.RoyaltyInstalled)
                    {
                        pawn.ChangePsylinkLevel(1,false);
                    }
                    AddImplant(pawn, DefDatabase<HediffDef>.GetNamed("TianQuan", false), DefDatabase<BodyPartDef>.GetNamed("Brain", false));
                    TraitDef specificTrait = DefDatabase<TraitDef>.GetNamed("CMC_CloneTraitLeng", false);
                    if (!pawn.story.traits.HasTrait(specificTrait))
                    {
                        pawn.story.traits.GainTrait(new Trait(specificTrait, 0), true);
                    }
                    TraitDef specificTrait2 = DefDatabase<TraitDef>.GetNamed("ShootingAccuracy", false);
                    if (!pawn.story.traits.HasTrait(specificTrait2))
                    {
                        pawn.story.traits.GainTrait(new Trait(specificTrait2, -1), true);
                    }
                    TraitDef specificTrait3 = DefDatabase<TraitDef>.GetNamed("Tough", false);
                    if (!pawn.story.traits.HasTrait(specificTrait3))
                    {
                        pawn.story.traits.GainTrait(new Trait(specificTrait3, 0, true), true);
                    }
                }
                else if(chance > 78)
                {
                    AddImplant(pawn, DefDatabase<HediffDef>.GetNamed("TianQuan", false), DefDatabase<BodyPartDef>.GetNamed("Brain", false));
                    TraitDef specificTrait = DefDatabase<TraitDef>.GetNamed("CMC_CloneTraitEpic", false);
                    if (!pawn.story.traits.HasTrait(specificTrait))
                    {
                        pawn.story.traits.GainTrait(new Trait(specificTrait, 0), true);
                    }
                }
                else if(chance > 28)
                {
                    TraitDef specificTrait = DefDatabase<TraitDef>.GetNamed("CMC_CloneTraitRare", false);
                    if (!pawn.story.traits.HasTrait(specificTrait))
                    {
                        pawn.story.traits.GainTrait(new Trait(specificTrait, 0), true);
                    }
                }
                else
                {
                    TraitDef specificTrait = DefDatabase<TraitDef>.GetNamed("CMC_CloneTraitCommon", false);
                    if (!pawn.story.traits.HasTrait(specificTrait))
                    {
                        pawn.story.traits.GainTrait(new Trait(specificTrait, 0), true);
                    }
                }
                pawn.guest.joinStatus = JoinStatus.JoinAsColonist;
                yield return pawn;
            }
        }
        private void AddImplant(Pawn pawn, HediffDef hediffDef, BodyPartDef partDef)
        {
            if (hediffDef == null || partDef == null) return;
            BodyPartRecord part = pawn.RaceProps.body.AllParts.FirstOrDefault(x => x.def == partDef);

            if (part != null)
            {
                if (!pawn.health.hediffSet.PartIsMissing(part))
                {
                    pawn.health.AddHediff(hediffDef, part);
                }
            }
        }
        private bool respectPopulationIntent;
        //public PawnKindDef slaveKindDef;
    }
}