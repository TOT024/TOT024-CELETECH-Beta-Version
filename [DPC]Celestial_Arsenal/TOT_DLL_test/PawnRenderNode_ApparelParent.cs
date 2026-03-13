using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using UnityEngine;

namespace TOT_DLL_test
{
    public class PawnRenderNode_ApparelParent : PawnRenderNode_Apparel
    {
        public PawnRenderNode_ApparelParent(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree, Apparel apparel) : base(pawn, props, tree, apparel)
        {
            this.apparel = apparel;
            this.useHeadMesh = true;
        }
        public override GraphicMeshSet MeshSetFor(Pawn pawn)
        {
            if (this.props.overrideMeshSize != null)
            {
                return MeshPool.GetMeshSetForSize(this.props.overrideMeshSize.Value.x, this.props.overrideMeshSize.Value.y);
            }
            return HumanlikeMeshPoolUtility.GetHumanlikeHeadSetForPawn(pawn, 1f, 1f);
        }
        public override Color ColorFor(Pawn pawn)
        {
            return apparel.DrawColor;
        }
        //public override Graphic GraphicFor(Pawn pawn)
        //{
        //    return GraphicDatabase.Get<Graphic_Multi>(base.Props.texPath, this.Props.shaderTypeDef.Shader, Vector2.one, this.ColorFor(pawn));
        //}
        //protected override IEnumerable<Graphic> GraphicsFor(Pawn pawn)
        //{
         //   yield return GraphicDatabase.Get<Graphic_Multi>(Props.texPath, Props.shaderTypeDef.Shader, Vector2.one, this.ColorFor(pawn));
         //   yield break;
        //}
    }
}
