using RimWorld;
using Verse;

namespace TOT_DLL_test
{
    public class CompWeaponGiveHediff_Sword : CompBiocodable
    {
        public new CompProperties_WeaponGiveHediff_Sword Props
        {
            get
            {
                return this.props as CompProperties_WeaponGiveHediff_Sword;
            }
        }

        public override void Notify_Equipped(Pawn pawn)
        {
            if (this.Biocodable && this.Props.biocodeOnEquip)
            {
                this.CodeFor(pawn);
            }
        }

        public override void CodeFor(Pawn pawn)
        {
            if (!this.Biocodable)
            {
                return;
            }
            this.biocoded = true;
            this.codedPawn = pawn;
            this.codedPawnLabel = pawn.Name.ToStringFull;
            if (named != null)
            {
                Hediff hediff = HediffMaker.MakeHediff(named, pawn, null);
                pawn.health.AddHediff(hediff, null, null, null);
            }
            this.OnCodedFor(pawn);
        }
        public override void UnCode()
        {
            this.biocoded = false;
            Pawn pawn = this.CodedPawn;
            Hediff hediff = HediffMaker.MakeHediff(named, pawn, null);
            pawn.health.RemoveHediff(hediff);
            this.codedPawn = null;
            this.codedPawnLabel = null;
        }

        public HediffDef named = DefDatabase<HediffDef>.GetNamed("NSwordBinded", true);
        public int Killcount = 0;
        public int TickToCheck;
    }
}
