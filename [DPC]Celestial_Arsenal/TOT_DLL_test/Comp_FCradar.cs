using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace TOT_DLL_test
{
    public class Comp_FCradar : ThingComp
    {
        public CompProperties_FCradar Properties
        {
            get
            {
                return (CompProperties_FCradar)this.props;
            }
        }
        public bool get_Active()
        {
            if(this.compPowerTrader == null)
            {
                this.compPowerTrader = this.parent.GetComp<CompPowerTrader>();
            }
            return this.compPowerTrader != null && this.compPowerTrader.PowerOn;
        }
        public override void CompTick()
        {
            if(get_Active())
            {
                this.rotatorAngle = (this.rotatorAngle + this.Properties.rotatorSpeed) % 360f;
            }
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<float>(ref this.rotatorAngle, "angle", 0f, false);
        }
        public override void PostDraw()
        {
            Vector3 vector;
            vector.x = 2.4f;
            vector.z = 2.4f;
            vector.y = AltitudeLayer.Building.AltitudeFor();
            Matrix4x4 matrix4x = default(Matrix4x4);
            Vector3 Pos = this.parent.DrawPos + Altitudes.AltIncVect;
            Pos.z += 0.5f;
            matrix4x.SetTRS(Pos, Quaternion.AngleAxis(this.rotatorAngle, Vector3.up), vector);
            Graphics.DrawMesh(MeshPool.plane10, matrix4x, Resources.rotatorTexture, 0);
        }
        [StaticConstructorOnStartup]
        public static class Resources
        {
            public static Material rotatorTexture = MaterialPool.MatFrom("Things/Buildings/CMC_FC_tex", ShaderDatabase.Cutout);
        }

        private float rotatorAngle = (float)Rand.Range(0, 360);
        private CompPowerTrader compPowerTrader;
    }
}
