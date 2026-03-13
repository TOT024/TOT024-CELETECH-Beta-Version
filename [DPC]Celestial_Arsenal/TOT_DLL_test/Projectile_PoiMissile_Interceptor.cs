using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TOT_DLL_test
{
    [StaticConstructorOnStartup]
    public class Projectile_PoiMissile_Interceptor : Projectile_Explosive
    {
        private static readonly Material ShadowMat = MaterialPool.MatFrom("Things/Others/DropThemSpotShadow", ShaderDatabase.Transparent, new Color(1f, 1f, 1f, 0.5f));
        private bool Tryinit = false;
        public Vector3 position1;
        public Vector3 position2;
        public Vector3 P0;
        public Vector3 P1;
        public Vector3 P2;
        public Vector3 P3;
        public float Randf1;
        public float Randf2;
        public float Randf3;
        private Vector3 lasttargetpos = new Vector3();
        private bool targetinit = false;
        private bool lostTargetMode = false;

        public Mote_ScaleAndRotate mote;

        public FloatRange Fleck_Angle = new FloatRange(-180f, 180f);
        public FloatRange Fleck_Speed = new FloatRange(0.1f, 0.2f);
        public FloatRange Fleck_Rotation = new FloatRange(-180f, 180f);
        public FleckDef FleckDef2 = DefDatabase<FleckDef>.GetNamed("CMC_Fleck_ProjectileSmoke_LongLasting", false) ?? FleckDefOf.Smoke;
        public Quaternion rotation;
        public float DCFExport;
        private List<Vector3> recentPositions = new List<Vector3>();
        private const int POSITION_HISTORY_COUNT = 10;
        private Vector3 positionTwoFramesAgo;
        private Vector3 previousPosition;

        public override Vector3 ExactPosition
        {
            get
            {
                if (this.position2 != null)
                    return this.position2;
                else
                    return base.ExactPosition;
            }
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            recentPositions = new List<Vector3>();
            for (int i = 0; i < POSITION_HISTORY_COUNT; i++)
            {
                recentPositions.Add(this.DrawPos);
            }
        }
        private List<Vector3> trailHistory = new List<Vector3>();
        private const int MaxTrailLength = 120;
        private const float TrailWidthStart = 1.23f;
        private const float TrailWidthEnd = 0.25f;
        private static readonly Material TrailMat = MaterialPool.MatFrom(
            GenDraw.LineTexPath,
            ShaderDatabase.Transparent,
            new Color(0.55f, 0.55f, 0.55f, 1f)
        );
        private void UpdateAndDrawTrail()
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
        protected override void Tick()
        {
            if (this.DestroyedOrNull()) return;

            positionTwoFramesAgo = previousPosition;
            previousPosition = this.DrawPos;

            position1 = this.BPos(base.DistanceCoveredFraction);
            position2 = this.BPos(base.DistanceCoveredFraction - 0.01f);

            if ((position1 - position2).sqrMagnitude > 0.0001f)
            {
                rotation = Quaternion.LookRotation(position1 - this.position2);
            }

            DCFExport = this.DistanceCoveredFraction;

            bool targetIsAlive = this.intendedTarget != null && !intendedTarget.Thing.DestroyedOrNull();

            if (targetIsAlive)
            {
                if (!targetinit)
                {
                    this.lasttargetpos = this.intendedTarget.Thing.DrawPos;
                    targetinit = true;
                }
                else
                {
                    this.destination = this.intendedTarget.Thing.DrawPos;
                    this.lasttargetpos = this.destination;

                    if (mote.DestroyedOrNull())
                    {
                        ThingDef mote_locked = CMC_Def.CMC_Mote_MissileLocked;
                        if (this.launcher != null && this.launcher.Faction != null)
                        {
                            mote_locked.graphicData.color = this.launcher.Faction.Color;
                        }
                        Vector3 offset = new Vector3(0f, 0f, 0f)
                        {
                            y = AltitudeLayer.PawnRope.AltitudeFor()
                        };
                        mote = (Mote_ScaleAndRotate)ThingMaker.MakeThing(mote_locked, null);
                        mote.Attach(this.intendedTarget.Thing, offset, false);
                        mote.Scale = this.def.graphicData.drawSize.x * 2f;
                        mote.iniscale = this.def.graphicData.drawSize.x * 2f;
                        mote.exactPosition = this.intendedTarget.Thing.DrawPos + offset;
                        mote.solidTimeOverride = 9999f;
                        mote.tickimpact = ticksToImpact + this.TickSpawned;
                        mote.tickspawned = TickSpawned;
                        GenSpawn.Spawn(mote, intendedTarget.Thing.Position, Map, WipeMode.Vanish);
                    }
                    else
                    {
                        mote.MaintainMote();
                    }
                }
            }
            else
            {
                if (!lostTargetMode)
                {
                    lostTargetMode = true;
                    if (mote != null && !mote.Destroyed)
                    {
                        mote.tickimpact = Find.TickManager.TicksGame;
                    }
                }

                this.destination = this.lasttargetpos;
            }
            base.Tick();
        }
        public Vector3 BPos(float t)
        {
            t = Mathf.Clamp01(t);
            bool flag = !this.Tryinit;
            if (flag)
            {
                this.Randf1 = Rand.Range(-0.1f, 0.1f);
                this.Randf2 = Rand.Range(-0.1f, 0.05f);
                this.Randf3 = Rand.Range(25f, 40f);
                this.Tryinit = true;
            }
            this.P0 = this.origin;
            this.P1 = this.origin + (this.destination - this.origin) * (0.3f + this.Randf1);
            this.P2 = this.origin + (this.destination - this.origin) * (0.8f + this.Randf2) + new Vector3(0f, 0f, this.Randf3);
            this.P3 = this.destination;
            return this.P0 * Mathf.Pow(1f - t, 3f) + 3f * this.P1 * t * Mathf.Pow(1f - t, 2f) + 3f * this.P2 * Mathf.Pow(t, 2f) * (1f - t) + this.P3 * Mathf.Pow(t, 3f);
        }
        protected override void DrawAt(Vector3 position, bool flip = false)
        {
            Vector3 b = this.BPos(base.DistanceCoveredFraction - 0.01f);
            position = this.BPos(base.DistanceCoveredFraction);

            if ((position - b).sqrMagnitude > 0.001f)
            {
                Quaternion rotation = Quaternion.LookRotation(position - b);

                Vector3 shadowPos = position;
                shadowPos.y = AltitudeLayer.Shadows.AltitudeFor();
                Vector3 shadowScale = new Vector3(this.def.graphicData.drawSize.x * 1.2f, 1f, this.def.graphicData.drawSize.y * 1.2f);
                Matrix4x4 shadowMatrix = Matrix4x4.TRS(shadowPos, rotation, shadowScale);
                Graphics.DrawMesh(MeshPool.plane10, shadowMatrix, ShadowMat, 0);

                Vector3 position2 = position;
                position2.y = AltitudeLayer.Projectile.AltitudeFor();
                Graphics.DrawMesh(MeshPool.GridPlane(this.def.graphicData.drawSize), position2, rotation, this.DrawMat, 0);
            }
            UpdateAndDrawTrail();
            base.Comps_PostDraw();
        }

        protected override void TickInterval(int delta)
        {
            base.TickInterval(delta);
            if (this.DestroyedOrNull()) return;
            UpdatePositionHistory(this.BPos(base.DistanceCoveredFraction - 0.02f));
        }

        private void UpdatePositionHistory(Vector3 newPosition)
        {
            recentPositions.Insert(0, newPosition);
            if (recentPositions.Count > POSITION_HISTORY_COUNT)
            {
                recentPositions.RemoveAt(recentPositions.Count - 1);
            }
        }
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            int smokeParticles = 7;
            for (int i = 0; i < smokeParticles; i++)
            {
                Vector3 center = Position.ToVector3Shifted();
                float angle = Rand.Range(0f, 360f);
                float dist = Rand.Range(0f, 1.5f);
                Vector3 offset = new Vector3(Mathf.Cos(angle) * dist, 2f, Mathf.Sin(angle) * dist);

                FleckCreationData data = FleckMaker.GetDataStatic(center + offset, Map, FleckDefOf.Smoke, Rand.Range(4.5f, 5.0f));
                data.rotation = Rand.Range(0f, 360f);
                data.velocityAngle = angle;
                data.velocitySpeed = Rand.Range(0.2f, 0.22f);
                Map.flecks.CreateFleck(data);
            }
            FleckMaker.ThrowHeatGlow(this.DrawPos.ToIntVec3(), Map, 1.5f);
            this.TryIntercept();
            this.Destroy(DestroyMode.Vanish);
        }
        public void TryIntercept()
        {
            IntVec3 center = IntVec3.FromVector3(this.DrawPos);
            IEnumerable<IntVec3> enumerable = GenRadial.RadialCellsAround(center, 5f, true);
            foreach (IntVec3 c in enumerable)
            {
                if (c.InBounds(base.Map))
                {
                    foreach (ThingDef def in ProjectileCache.ProjectileDefs)
                    {
                        Thing thing = c.GetFirstThing(Map, def);
                        if (thing != null && Rand.Chance(0.75f) && thing != this)
                        {
                            FleckMaker.ThrowMicroSparks(thing.DrawPos, Map);
                            CMC_Def.CMC_Bomb_Air.Spawn(thing.DrawPos.ToIntVec3(), base.Map, Rand.Range(3.7f, 3.1f));
                            thing.Destroy();
                        }
                    }
                }
            }
        }
    }
}