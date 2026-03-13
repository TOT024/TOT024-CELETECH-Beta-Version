using System;
using Verse;

namespace TOT_DLL_test
{
    public class PawnRenderNodeProperties_UAVRework : PawnRenderNodeProperties
    {
        public PawnRenderNodeProperties_UAVRework()
        {
            this.nodeClass = typeof(PawnRenderNode_UAVRework);
            this.workerClass = typeof(PawnRenderNodeWorker_UAVRework);
        }
        public bool drawUndrafted = true;
        public bool isApparel = true;
        public bool useBodyPartAnchor = false;
        public bool useforcedColor = true; 
    }
}
