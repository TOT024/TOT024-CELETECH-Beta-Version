using System.Collections.Generic;
using Verse;

namespace TOT_DLL_test
{
    public class Comp_FloatSwordAttack : HediffComp
    {
        public CompProperties_FloatSwordAttack Props
        {
            get
            {
                return (CompProperties_FloatSwordAttack)this.props;
            }
        }
        public void Tick()
        {
            if(Find.TickManager.TicksGame % 300 == 0)
            {
                Log.Message(this.Holder);
                if(this.Holder != null && this.Holder.drafter.Drafted)
                {
                    List<Thing> list;
                    list =  this.Holder.Map.listerThings.ThingsInGroup(ThingRequestGroup.Pawn);
                    Log.Message(list);
                }
            }
        }
        public Pawn Holder
        {
            get
            {
                return this.Pawn;
            }
        }
        public LocalTargetInfo LastAttackedTarget
        {
            get
            {
                return this.lastAttackedTarget;
            }
        }
        public int LastAttackTargetTick
        {
            get
            {
                return this.lastAttackTargetTick;
            }
        }
        private LocalTargetInfo lastAttackedTarget = LocalTargetInfo.Invalid;
        private int lastAttackTargetTick = 0;
    }
}
