using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace TOT_DLL_test
{
    public class Comp_ApparelHediffAdder : ThingComp
    {
        private CompProperties_ApparelHediffAdder Props => (CompProperties_ApparelHediffAdder)props;
        public CompApparelReloadable CompApparelReloadable
        {
            get
            {
                if(CompApparelReloadableSaved == null)
                {
                    CompApparelReloadableSaved = this.parent.TryGetComp<CompApparelReloadable>();
                }
                return CompApparelReloadableSaved;
            }
        }
        public override void CompDrawWornExtras()
        {
            base.CompDrawWornExtras();
        }
        public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
        {
            foreach (Gizmo item in base.CompGetWornGizmosExtra())
            {
                yield return item;
            }
            foreach (Gizmo gizmo in this.GetGizmo())
            {
                yield return gizmo;
            }
            yield break;
        }
        private Pawn Wearer
        {
            get
            {
                if(PawnSaved == null)
                {
                    Apparel apparel = this.parent as Apparel;
                    PawnSaved = apparel.Wearer;
                }
                return PawnSaved;
            }
        }
        private IEnumerable<Gizmo> GetGizmo()
        {
            if(Wearer != null && Wearer.IsPlayerControlled && Wearer.Drafted)  
            {
                if (Find.Selector.SingleSelectedThing == this.Wearer)
                {
                    if (gizmo_ApparelReloadableExtra == null)
                    {
                        gizmo_ApparelReloadableExtra = new Gizmo_ApparelReloadableExtra(this.CompApparelReloadable);
                    }
                    yield return gizmo_ApparelReloadableExtra;
                    
                }
                if(CompApparelReloadable.RemainingCharges > 0)
                {
                    Command_Action command1 = new Command_Action
                    {
                        defaultLabel = this.Props.Label.Translate(),
                        icon = new CachedTexture(Props.UIPath).Texture,
                        action = delegate ()
                        {
                            HediffDef named = DefDatabase<HediffDef>.GetNamed(Props.HediffName, true);
                            if (named != null && this.Wearer != null)
                            {
                                bool hashediff = Wearer.health.hediffSet.TryGetHediff(named, out Hediff hediff2);
                                if (!hashediff)
                                {
                                    Hediff hediff = HediffMaker.MakeHediff(named, this.Wearer, null);
                                    hediff.TryGetComp<HediffComp_Disappears>().ticksToDisappear = Props.HediffTickToDisappear;
                                    this.Wearer.health.AddHediff(hediff, null, null, null);
                                }
                                else
                                {
                                    hediff2.TryGetComp<HediffComp_Disappears>().ticksToDisappear += Props.HediffTickToDisappear;
                                }
                                CompApparelReloadable?.UsedOnce();
                            }
                        }
                    };
                    yield return command1;
                }
            }
            yield break;
        }
        private Gizmo_ApparelReloadableExtra gizmo_ApparelReloadableExtra;
        private CompApparelReloadable CompApparelReloadableSaved;
        private Pawn PawnSaved;
    }
    public class CompProperties_ApparelHediffAdder : CompProperties
    {
        public string Label = "Default Label";
        public string UIPath;
        public string HediffName = "PsychicInvisibility";
        public int HediffTickToDisappear = 1200;

        public CompProperties_ApparelHediffAdder()
        {
            compClass = typeof(Comp_ApparelHediffAdder);
        }
    }
}
