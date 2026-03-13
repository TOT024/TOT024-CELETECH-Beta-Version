using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace TOT_DLL_test
{
    public class PawnRenderNodeProperties_ApparelParent : PawnRenderNodeProperties
    {
        public PawnRenderNodeProperties_ApparelParent()
        {
            this.useGraphic = false;
            this.nodeClass = typeof(PawnRenderNode_ApparelParent);
            this.colorType = AttachmentColorType.Custom;
        }
    }
}
