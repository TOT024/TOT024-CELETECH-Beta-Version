using System;
using RimWorld;
using Verse;

namespace MYDE_CMC_Dll
{
    [DefOf]
    public static class MYDE_ThingDefOf
    {
        static MYDE_ThingDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(MYDE_ThingDefOf));
        }
        public static ThingDef CMC_DECO_WIFI;
    }
    public static class MYDE_SoundDefOf
    {
        static MYDE_SoundDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(MYDE_SoundDefOf));
        }

        public static SoundDef PL;
    }
    namespace MYDE_CMC_Dll
    {
        [DefOf]
        public static class MYDE_DamageDefOf
        {
            static MYDE_DamageDefOf()
            {
                DefOfHelper.EnsureInitializedInCtor(typeof(MYDE_DamageDefOf));
            }
            public static DamageDef InfernoBeam;
        }
    }

}
