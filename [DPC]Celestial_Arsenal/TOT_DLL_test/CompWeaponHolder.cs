using RimWorld;
using UnityEngine;
using Verse;

namespace TOT_DLL_test
{
    public class CompProperties_WeaponHolder : CompProperties
    {
        public Vector3 weaponDrawOffset;

        public CompProperties_WeaponHolder()
        {
            this.compClass = typeof(CompWeaponHolder);
        }
    }
    public class CompWeaponHolder : ThingComp
    {
        public CompProperties_WeaponHolder Props => (CompProperties_WeaponHolder)this.props;
        private Thing heldWeapon = null;
        public Thing HeldWeapon => heldWeapon;
        public bool IsOccupied => heldWeapon != null;

        public void InstallWeapon(Thing weapon)
        {
            if (IsOccupied) return;
            if (weapon.Spawned) weapon.DeSpawn();
            this.heldWeapon = weapon;
        }
        public Thing UninstallWeapon()
        {
            if (!IsOccupied) return null;
            Thing weapon = this.heldWeapon;
            this.heldWeapon = null;
            return weapon;
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Deep.Look(ref heldWeapon, "heldWeapon");
        }
    }
}