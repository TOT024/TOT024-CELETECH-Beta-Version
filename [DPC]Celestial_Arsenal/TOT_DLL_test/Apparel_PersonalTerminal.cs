using RimWorld;
using Verse;

namespace TOT_DLL_test
{
    [StaticConstructorOnStartup]
    public class Apparel_PersonalTerminal : Apparel
    {
        private static bool IsHashIntervalTick(Thing t, int interval)
        {
            return t.HashOffsetTicks() % interval == 0;
        }
        protected override void Tick()
        {
            base.Tick();
            if (IsHashIntervalTick(this, 5900))
            {
                if(this.Wearer!= null && !Wearer.Dead && ModLister.IdeologyInstalled)
                {
                    HediffDef named = DefDatabase<HediffDef>.GetNamed("NeuralSupercharge", true);
                    Hediff hediff = HediffMaker.MakeHediff(named, Wearer, null);
                    hediff.TryGetComp<HediffComp_Disappears>().ticksToDisappear = 6000;
                    this.Wearer.health.AddHediff(hediff, null, null, null);
                }
            }
        }
    }
}
