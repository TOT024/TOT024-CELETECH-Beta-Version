using System;
using Verse;

namespace TOT_DLL_test
{
    public class HediffCompProperties_ConstancyDamage : HediffCompProperties
    {
        public HediffCompProperties_ConstancyDamage()
        {
            this.compClass = typeof(HediffComp_ConstancyDamage);
        }
        public int DamageTickMax;
        public DamageDef DamageDef;
        public int DamageNum;
        public float DamageArmorPenetration;
    }
}
