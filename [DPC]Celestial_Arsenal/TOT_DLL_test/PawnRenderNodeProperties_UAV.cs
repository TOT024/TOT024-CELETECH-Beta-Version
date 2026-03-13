using System;
using Verse;

namespace TOT_DLL_test
{
    public class PawnRenderNodeProperties_UAV : PawnRenderNodeProperties
    {
        public PawnRenderNodeProperties_UAV()
        {
            this.nodeClass = typeof(PawnRenderNode_UAV);
            this.workerClass = typeof(PawnRenderNodeWorker_UAV);
        }
        public bool drawUndrafted = true;
        public bool isApparel = true;
    }
}
