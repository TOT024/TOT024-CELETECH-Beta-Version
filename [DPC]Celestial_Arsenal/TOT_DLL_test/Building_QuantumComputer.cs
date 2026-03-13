using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;

namespace TOT_DLL_test
{
    [StaticConstructorOnStartup]
    public class Building_QuantumComputer : Building
    {
        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            base.DrawAt(drawLoc, flip);
            this.DrawScreen();
        }
        public void DrawScreen()
        {
            Matrix4x4 matrix = default;
            Vector3 pos = this.DrawPos + Altitudes.AltIncVect + this.def.graphicData.drawOffset;
            pos.y = AltitudeLayer.BuildingBelowTop.AltitudeFor() + 0.1f;
            matrix.SetTRS(pos, Quaternion.identity, vector);
            Graphics.DrawMesh(MeshPool.plane10, matrix, QCGlowTexture, 0);
        }
        private static readonly Vector3 vector = new Vector3(2.46875f, 0f, 2.46875f);
        private static readonly Material QCGlowTexture = MaterialPool.MatFrom("Things/Buildings/CMC_Beacon_Light", ShaderDatabase.MoteGlow);
    }
}
