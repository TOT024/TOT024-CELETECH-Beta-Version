using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace TOT_DLL_test
{
    [StaticConstructorOnStartup]
    public class Projectile_PoiMissile : Projectile_Explosive
    {
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            this.modExtension = this.def.GetModExtension<ProjectileExtension_CMC>();
        }
        public override Vector3 ExactPosition
        {
            get
            {
                if(this.position2 != null)
                    return this.position2;
                else
                    return base.ExactPosition;
            }
        }
        private void FindNextTarget(Vector3 d)
        {
            IntVec3 center = IntVec3.FromVector3(d);
            IEnumerable<IntVec3> enumerable = GenRadial.RadialCellsAround(center, 11f, true);
            foreach (IntVec3 c in enumerable)
            {
                bool flag = c.InBounds(base.Map);
                if (flag)
                {
                    Pawn firstPawn = c.GetFirstPawn(base.Map);
                    bool flag2 = firstPawn != null;
                    if (flag2)
                    {
                        bool flag3 = (firstPawn.Faction.HostileTo(this.launcher.Faction) || this.launcher == null) && !firstPawn.Downed && !firstPawn.Dead;
                        if (flag3)
                        {
                            this.intendedTarget = firstPawn;
                            return;
                        }
                    }
                }
            }
            this.intendedTarget = CellRect.CenteredOn(center, 7).RandomCell;
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
        protected override void Tick()
        {
            if (this.DestroyedOrNull()) return;
            position1 = this.BPos(base.DistanceCoveredFraction);
            position2 = this.BPos(base.DistanceCoveredFraction - 0.01f);
            rotation = Quaternion.LookRotation(position1 - this.position2);
            position1.y = AltitudeLayer.Projectile.AltitudeFor();
            position2.y = AltitudeLayer.Projectile.AltitudeFor();
            DCFExport = this.DistanceCoveredFraction;
            if (this.intendedTarget != null && this.intendedTarget.Thing != null)
            {
                if (!targetinit)
                {
                    this.lasttargetpos = this.intendedTarget.Thing.DrawPos;
                    targetinit = true;
                }
                if (this.intendedTarget != null && this.lasttargetpos != null && ((this.intendedTarget.Thing.DrawPos - this.lasttargetpos).magnitude > 5f || this.intendedTarget.Cell.AnyGas(Map, GasType.BlindSmoke))) 
                { 
                    this.intendedTarget = null;
                    Messages.Message("Message_MissileLostTarget".Translate(), MessageTypeDefOf.SilentInput, true);
                }
                else
                {
                    this.destination = this.intendedTarget.Thing.DrawPos;
                    this.lasttargetpos = this.destination;
                    if (mote.DestroyedOrNull())
                    {
                        ThingDef mote_locked = CMC_Def.CMC_Mote_MissileLocked;
                        if(this.launcher!= null && this.launcher.Faction != null)
                        {
                            mote_locked.graphicData.color = this.launcher.Faction.Color;
                        }
                        Vector3 offset = new Vector3(0f, 0f, 0f);
                        offset.y = AltitudeLayer.PawnRope.AltitudeFor();
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
                bool flag2 = base.DistanceCoveredFraction < 0.67f;
                if (flag2)
                {
                    this.FindNextTarget(this.destination);
                }
            }
            base.Tick();
        }
        protected override void TickInterval(int delta)
        {
            base.TickInterval(delta);
            if (!this.DestroyedOrNull())
            {
                Vector3 vector = this.DrawPos;
                vector.y = AltitudeLayer.Projectile.AltitudeFor();
                float num = (vector - this.intendedTarget.CenterVector3).AngleFlat();
                float velocityAngle = this.Fleck_Angle.RandomInRange + num;
                float scale = this.def.graphicData.drawSize.x / 1.92f;
                float randomInRange2 = this.Fleck_Speed2.RandomInRange;
                FleckCreationData dataStatic = FleckMaker.GetDataStatic(vector, Map, this.FleckDef2, scale);
                dataStatic.rotationRate = this.Fleck_Rotation.RandomInRange;
                dataStatic.velocityAngle = velocityAngle;
                dataStatic.velocitySpeed = randomInRange2;
                dataStatic.rotation = (float)Rand.Range(0, 360);
                base.Map.flecks.CreateFleck(dataStatic);
            }
        }
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            ModExtentsion_Fragments modExtension = this.def.GetModExtension<ModExtentsion_Fragments>();
            FleckMaker.ThrowFireGlow(Position.ToVector3Shifted(), Map, 3.5f);
            FleckMaker.ThrowSmoke(Position.ToVector3Shifted(), Map, 5.5f);
            FleckMaker.ThrowHeatGlow(Position, Map, 3.5f);
            if (modExtension != null)
            {
                IntVec3 center = IntVec3.FromVector3(Position.ToVector3Shifted());
                IEnumerable<IntVec3> enumerable = GenRadial.RadialCellsAround(center, modExtension.radius, true);
                foreach (IntVec3 c in enumerable)
                {
                    bool flag = c.InBounds(base.Map);
                    if (flag && Rand.Chance(0.1f * Mathf.Sqrt((c - center).Magnitude / modExtension.radius)))
                    {
                        ProjectileHitFlags projectileHitFlags = ProjectileHitFlags.All;
                        Projectile projectile = ThingMaker.MakeThing(CMC_Def.Bullet_CMC_Fragments) as Projectile;
                        Projectile projectile2 = (Projectile)GenSpawn.Spawn(projectile, Position, Map, WipeMode.Vanish);
                        projectile2.Launch(this.launcher, Position.ToVector3Shifted(), c, c, projectileHitFlags, this.preventFriendlyFire, null, targetCoverDef);
                    }
                }
            }
            base.Impact(hitThing, blockedByShield);
        }
        public ProjectileExtension_CMC modExtension;
        public FleckDef FleckDef = DefDatabase<FleckDef>.GetNamed("CMC_FireGlow_Exp", true);
        public FleckDef FleckDef2 = DefDatabase<FleckDef>.GetNamed("CMC_Fleck_ProjectileSmoke_LongLasting", true);
        public int Fleck_MakeFleckTickMax = 1;
        public IntRange Fleck_MakeFleckNum = new IntRange(3, 6);
        public FloatRange Fleck_Angle = new FloatRange(-180f, 180f);
        public FloatRange Fleck_Speed = new FloatRange(0.1f, 0.2f);
        public FloatRange Fleck_Speed2 = new FloatRange(0.1f, 0.2f);
        public FloatRange Fleck_Rotation = new FloatRange(-180f, 180f);
        public int Fleck_MakeFleckTick;
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
        public Mote_ScaleAndRotate mote;
        public Quaternion rotation;
        public float DCFExport;
    }
}
