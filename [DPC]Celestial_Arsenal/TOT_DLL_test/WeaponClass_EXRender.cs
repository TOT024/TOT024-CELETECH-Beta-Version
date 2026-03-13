using UnityEngine;
using Verse;

namespace TOT_DLL_test
{
    public class WeaponClass_EXRender : ThingWithComps
    {
        public Pawn Holder
        {
            get
            {
                ThingWithComps thing = this;
                Pawn_EquipmentTracker pawn_EquipmentTracker = ((thing != null) ? thing.ParentHolder : null) as Pawn_EquipmentTracker;
                if (pawn_EquipmentTracker == null)
                {
                    return null;
                }
                return pawn_EquipmentTracker.pawn;
            }
        }
        public Color GunCamoColor = Color.gray;
        public Color GunLightColor = Color.white;
    }
}
