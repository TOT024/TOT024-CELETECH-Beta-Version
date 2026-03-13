using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace TOT_DLL_test
{
    [StaticConstructorOnStartup]
    public class Comp_PrismTowerTop : ThingComp
    {
        private const int TotalFrames = 8;
        private static MaterialPropertyBlock matPropBlock;

        public CompProperties_PrismTowerTop Properties
        {
            get
            {
                return (CompProperties_PrismTowerTop)this.props;
            }
        }

        public bool get_Active()
        {
            if (this.compPowerTrader == null)
            {
                this.compPowerTrader = this.parent.GetComp<CompPowerTrader>();
            }
            return this.compPowerTrader != null && this.compPowerTrader.PowerOn;
        }

        public override void CompTick()
        {
            if (get_Active())
            {
                act = true;
                if (Find.TickManager.TicksGame % 7 == 0)
                {
                    i = (i + 1) % TotalFrames;
                }
            }
            else
            {
                act = false;
            }
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<int>(ref this.i, "i", 0, false);
        }

        public override void PostDraw()
        {
            Vector3 vector = new Vector3(2f, 0f, 2f);
            Vector3 vector3 = new Vector3(4.3f, 0f, 4.3f);
            Matrix4x4 matrix4x = default(Matrix4x4);
            Matrix4x4 matrix4x2 = default(Matrix4x4);

            Vector3 Pos = this.parent.DrawPos + Altitudes.AltIncVect;
            Pos.y = AltitudeLayer.BuildingOnTop.AltitudeFor() - 0.1f;

            Vector3 Pos2 = Pos;
            Pos2.z += 0.72f;
            matrix4x2.SetTRS(Pos2, Quaternion.identity, vector3);

            Pos.z += 2.2f;
            matrix4x.SetTRS(Pos, Quaternion.AngleAxis(0f, Vector3.up), vector);
            if (act)
            {
                Graphics.DrawMesh(MeshPool.plane10, matrix4x2, staticLight, 0);
            }

            if (matPropBlock == null)
            {
                matPropBlock = new MaterialPropertyBlock();
            }
            float tileX = 1.0f / TotalFrames;
            float tileY = 1.0f;
            float offsetX = i * tileX;
            float offsetY = 0f;
            matPropBlock.SetVector("_MainTex_ST", new Vector4(tileX, tileY, offsetX, offsetY));
            matPropBlock.SetTexture("_MainTex", rotatorSheetMaterial.mainTexture);
            Graphics.DrawMesh(
                MeshPool.plane10,
                matrix4x,
                rotatorSheetMaterial,
                0,
                null,
                0,
                matPropBlock
            );
        }
        public static readonly Material rotatorSheetMaterial = MaterialPool.MatFrom("Things/Buildings/PrismTowerTop/CMC_PT", ShaderDatabase.Cutout);
        public static readonly Material staticLight = MaterialPool.MatFrom("Things/Buildings/CMC_LaserTower_Light", ShaderDatabase.MoteGlow);

        private CompPowerTrader compPowerTrader;
        private bool act = false;
        public int i = 0;
    }
}