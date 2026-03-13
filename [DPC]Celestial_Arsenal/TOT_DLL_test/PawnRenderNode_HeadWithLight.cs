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
    public class PawnRenderNode_HeadWithLight : PawnRenderNode_Apparel
    {
        public PawnRenderNode_HeadWithLight(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree, Apparel apparel) : base(pawn, props, tree, apparel)
        {
            this.apparel = apparel;
            this.useHeadMesh = true;
        }
        protected override IEnumerable<Graphic> GraphicsFor(Pawn pawn)
        {
           yield return GraphicDatabase.Get<Graphic_Multi>(Props.texPath, Props.shaderTypeDef.Shader, Vector2.one, this.ColorFor(pawn));
           yield break;
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
    }
}
