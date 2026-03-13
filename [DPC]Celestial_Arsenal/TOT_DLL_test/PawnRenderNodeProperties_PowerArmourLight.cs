using System;
using Verse;

namespace TOT_DLL_test
{
    public class PawnRenderNodeProperties_PowerArmourLight : PawnRenderNodeProperties
    {
        public PawnRenderNodeProperties_PowerArmourLight()
        {
            this.nodeClass = typeof(PawnRenderNode_PowerArmourLight);
            this.workerClass = typeof(PawnRenderNodeWorker_PowerArmourLight);
        }
        public bool isApparel = true;
    }
}
