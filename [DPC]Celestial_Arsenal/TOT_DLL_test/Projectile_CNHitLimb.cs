using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TOT_DLL_test
{
    public class Projectile_CNHitLimb : Projectile_PoiBullet
    {
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Pawn launcherPawn = this.launcher as Pawn;
            bool instigatorGuilty = launcherPawn == null || !launcherPawn.Drafted;

            Map map = base.Map;
            IntVec3 position = base.Position;
            BattleLogEntry_RangedImpact battleLogEntry = new BattleLogEntry_RangedImpact(
                this.launcher,
                hitThing,
                this.intendedTarget.Thing,
                this.equipmentDef,
                this.def,
                this.targetCoverDef
            );
            Find.BattleLog.Add(battleLogEntry);
            this.NotifyImpact(hitThing, map, position);

            if (hitThing != null)
            {
                DamageInfo dinfo = this.RefDinfo(hitThing, battleLogEntry);
                hitThing.TakeDamage(dinfo).AssociateWithLog(battleLogEntry);

                Pawn hitPawn = hitThing as Pawn;
                if (hitPawn != null && hitPawn.stances != null)
                {
                    hitPawn.stances.stagger.Notify_BulletImpact(this);
                }
                if (this.def.projectile.extraDamages != null)
                {
                    foreach (ExtraDamage extraDamage in this.def.projectile.extraDamages)
                    {
                        if (Rand.Chance(extraDamage.chance))
                        {
                            DamageInfo dinfo2 = new DamageInfo(
                                extraDamage.def,
                                extraDamage.amount,
                                extraDamage.AdjustedArmorPenetration(),
                                this.ExactRotation.eulerAngles.y,
                                this.launcher,
                                null,
                                this.equipmentDef,
                                DamageInfo.SourceCategory.ThingOrUnknown,
                                this.intendedTarget.Thing,
                                instigatorGuilty,
                                true
                            );
                            hitThing.TakeDamage(dinfo2).AssociateWithLog(battleLogEntry);
                        }
                    }
                }
            }
            else
            {
                if (!blockedByShield)
                {
                    SoundDefOf.BulletImpact_Ground.PlayOneShot(new TargetInfo(base.Position, map, false));
                    if (base.Position.GetTerrain(map).takeSplashes)
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
        private DamageInfo RefDinfo(Thing hitThing, BattleLogEntry_RangedImpact logEntry)
        {
            Pawn launcherPawn = this.launcher as Pawn;
            bool instigatorGuilty = launcherPawn == null || !launcherPawn.Drafted;
            float baseDamage = this.DamageAmount;

            Pawn hitPawn = hitThing as Pawn;
            if (hitPawn == null)
            {
                return new DamageInfo(this.def.projectile.damageDef, baseDamage, this.ArmorPenetration, this.ExactRotation.eulerAngles.y, this.launcher, null, this.equipmentDef, DamageInfo.SourceCategory.ThingOrUnknown, this.intendedTarget.Thing, instigatorGuilty, true);
            }
            if (hitPawn.RaceProps.FleshType == FleshTypeDefOf.Mechanoid)
            {
                baseDamage *= 0.5f;
            }

            BodyPartRecord targetPart = null;
            float finalDamage = baseDamage;
            IEnumerable<BodyPartRecord> validLimbs = hitPawn.health.hediffSet.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Outside)
                .Where(p => p.def.tags.Contains(BodyPartTagDefOf.MovingLimbCore) || p.def.tags.Contains(BodyPartTagDefOf.ManipulationLimbCore));
            List<BodyPartRecord> targetableLimbs = validLimbs.Where(p => hitPawn.health.hediffSet.GetPartHealth(p) > 1f).ToList();

            if (targetableLimbs.Count > 0)
            {
                targetPart = targetableLimbs.RandomElement();
                float currentPartHealth = hitPawn.health.hediffSet.GetPartHealth(targetPart);
                if (baseDamage >= currentPartHealth)
                {
                    finalDamage = Mathf.Max(1f, currentPartHealth - 1f);
                }
            }
            DamageInfo dinfo = new DamageInfo(
                this.def.projectile.damageDef,
                finalDamage,
                this.ArmorPenetration,
                this.ExactRotation.eulerAngles.y,
                this.launcher,
                targetPart,
                this.equipmentDef,
                DamageInfo.SourceCategory.ThingOrUnknown,
                this.intendedTarget.Thing,
                instigatorGuilty,
                true
            );
            if (targetPart != null)
            {
                dinfo.SetHitPart(targetPart);
            }

            return dinfo;
        }

        private void NotifyImpact(Thing hitThing, Map map, IntVec3 position)
        {
            BulletImpactData impactData = new BulletImpactData
            {
                bullet = this,
                hitThing = hitThing,
                impactPosition = position
            };
            if (hitThing != null)
            {
                hitThing.Notify_BulletImpactNearby(impactData);
            }
            int num = 9;
            for (int i = 0; i < num; i++)
            {
                IntVec3 c = position + GenRadial.RadialPattern[i];
                if (c.InBounds(map))
                {
                    List<Thing> thingList = c.GetThingList(map);
                    for (int j = 0; j < thingList.Count; j++)
                    {
                        if (thingList[j] != hitThing)
                        {
                            thingList[j].Notify_BulletImpactNearby(impactData);
                        }
                    }
                }
            }
        }
    }
}