using RimWorld;
using System;
using Verse;
using Verse.AI.Group;

namespace TOT_DLL_test
{
    public class Verb_DeployMech : Verb
    {
        protected override bool TryCastShot()
        {
            return Deploy(base.ReloadableCompSource);
        }
        public bool Deploy(CompApparelReloadable comp)
        {
            string text;
            if (comp == null || !comp.CanBeUsed(out text))
            {
                return false;
            }
            Pawn wearer = comp.Wearer;
            if (wearer == null)
            {
                return false ;
            }
            Map map = wearer.Map;
            int num = GenRadial.NumCellsInRadius(4f);
            for (int i = 0; i < num; i++)
            {
                IntVec3 intVec = wearer.Position + GenRadial.RadialPattern[i];
                if (intVec.IsValid && intVec.InBounds(map) && intVec.GetFirstPawn(map)== null)
                {
                    Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Mech_Warqueen, wearer.Faction, PawnGenerationContext.NonPlayer, -1, true, false, false, false, true, 1f, true, true, true, false, true, false, false, false, false, 0f, 0f, null, 1f, null, null, null, null, null, new float?(0f), new float?(0f), null, null, null, null, null, false, false, false, false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null, false, false, false, -1, 0, false));
                    Pawn p;
                    Log.Message("1.2");
                    Lord lord = ((p = (wearer as Pawn)) != null) ? p.GetLord() : null;
                    GenPlace.TryPlaceThing(pawn, wearer.Position, wearer.Map, ThingPlaceMode.Near, null, null, default(Rot4));
                    Log.Message("1.3");
                    if (lord != null)
                    {
                        lord.AddPawn(pawn);
                    }
                    comp.UsedOnce();
                    Log.Message("1.4");
                    //if (this.Props.effecterDef != null)
                    //{
                    //    Effecter effecter = new Effecter(this.Props.effecterDef);
                     //   effecter.Trigger(new TargetInfo(pawn.Position, pawn.Map, false), TargetInfo.Invalid, -1);
                     //   effecter.Cleanup();
                    //}
                    return true;
                }
            }
            Messages.Message("AbilityNotEnoughFreeSpace".Translate(), wearer, MessageTypeDefOf.RejectInput, false);
            return false;
        }
    }
}
