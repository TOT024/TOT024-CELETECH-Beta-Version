using Verse;


namespace TOT_DLL_test
{
    public class CompProperties_WeaponGiveHediff_Sword : CompProperties
    {
        public CompProperties_WeaponGiveHediff_Sword()
        {
            this.compClass = typeof(CompWeaponGiveHediff_Sword);
        }
        public bool biocodeOnEquip;
    }
}
