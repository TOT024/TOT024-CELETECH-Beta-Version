using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace TOT_DLL_test
{
    public class PawnRenderNodeProperties_HeadWithLight : PawnRenderNodeProperties
    {
        public PawnRenderNodeProperties_HeadWithLight()
        {
            this.nodeClass = typeof(PawnRenderNode_HeadWithLight);
            this.workerClass = typeof(PawnRenderNodeWorker_HeadWithLight);
        }
        public bool isApparel = true;
    }
}
