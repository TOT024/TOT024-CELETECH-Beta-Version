using RimWorld;
using Verse;

namespace TOT_DLL_test
{
    public class Verb_Invisible : Verb_CastAbility
    {
        protected override bool TryCastShot()
        {
            HediffDef named = DefDatabase<HediffDef>.GetNamed("PsychicInvisibility", true);
            if (named != null && this.CasterPawn != null)
            {
                Hediff hediff = HediffMaker.MakeHediff(named, this.CasterPawn, null);
                hediff.TryGetComp<HediffComp_Disappears>().ticksToDisappear = 3600;
                this.CasterPawn.health.AddHediff(hediff, null, null, null);
                CompApparelReloadable reloadableCompSource = base.ReloadableCompSource;
                if (reloadableCompSource != null)
                {
                    reloadableCompSource.UsedOnce();
                }
                return true;
            }
            return false;
        }
    }
}
