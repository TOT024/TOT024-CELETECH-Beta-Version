using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace TOT_DLL_test
{
    [StaticConstructorOnStartup]
    public class Projectile_RocketUniversal : Projectile_Explosive
    {
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            this.modExtension2 = this.def.GetModExtension<ProjectileExtension_CMC>();
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
            if (modExtension2.IsHoming)
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
            if (this.landed)
            {
                this.ticksToDetonation--;
                if (this.ticksToDetonation <= 0)
                {
                    this.Explode();
                }
                return;
            }
            base.Position = this.DrawPos.ToIntVec3();
            this.lastPos = this.DrawPos;
            this.Fleck_MakeFleckTick++;
            this.livecount++;
            if(modExtension2.IsHoming)
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
            bool flag = blockedByShield || this.def.projectile.explosionDelay == 0;
            if (flag)
            {
                for (int i = 0; i < 2; i++)
                {
                    CMC_EffectsTool.SpawnExplosionBlask(base.Position.ToVector3(), base.Map, this.FleckDef2, Rand.Range(2.5f, 3.3f), this.Fleck_Rotation.RandomInRange, Rand.Range(0.5f, 1.3f), (float)Rand.Range(0, 360));
                }
                ModExtentsion_Fragments modExtension = this.def.GetModExtension<ModExtentsion_Fragments>();
                FleckMaker.ThrowFireGlow(Position.ToVector3Shifted(), Map, 3.5f);
                FleckMaker.ThrowSmoke(Position.ToVector3Shifted(), Map, 5.5f);
                FleckMaker.ThrowHeatGlow(Position, Map, 3.5f);
                CMC_EffectsTool.SpawnExplosionBlask(base.Position.ToVector3(), base.Map, CMC_EffectsTool.FleckDef_Blask, Rand.Range(1.5f, 2.3f), 0f, 0f, (float)Rand.Range(0, 360));
                if (modExtension != null)
                {
                    IntVec3 center = IntVec3.FromVector3(Position.ToVector3Shifted());
                    IEnumerable<IntVec3> enumerable = GenRadial.RadialCellsAround(center, modExtension.radius, true);
                    foreach (IntVec3 c in enumerable)
                    {
                        bool flag2 = c.InBounds(base.Map);
                        if (flag2 && Rand.Chance(0.1f * Mathf.Sqrt((c - center).Magnitude / modExtension.radius)))
                        {
                            ProjectileHitFlags projectileHitFlags = ProjectileHitFlags.All;
                            Projectile projectile = ThingMaker.MakeThing(CMC_Def.Bullet_CMC_Fragments) as Projectile;
                            Projectile projectile2 = (Projectile)GenSpawn.Spawn(projectile, Position, Map, WipeMode.Vanish);
                            projectile2.Launch(this.launcher, Position.ToVector3Shifted(), c, c, projectileHitFlags, this.preventFriendlyFire, null, targetCoverDef);
                        }
                    }
                }
                this.Explode();
            }
            else
            {
                this.landed = true;
                this.ticksToDetonation = this.def.projectile.explosionDelay;
                GenExplosion.NotifyNearbyPawnsOfDangerousExplosive(this, this.def.projectile.damageDef, this.launcher.Faction, this.launcher);
            }
        }
        protected override void Explode()
        {
            Map map = base.Map;
            IntVec3 position = base.Position;
            Map map2 = map;
            float explosionRadius = this.def.projectile.explosionRadius;
            DamageDef damageDef = this.def.projectile.damageDef;
            Thing launcher = this.launcher;
            int damageAmount = this.DamageAmount;
            float armorPenetration = this.ArmorPenetration;
            SoundDef soundExplode = this.def.projectile.soundExplode;
            ThingDef equipmentDef = this.equipmentDef;
            ThingDef def = this.def;
            ThingDef postExplosionSpawnThingDef = this.def.projectile.postExplosionSpawnThingDef ?? this.def.projectile.filth;
            ThingDef postExplosionSpawnThingDefWater = this.def.projectile.postExplosionSpawnThingDefWater;
            float postExplosionSpawnChance = this.def.projectile.postExplosionSpawnChance;
            int postExplosionSpawnThingCount = this.def.projectile.postExplosionSpawnThingCount;
            GasType? postExplosionGasType = this.def.projectile.postExplosionGasType;
            ThingDef preExplosionSpawnThingDef = this.def.projectile.preExplosionSpawnThingDef;
            float preExplosionSpawnChance = this.def.projectile.preExplosionSpawnChance;
            int preExplosionSpawnThingCount = this.def.projectile.preExplosionSpawnThingCount;
            bool applyDamageToExplosionCellsNeighbors = this.def.projectile.applyDamageToExplosionCellsNeighbors;
            ThingDef preExplosionSpawnThingDef2 = preExplosionSpawnThingDef;
            float preExplosionSpawnChance2 = preExplosionSpawnChance;
            int preExplosionSpawnThingCount2 = preExplosionSpawnThingCount;
            float explosionChanceToStartFire = this.def.projectile.explosionChanceToStartFire;
            bool explosionDamageFalloff = this.def.projectile.explosionDamageFalloff;
            float? direction = new float?(this.origin.AngleToFlat(this.destination));
            List<Thing> ignoredThings = null;
            FloatRange? affectedAngle = null;
            float expolosionPropagationSpeed = this.def.projectile.damageDef.expolosionPropagationSpeed;
            float screenShakeFactor = this.def.projectile.screenShakeFactor;
            bool flag = this.def.projectile.explosionEffect != null;
            if (flag)
            {
                Effecter effecter = this.def.projectile.explosionEffect.Spawn();
                bool flag2 = this.def.projectile.explosionEffectLifetimeTicks != 0;
                if (flag2)
                {
                    map.effecterMaintainer.AddEffecterToMaintain(effecter, base.Position.ToVector3().ToIntVec3(), this.def.projectile.explosionEffectLifetimeTicks);
                }
                else
                {
                    effecter.Trigger(new TargetInfo(base.Position, map, false), new TargetInfo(base.Position, map, false), -1);
                    effecter.Cleanup();
                }
            }
            GenExplosion.DoExplosion(position, map2, explosionRadius, damageDef, launcher, damageAmount, armorPenetration, soundExplode, equipmentDef, def, null, postExplosionSpawnThingDef, postExplosionSpawnChance, postExplosionSpawnThingCount, postExplosionGasType, null, 255, applyDamageToExplosionCellsNeighbors, preExplosionSpawnThingDef2, preExplosionSpawnChance2, preExplosionSpawnThingCount2, explosionChanceToStartFire, explosionDamageFalloff, direction, ignoredThings, affectedAngle, this.def.projectile.doExplosionVFX, expolosionPropagationSpeed, 0f, true, postExplosionSpawnThingDefWater, screenShakeFactor, null, null);
            this.Destroy(DestroyMode.Vanish);
        }
        private List<Vector3> trailHistory = new List<Vector3>();
        private const int MaxTrailLength = 80;
        private const float TrailWidthStart = 0.33f;
        private const float TrailWidthEnd = 0.05f;
        private static readonly Material TrailMat = MaterialPool.MatFrom(
            GenDraw.LineTexPath,
            ShaderDatabase.Transparent,
            new Color(0.55f, 0.55f, 0.55f, 0.75f)
        );
        public ProjectileExtension_CMC modExtension2;
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
