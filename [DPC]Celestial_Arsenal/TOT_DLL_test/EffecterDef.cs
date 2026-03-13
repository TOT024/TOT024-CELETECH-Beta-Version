using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace TOT_DLL_test
{
    [DefOf]
    public static class CMCEffecterDefOf
    {
        static CMCEffecterDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(EffecterDefOf));
        }
        public static EffecterDef CMC_AABomb;
    }
}
