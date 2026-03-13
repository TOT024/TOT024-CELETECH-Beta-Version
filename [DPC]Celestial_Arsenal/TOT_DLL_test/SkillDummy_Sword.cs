using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TOT_DLL_test
{
    public class SkillDummy_Sword : ThingWithComps, IThingHolder
    {
        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, this.GetDirectlyHeldThings());
        }
        public ThingOwner GetDirectlyHeldThings()
        {
            return this.innerContainer;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look<ThingOwner>(ref this.innerContainer, "savedpawn", new object[]
            {
        this
            });
            Scribe_Values.Look<int>(ref this.tickdown, "tickleft", 0, false);
            Scribe_Values.Look<bool>(ref this.IsSword, "sword", false, false);
            Scribe_Deep.Look<Mote>(ref this.CMC_SSMote, "CMC_Mote", Array.Empty<object>());
        }
        public void SpawnSetUp(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
        }
        public void Insert(Pawn pawn)
        {
            this.innerContainer.ClearAndDestroyContents(DestroyMode.Vanish);
            tickdown = 1200;
            selected = Find.Selector.IsSelected(pawn);
            pawn.DeSpawn();
            if (pawn.holdingOwner != null)
            {
                pawn.holdingOwner.TryTransferToContainer(pawn, this.innerContainer, true);
            }
            else
            {
                this.innerContainer.TryAdd(pawn, true);
            }
        }
        protected override void Tick()
        {
            tickdown--;
            if (CMC_SSMote.DestroyedOrNull())
            {
                ThingDef Mote = CMC_Def.CMC_Mote_SwordShowerPawnBG;
                Vector3 Offset = new Vector3(0f, 0f, 0.05f);
                this.CMC_SSMote = MoteMaker.MakeAttachedOverlay(this, Mote, PawnDrawOffset(), 2.3f);
                this.CMC_SSMote.exactRotation = 0f;
            }
            this.CMC_SSMote.Maintain();
            if (tickdown <= 0)
            {
                this.innerContainer.TryDropAll(this.Position, Map, ThingPlaceMode.Direct, null, null, false);
                Pawn pawntosave = this.Position.GetFirstPawn(Map);
                pawntosave.drafter.Drafted = true;
                if (selected)
                {
                    Find.Selector.Select(pawntosave);
                }
                this.Destroy(DestroyMode.Vanish);
            }
            base.Tick();
        }
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            Map map = base.Map;
            base.Destroy(mode);
            if (this.innerContainer.Count > 0 && (mode == DestroyMode.Deconstruct || mode == DestroyMode.KillFinalize))
            {
                this.innerContainer.TryDropAll(base.Position, map, ThingPlaceMode.Near, null, null, true);
            }
            this.innerContainer.ClearAndDestroyContents(DestroyMode.Vanish);
        }
        public void CastSwordShower()
        {

        }
        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            base.DrawAt(drawLoc, flip);
            Pawn heldPawn = this.HeldPawn;
            if (heldPawn != null)
            {
                Rot4 value = Rot4.South;
                heldPawn.Drawer.renderer.RenderPawnAt(drawLoc + PawnDrawOffset(), value, false);
            }
        }
        public Vector3 PawnDrawOffset()
        {
            float x = Mathf.Sin((float)Find.TickManager.TicksGame * 0.01f) * 0.04f;
            return new Vector3(0f, 5f, 1.6f + x);
        }
        public Pawn HeldPawn
        {
            get
            {
                return this.innerContainer.FirstOrDefault((Thing x) => x is Pawn) as Pawn;
            }
        }

        public float HeldPawnBodyAngle
        {
            get
            {
                return 0f;
            }
        }
        public float HeldPawnDrawPos_Y
        {
            get
            {
                return this.DrawPos.y + 3f;
            }
        }
        public PawnPosture HeldPawnPosture
        {
            get
            {
                return PawnPosture.Standing;
            }
        }
        public ThingOwner innerContainer = new ThingOwner<Thing>();
        public int tickdown = 1200;
        public bool selected = false;
        public bool IsSword = false;
        public bool ShouldDrawPawn = false;
        public Mote CMC_SSMote;
    }
}
