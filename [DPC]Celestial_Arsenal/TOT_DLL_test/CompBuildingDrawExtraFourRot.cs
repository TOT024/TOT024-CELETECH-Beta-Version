using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace TOT_DLL_test
{
    public class CompProperties_BuildingDrawExtraFourRot : CompProperties
    {
        public CompProperties_BuildingDrawExtraFourRot()
        {
            this.compClass = typeof(CompBuildingDrawExtraFourRot);
        }
        public GraphicData graphicDataExtra;
        public bool ChangeColor = false;
    }
    public class CompBuildingDrawExtraFourRot : ThingComp
    {
        public CompProperties_BuildingDrawExtraFourRot Properties
        {
            get
            {
                return (CompProperties_BuildingDrawExtraFourRot)this.props;
            }
        }
        private CompPowerTrader PowerComp
        {
            get
            {
                if (_powerComp == null)
                {
                    _powerComp = this.parent.GetComp<CompPowerTrader>();
                }
                return _powerComp;
            }
        }
        private CompGlower GlowerComp
        {
            get
            {
                if (_glowerComp == null)
                {
                    _glowerComp = this.parent.GetComp<CompGlower>();
                }
                return _glowerComp;
            }
        }
        public override void PostDraw()
        {
            base.PostDraw();
            if (PowerComp.PowerOn || PowerComp == null)
            {
                Mesh mesh = this.Properties.graphicDataExtra.Graphic.MeshAt(this.parent.Rotation);
                Material baseMat = this.Properties.graphicDataExtra.Graphic.MatAt(this.parent.Rotation, null);
                Material materialToDraw = baseMat;
                if (Properties.ChangeColor && GlowerComp!=null)
                {
                    Color color = GlowerComp.GlowColor.ToColor;
                    color.a = 0.67f;
                    Texture mainTex = baseMat.mainTexture;
                    Shader shader = baseMat.shader;
                    MaterialRequest req = new MaterialRequest(mainTex, shader, color);
                    materialToDraw = MaterialPool.MatFrom(req);
                }
                Graphics.DrawMesh(mesh, this.parent.DrawPos + new Vector3(0f, 1f, 0f) + this.Properties.graphicDataExtra.DrawOffsetForRot(this.parent.Rotation), Quaternion.AngleAxis(0, Vector3.right), materialToDraw, 0);
            }
        }
        private CompPowerTrader _powerComp;
        private CompGlower _glowerComp;
    }
}
