using UnityEngine;
using Verse;
using RimWorld;
using System.Collections.Generic;

namespace TOT_DLL_test
{
    public class CompProperties_Camo : CompProperties
    {
        public CompProperties_Camo() => this.compClass = typeof(CompCamo);
    }

    public class CompCamo : ThingComp
    {
        public Color colorOne = Color.white;
        public Color colorTwo = Color.gray;
        public Color colorThree = Color.blue;
        public string maskPath = "Patterns/Hex";

        public Vector2 maskScale = Vector2.one;
        public Vector2 maskOffset = Vector2.zero;
        public bool HasCamoData => maskPath != null;

        private Dictionary<string, Graphic> _graphicCache = new Dictionary<string, Graphic>();

        public void ClearCache()
        {
            _graphicCache?.Clear();
        }

        public void SetData(Color c1, Color c2, Color c3, string mask, Vector2 scale, Vector2 offset)
        {
            this.colorOne = c1;
            this.colorTwo = c2;
            this.colorThree = c3;
            this.maskPath = mask;
            this.maskScale = scale;
            this.maskOffset = offset;

            ClearCache();
            UpdateRender();
        }

        public Graphic GetCamoGraphic(Graphic originalGraphic)
        {
            if (originalGraphic == null)
                return null;
            if (ShaderLoader.MaskedShader == null)
                return originalGraphic;
            string path = originalGraphic.path;
            if (string.IsNullOrEmpty(path))
                return originalGraphic;

            if (_graphicCache.TryGetValue(path, out Graphic cached)) return cached;
            string currentMaskPath = !string.IsNullOrEmpty(this.maskPath) ? this.maskPath : "Patterns/Hex";

            GraphicRequest req = new GraphicRequest(
                originalGraphic.GetType(),
                path,
                ShaderLoader.MaskedShader,
                originalGraphic.drawSize,
                colorOne, colorTwo, null, 0, null, currentMaskPath
            );

            Graphic newGraphic = (Graphic)System.Activator.CreateInstance(originalGraphic.GetType());
            newGraphic.Init(req);
            Texture2D camoPatternTex = ContentFinder<Texture2D>.Get(currentMaskPath, false);
            if (camoPatternTex != null)
            {
                camoPatternTex.wrapMode = TextureWrapMode.Repeat;
                camoPatternTex.filterMode = FilterMode.Bilinear;
            }
            void SetupMat(Material mat, Texture2D apparelMaskTex)
            {
                if (mat == null) return;
                mat.SetColor("_ColorOne", colorOne);
                mat.SetColor("_ColorTwo", colorTwo);
                mat.SetColor("_ColorThree", colorThree);

                if (camoPatternTex != null)
                {
                    mat.SetTexture("_CamoTex", camoPatternTex);
                    mat.SetTextureScale("_CamoTex", this.maskScale);
                    mat.SetTextureOffset("_CamoTex", this.maskOffset);
                }
                if (apparelMaskTex != null)
                {
                    mat.SetTexture("_ApparelMask", apparelMaskTex);
                }
            }
            try
            {
                if (newGraphic is Graphic_Multi multi)
                {
                    Texture2D maskNorth = ContentFinder<Texture2D>.Get(path + "_northm", false);
                    Texture2D maskSouth = ContentFinder<Texture2D>.Get(path + "_southm", false);
                    Texture2D maskEast = ContentFinder<Texture2D>.Get(path + "_eastm", false);
                    Texture2D maskWest = ContentFinder<Texture2D>.Get(path + "_westm", false);

                    if (maskWest == null)
                        maskWest = maskEast;
                    SetupMat(multi.MatNorth, maskNorth);
                    SetupMat(multi.MatSouth, maskSouth);
                    SetupMat(multi.MatEast, maskEast);
                    SetupMat(multi.MatWest, maskWest);
                }
                else if (newGraphic is Graphic_Single single)
                {
                    Texture2D maskSingle = ContentFinder<Texture2D>.Get(path + "m", false);
                    SetupMat(single.MatSingle, maskSingle);
                }
            }
            catch (System.Exception ex)
            {
                Log.Warning($"[CamoComp] Failed to setup materials for {path}: {ex.Message}");
                return originalGraphic;
            }

            _graphicCache[path] = newGraphic;
            return newGraphic;
        }

        private void UpdateRender()
        {
            if (parent.Map != null) parent.DirtyMapMesh(parent.Map);

            Pawn wearer = (parent as Apparel)?.Wearer;
            if (wearer != null && wearer.Drawer != null && wearer.Drawer.renderer != null)
            {
                wearer.Drawer.renderer.SetAllGraphicsDirty();
                PortraitsCache.SetDirty(wearer);
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref colorOne, "colorOne", Color.white);
            Scribe_Values.Look(ref colorTwo, "colorTwo", Color.gray);
            Scribe_Values.Look(ref colorThree, "colorThree", Color.blue); 
            Scribe_Values.Look(ref maskPath, "maskPath", "Patterns/Default_R");

            Scribe_Values.Look(ref maskScale, "maskScale", Vector2.one);
            Scribe_Values.Look(ref maskOffset, "maskOffset", Vector2.zero);
        }
    }
}