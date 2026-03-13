using RimWorld;
using Verse;

namespace TOT_DLL_test
{
    public class Comp_BodyshapeAjuster : ThingComp
    {
        public CompProperties_BodyShapeAjuster Props
        {
            get
            {
                return (CompProperties_BodyShapeAjuster)this.props;
            }
        }
        public override void Notify_Equipped(Pawn pawn)
        {
            base.Notify_Equipped(pawn);
            if (pawn.story.bodyType == BodyTypeDefOf.Hulk || pawn.story.bodyType == BodyTypeDefOf.Fat)
            {
                BodyShape = pawn.story.bodyType;
                ChangedBS = true;
                if(pawn.gender is Gender.Male)
                {
                    pawn.story.bodyType = BodyTypeDefOf.Male;
                }
                else
                {
                    pawn.story.bodyType = BodyTypeDefOf.Female;
                }
            }
        }
        public override void Notify_Unequipped(Pawn pawn)
        {
            base.Notify_Unequipped(pawn);
            if(ChangedBS == true)
            {
                pawn.story.bodyType = BodyShape;
                ChangedBS = false;
            }
        }
        public void ExposeData()
        {
            Scribe_Values.Look<BodyTypeDef>(ref this.BodyShape, "original bodytype", BodyTypeDefOf.Thin, false);
            Scribe_Values.Look<bool>(ref this.ChangedBS, "if bodytype changed", false, false);
        }
        private BodyTypeDef BodyShape;
        private bool ChangedBS = false;
    }
}
