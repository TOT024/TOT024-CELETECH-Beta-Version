using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace TOT_DLL_test
{
    [StaticConstructorOnStartup]
    public class Building_CMCAdvancedGeoThermoGen : Building
    {
        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            base.DrawAt(drawLoc, flip);
            Matrix4x4 matrix = default;
            Vector3 pos = this.DrawPos + Altitudes.AltIncVect + this.def.graphicData.drawOffset;
            pos.y = AltitudeLayer.Building.AltitudeFor() + 0.3f;
            matrix.SetTRS(pos, Quaternion.identity, vec);
            Graphics.DrawMesh(MeshPool.plane10, matrix, LightTexture, 0);
        }
        private static readonly Vector3 vec = new Vector3(6f, 0f, 9.4f);
        private static readonly Material LightTexture = MaterialPool.MatFrom("Things/Buildings/CMC_ThermoGen_Light", ShaderDatabase.MoteGlow);
    }
}
