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
    public class CompAnimatedDraw_Projectile : ThingComp
    {
        private CompProperties_AnimatedDraw_Projectile Props => (CompProperties_AnimatedDraw_Projectile)props;
        private Material mat;
        private int startFrameOffset = -1;
        public override void PostDraw()
        {
            if (startFrameOffset == -1)
            {
                startFrameOffset = Rand.Range(0, Props.totalFrames);
            }
            int frameIndex = (Find.TickManager.TicksGame / Props.ticksPerFrame + startFrameOffset) % Props.totalFrames;

            Vector2 frameSize = new Vector2(1f / Props.totalFrames, 1f);
            Vector2 offset = new Vector2(frameIndex * frameSize.x, 0f);

            if (mat == null)
                mat = MaterialPool.MatFrom(Props.texturePath, Props.ShaderDef.Shader);
            mat.mainTextureOffset = offset;
            mat.mainTextureScale = frameSize;
            Mesh animationmesh = MeshPool.GridPlane(Props.DrawSize);
            if (this.parent is Projectile projectile && Find.TickManager.TicksGame - projectile.TickSpawned > 5)
            {
                Graphics.DrawMesh(animationmesh, projectile.DrawPos, projectile.ExactRotation, mat, 0);
            }
        }
    }
    public class CompProperties_AnimatedDraw_Projectile : CompProperties
    {
        public string texturePath;
        public int totalFrames;
        public int ticksPerFrame;
        public Vector2 DrawSize = Vector2.zero;
        public ShaderTypeDef ShaderDef = ShaderTypeDefOf.Cutout;
        public CompProperties_AnimatedDraw_Projectile()
        {
            compClass = typeof(CompAnimatedDraw_Projectile);
        }
    }
}
