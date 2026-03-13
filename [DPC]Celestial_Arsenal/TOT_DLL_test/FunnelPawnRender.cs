using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace TOT_DLL_test
{
    public class FunnelDockPoint : IExposable
    {
        public Vector3 northOffset;
        public Vector3 eastOffset;
        public Vector3 southOffset;
        public Vector3 westOffset;

        public float northAngle;
        public float eastAngle;
        public float southAngle;
        public float westAngle;

        public Vector3 OffsetFor(Rot4 rot)
        {
            switch (rot.AsInt)
            {
                case 0: return northOffset;
                case 1: return eastOffset;
                case 2: return southOffset;
                case 3: return westOffset;
                default: return southOffset;
            }
        }
        public float AngleFor(Rot4 rot)
        {
            switch (rot.AsInt)
            {
                case 0: return northAngle;
                case 1: return eastAngle;
                case 2: return southAngle;
                case 3: return westAngle;
                default: return southAngle;
            }
        }
        public void ExposeData()
        {
            Scribe_Values.Look(ref northOffset, "northOffset");
            Scribe_Values.Look(ref eastOffset, "eastOffset");
            Scribe_Values.Look(ref southOffset, "southOffset");
            Scribe_Values.Look(ref westOffset, "westOffset");

            Scribe_Values.Look(ref northAngle, "northAngle", 0f);
            Scribe_Values.Look(ref eastAngle, "eastAngle", 0f);
            Scribe_Values.Look(ref southAngle, "southAngle", 0f);
            Scribe_Values.Look(ref westAngle, "westAngle", 0f);
        }
    }
    public class DynamicPawnRenderNodeSetup_FunnelDock : DynamicPawnRenderNodeSetup
    {
        private static readonly List<Type> setupAfter = new List<Type> { typeof(DynamicPawnRenderNodeSetup_Apparel) };
        public override List<Type> SetupAfter => setupAfter;
        public override bool HumanlikeOnly => true;

        public override IEnumerable<(PawnRenderNode node, PawnRenderNode parent)> GetDynamicNodes(Pawn pawn, PawnRenderTree tree)
        {
            if (pawn?.apparel == null || pawn.apparel.WornApparelCount == 0) yield break;

            if (!tree.TryGetNodeByTag(PawnRenderNodeTagDefOf.ApparelBody, out var apparelBodyNode))
            {
                tree.TryGetNodeByTag(PawnRenderNodeTagDefOf.Body, out apparelBodyNode);
            }
            if (apparelBodyNode == null) yield break;
            foreach (Apparel ap in pawn.apparel.WornApparel)
            {
                CompFunnelHauler comp = ap.TryGetComp<CompFunnelHauler>();
                if (comp == null) continue;

                int slotCount = comp.GetDockSlotCountForRender();
                if (slotCount <= 0) continue;
                float layerBase = apparelBodyNode.Props.baseLayer + 10f + comp.Props.renderBaseLayerOffset;
                for (int slot = 0; slot < slotCount; slot++)
                {
                    var pBase = new PawnRenderNodeProperties
                    {
                        debugLabel = $"FunnelDock_Base_{ap.def.defName}_{slot}",
                        workerClass = typeof(PawnRenderNodeWorker_FunnelDock),
                        parentTagDef = PawnRenderNodeTagDefOf.ApparelBody,
                        overrideMeshSize = new Vector2(1f, 1f),
                        baseLayer = layerBase,
                        drawSize = Vector2.one,
                        side = PawnRenderNodeProperties.Side.Center,
                        flipGraphic = false,
                        rotateIndependently = false,
                        useGraphic = false
                    };
                    if (tree.ShouldAddNodeToTree(pBase))
                    {
                        yield return (new PawnRenderNode_FunnelDock(pawn, pBase, tree, ap, slot, glowLayer: false), apparelBodyNode);
                    }
                    var pGlow = new PawnRenderNodeProperties
                    {
                        debugLabel = $"FunnelDock_Glow_{ap.def.defName}_{slot}",
                        workerClass = typeof(PawnRenderNodeWorker_FunnelDock),
                        parentTagDef = PawnRenderNodeTagDefOf.ApparelBody,
                        overrideMeshSize = new Vector2(1f, 1f),
                        baseLayer = layerBase,
                        drawSize = Vector2.one,
                        side = PawnRenderNodeProperties.Side.Center,
                        flipGraphic = false,
                        rotateIndependently = false,
                        useGraphic = false
                    };
                    if (tree.ShouldAddNodeToTree(pGlow))
                    {
                        yield return (new PawnRenderNode_FunnelDock(pawn, pGlow, tree, ap, slot, glowLayer: true), apparelBodyNode);
                    }
                }
            }
        }
    }
    public class PawnRenderNode_FunnelDock : PawnRenderNode
    {
        public readonly int slotIndex;
        public readonly bool glowLayer;
        public PawnRenderNode_FunnelDock(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree, Apparel apparel, int slotIndex, bool glowLayer)
            : base(pawn, props, tree)
        {
            this.apparel = apparel;
            this.slotIndex = slotIndex;
            this.glowLayer = glowLayer;
        }
        public bool TryGetHaulerComp(out CompFunnelHauler comp)
        {
            comp = apparel?.TryGetComp<CompFunnelHauler>();
            return comp != null;
        }
        public override bool FlipGraphic(PawnDrawParms parms)
        {
            bool flip = base.FlipGraphic(parms);
            if (parms.facing == Rot4.North || parms.facing == Rot4.South)
            {
                if (slotIndex >= 3)
                {
                    flip = !flip;
                }
            }

            return flip;
        }
    }
    public class PawnRenderNodeWorker_FunnelDock : PawnRenderNodeWorker
    {
        public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
        {
            if (!base.CanDrawNow(node, parms)) return false;
            if (!(node is PawnRenderNode_FunnelDock dockNode)) return false;
            if (!dockNode.TryGetHaulerComp(out CompFunnelHauler comp)) return false;

            int slot = dockNode.slotIndex;
            slot = CompFunnelHauler.UiOrderToSlot[slot];
            return comp.ShouldDrawDockSlot(slot);
        }
        public override void AppendDrawRequests(PawnRenderNode node, PawnDrawParms parms, List<PawnGraphicDrawRequest> requests)
        {
            if (!(node is PawnRenderNode_FunnelDock dockNode)) return;
            if (!dockNode.TryGetHaulerComp(out CompFunnelHauler comp)) return;
            if (!comp.TryGetDockMaterial(dockNode.slotIndex, dockNode.glowLayer, out Material mat, out _))
                return;
            Mesh mesh = node.GetMesh(parms);
            if (mesh == null || mat == null) return;
            requests.Add(new PawnGraphicDrawRequest(node, mesh, mat));
        }
        public override MaterialPropertyBlock GetMaterialPropertyBlock(PawnRenderNode node, Material material, PawnDrawParms parms)
        {
            MaterialPropertyBlock block = node.MatPropBlock;

            PawnRenderNode_FunnelDock dockNode = node as PawnRenderNode_FunnelDock;
            if (dockNode != null && dockNode.glowLayer)
            {
                block.SetColor(ShaderPropertyIDs.Color, material.color);
                return block;
            }
            Color apColor = Color.white;
            if (dockNode != null && dockNode.apparel != null)
            {
                apColor = dockNode.apparel.DrawColor;
            }
            Color tint = parms.tint;
            if (parms.Statue && parms.statueColor.HasValue)
            {
                block.SetColor(ShaderPropertyIDs.Color, parms.statueColor.Value * material.color);
                return block;
            }

            block.SetColor(ShaderPropertyIDs.Color, tint * apColor * material.color);
            return block;
        }
        public override Vector3 OffsetFor(PawnRenderNode node, PawnDrawParms parms, out Vector3 pivot)
        {
            Vector3 offset = base.OffsetFor(node, parms, out pivot);

            PawnRenderNode_FunnelDock dockNode = node as PawnRenderNode_FunnelDock;
            if (dockNode == null) return offset;

            CompFunnelHauler comp;
            if (!dockNode.TryGetHaulerComp(out comp)) return offset;

            Vector3 dockOffset;
            float ang;
            int t = GenTicks.TicksGame;
            if (comp.TryGetDockTransformWithBob(dockNode.slotIndex, parms.facing, t, out dockOffset, out ang))
                offset += dockOffset;

            return offset;
        }
        public override Quaternion RotationFor(PawnRenderNode node, PawnDrawParms parms)
        {
            Quaternion baseQ = Quaternion.identity;
            if (!(node is PawnRenderNode_FunnelDock dockNode)) return baseQ;
            if (!dockNode.TryGetHaulerComp(out CompFunnelHauler comp)) return baseQ;

            if (comp.TryGetDockTransform(dockNode.slotIndex, parms.facing, out _, out float ang))
            {
                return baseQ * Quaternion.AngleAxis(ang, Vector3.up);
            }
            return baseQ;
        }
        public override Vector3 ScaleFor(PawnRenderNode node, PawnDrawParms parms)
        {
            if (!(node is PawnRenderNode_FunnelDock dockNode)) return Vector3.one;
            if (!dockNode.TryGetHaulerComp(out CompFunnelHauler comp)) return Vector3.one;

            if (comp.TryGetDockDrawSize(dockNode.slotIndex, out Vector2 size))
                return new Vector3(size.x, 1f, size.y);

            return Vector3.one;
        }
        public override float LayerFor(PawnRenderNode n, PawnDrawParms parms)
        {
            PawnRenderNode_FunnelDock dockNode = n as PawnRenderNode_FunnelDock;
            if (dockNode == null) return base.LayerFor(n, parms);

            CompFunnelHauler comp;
            if (!dockNode.TryGetHaulerComp(out comp)) return base.LayerFor(n, parms);
            float visBase = (parms.facing == Rot4.North) ? 90f : -10f;
            visBase += comp.Props.renderBaseLayerOffset;
            int slot = dockNode.slotIndex;
            int rank = 0;
            switch (slot)
            {
                case 0: rank = 4; break;
                case 1: rank = 0; break;
                case 2: rank = 1; break;
                case 3: rank = 3; break;
                case 4: rank = 2; break;
                case 5: rank = 5; break;
                default: rank = 0; break;
            }
            float layer = visBase + rank * 0.05f;
            if (dockNode.glowLayer) layer += 0.01f;
            layer += n.debugLayerOffset;

            return layer;
        }
    }
}