using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace TOT_DLL_test
{
    public class Mote_ScaleAndRotate : Mote
    {
        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            this.Graphic.Draw(drawLoc, base.Rotation, this, exactRotation);
        }
        protected override void TimeInterval(float deltaTime)
        {
            if (this.EndOfLife && !base.Destroyed)
            {
                this.Destroy(DestroyMode.Vanish);
                return;
            }
            if (this.def.mote.needsMaintenance && Find.TickManager.TicksGame - 1 > this.lastMaintainTick)
            {
                int num = this.def.mote.fadeOutTime.SecondsToTicks();
                if (!this.def.mote.fadeOutUnmaintained || Find.TickManager.TicksGame - this.lastMaintainTick > num)
                {
                    this.Destroy(DestroyMode.Vanish);
                    return;
                }
            }
            if (this.def.mote.scalers != null)
            {
                this.curvedScale = this.def.mote.scalers.ScaleAtTime(this.AgeSecs);
            }
        }
        public void MaintainMote()
        {
            this.lastMaintainTick = Find.TickManager.TicksGame;
        }

        protected override void Tick()
        {
            base.Tick();
            this.exactRotation = Find.TickManager.TicksGame % 360f;
            if (Mathf.Abs(this.tickimpact - tickspawned) > 0)
            {
                this.currentscale = iniscale * ((float)(Find.TickManager.TicksGame - tickspawned) / (float)(this.tickimpact - tickspawned) * 0.5f + 1f); ;
                this.linearScale = new Vector3(currentscale, currentscale, currentscale);
                this.Graphic.drawSize = this.linearScale;
            }
            if (this.link1.Linked)
            {
                bool flag = this.detachAfterTicks == -1 || Find.TickManager.TicksGame - this.spawnTick < this.detachAfterTicks;
                if (!this.link1.Target.ThingDestroyed && flag)
                {
                    this.link1.UpdateDrawPos();
                    if (this.link1.rotateWithTarget)
                    {
                        base.Rotation = this.link1.Target.Thing.Rotation;
                    }
                }
                Vector3 b = this.def.mote.attachedDrawOffset;
                this.exactPosition = this.link1.LastDrawPos + b;
                IntVec3 intVec = this.exactPosition.ToIntVec3();
                if (base.Spawned && !intVec.InBounds(base.Map))
                {
                    this.Destroy(DestroyMode.Vanish);
                    return;
                }
                base.Position = intVec;
            }
        }
        public float iniscale;
        public float currentscale;
        public int tickimpact;
        public int tickspawned;
        private int lastMaintainTick;
    }
}
