using System;
using UnityEngine;
using UnityEngine.XR;
using Verse;

namespace TOT_DLL_test
{
    public class PawnRenderNodeWorker_UAV : PawnRenderNodeWorker_FlipWhenCrawling
    {
        public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
        {
            if (!base.CanDrawNow(node, parms))
            {
                return false;
            }
            if (parms.pawn == null)
            {
                return false;
            }
            return node != null;
        }
        public override Quaternion RotationFor(PawnRenderNode node, PawnDrawParms parms)
        {
            return base.RotationFor(node, parms);
        }
        private float PosX(PawnDrawParms parms)
        {
            return Mathf.Sin(Find.TickManager.TicksGame * 0.005f) * 0.04f;
        }
        private float PosY(PawnDrawParms parms)
        {
            return Mathf.Sin(Find.TickManager.TicksGame * 0.01f) * 0.1f;
        }
        public override Vector3 OffsetFor(PawnRenderNode node, PawnDrawParms parms, out Vector3 pivot)
        {
            Vector3 finalOffset = base.OffsetFor(node, parms, out pivot);
            PawnRenderNode_UAV uavNode = node as PawnRenderNode_UAV;
            if (uavNode != null)
            {
                float animX = PosX(parms);
                float animZ = PosY(parms) + 0.134f;
                float layerOffset;
                if (parms.facing == Rot4.North)
                {
                    layerOffset = 1f;
                }
                else
                {
                    layerOffset = -1f;
                }
                finalOffset += new Vector3(animX, layerOffset, animZ);
            }

            return finalOffset;
        }
    }
}