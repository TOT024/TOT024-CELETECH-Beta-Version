using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace TOT_DLL_test
{
    public class CompProperties_FloatSwordAttack : HediffCompProperties
    {
        public CompProperties_FloatSwordAttack()
        {
            this.compClass = typeof(Comp_FloatSwordAttack);
        }
    }
}
