using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace TOT_DLL_test
{
    internal class ThingDef_WithRenderNode : ThingDef
    {
        public List<PawnRenderNodeProperties> RenderNodeProperties
        {
            get
            {
                return this.renderNodeProperties;
            }
        }

        public List<PawnRenderNodeProperties> renderNodeProperties;
    }
}
