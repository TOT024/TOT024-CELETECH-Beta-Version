using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace TOT_DLL_test
{
    [StaticConstructorOnStartup]
    public class Projectile_RocketAntiAir : Projectile_Explosive
    {
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
        }
        private Quaternion adjustedRot
        {
            get
            {
                if (!(this.lastPos == Vector3.zero))
                {
                    return Quaternion.LookRotation((this.DrawPos - this.lastPos).Yto0());
                }
                return this.ExactRotation;
            }
        }
        private float ArcHeightFactor
        {
            get
            {
                float num = this.def.projectile.arcHeightFactor;
                float num2 = (this.destination - this.origin).MagnitudeHorizontalSquared();
                if (num * num > num2 * 0.2f * 0.2f)
                {
                    num = Mathf.Sqrt(num2) * 0.2f;
                }
                return num;
            }
        }
        public override Vector3 DrawPos
        {
            get
            {
                Vector3 basePos = this.ExactPosition;
                Vector3 arcOffset = new Vector3(0f, 0f, 1f) * (this.ArcHeightFactor * GenMath.InverseParabola(base.DistanceCoveredFraction));
                Vector3 wobbleOffset = Vector3.zero;
                if (this.destination != this.origin)
                {
                    Vector3 flightVec = this.destination - this.origin;
                    float totalDist = flightVec.MagnitudeHorizontal();
                    Vector3 normalizedDir = flightVec.normalized;
                    Vector3 perpendicular = new Vector3(-normalizedDir.z, 0f, normalizedDir.x);
                    float wavePhase = (this.livecount + this.thingIDNumber * 13) * 0.25f;
                    float sinWave = Mathf.Sin(wavePhase);
                    float travelCurve = GenMath.InverseParabola(base.DistanceCoveredFraction);
                    float distScale = Mathf.Clamp(totalDist / 15f, 0.2f, 1.5f);
                    float amplitude = 0.07f * distScale * travelCurve;

                    wobbleOffset = perpendicular * sinWave * amplitude;
                }
                return basePos + arcOffset + wobbleOffset;
            }
        }
        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            Graphics.DrawMesh(MeshPool.GridPlane(this.def.graphicData.drawSize), this.DrawPos, this.adjustedRot, this.DrawMat, 0);
            base.Comps_PostDraw();
            if (true)
            {
                if (this.intendedTarget != null && this.intendedTarget.Thing != null)
                {
                    Vector3 vector = this.intendedTarget.Thing.DrawPos;
                    Vector3 a = this.DrawPos;
                    vector.y = AltitudeLayer.Projectile.AltitudeFor();
                    a.y = vector.y;
                    GenDraw.DrawLineBetween(a, vector, TargetLineMat, 0.25f);
                }
            }
            UpdateAndDrawTrail();
        }
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
            base.Position = this.DrawPos.ToIntVec3();
            this.lastPos = this.DrawPos;
            this.Fleck_MakeFleckTick++;
            this.livecount++;
            if(true)
            {
                if (this.intendedTarget != null && this.intendedTarget.Thing != null)
                {
                    if (!targetinit)
                    {
                        this.lasttargetpos = this.intendedTarget.Thing.DrawPos;
                        targetinit = true;
                    }
                    if (this.intendedTarget != null && this.lasttargetpos != null && ((this.intendedTarget.Thing.DrawPos - this.lasttargetpos).magnitude > 0.95f || this.intendedTarget.Cell.AnyGas(Map, GasType.BlindSmoke)))
                    {
                        this.intendedTarget = null;
                    }
                    else
                    {
                        this.destination = this.intendedTarget.Thing.DrawPos;
                        this.lasttargetpos = this.destination;
                        if (this.mote.DestroyedOrNull())
                        {
                            ThingDef cmc_Mote_SWTargetLocked = CMC_Def.CMC_Mote_SWTargetLocked;
                            Vector3 offset = new Vector3(0f, 0f, 0f)
                            {
                                y = AltitudeLayer.PawnRope.AltitudeFor()
                            };
                            this.mote = MoteMaker.MakeAttachedOverlay(this.intendedTarget.Thing, cmc_Mote_SWTargetLocked, offset, 1f, 1f);
                            this.mote.exactRotation = 45f;
                        }
                        else
                        {
                            this.mote.Maintain();
                        }
                        if (this.mote2.DestroyedOrNull())
                        {
                            ThingDef cmc_Mote_SWTargetLocked = CMC_Def.CMC_Mote_SWTargetLocked_Circle;
                            Vector3 offset = new Vector3(0f, 0f, 0f)
                            {
                                y = AltitudeLayer.PawnRope.AltitudeFor()
                            };
                            this.mote2 = MoteMaker.MakeAttachedOverlay(this, cmc_Mote_SWTargetLocked, offset, 1.2f, 1f);
                            this.mote2.exactRotation = 0f;
                        }
                        else
                        {
                            this.mote2.Maintain();
                        }
                    }
                }
            }
            if (base.DistanceCoveredFraction > 0.99f)
            {
                this.ImpactSomething();
            }
            else
            {
                base.Tick();
            }
        }
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            int smokeParticles = 5;
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
            //CMC_EffectsTool.SpawnExplosionBlask(base.Position.ToVector3() - new Vector3(0f, 0f, 2f), base.Map, CMC_EffectsTool.FleckDef_Blask, Rand.Range(1.5f, 2.3f), 0f, 0f, (float)Rand.Range(0, 360));
            FleckMaker.ThrowHeatGlow(this.DrawPos.ToIntVec3(), Map, 0.75f);
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
        private List<Vector3> trailHistory = new List<Vector3>();
        private const int MaxTrailLength = 80;
        private const float TrailWidthStart = 1.33f;
        private const float TrailWidthEnd = 0.05f;
        private static readonly Material TrailMat = MaterialPool.MatFrom(
            GenDraw.LineTexPath,
            ShaderDatabase.Transparent,
            new Color(0.55f, 0.55f, 0.55f, 1f)
        );
        public FleckDef FleckDef = DefDatabase<FleckDef>.GetNamed("CMC_FireGlow_Exp", true);
        public FleckDef FleckDef2 = DefDatabase<FleckDef>.GetNamed("CMC_Fleck_ProjectileSmoke_LongLasting", true);
        public int Fleck_MakeFleckTickMax = 1;
        public IntRange Fleck_MakeFleckNum = new IntRange(2, 4);
        public FloatRange Fleck_Angle = new FloatRange(-180f, 180f);
        public FloatRange Fleck_Speed = new FloatRange(0.05f, 0.12f);
        public FloatRange Fleck_Speed2 = new FloatRange(0.05f, 0.12f);
        public FloatRange Fleck_Rotation = new FloatRange(-180f, 180f);
        public int Fleck_MakeFleckTick;
        public int livecount = 0;
        public bool ThrowLauncherFlag = true;
        private int ticksToDetonation;
        private Vector3 lastPos;
        private bool targetinit = false;
        private Vector3 lasttargetpos = new Vector3();
        public Mote mote = null;
        public Mote mote2 = null;
        private static Material TargetLineMat = MaterialPool.MatFrom(GenDraw.OneSidedLineTexPath, ShaderDatabase.MoteGlow, new Color(0f, 1f, 1f));
    }
}
