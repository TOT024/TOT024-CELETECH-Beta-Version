using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TOT_DLL_test
{
    // Token: 0x0200006C RID: 108
    public class Verb_RocketShoot : Verb_LaunchProjectile
    {
        // Token: 0x17000065 RID: 101
        // (get) Token: 0x06000219 RID: 537 RVA: 0x000138CC File Offset: 0x00011ACC
        protected override int ShotsPerBurst
        {
            get
            {
                return this.verbProps.burstShotCount;
            }
        }

        // Token: 0x0600021A RID: 538 RVA: 0x000138EC File Offset: 0x00011AEC
        protected override bool TryCastShot()
        {
            Vector3 drawPos = this.caster.DrawPos;
            float num = (base.CurrentTarget.CenterVector3 - drawPos).AngleFlat();
            float num2 = num + 36f - 90f;
            float num3 = num - 36f - 90f;
            bool flag = num2 > 180f;
            if (flag)
            {
                num2 -= 360f;
            }
            bool flag2 = num2 < -180f;
            if (flag2)
            {
                num2 += 360f;
            }
            bool flag3 = num3 > 180f;
            if (flag3)
            {
                num3 -= 360f;
            }
            bool flag4 = num3 < -180f;
            if (flag4)
            {
                num3 += 360f;
            }
            Vector3 item = drawPos + new Vector3(1.5f, 0f, 0f).RotatedBy(num2);
            Vector3 item2 = drawPos + new Vector3(1.5f, 0f, 0f).RotatedBy(num3);
            List<Vector3> list = new List<Vector3>
            {
                item,
                item2
            };
            bool flag5 = this.currentTarget.HasThing && this.currentTarget.Thing.Map != this.caster.Map;
            bool result;
            if (flag5)
            {
                result = false;
            }
            else
            {
                ThingDef projectile = this.Projectile;
                bool flag6 = projectile == null;
                if (flag6)
                {
                    result = false;
                }
                else
                {
                    ShootLine shootLine;
                    bool flag7 = base.TryFindShootLineFromTo(this.caster.Position, this.currentTarget, out shootLine, false);
                    bool flag8 = this.verbProps.stopBurstWithoutLos && !flag7;
                    if (flag8)
                    {
                        result = false;
                    }
                    else
                    {
                        bool flag9 = base.EquipmentSource != null;
                        if (flag9)
                        {
                            CompChangeableProjectile comp = base.EquipmentSource.GetComp<CompChangeableProjectile>();
                            bool flag10 = comp != null;
                            if (flag10)
                            {
                                comp.Notify_ProjectileLaunched();
                            }
                            CompApparelVerbOwner_Charged comp2 = base.EquipmentSource.GetComp<CompApparelVerbOwner_Charged>();
                            bool flag11 = comp2 != null;
                            if (flag11)
                            {
                                comp2.UsedOnce();
                            }
                        }
                        this.lastShotTick = Find.TickManager.TicksGame;
                        Thing thing = this.caster;
                        Thing equipment = base.EquipmentSource;
                        CompMannable compMannable = this.caster.TryGetComp<CompMannable>();
                        bool flag12 = ((compMannable != null) ? compMannable.ManningPawn : null) != null;
                        if (flag12)
                        {
                            thing = compMannable.ManningPawn;
                            equipment = this.caster;
                        }
                        Projectile projectile2 = (Projectile)GenSpawn.Spawn(projectile, shootLine.Source, this.caster.Map, WipeMode.Vanish);
                        bool flag13 = this.verbProps.ForcedMissRadius > 0.5f;
                        if (flag13)
                        {
                            float num4 = this.verbProps.ForcedMissRadius;
                            Pawn caster;
                            bool flag14 = (caster = (thing as Pawn)) != null;
                            if (flag14)
                            {
                                num4 *= this.verbProps.GetForceMissFactorFor(equipment, caster);
                            }
                            float num5 = VerbUtility.CalculateAdjustedForcedMiss(num4, this.currentTarget.Cell - this.caster.Position);
                            bool flag15 = num5 > 0.5f;
                            if (flag15)
                            {
                                IntVec3 forcedMissTarget = base.GetForcedMissTarget(num5);
                                bool flag16 = forcedMissTarget != this.currentTarget.Cell;
                                if (flag16)
                                {
                                    ProjectileHitFlags projectileHitFlags = ProjectileHitFlags.NonTargetWorld;
                                    bool flag17 = Rand.Chance(0.5f);
                                    if (flag17)
                                    {
                                        projectileHitFlags = ProjectileHitFlags.All;
                                    }
                                    bool flag18 = !this.canHitNonTargetPawnsNow;
                                    if (flag18)
                                    {
                                        projectileHitFlags &= ~ProjectileHitFlags.NonTargetPawns;
                                    }
                                    projectile2.Launch(thing, list[this.burstShotsLeft % 2], forcedMissTarget, this.currentTarget, projectileHitFlags, this.preventFriendlyFire, equipment, null);
                                    return true;
                                }
                            }
                        }
                        ShotReport shotReport = ShotReport.HitReportFor(this.caster, this, this.currentTarget);
                        Thing randomCoverToMissInto = shotReport.GetRandomCoverToMissInto();
                        ThingDef targetCoverDef = (randomCoverToMissInto != null) ? randomCoverToMissInto.def : null;
                        bool flag19 = this.verbProps.canGoWild && !Rand.Chance(shotReport.AimOnTargetChance_IgnoringPosture);
                        if (flag19)
                        {
                            bool flag20 = projectile2 == null;
                            bool flag21;
                            if (flag20)
                            {
                                flag21 = false;
                            }
                            else
                            {
                                ThingDef def = projectile2.def;
                                flag21 = (((def != null) ? def.projectile : null) != null);
                            }
                            bool flyOverhead = flag21 && projectile2.def.projectile.flyOverhead;
                            ProjectileHitFlags projectileHitFlags2 = ProjectileHitFlags.NonTargetWorld;
                            bool flag22 = Rand.Chance(0.5f) && this.canHitNonTargetPawnsNow;
                            if (flag22)
                            {
                                projectileHitFlags2 |= ProjectileHitFlags.NonTargetPawns;
                            }
                            projectile2.Launch(thing, list[this.burstShotsLeft % 2], shootLine.Dest, this.currentTarget, projectileHitFlags2, this.preventFriendlyFire, equipment, targetCoverDef);
                            result = true;
                        }
                        else
                        {
                            bool flag23 = this.currentTarget.Thing != null && this.currentTarget.Thing.def.CanBenefitFromCover && !Rand.Chance(shotReport.PassCoverChance);
                            if (flag23)
                            {
                                ProjectileHitFlags projectileHitFlags3 = ProjectileHitFlags.NonTargetWorld;
                                bool canHitNonTargetPawnsNow = this.canHitNonTargetPawnsNow;
                                if (canHitNonTargetPawnsNow)
                                {
                                    projectileHitFlags3 |= ProjectileHitFlags.NonTargetPawns;
                                }
                                projectile2.Launch(thing, list[this.burstShotsLeft % 2], randomCoverToMissInto, this.currentTarget, projectileHitFlags3, this.preventFriendlyFire, equipment, targetCoverDef);
                                result = true;
                            }
                            else
                            {
                                ProjectileHitFlags projectileHitFlags4 = ProjectileHitFlags.IntendedTarget;
                                bool canHitNonTargetPawnsNow2 = this.canHitNonTargetPawnsNow;
                                if (canHitNonTargetPawnsNow2)
                                {
                                    projectileHitFlags4 |= ProjectileHitFlags.NonTargetPawns;
                                }
                                bool flag24 = !this.currentTarget.HasThing || this.currentTarget.Thing.def.Fillage == FillCategory.Full;
                                if (flag24)
                                {
                                    projectileHitFlags4 |= ProjectileHitFlags.NonTargetWorld;
                                }
                                bool flag25 = this.currentTarget.Thing != null;
                                if (flag25)
                                {
                                    projectile2.Launch(thing, list[this.burstShotsLeft % 2], this.currentTarget, this.currentTarget, projectileHitFlags4, this.preventFriendlyFire, equipment, targetCoverDef);
                                }
                                else
                                {
                                    projectile2.Launch(thing, list[this.burstShotsLeft % 2], shootLine.Dest, this.currentTarget, projectileHitFlags4, this.preventFriendlyFire, equipment, targetCoverDef);
                                }
                                result = true;
                            }
                        }
                    }
                }
            }
            return result;
        }
    }
}
