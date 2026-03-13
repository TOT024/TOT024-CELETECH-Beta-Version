using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TOT_DLL_test
{
    [StaticConstructorOnStartup]
    public class Projectile_PoiBullet : Bullet
    {
        private List<Vector3> trailHistory = new List<Vector3>();
        private const int MaxTrailLength = 20;
        private const float TrailWidthStart = 0.10f;
        private const float TrailWidthEnd = 0.01f;

        private static readonly Material TrailMat = MaterialPool.MatFrom(
            GenDraw.LineTexPath,
            ShaderDatabase.MoteGlow,
            new Color(0.8f, 0.8f, 0.8f, 0.75f)
        );
        private void RandFactor()
        {
            FloatRange floatRange = new FloatRange(-0.5f, 0.5f);
            FloatRange floatRange2 = new FloatRange(-0.5f, 0.5f);
            this.Randdd.x = floatRange.RandomInRange;
            this.Randdd.z = floatRange2.RandomInRange;
            this.flag2 = true;
        }
        public Vector3 BPos(float t)
        {
            bool flag = !this.flag2;
            if (flag)
            {
                this.RandFactor();
            }
            Vector3 origin = this.origin;
            Vector3 a = (this.origin + this.destination) / 2f;
            a += this.Randdd;
            a.y = this.destination.y;
            Vector3 destination = this.destination;
            return (1f - t) * (1f - t) * origin + 2f * t * (1f - t) * a + t * t * destination;
        }
        private void FindRandCell(Vector3 d)
        {
            IntVec3 center = IntVec3.FromVector3(d);
            this.intendedTarget = CellRect.CenteredOn(center, 2).RandomCell;
        }
        protected override void DrawAt(Vector3 position, bool flip = false)
        {
            Vector3 b = this.BPos(base.DistanceCoveredFraction - 0.01f);
            position = this.BPos(base.DistanceCoveredFraction);
            Quaternion rotation = Quaternion.LookRotation(position - b);
            bool flag = this.tickcount >= 2;
            if (flag)
            {
                base.Comps_PostDraw();
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
        public override void Launch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, bool preventFriendlyFire = false, Thing equipment = null, ThingDef targetCoverDef = null)
        {
            this.lastposition = origin;
            this.tickcount = 0;
            base.Launch(launcher, origin, usedTarget, intendedTarget, hitFlags, preventFriendlyFire, equipment, targetCoverDef);
        }
        protected override void Tick()
        {
            this.tickcount++;
            bool flag3 = this.flag3;
            if (flag3)
            {
                this.CalHit = this.CanHitTarget();
                this.flag3 = false;
            }
            bool flag4 = !this.CalHit;
            if (flag4)
            {
                this.FindRandCell(this.intendedTarget.CenterVector3);
                this.intendedTarget = null;
            }
            if (this.intendedTarget.Thing != null)
            {
                Pawn pawn = this.intendedTarget.Thing as Pawn;
                if (pawn != null && !pawn.DeadOrDowned)
                {
                    this.destination = this.intendedTarget.Thing.DrawPos;
                    bool flag7 = this.mote.DestroyedOrNull();
                    if (flag7)
                    {
                        ThingDef cmc_Mote_SWTargetLocked = CMC_Def.CMC_Mote_SWTargetLocked;
                        Vector3 offset = new Vector3(0f, 0f, 0f);
                        offset.y = AltitudeLayer.PawnRope.AltitudeFor();
                        this.mote = MoteMaker.MakeAttachedOverlay(this.intendedTarget.Thing, cmc_Mote_SWTargetLocked, offset, 1f, 1f);
                        this.mote.exactRotation = 45f;
                    }
                    else
                    {
                        this.mote.Maintain();
                    }
                }
            }
            base.Tick();
        }
        public virtual bool CanHitTarget()
        {
            return Rand.Chance(this.Hitchance());
        }
        private float Hitchance()
        {
            Pawn pawn = this.launcher as Pawn;
            bool flag = pawn != null && !pawn.NonHumanlikeOrWildMan();
            int level = 0;
            if (flag)
            {
                SkillDef named = DefDatabase<SkillDef>.GetNamed("Intellectual", true);
                SkillRecord skill = pawn.skills.GetSkill(named);
                if (skill != null)
                {
                    level = skill.GetLevel(true);
                }
                else
                {
                    level = 0;
                }
            }
            else
            {
                level = 12;
            }
            float t = Mathf.Clamp01(level / 20f);
            float smoothStep = 3 * t * t - 2 * t * t * t;
            return 0.33f + 0.62f * smoothStep;
        }

        public float GetDamageMultiplier(float distance, float d0, float m)
        {
            if (distance <= d0) return 1f;
            float ratio = Mathf.Pow(distance - d0, 2) / Mathf.Pow(distance, 2);
            return Mathf.Clamp(m + (1 - m) * ratio, m, 1f);
        }

        public float GetPenetrationMultiplier(float distance, float d0, float mp)
        {
            if (distance <= d0) return 1f;
            float ratio = Mathf.Pow(distance - d0, 3) / Mathf.Pow(distance, 3);
            return Mathf.Clamp(ratio, mp, 1f);
        }
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            if(hitThing == null && intendedTarget.Thing != null)
            {
                hitThing = intendedTarget.Thing;
            }
            Map map = base.Map;
            IntVec3 position = base.Position;
            BattleLogEntry_RangedImpact battleLogEntry_RangedImpact = new BattleLogEntry_RangedImpact(this.launcher, hitThing, this.intendedTarget.Thing, this.equipmentDef, this.def, this.targetCoverDef);
            Find.BattleLog.Add(battleLogEntry_RangedImpact);
            this.NotifyImpact(hitThing, map, position);
            bool flag2 = hitThing != null && !blockedByShield;
            if (flag2)
            {
                Pawn pawn = this.launcher as Pawn;
                bool instigatorGuilty = pawn == null || !pawn.Drafted;
                float damagemultiplier = 1f;
                float penemultiplier = 1f;
                if (pawn != null)
                {
                    CompSmartWeapon smartWeapon = pawn.equipment.Primary.TryGetComp<CompSmartWeapon>();
                    if (smartWeapon != null)
                    {
                        damagemultiplier = GetDamageMultiplier((this.origin - this.DrawPos).magnitude, smartWeapon.Props.DamageDeductionRange, smartWeapon.Props.MinDamageMultiplier);
                        penemultiplier = GetPenetrationMultiplier((this.origin - this.DrawPos).magnitude, smartWeapon.Props.DamageDeductionRange, smartWeapon.Props.MinPenetrationMultiplier);
                    }
                }
                DamageInfo dinfo = new DamageInfo(this.def.projectile.damageDef, (float)this.DamageAmount * damagemultiplier, this.ArmorPenetration * damagemultiplier, this.ExactRotation.eulerAngles.y, this.launcher, null, this.equipmentDef, DamageInfo.SourceCategory.ThingOrUnknown, this.intendedTarget.Thing, instigatorGuilty, true, QualityCategory.Normal, true);
                hitThing.TakeDamage(dinfo).AssociateWithLog(battleLogEntry_RangedImpact);

                Pawn pawn2 = hitThing as Pawn;
                bool flag3 = pawn2 != null && pawn2.stances != null;
                if (flag3)
                {
                    pawn2.stances.stagger.Notify_BulletImpact(this);
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
                bool flag7 = !blockedByShield;
                if (flag7)
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
            this.Destroy(DestroyMode.Vanish);
        }

        private void NotifyImpact(Thing hitThing, Map map, IntVec3 position)
        {
            BulletImpactData impactData = new BulletImpactData
            {
                bullet = this,
                hitThing = hitThing,
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

        private bool flag2 = false;
        private bool flag3 = true;
        private bool CalHit = false;
        private Vector3 Randdd;
        private int tickcount;
        public Mote MoteonTarget;
        public Mote mote = null;
        public FleckDef FleckDef = DefDatabase<FleckDef>.GetNamed("CMC_GunTail_Blue", true);
        public int Fleck_MakeFleckTickMax = 1;
        public int Fleck_MakeFleckTick;
        public Vector3 lastposition;
        private float probeStep = 0.2f;
    }
}