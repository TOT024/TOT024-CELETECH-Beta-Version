using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace TOT_DLL_test
{
    public class CompPreperties_SmartWeapon : CompProperties
    {
        public CompPreperties_SmartWeapon()
        {
            this.compClass = typeof(CompSmartWeapon);
        }
        public int DamageDeductionRange = 5;
        public float MinDamageMultiplier = 0.5f;
        public float MinPenetrationMultiplier = 0.13f;
    }
}
