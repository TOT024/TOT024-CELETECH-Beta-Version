using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace TOT_DLL_test
{
    public class HediffComp_BoostedEffect : HediffComp
    {
        public HediffCompProperties_BoostedEffect Props
        {
            get
            {
                return (HediffCompProperties_BoostedEffect)this.props;
            }
        }
        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Deep.Look<Mote>(ref this.CMC_Mote, "CMC_Mote", Array.Empty<object>());
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            Pawn pawn = this.parent.pawn;
            base.CompPostTick(ref severityAdjustment);
            bool flag = !pawn.InBed() && pawn.Awake() && !pawn.Downed;
            if (CMC_Mote.DestroyedOrNull())
            {
                ThingDef Mote = CMC_Def.CMC_Mote_ChipBoosted;
                Vector3 Offset = new Vector3(0f, 0f, -0.05f);
                this.CMC_Mote = MoteMaker.MakeAttachedOverlay(this.parent.pawn, Mote, Offset, 2.3f, 1.0f);
                this.CMC_Mote.exactRotation = 0f;
            }
            if(flag)
            {
                this.CMC_Mote.Maintain();
            }
        }
        public Mote CMC_Mote;
    }
}
