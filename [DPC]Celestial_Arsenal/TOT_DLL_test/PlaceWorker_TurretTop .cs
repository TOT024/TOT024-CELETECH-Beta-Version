using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace TOT_DLL_test
{
    // Token: 0x0200000B RID: 11
    public class PlaceWorker_TurretTop : PlaceWorker
    {
        // Token: 0x06000065 RID: 101 RVA: 0x000047BC File Offset: 0x000029BC
        public override void DrawGhost(ThingDef def, IntVec3 loc, Rot4 rot, Color ghostCol, Thing thing = null)
        {
            GhostUtility.GhostGraphicFor(GraphicDatabase.Get<Graphic_Single>(def.building.turretGunDef.graphicData.texPath, ShaderDatabase.Cutout, new Vector2(def.building.turretTopDrawSize, def.building.turretTopDrawSize), Color.white), def, ghostCol, null).DrawFromDef(GenThing.TrueCenter(loc, rot, def.Size, AltitudeLayer.MetaOverlays.AltitudeFor()), rot, def, 0f);
        }
    }
}
