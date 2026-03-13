using System;
using Verse;

namespace TOT_DLL_test
{
    public class HediffComp_ConstancyDamage : HediffComp
    {
        public override void CompPostTick(ref float severityAdjustment)
        {
            this.DamageTick++;
            bool flag = this.DamageTick >= this.Props.DamageTickMax;
            if (flag)
            {
                DamageInfo dinfo = new DamageInfo(this.Props.DamageDef, (float)this.Props.DamageNum, this.Props.DamageArmorPenetration, -1f, base.Pawn, null, null, DamageInfo.SourceCategory.ThingOrUnknown, null, true, true);
                base.Pawn.TakeDamage(dinfo);
                this.DamageTick = 0;
            }
        }
        public HediffCompProperties_ConstancyDamage Props
        {
            get
            {
                return (HediffCompProperties_ConstancyDamage)this.props;
            }
        }
        private int DamageTick;
    }
}