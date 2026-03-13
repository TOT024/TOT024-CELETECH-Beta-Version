using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace TOT_DLL_test
{
    [DefOf]
    public static class CMCResearchProjectDefOf
    {
        static CMCResearchProjectDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(ResearchProjectDefOf));
        }
        public static ResearchProjectDef CMCGunTurrets;
        public static ResearchProjectDef CMC_Smart_I;
        public static ResearchProjectDef CMC_EMweaponTech_AP;
        public static ResearchProjectDef CMC_EMweaponTech_APII;
        public static ResearchProjectDef CMC_CAS_Init;
        public static ResearchProjectDef CMC_CASDirectionControl;
        public static ResearchProjectDef CMC_CAS_HangerUpgrade;
        public static ResearchProjectDef CMC_CAS_AmmoUpgrade;
    }
}
