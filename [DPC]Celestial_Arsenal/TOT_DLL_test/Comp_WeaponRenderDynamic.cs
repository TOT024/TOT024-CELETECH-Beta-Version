using RimWorld;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace TOT_DLL_test
{
    public class Comp_WeaponRenderDynamic : ThingComp
    {
        private CompProperties_WeaponRenderDynamic Props => (CompProperties_WeaponRenderDynamic)props;
        public override void PostDraw()
        {
            Matrix4x4 matrix = default;
            Vector3 pos = this.parent.DrawPos + new Vector3(0f, 0.1f, 0f) + this.parent.Graphic.DrawOffset(parent.Rotation);
            Vector3 scaleVec = new Vector3(this.parent.Graphic.drawSize.x, 1f, this.parent.Graphic.drawSize.y);
            matrix.SetTRS(pos, Quaternion.AngleAxis(AngleOnGround,Vector3.up), scaleVec);
            PostDrawExtraGlower(DefaultMesh, matrix);
        }
        private float AngleOnGround
        {
            get
            {
                 return DrawAngle(parent.DrawPos, parent.def, parent);
            }
        }
        public float DrawAngle(Vector3 loc,ThingDef thingDef, Thing thing)
        {
            float num = 0f;
            float? rotInRack = this.GetRotInRack(thing, thingDef, loc.ToIntVec3());
            if (rotInRack != null)
            {
                num = rotInRack.Value;
            }
            else if (thing != null)
            {
                num = -this.parent.def.graphicData.onGroundRandomRotateAngle + (float)(thing.thingIDNumber * 542) % (this.parent.def.graphicData.onGroundRandomRotateAngle * 2f);
            }
            return num;
        }
        private float? GetRotInRack(Thing thing, ThingDef thingDef, IntVec3 loc)
        {
            if (thing == null || !thingDef.IsWeapon || !thing.Spawned || !loc.InBounds(thing.Map) || loc.GetEdifice(thing.Map) == null || loc.GetItemCount(thing.Map) < 2)
            {
                return null;
            }
            if (thingDef.rotateInShelves)
            {
                return new float?(-90f);
            }
            return new float?(0f);
        }
        public void PostDrawExtraGlower(Mesh mesh, Matrix4x4 matrix)
        {
            int frameIndex = (Find.TickManager.TicksGame / Props.ticksPerFrame) % Props.totalFrames;
            Vector2 frameSize = new Vector2(1f / Props.totalFrames, 1f);
            Vector2 offset = new Vector2(frameIndex * frameSize.x, 0f);
            Material mat = GetMaterial;
            mat.mainTextureOffset = offset;
            mat.mainTextureScale = frameSize;
            mat.shader = ShaderTypeDefOf.MoteGlow.Shader;
            Graphics.DrawMesh(mesh, matrix, mat, 0);
        }
        private Material GetMaterial
        {
            get
            {
                if(MaterialS != null)
                    return MaterialS;
                MaterialS = MaterialPool.MatFrom(Props.TexturePath, ShaderTypeDefOf.MoteGlow.Shader);
                return MaterialS;
            }
        }
        public override void PostExposeData()
        {
            Scribe_Values.Look<Color>(ref this.Camocolor, "Camocolor");
            base.PostExposeData();
        }
        private Material MaterialS;
        private readonly Mesh DefaultMesh = MeshPool.plane10;
        public Color Camocolor = Color.white;
    }
    public class CompProperties_WeaponRenderDynamic : CompProperties
    {
        public String TexturePath;
        public int totalFrames;
        public int ticksPerFrame;
        public Vector2 DrawSize = Vector2.zero;
        public Vector3 Offset = Vector3.zero;
        public CompProperties_WeaponRenderDynamic()
        {
            compClass = typeof(Comp_WeaponRenderDynamic);
        }
    }
}
