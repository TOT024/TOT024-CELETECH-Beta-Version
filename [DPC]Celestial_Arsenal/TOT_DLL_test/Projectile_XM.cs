using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TOT_DLL_test
{
    public class Projectile_XM : Bullet
    {
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
        }
        public override void Launch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, bool preventFriendlyFire = false, Thing equipment = null, ThingDef targetCoverDef = null)
        {
            this.lastposition = origin;
            this.tickcount = 0;
            base.Launch(launcher, origin, usedTarget, intendedTarget, hitFlags, preventFriendlyFire, equipment, targetCoverDef);
        }
        protected override void Tick()
        {
            this.tickcount++;
            bool flag = this.intendedTarget.Thing != null;
            if (flag)
            {
                this.destination = this.intendedTarget.Thing.DrawPos;
            }
            this.Fleck_MakeFleckTick++;
            if (this.Fleck_MakeFleckTick >= this.Fleck_MakeFleckTickMax && this.tickcount >= 8)
            {
                this.Fleck_MakeFleckTick = 0;
                Map map = base.Map;
                Vector3 start = this.CurretPos(base.DistanceCoveredFraction + 0.02f);
                FleckMaker.ConnectingLine(start, this.lastposition, this.FleckDef, map, 0.09f);
                this.lastposition = start;
            }
            bool landed = this.landed;
            if (!landed)
            {
                Vector3 exactPosition = this.ExactPosition;
                this.ticksToImpact--;
                bool flag4 = !this.ExactPosition.InBounds(base.Map);
                if (flag4)
                {
                    this.ticksToImpact++;
                    base.Position = this.ExactPosition.ToIntVec3();
                    this.Destroy(DestroyMode.Vanish);
                }
                else
                {
                    Vector3 exactPosition2 = this.ExactPosition;
                    base.Position = this.ExactPosition.ToIntVec3();
                    bool flag5 = this.ticksToImpact == 60 && Find.TickManager.CurTimeSpeed == TimeSpeed.Normal && this.def.projectile.soundImpactAnticipate != null;
                    if (flag5)
                    {
                        this.def.projectile.soundImpactAnticipate.PlayOneShot(this);
                    }
                    bool flag6 = this.ticksToImpact <= 0;
                    if (flag6)
                    {
                        bool flag7 = base.DestinationCell.InBounds(base.Map);
                        if (flag7)
                        {
                            base.Position = base.DestinationCell;
                        }
                        this.ImpactSomething();
                    }
                }
            }
        }
        private void spawnFleck()
        {
            Vector3 drawPos = this.DrawPos;
            FleckCreationData fleckData = new FleckCreationData
            {
                def = DefDatabase<FleckDef>.GetNamed("CMC_Fleck_HitFlash", true),
                spawnPosition = drawPos,
                scale = 1.9f,
                rotation = (float)Rand.Range(0, 360),
                velocitySpeed = 0f,
                rotationRate = 0f,
                orbitSpeed = 0f,
                ageTicksOverride = -1
            };
            base.Map.flecks.CreateFleck(fleckData);
            for(int i=0; i<=7; i++)
            {
                FleckCreationData fleckData2 = new FleckCreationData
                {
                    def = DefDatabase<FleckDef>.GetNamed("SparkFlash", true),
                    spawnPosition = drawPos,
                    scale = 4f,
                    velocitySpeed = 0f,
                    rotationRate = 0f,
                    rotation = (float)Rand.Range(0, 360),
                    ageTicksOverride = -1
                };
                base.Map.flecks.CreateFleck(fleckData2);
            }
            FleckMaker.ThrowFireGlow(drawPos, Map, 1f);
            FleckMaker.ThrowAirPuffUp(drawPos, Map);         
        }
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            this.spawnFleck();
            Map map = base.Map;
            IntVec3 position = base.Position;
            if(this.intendedTarget.Thing != null)
            {
                hitThing = this.intendedTarget.Thing;
                base.Impact(this.intendedTarget.Thing, blockedByShield);
            }
            else
            {
                this.Destroy(DestroyMode.Vanish);
                return;
            }
            BattleLogEntry_RangedImpact battleLogEntry_RangedImpact = new BattleLogEntry_RangedImpact(this.launcher, hitThing, this.intendedTarget.Thing, this.equipmentDef, this.def, this.targetCoverDef);
            Find.BattleLog.Add(battleLogEntry_RangedImpact);
            this.NotifyImpact(hitThing, map, position);
            bool flag = hitThing != null;
            if (flag)
            {
                Pawn pawn;
                bool instigatorGuilty = (pawn = (this.launcher as Pawn)) == null || !pawn.Drafted;
                DamageInfo dinfo = new DamageInfo(this.def.projectile.damageDef, (float)this.DamageAmount, this.ArmorPenetration, this.ExactRotation.eulerAngles.y, this.launcher, null, this.equipmentDef, DamageInfo.SourceCategory.ThingOrUnknown, this.intendedTarget.Thing, instigatorGuilty, true, QualityCategory.Normal, true);
                dinfo.SetWeaponQuality(this.equipmentQuality);
                hitThing.TakeDamage(dinfo).AssociateWithLog(battleLogEntry_RangedImpact);
                Pawn pawn2 = hitThing as Pawn;
                bool flag2 = pawn2 != null;
                if (flag2)
                {
                    Pawn_StanceTracker stances = pawn2.stances;
                    bool flag3 = stances != null;
                    if (flag3)
                    {
                        stances.stagger.Notify_BulletImpact(this);
                    }
                }
                bool flag4 = this.def.projectile.extraDamages != null;
                if (flag4)
                {
                    foreach (ExtraDamage extraDamage in this.def.projectile.extraDamages)
                    {
                        bool flag5 = Rand.Chance(extraDamage.chance);
                        if (flag5)
                        {
                            DamageInfo dinfo2 = new DamageInfo(extraDamage.def, extraDamage.amount, extraDamage.AdjustedArmorPenetration(), this.ExactRotation.eulerAngles.y, this.launcher, null, this.equipmentDef, DamageInfo.SourceCategory.ThingOrUnknown, this.intendedTarget.Thing, instigatorGuilty, true, QualityCategory.Normal, true);
                            hitThing.TakeDamage(dinfo2).AssociateWithLog(battleLogEntry_RangedImpact);
                        }
                    }
                }
            }
            else
            {
                bool flag6 = !blockedByShield;
                if (flag6)
                {
                    SoundDefOf.BulletImpact_Ground.PlayOneShot(new TargetInfo(base.Position, map, false));
                    bool takeSplashes = base.Position.GetTerrain(map).takeSplashes;
                    if (takeSplashes)
                    {
                        FleckMaker.WaterSplash(this.ExactPosition, map, Mathf.Sqrt((float)this.DamageAmount) * 1f, 4f);
                    }
                    else
                    {
                        FleckMaker.Static(this.ExactPosition, map, FleckDefOf.ShotHit_Dirt, 1f);
                    }
                }
            }
        }
        private void NotifyImpact(Thing hitThing, Map map, IntVec3 position)
        {
            BulletImpactData impactData = new BulletImpactData
            {
                bullet = this,
                hitThing = this.intendedTarget.Thing,
                impactPosition = position
            };
            bool flag = hitThing != null;
            if (flag)
            {
                hitThing.Notify_BulletImpactNearby(impactData);
            }
            int num = 9;
            for (int i = 0; i < num; i++)
            {
                IntVec3 c = position + GenRadial.RadialPattern[i];
                bool flag2 = c.InBounds(map);
                if (flag2)
                {
                    List<Thing> thingList = c.GetThingList(map);
                    for (int j = 0; j < thingList.Count; j++)
                    {
                        bool flag3 = thingList[j] != hitThing;
                        if (flag3)
                        {
                            thingList[j].Notify_BulletImpactNearby(impactData);
                        }
                    }
                }
            }
        }
        public FleckDef FleckDef = DefDatabase<FleckDef>.GetNamed("CMC_GunTail_Blue", true);
        public int Fleck_MakeFleckTickMax = 1;
        public IntRange Fleck_MakeFleckNum = new IntRange(1, 1);
        public FloatRange Fleck_Angle = new FloatRange(-180f, 180f);
        public FloatRange Fleck_Scale = new FloatRange(1.6f, 1.7f);
        public FloatRange Fleck_Speed = new FloatRange(5f, 7f);
        public FloatRange Fleck_Speed2 = new FloatRange(0.1f, 0.2f);
        public FloatRange Fleck_Rotation = new FloatRange(-180f, 180f);
        public int Fleck_MakeFleckTick;
        public int CurrentTick;
        public int tickcount;
        public Vector3 lastposition;
    }
}
