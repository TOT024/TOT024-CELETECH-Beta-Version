using RimWorld;
using UnityEngine;
using Verse;

namespace TOT_DLL_test
{
    [StaticConstructorOnStartup]
    public class CompAnimatedDraw_Projectile_Inregular : ThingComp
    {
        private CompProperties_AnimatedDraw_Projectile_Inregular Props => (CompProperties_AnimatedDraw_Projectile_Inregular)props;

        private Material mat;
        private Material mat2 = MaterialPool.MatFrom("Things/Projectile/MissileTail", ShaderTypeDefOf.MoteGlow.Shader);
        private int startFrameOffset = -1;
        private const int TailFrames = 4;
        private static MaterialPropertyBlock propBlock = new MaterialPropertyBlock();

        public override void PostDraw()
        {
            if (startFrameOffset == -1)
            {
                startFrameOffset = Rand.Range(0, Props.totalFrames);
            }
            if (mat == null)
                mat = MaterialPool.MatFrom(Props.texturePath, ShaderTypeDefOf.Cutout.Shader);
            int baseTick = Find.TickManager.TicksGame / Props.ticksPerFrame + startFrameOffset;
            int bodyFrameIndex = baseTick % Props.totalFrames;
            float bodyScaleX = 1f / Props.totalFrames;
            float bodyOffsetX = bodyFrameIndex * bodyScaleX;
            propBlock.Clear();
            propBlock.SetVector("_MainTex_ST", new Vector4(bodyScaleX, 1f, bodyOffsetX, 0f));
            Mesh animationmesh = MeshPool.GridPlane(Props.DrawSize);

            if (this.parent is Projectile_PoiMissile projectile)
            {
                Graphics.DrawMesh(animationmesh, projectile.position1, projectile.rotation, mat, 0, null, 0, propBlock);
                int tailFrameIndex = baseTick % TailFrames;
                float tailScaleX = 1f / TailFrames; // 0.25
                float tailOffsetX = tailFrameIndex * tailScaleX;
                propBlock.SetVector("_MainTex_ST", new Vector4(tailScaleX, 1f, tailOffsetX, 0f));
                Mesh animationmesh2 = MeshPool.GridPlane(Props.DrawSize * 1.45f + new Vector2(0.5f, -2f * projectile.DCFExport * projectile.DCFExport + 2f * projectile.DCFExport + 1.5f));
                Graphics.DrawMesh(animationmesh2, projectile.position2 - new Vector3(0f, -1f, 0f), projectile.rotation, mat2, 0, null, 0, propBlock);
            }
            else if (this.parent is Projectile_PoiMissile_Interceptor projectile1)
            {
                Graphics.DrawMesh(animationmesh, projectile1.position1, projectile1.rotation, mat, 0, null, 0, propBlock);
                int tailFrameIndex = baseTick % TailFrames;
                float tailScaleX = 1f / TailFrames;
                float tailOffsetX = tailFrameIndex * tailScaleX;
                propBlock.SetVector("_MainTex_ST", new Vector4(tailScaleX, 1f, tailOffsetX, 0f));
                Mesh animationmesh2 = MeshPool.GridPlane(Props.DrawSize * 1.45f + new Vector2(0.5f, -2f * projectile1.DCFExport * projectile1.DCFExport + 2f * projectile1.DCFExport + 1.5f));
                Graphics.DrawMesh(animationmesh2, projectile1.position2 - new Vector3(0f, -1f, 0f), projectile1.rotation, mat2, 0, null, 0, propBlock);
            }
            else if (this.parent is Projectile_PoiMissile_ASG projectile2)
            {
                Graphics.DrawMesh(animationmesh, projectile2.position1, projectile2.rotation, mat, 0, null, 0, propBlock);
                int tailFrameIndex = baseTick % TailFrames;
                float tailScaleX = 1f / TailFrames;
                float tailOffsetX = tailFrameIndex * tailScaleX;
                propBlock.SetVector("_MainTex_ST", new Vector4(tailScaleX, 1f, tailOffsetX, 0f));
                Mesh animationmesh2 = MeshPool.GridPlane(Props.DrawSize * 1.45f + new Vector2(0.5f, -2f * projectile2.DCFExport * projectile2.DCFExport + 2f * projectile2.DCFExport + 1.5f));
                Graphics.DrawMesh(animationmesh2, projectile2.position2 - new Vector3(0f, -1f, 0f), projectile2.rotation, mat2, 0, null, 0, propBlock);
            }
            else
            {
                return;
            }
        }
    }
    public class CompProperties_AnimatedDraw_Projectile_Inregular : CompProperties
    {
        public string texturePath;
        public int totalFrames;
        public int ticksPerFrame;
        public Vector2 DrawSize = Vector2.zero;
        public CompProperties_AnimatedDraw_Projectile_Inregular()
        {
            compClass = typeof(CompAnimatedDraw_Projectile_Inregular);
        }
    }
}