using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace TOT_DLL_test
{
    public class PawnRenderNodeProperties_Weapon : PawnRenderNodeProperties
    {
        public PawnRenderNodeProperties_Weapon()
        {
            this.nodeClass = typeof(PawnRenderNode_Weapon);
            this.workerClass = typeof(PawnRenderNodeWorker_Weapon);
        }
        [NoTranslate]
        public string texPath_Undrafted;
        public bool colored = true;
    }
    public class PawnRenderNodeWorker_Weapon : PawnRenderNodeWorker
    {
        public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
        {
            bool flag = !base.CanDrawNow(node, parms);
            return !flag;
        }
        protected override Graphic GetGraphic(PawnRenderNode node, PawnDrawParms parms)
        {
            PawnRenderNodeProperties_Weapon pawnRenderNodeProperties_Weapon = node.Props as PawnRenderNodeProperties_Weapon;
            bool flag = !PawnRenderUtility.CarryWeaponOpenly(parms.pawn) && pawnRenderNodeProperties_Weapon.texPath_Undrafted != null;
            Graphic result;
            if (flag)
            {
                result = node.Graphics[0];
            }
            else
            {
                result = node.Graphics[1];
            }
            return result;
        }
    }
    public class PawnRenderNode_Weapon : PawnRenderNode
    {
        public PawnRenderNode_Weapon(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree) : base(pawn, props, tree)
        {
            PawnRenderNodeProperties_Weapon pawnRenderNodeProperties_Weapon = props as PawnRenderNodeProperties_Weapon;
        }
        protected override IEnumerable<Graphic> GraphicsFor(Pawn pawn)
        {
            PawnRenderNodeProperties_Weapon Props = this.props as PawnRenderNodeProperties_Weapon;
            yield return GraphicDatabase.Get<Graphic_Multi>(Props.texPath_Undrafted, ShaderFor(pawn), Vector2.one, ColorFor(pawn));
            yield return GraphicDatabase.Get<Graphic_Multi>(Props.texPath, ShaderFor(pawn), Vector2.one, ColorFor(pawn));
        }
        public override Color ColorFor(Pawn pawn)
        {
            Color result = Color.white;
            PawnRenderNodeProperties_Weapon pawnRenderNodeProperties_Weapon = this.props as PawnRenderNodeProperties_Weapon;
            bool flag = this.weapon.def.MadeFromStuff && pawnRenderNodeProperties_Weapon.colored;
            if (flag)
            {
                result = this.weapon.Stuff.stuffProps.color;
            }
            return result;
        }
        public ThingWithComps weapon;
    }
}
