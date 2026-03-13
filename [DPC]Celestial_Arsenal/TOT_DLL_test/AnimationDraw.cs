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
    public class CompAnimatedDraw : ThingComp
    {
        private CompProperties_AnimatedDraw Props => (CompProperties_AnimatedDraw)props;
        private Material mat;
        public override void PostDraw()
        {
            int frameIndex = (Find.TickManager.TicksGame / Props.ticksPerFrame) % Props.totalFrames;

            Vector2 frameSize = new Vector2(1f / Props.totalFrames, 1f);
            Vector2 offset = new Vector2(frameIndex * frameSize.x, 0f);

            if (mat == null)
                mat = MaterialPool.MatFrom(Props.texturePath, ShaderDatabase.Cutout);

            mat.mainTextureOffset = offset;
            mat.mainTextureScale = frameSize;
            mat.shader = Props.ShaderDef.Shader;
            Graphics.DrawMesh(Mesh, parent.DrawPos + Props.Offset, this.parent.Rotation.AsQuat, mat, 0);
        }
        private Mesh Mesh
        {
            get
            {
                if(animationmesh == null)
                {
                    animationmesh = MeshPool.GridPlane(Props.DrawSize);
                }
                return animationmesh;
            }
        }
        private Mesh animationmesh;
    }
    public class CompProperties_AnimatedDraw : CompProperties
    {
        public string texturePath;
        public int totalFrames;
        public int ticksPerFrame;
        public Vector2 DrawSize = Vector2.zero;
        public Vector3 Offset = Vector3.zero;
        public ShaderTypeDef ShaderDef = ShaderTypeDefOf.Cutout;
        public CompProperties_AnimatedDraw()
        {
            compClass = typeof(CompAnimatedDraw);
        }
    }
}
