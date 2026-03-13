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
    public class Building_LandingPlatform : Building
    {
        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            base.DrawAt(drawLoc, flip);
            Matrix4x4 matrix = default;
            Vector3 pos = this.DrawPos + Altitudes.AltIncVect;
            pos.y = AltitudeLayer.DoorMoveable.AltitudeFor() + 0.1f;
            matrix.SetTRS(pos, Quaternion.identity, vec);
            Graphics.DrawMesh(MeshPool.plane10, matrix, NormalLight, 0);
        }
        private static Vector3 vec = new Vector3(5f, 0f, 5f);
        private static readonly Material NormalLight = MaterialPool.MatFrom("Things/Buildings/LandingZone_LightNormal", ShaderDatabase.MoteGlow);
        private static readonly Material LandingLight = MaterialPool.MatFrom("Things/Buildings/LandingZone_Light_ShuttleLanding", ShaderDatabase.MoteGlow);
    }
}
