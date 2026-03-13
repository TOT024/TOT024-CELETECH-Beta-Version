using RimWorld;
using Verse;
using System.Linq;

namespace TOT_DLL_test
{
    public class CompProperties_WeaponAmmo : CompProperties
    {
        public int maxMagAmmo = 1;          
        public int reloadTicks = 120;         
        public bool defaultAutoSwapBack = true; 
        public CompProperties_WeaponAmmo()
        {
            this.compClass = typeof(CompWeaponAmmo);
        }
    }
    public class CompWeaponAmmo : ThingComp
    {
        public int currentMagAmmo = 0;
        public int maxMagAmmo = 1;
        public CompProperties_WeaponAmmo Props => (CompProperties_WeaponAmmo)props;
        public CompBackpackAmmo ConnectedBackpack
        {
            get
            {
                Pawn wielder = null;
                wielder = this.Verb.caster as Pawn;
                if (wielder != null && wielder.apparel != null)
                {
                    foreach (var apparel in wielder.apparel.WornApparel)
                    {
                        var pack = apparel.TryGetComp<CompBackpackAmmo>();
                        if (pack != null) return pack;
                    }
                }
                return null;
            }
        }
        private Verb verbInt = null;
        private CompEquippable compEquippableInt;
        private Verb Verb
        {
            get
            {
                bool flag = this.verbInt == null;
                if (flag)
                {
                    this.verbInt = this.EquipmentSource.PrimaryVerb;
                }
                return this.verbInt;
            }
        }
        private CompEquippable EquipmentSource
        {
            get
            {
                bool flag = this.compEquippableInt != null;
                CompEquippable result;
                if (flag)
                {
                    result = this.compEquippableInt;
                }
                else
                {
                    this.compEquippableInt = this.parent.TryGetComp<CompEquippable>();
                    result = this.compEquippableInt;
                }
                return result;
            }
        }
        public void TryReloadFromBackpack()
        {
            if (currentMagAmmo == maxMagAmmo) return;
            var pack = ConnectedBackpack;
            if (pack != null && pack.RemainingCharges > 0)
            {
                int needed = maxMagAmmo - currentMagAmmo;
                int taken = pack.ExtractAmmo(needed);
                currentMagAmmo += taken;
            }
        }
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if(!respawningAfterLoad)
            {
                this.currentMagAmmo = maxMagAmmo;
            }
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref currentMagAmmo, "currentMagAmmo", 0);
        }
    }
}