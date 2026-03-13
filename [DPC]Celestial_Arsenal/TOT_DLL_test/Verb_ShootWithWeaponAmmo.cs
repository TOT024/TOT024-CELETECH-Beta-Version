using System.Runtime.Remoting.Messaging;
using Verse;

namespace TOT_DLL_test
{
    public class Verb_ShootWithWeaponAmmo : Verb_Shoot
    {
        private CompWeaponAmmo WeaponAmmo => EquipmentSource?.TryGetComp<CompWeaponAmmo>();
        public override bool Available()
        {
            return base.Available() && WeaponAmmo != null && WeaponAmmo.currentMagAmmo > 0;
        }
        protected override bool TryCastShot()
        {
            bool result = false;
            if (WeaponAmmo != null && WeaponAmmo.currentMagAmmo > 0)
            {
                if (base.TryCastShot())
                {
                    WeaponAmmo.currentMagAmmo--;
                    result = true;
                }
            }
            if (WeaponAmmo.currentMagAmmo <= 0)
            {
                TryAutoHolster(this.CasterPawn, this.EquipmentSource);
            }
            return result;
        }
        private void TryAutoHolster(Pawn pawn, ThingWithComps weapon)
        {
            if (pawn?.apparel == null) return;
            foreach (var apparel in pawn.apparel.WornApparel)
            {
                var holder = apparel.GetComp<CompApparelWeaponHolder>();

                if (holder != null && !holder.AnyWeaponInBelt)
                {
                    holder.RequestHolster(this.EquipmentSource);
                    break;
                }
            }
        }
    }
}