using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace TOT_DLL_test
{
    public class HediffCompProperties_BoostedEffect : HediffCompProperties
    {
        public HediffCompProperties_BoostedEffect()
        {
            this.compClass = typeof(HediffComp_BoostedEffect);
        }
        public float R = 255f;
        public float G = 255f;
        public float B = 255f;
        public float yOffset = 0f;
    }
}
