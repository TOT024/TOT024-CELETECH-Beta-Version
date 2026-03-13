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
    public class Graphic_UniversalCamo : Graphic_Single
    {
        private static Material _fallbackMat;

        public override void Init(GraphicRequest req)
        {
            base.Init(req);
            if (ShaderLoader.CustomShader == null) return;
            Material newMat = new Material(ShaderLoader.CustomShader);
            newMat.SetTexture("_MainTex", this.mat.mainTexture);
            string maskPath = "Patterns/DefaultMask";
            Texture2D maskTex = ContentFinder<Texture2D>.Get(maskPath, false);
            if (maskTex != null)
            {
                newMat.SetTexture("_MaskTex", maskTex);
            }
            else
            {
                newMat.SetTexture("_MaskTex", BaseContent.WhiteTex);
            }
            newMat.SetColor("_ColorOne", req.color);
            newMat.SetColor("_ColorTwo", req.colorTwo);
            newMat.SetColor("_ColorThree", Color.white);
            this.mat = newMat;
        }
    }
}