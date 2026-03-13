using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TOT_DLL_test
{
    public class CompProperties_AntiInv : CompProperties_AbilityEffect
    { 
        public CompProperties_AntiInv()
        {
            this.compClass = typeof(CompProperties_AntiInv);
        }

        public float SpotRange = 10f;
        public float DetectRange = 20f;
    }
}
