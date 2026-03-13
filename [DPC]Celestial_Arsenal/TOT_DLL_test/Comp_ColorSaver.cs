using UnityEngine;
using Verse;

namespace TOT_DLL_test
{
    public class Comp_ColorSaver : ThingComp
    {
        public CompProperties_ColorSaver Properties
        {
            get
            {
                return (CompProperties_ColorSaver)this.props;
            }
        }

        public Pawn Holder
        {
            get
            {
                ThingWithComps thing = this.parent;
                Pawn_EquipmentTracker pawn_EquipmentTracker = ((thing != null) ? thing.ParentHolder : null) as Pawn_EquipmentTracker;
                if (pawn_EquipmentTracker == null)
                {
                    return null;
                }
                return pawn_EquipmentTracker.pawn;
            }
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<Color>(ref this.GunCamoColor, "color", Color.white, true);
        }
        public Color GunCamoColor = Color.white;
    }
}
