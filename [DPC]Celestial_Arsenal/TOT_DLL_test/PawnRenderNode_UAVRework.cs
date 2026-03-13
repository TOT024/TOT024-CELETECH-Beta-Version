using System;
using UnityEngine;
using Verse;

namespace TOT_DLL_test
{
    public class PawnRenderNode_UAVRework : PawnRenderNode
    {
        public PawnRenderNode_UAVRework(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree) : base(pawn, props, tree)
        {
        }
        public override Graphic GraphicFor(Pawn pawn)
        {
            Graphic result;
            if (base.Props.texPath != null)
            {
                Shader shader = this.props.shaderTypeDef.Shader;
                PawnRenderNodeProperties_UAVRework props_UAV = props as PawnRenderNodeProperties_UAVRework;
                if(props_UAV.useforcedColor)
                {
                    result = GraphicDatabase.Get<Graphic_Multi>(base.Props.texPath, shader, (Vector2)Props.overrideMeshSize, ColorFor(pawn));
                }
                else
                {
                    result = GraphicDatabase.Get<Graphic_Multi>(base.Props.texPath, shader);
                }
            }
            else
            {
                result = GraphicDatabase.Get<Graphic_Single>(this.turretComp.Props.turretDef.graphicData.texPath, ShaderDatabase.CutoutComplex);
            }
            return result;
        }
        public Comp_FloatingGunRework turretComp
        {
            get
            {
                if (this.TurretComp == null)
                {
                    TurretComp = this.apparel.TryGetComp<Comp_FloatingGunRework>();
                }
                return this.TurretComp;
            }
        }
        public override Color ColorFor(Pawn pawn)
        {
            return turretComp.parent.DrawColor;
        }
        public Comp_FloatingGunRework TurretComp;
    }
}
