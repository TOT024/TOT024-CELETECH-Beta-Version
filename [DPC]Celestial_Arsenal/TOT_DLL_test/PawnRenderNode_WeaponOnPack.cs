using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace TOT_DLL_test
{
    public class PawnRenderNodeProperties_WeaponOnPack : PawnRenderNodeProperties
    {
        public PawnRenderNodeProperties_WeaponOnPack()
        {
            this.nodeClass = typeof(PawnRenderNode_WeaponOnPack);
            this.workerClass = typeof(PawnRenderNodeWorker_WeaponOnPack);
        }
        [NoTranslate]
        public bool colored = true;
    }
    public class PawnRenderNodeWorker_WeaponOnPack : PawnRenderNodeWorker
    {
        public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
        {
            if (!base.CanDrawNow(node, parms)) return false;
            CompApparelWeaponHolder comp = node.apparel?.GetComp<CompApparelWeaponHolder>();
            return comp != null && comp.AnyWeaponInBelt;
        }
        protected override Graphic GetGraphic(PawnRenderNode node, PawnDrawParms parms)
        {
            PawnRenderNodeProperties_Weapon pawnRenderNodeProperties_Weapon = node.Props as PawnRenderNodeProperties_Weapon;
            return node.Graphics[0];
        }
    }
    public class PawnRenderNode_WeaponOnPack : PawnRenderNode
    {
        public PawnRenderNode_WeaponOnPack(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree) : base(pawn, props, tree)
        {
            PawnRenderNodeProperties_WeaponOnPack pawnRenderNodeProperties_Weapon = props as PawnRenderNodeProperties_WeaponOnPack;
        }
        protected override IEnumerable<Graphic> GraphicsFor(Pawn pawn)
        {
            PawnRenderNodeProperties_WeaponOnPack Props = this.props as PawnRenderNodeProperties_WeaponOnPack;
            yield return GraphicDatabase.Get<Graphic_Multi>(Props.texPath, ShaderFor(pawn), Vector2.one, ColorFor(pawn));
        }
        public ThingWithComps weapon;
    }
}
