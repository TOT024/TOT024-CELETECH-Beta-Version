using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace TOT_DLL_test
{
    public class Comp_WeaponRenderStatic : ThingComp
    {
        public CompProperties_WeaponRenderStatic Props => (CompProperties_WeaponRenderStatic)props;
        public Color colorOne = new Color(0.93f, 0.93f, 0.93f, 1f);
        public Color colorTwo = new Color(0.81f, 0.81f, 0.81f, 1f);
        public Color colorThree = new Color(0.71f, 0.71f, 0.71f, 1f);
        public string currentMaskPath = "Patterns/Hex";
        public Vector2 maskOffset = Vector2.zero;
        public Vector2 maskScale = Vector2.one;

        private readonly Mesh DefaultMesh = MeshPool.plane10;
        private Material Material_Glow;
        private Material Material_Camo;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref this.colorOne, "colorOne", Color.white);
            Scribe_Values.Look(ref this.colorTwo, "colorTwo", Color.white);
            Scribe_Values.Look(ref this.colorThree, "colorThree", Color.white);
            Scribe_Values.Look(ref this.maskOffset, "maskOffset", Vector2.zero);
            Scribe_Values.Look(ref this.maskScale, "maskScale", Vector2.one);
            Scribe_Values.Look(ref this.currentMaskPath, "currentMaskPath");
        }
        public override void PostDraw()
        {
            if (this.parent.Spawned)
            {
                Matrix4x4 matrix = default;
                Vector3 pos = this.parent.DrawPos + new Vector3(0f, 0.1f, 0f) + this.parent.Graphic.DrawOffset(parent.Rotation);
                Vector3 scaleVec = new Vector3(this.parent.Graphic.drawSize.x, 1f, this.parent.Graphic.drawSize.y);
                matrix.SetTRS(pos, Quaternion.AngleAxis(AngleOnGround, Vector3.up), scaleVec);
                PostDrawExtraGlower(DefaultMesh, matrix);
            }
        }
        public void PostDrawExtraGlower(Mesh mesh, Matrix4x4 matrix)
        {
            if (GetMaterial_Camo != null)
            {
                Graphics.DrawMesh(mesh, matrix, GetMaterial_Camo, 0);
            }
            if (GetMaterial_Glow != null)
            {
                Graphics.DrawMesh(mesh, matrix, GetMaterial_Glow, 0);
            }
        }
        public Material GetMaterial_Camo
        {
            get
            {
                if (Material_Camo != null) return Material_Camo;
                if (ShaderLoader.CustomShader == null) return null;
                if (string.IsNullOrEmpty(Props.TexturePath_Camo)) return null;

                Material newMat = new Material(ShaderLoader.CustomShader);
                Texture2D camoPartTex = ContentFinder<Texture2D>.Get(Props.TexturePath_Camo);
                newMat.SetTexture("_MainTex", camoPartTex);
                newMat.mainTextureScale = Vector2.one;
                string maskToLoad = !string.IsNullOrEmpty(currentMaskPath) ? currentMaskPath : "Patterns/DefaultMask";
                Texture2D maskTex = ContentFinder<Texture2D>.Get(maskToLoad, false);
                if (maskTex != null)
                {
                    newMat.SetTexture("_MaskTex", maskTex);
                    Vector2 finalScale = (maskScale == Vector2.zero) ? Vector2.one : maskScale;

                    newMat.SetTextureScale("_MaskTex", finalScale);
                    newMat.SetTextureOffset("_MaskTex", maskOffset);
                }
                else
                {
                    newMat.SetTexture("_MaskTex", BaseContent.WhiteTex);
                }
                newMat.SetColor("_ColorOne", colorOne);
                newMat.SetColor("_ColorTwo", colorTwo);
                newMat.SetColor("_ColorThree", colorThree);
                Material_Camo = newMat;
                return Material_Camo;
            }
        }

        public Graphic GetSkinGraphic()
        {
            Graphic_Single graphic = new Graphic_Single();
            string texPath = Props.TexturePath_Camo ?? this.parent.def.graphicData.texPath;

            GraphicRequest req = new GraphicRequest(
               typeof(Graphic_Single),
               texPath,
               ShaderDatabase.Cutout,
               this.parent.def.graphicData.drawSize,
               Color.white, Color.white,
               null, 0, null, null
            );
            graphic.Init(req);

            Material myMat = GetMaterial_Camo;
            if (myMat != null)
            {
                System.Reflection.FieldInfo matField = typeof(Graphic).GetField("mat",
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Public);

                if (matField != null) matField.SetValue(graphic, myMat);
            }
            return graphic;
        }

        public void UpdateSkin()
        {
            Material_Camo = null;
        }

        private Material GetMaterial_Glow
        {
            get
            {
                if (Props.TexturePath == null) return null;
                if (Material_Glow != null) return Material_Glow;
                Material_Glow = MaterialPool.MatFrom(Props.TexturePath, ShaderTypeDefOf.MoteGlow.Shader);
                return Material_Glow;
            }
        }

        private float AngleOnGround => this.DrawAngle(this.parent.DrawPos, this.parent.def, this.parent);

        public float DrawAngle(Vector3 loc, ThingDef thingDef, Thing thing)
        {
            float result = 0f;
            float? rotInRack = this.GetRotInRack(thing, thingDef, loc.ToIntVec3());
            if (rotInRack != null)
            {
                result = rotInRack.Value;
            }
            else if (thing != null)
            {
                result = -this.parent.def.graphicData.onGroundRandomRotateAngle + (float)(thing.thingIDNumber * 542) % (this.parent.def.graphicData.onGroundRandomRotateAngle * 2f);
            }
            return result;
        }

        private float? GetRotInRack(Thing thing, ThingDef thingDef, IntVec3 loc)
        {
            bool flag = thing == null || !thingDef.IsWeapon || !thing.Spawned || !loc.InBounds(thing.Map) || loc.GetEdifice(thing.Map) == null || loc.GetItemCount(thing.Map) < 2;
            if (flag) return null;
            return thingDef.rotateInShelves ? -90f : 0f;
        }
    }

    public class CompProperties_WeaponRenderStatic : CompProperties
    {
        public string TexturePath;
        public string TexturePath_Camo;

        public CompProperties_WeaponRenderStatic()
        {
            compClass = typeof(Comp_WeaponRenderStatic);
        }
    }
}