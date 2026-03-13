using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace TOT_DLL_test
{
    public class PawnRenderNodeWorker_UAVRework : PawnRenderNodeWorker
    {
        public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
        {
            bool result;
            if (!base.CanDrawNow(node, parms))
            {
                result = false;
            }
            else
            {
                PawnRenderNode_UAVRework uavNode = node as PawnRenderNode_UAVRework;
                result = uavNode.turretComp != null && uavNode.turretComp.PawnOwner != null && uavNode.turretComp.Released;
            }
            return result;
        }
        public override float LayerFor(PawnRenderNode node, PawnDrawParms parms)
        {
            DrawData drawData = node.Props.drawData;
            float result = ((drawData != null) ? drawData.LayerForRot(parms.facing, node.Props.baseLayer) : node.Props.baseLayer) + node.debugLayerOffset; ;
            PawnRenderNode_UAVRework uavNode = node as PawnRenderNode_UAVRework;
            if (uavNode.turretComp.currentPosition.z < parms.pawn.DrawPos.z)
            {
                result += 100f;
            }
            return result;
        }
        public override Quaternion RotationFor(PawnRenderNode node, PawnDrawParms parms)
        {
            return Quaternion.AngleAxis(0, Vector3.up);
        }
        public override Vector3 OffsetFor(PawnRenderNode node, PawnDrawParms parms, out Vector3 pivot)
        {
            PawnRenderNode_UAVRework uavNode = node as PawnRenderNode_UAVRework;
            Vector3 vector = Vector3.zero;
            pivot = PivotFor(node, parms);
            if (node.Props.drawData != null)
            {
                vector += node.Props.drawData.OffsetForRot(parms.facing);
            }
            vector += node.DebugOffset;
            Vector3 b;
            if (!parms.flags.FlagSet(PawnRenderFlags.Portrait) && node.TryGetAnimationOffset(parms, out b))
            {
                vector += b;
            }
            return vector + uavNode.turretComp.currentPosition - parms.pawn.DrawPos;
        }
        protected override Vector3 PivotFor(PawnRenderNode node, PawnDrawParms parms)
        {
            return parms.pawn.DrawPos;
        }
    }
}