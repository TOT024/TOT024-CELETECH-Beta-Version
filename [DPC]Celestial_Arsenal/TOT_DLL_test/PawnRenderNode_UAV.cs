using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace TOT_DLL_test
{
    public class PawnRenderNode_UAV : PawnRenderNode
    {
        public PawnRenderNode_UAV(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree) : base(pawn, props, tree)
        {
        }
        public override Graphic GraphicFor(Pawn pawn)
        {
            if(this.Props.shaderTypeDef == ShaderTypeDefOf.CutoutComplex)
                return GraphicDatabase.Get<Graphic_Multi>(base.Props.texPath, ShaderDatabase.CutoutComplex, Vector2.one, this.ColorFor(pawn));
            else
                return GraphicDatabase.Get<Graphic_Multi>(base.Props.texPath, ShaderDatabase.MoteGlow);
        }
        public override Color ColorFor(Pawn pawn)
        {
            return this.apparel.DrawColor;
        }
    }
}
