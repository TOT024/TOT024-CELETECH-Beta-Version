using RimWorld;
using System;
using TOT_DLL_test;
using Verse;

namespace MYDE_CMC_Dll
{
    public class HediffComp_ConstancyDamage : HediffComp
    {
        public override void CompPostTick(ref float severityAdjustment)
        {
            this.DamageTick++;
            bool flag = this.DamageTick >= this.Props.DamageTickMax;
            if (flag)
            {
                this.TakeDamage();
                this.DamageTick = 0;
            }
        }
        public virtual void TakeDamage()
        {
            if (this.Props.DamageDef != null)
            {
                DamageInfo dinfo = new DamageInfo(this.Props.DamageDef, this.Props.DamageNum, this.Props.DamageArmorPenetration, -1f, null, null, null, DamageInfo.SourceCategory.ThingOrUnknown, null, true, true, QualityCategory.Normal, true);
                base.Pawn.TakeDamage(dinfo);
            }
        }
        public HediffCompProperties_ConstancyDamage Props
        {
            get
            {
                return (HediffCompProperties_ConstancyDamage)this.props;
            }
        }
        private int DamageTick = 0;
    }
}
