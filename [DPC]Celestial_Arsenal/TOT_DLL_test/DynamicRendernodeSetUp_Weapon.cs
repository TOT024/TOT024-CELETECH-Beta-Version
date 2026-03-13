using System;
using System.Collections.Generic;
using Verse;
using RimWorld;

namespace TOT_DLL_test
{
    public class DynamicPawnRenderNodeSetup_Weapon : DynamicPawnRenderNodeSetup
    {
        public override bool HumanlikeOnly
        {
            get
            {
                return false;
            }
        }

        public override IEnumerable<(PawnRenderNode node, PawnRenderNode parent)> GetDynamicNodes(Pawn pawn, PawnRenderTree tree)
        {
            ThingDef_WithRenderNode def = pawn.equipment?.Primary?.def as ThingDef_WithRenderNode;
            if (def?.RenderNodeProperties == null)
            {
                yield break;
            }
            foreach(PawnRenderNodeProperties renderNodeProperties in def.RenderNodeProperties)
            {
                if(tree.ShouldAddNodeToTree(renderNodeProperties))
                {
                    PawnRenderNode_Weapon pawnRenderNode_Weapon = (PawnRenderNode_Weapon)Activator.CreateInstance(renderNodeProperties.nodeClass, pawn, renderNodeProperties, tree);
                    pawnRenderNode_Weapon.weapon = pawn.equipment.Primary;
                    yield return (node: pawnRenderNode_Weapon, parent: null);
                }
            }
        }
    }
}