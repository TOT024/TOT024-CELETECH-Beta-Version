using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TOT_DLL_test
{
    [StaticConstructorOnStartup]
    public class Projectile_XM_Re : Projectile_Piercing
    {
        private List<Vector3> trailHistory = new List<Vector3>();
        private const int MaxTrailLength = 40;
        private const float TrailWidthStart = 0.12f;
        private const float TrailWidthEnd = 0.01f;
        private static readonly Material TrailMat = MaterialPool.MatFrom(
            GenDraw.LineTexPath,
            ShaderDatabase.MoteGlow,
            new Color(0.1f, 0.8f, 0.8f, 1f)
        );
        private Vector3 CurretPos(float t)
        {
            return this.origin + (this.destination - this.origin) * t;
        }
        protected override void DrawAt(Vector3 position, bool flip = false)
        {
            Vector3 b = this.CurretPos(base.DistanceCoveredFraction - 0.01f);
            position = this.CurretPos(base.DistanceCoveredFraction);
            Quaternion rotation = Quaternion.LookRotation(position - b);
            if (this.tickcount > 8)
            {
                Vector3 position2 = this.lastposition;
                position2.y = AltitudeLayer.Projectile.AltitudeFor();
                Graphics.DrawMesh(MeshPool.GridPlane(this.def.graphicData.drawSize), position2, rotation, this.DrawMat, 0);
                base.Comps_PostDraw();
            }
            UpdateAndDrawTrail();
        }
        public void UpdateAndDrawTrail()
        {
            if (!Find.TickManager.Paused)
            {
                Vector3 enginePos = DrawPos;
                enginePos.y -= 0.1f;
                trailHistory.Insert(0, enginePos);

                if (trailHistory.Count > MaxTrailLength)
                {
                    trailHistory.RemoveAt(trailHistory.Count - 1);
                }
            }
            if (trailHistory.Count < 2) return;
            for (int i = 0; i < trailHistory.Count - 1; i++)
            {
                Vector3 start = trailHistory[i];
                Vector3 end = trailHistory[i + 1];
                float pct = (float)i / trailHistory.Count;
                float width = Mathf.Lerp(TrailWidthStart, TrailWidthEnd, pct);
                GenDraw.DrawLineBetween(start, end, TrailMat, width);
            }
        }
        public override void Launch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, bool preventFriendlyFire = false, Thing equipment = null, ThingDef targetCoverDef = null)
        {
            this.lastposition = origin;
            this.tickcount = 0;
            base.Launch(launcher, origin, usedTarget, intendedTarget, hitFlags, preventFriendlyFire, equipment, targetCoverDef);
        }
        protected override void Tick()
        {
            base.Tick();
            this.tickcount++;
            if (this.intendedTarget.Thing != null)
            {
                this.destination = this.intendedTarget.Thing.DrawPos;
            }
        }
        private void SpawnFleck()
        {
            Map map = base.Map;
            if (map == null)
            {
                return;
            }
            Vector3 drawPos = this.DrawPos;

            FleckCreationData fleckData = new FleckCreationData
            {
                def = DefDatabase<FleckDef>.GetNamed("CMC_Fleck_HitFlash", true),
                spawnPosition = drawPos,
                scale = 0.39f,
                rotation = (float)Rand.Range(0, 360),
                velocitySpeed = 0f,
                rotationRate = 0f,
                orbitSpeed = 0f,
                ageTicksOverride = -1
            };
            map.flecks.CreateFleck(fleckData);

            for (int i = 0; i <= 7; i++)
            {
                FleckCreationData fleckData2 = new FleckCreationData
                {
                    def = DefDatabase<FleckDef>.GetNamed("SparkFlash", true),
                    spawnPosition = drawPos,
                    scale = 0.64f,
                    velocitySpeed = 0f,
                    rotationRate = 0f,
                    rotation = (float)Rand.Range(0, 360),
                    ageTicksOverride = -1
                };
                map.flecks.CreateFleck(fleckData2);
            }
            FleckMaker.ThrowAirPuffUp(drawPos, map);
        }
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            if(this.intendedTarget.Thing != null && (this.DrawPos - this.intendedTarget.Thing.DrawPos).magnitude < 1f)
            {
                hitThing = this.intendedTarget.Thing;
            }
            base.Impact(hitThing, blockedByShield);
            this.SpawnFleck();
        }
        public int CurrentTick;
        public int tickcount;
        public Vector3 lastposition;
    }
}
