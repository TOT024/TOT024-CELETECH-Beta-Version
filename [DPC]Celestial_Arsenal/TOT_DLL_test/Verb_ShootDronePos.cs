using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;

namespace TOT_DLL_test
{
    public class Verb_ShootDronePos : Verb_Shoot
    {
        protected override int ShotsPerBurst
        {
            get
            {
                return this.verbProps.burstShotCount;
            }
        }
        private CMC_Drone Drone
        {
            get
            {
                if (this._Drone == null)
                {
                    _Drone = this.caster as CMC_Drone;
                }
                return _Drone;
            }
        }
        protected override bool TryCastShot()
        {
            if (Drone == null || !Drone.Spawned)
            {
                return false;
            }
            if (this.currentTarget.HasThing && this.currentTarget.Thing.Map != this.caster.Map)
            {
                return false;
            }
            ThingDef projectile = this.Projectile;
            if (projectile == null)
            {
                return false;
            }
            ShootLine shootLine;
            bool flag = base.TryFindShootLineFromTo(Drone.flyingDrawPos.ToIntVec3(), this.currentTarget, out shootLine, false);
            if (this.verbProps.stopBurstWithoutLos && !flag)
            {
                return false;
            }
            if (base.EquipmentSource != null)
            {
                CompChangeableProjectile comp = base.EquipmentSource.GetComp<CompChangeableProjectile>();
                if (comp != null)
                {
                    comp.Notify_ProjectileLaunched();
                }
                CompApparelVerbOwner_Charged comp2 = base.EquipmentSource.GetComp<CompApparelVerbOwner_Charged>();
                if (comp2 != null)
                {
                    comp2.UsedOnce();
                }
            }
            this.lastShotTick = Find.TickManager.TicksGame;
            Thing thing = this.caster;
            Thing thing2 = base.EquipmentSource;
            CompMannable compMannable = this.caster.TryGetComp<CompMannable>();
            if (((compMannable != null) ? compMannable.ManningPawn : null) != null)
            {
                thing = compMannable.ManningPawn;
                thing2 = this.caster;
            }
            Vector3 drawPos = Drone.flyingDrawPos;
            Projectile projectile2 = (Projectile)GenSpawn.Spawn(projectile, shootLine.Source, this.caster.Map, WipeMode.Vanish);
            CompUniqueWeapon compUniqueWeapon;
            if (thing2.TryGetComp(out compUniqueWeapon))
            {
                foreach (WeaponTraitDef weaponTraitDef in compUniqueWeapon.TraitsListForReading)
                {
                    if (weaponTraitDef.damageDefOverride != null)
                    {
                        projectile2.damageDefOverride = weaponTraitDef.damageDefOverride;
                    }
                    if (!weaponTraitDef.extraDamages.NullOrEmpty<ExtraDamage>())
                    {
                        Projectile projectile3 = projectile2;
                        if (projectile3.extraDamages == null)
                        {
                            projectile3.extraDamages = new List<ExtraDamage>();
                        }
                        projectile2.extraDamages.AddRange(weaponTraitDef.extraDamages);
                    }
                }
            }
            if (this.verbProps.ForcedMissRadius > 0.5f)
            {
                float num = this.verbProps.ForcedMissRadius;
                Pawn pawn = thing as Pawn;
                if (pawn != null)
                {
                    num *= this.verbProps.GetForceMissFactorFor(thing2, pawn);
                }
                float num2 = VerbUtility.CalculateAdjustedForcedMiss(num, this.currentTarget.Cell - Drone.flyingDrawPos.ToIntVec3());
                if (num2 > 0.5f)
                {
                    IntVec3 forcedMissTarget = this.GetForcedMissTarget(num2);
                    if (forcedMissTarget != this.currentTarget.Cell)
                    {
                        ProjectileHitFlags projectileHitFlags = ProjectileHitFlags.NonTargetWorld;
                        if (Rand.Chance(0.5f))
                        {
                            projectileHitFlags = ProjectileHitFlags.All;
                        }
                        if (!this.canHitNonTargetPawnsNow)
                        {
                            projectileHitFlags &= ~ProjectileHitFlags.NonTargetPawns;
                        }
                        projectile2.Launch(thing, drawPos, forcedMissTarget, this.currentTarget, projectileHitFlags, this.preventFriendlyFire, thing2, null);
                        return true;
                    }
                }
            }
            ShotReport shotReport = ShotReport.HitReportFor(this.caster, this, this.currentTarget);
            Thing randomCoverToMissInto = shotReport.GetRandomCoverToMissInto();
            ThingDef targetCoverDef = (randomCoverToMissInto != null) ? randomCoverToMissInto.def : null;
            if (this.verbProps.canGoWild && !Rand.Chance(shotReport.AimOnTargetChance_IgnoringPosture))
            {
                bool flag2;
                if (projectile2 == null)
                {
                    flag2 = (null != null);
                }
                else
                {
                    ThingDef def = projectile2.def;
                    flag2 = (((def != null) ? def.projectile : null) != null);
                }
                bool flyOverhead = flag2 && projectile2.def.projectile.flyOverhead;
                shootLine.ChangeDestToMissWild(shotReport.AimOnTargetChance_StandardTarget, flyOverhead, this.caster.Map);
                ProjectileHitFlags projectileHitFlags2 = ProjectileHitFlags.NonTargetWorld;
                if (Rand.Chance(0.5f) && this.canHitNonTargetPawnsNow)
                {
                    projectileHitFlags2 |= ProjectileHitFlags.NonTargetPawns;
                }
                projectile2.Launch(thing, drawPos, shootLine.Dest, this.currentTarget, projectileHitFlags2, this.preventFriendlyFire, thing2, targetCoverDef);
                return true;
            }
            if (this.currentTarget.Thing != null && this.currentTarget.Thing.def.CanBenefitFromCover && !Rand.Chance(shotReport.PassCoverChance))
            {
                ProjectileHitFlags projectileHitFlags3 = ProjectileHitFlags.NonTargetWorld;
                if (this.canHitNonTargetPawnsNow)
                {
                    projectileHitFlags3 |= ProjectileHitFlags.NonTargetPawns;
                }
                projectile2.Launch(thing, drawPos, randomCoverToMissInto, this.currentTarget, projectileHitFlags3, this.preventFriendlyFire, thing2, targetCoverDef);
                return true;
            }
            ProjectileHitFlags projectileHitFlags4 = ProjectileHitFlags.IntendedTarget;
            if (this.canHitNonTargetPawnsNow)
            {
                projectileHitFlags4 |= ProjectileHitFlags.NonTargetPawns;
            }
            if (!this.currentTarget.HasThing || this.currentTarget.Thing.def.Fillage == FillCategory.Full)
            {
                projectileHitFlags4 |= ProjectileHitFlags.NonTargetWorld;
            }
            if (this.currentTarget.Thing != null)
            {
                projectile2.Launch(thing, drawPos, this.currentTarget, this.currentTarget, projectileHitFlags4, this.preventFriendlyFire, thing2, targetCoverDef);
            }
            else
            {
                projectile2.Launch(thing, drawPos, shootLine.Dest, this.currentTarget, projectileHitFlags4, this.preventFriendlyFire, thing2, targetCoverDef);
            }
            return true;
        }
        public CMC_Drone _Drone;
    }
}
