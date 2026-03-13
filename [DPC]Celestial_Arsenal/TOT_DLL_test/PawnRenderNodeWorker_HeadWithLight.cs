using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace TOT_DLL_test
{
    public class PawnRenderNodeWorker_HeadWithLight : PawnRenderNodeWorker_Apparel_Head
    {
        public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
        {
            return base.CanDrawNow(node, parms);
        }
        public override Quaternion RotationFor(PawnRenderNode node, PawnDrawParms parms)
        {
            Quaternion quaternion = base.RotationFor(node, parms);
            return quaternion;
        }
        public override void PreDraw(PawnRenderNode node, Material mat, PawnDrawParms parms)
        {
            node.MatPropBlock.SetColor(ShaderPropertyIDs.Color, parms.tint * mat.color);
        }
    }
}
