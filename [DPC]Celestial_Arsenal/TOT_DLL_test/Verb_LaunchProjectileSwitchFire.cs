using System.Collections.Generic;
using Verse;
using RimWorld;
using UnityEngine;
using UnityEngine.PlayerLoop;
using System.Linq;

namespace TOT_DLL_test
{
    public class Verb_LauncherProjectileSwitchFire : Verb
    {
        public virtual ThingDef Projectile
        {
            get
            {
                ThingWithComps equipmentSource = base.EquipmentSource;
                CompChangeableProjectile compChangeableProjectile = (equipmentSource != null) ? equipmentSource.GetComp<CompChangeableProjectile>() : null;
                bool flag = compChangeableProjectile != null && compChangeableProjectile.Loaded;
                ThingDef result;
                if (flag)
                {
                    result = compChangeableProjectile.Projectile;
                }
                else
                {
                    result = this.verbProps.defaultProjectile;
                }
                return result;
            }
        }
        protected void resetRetarget()
        {
            this.shootingAtDowned = false;
            this.lastTarget = null;
            this.lastTargetPos = IntVec3.Invalid;
        }
        public override void WarmupComplete()
        {
            base.WarmupComplete();
            BattleLog battleLog = Find.BattleLog;
            Thing caster = this.caster;
            Thing target = this.currentTarget.HasThing ? this.currentTarget.Thing : null;
            ThingWithComps equipmentSource = base.EquipmentSource;
            battleLog.Add(new BattleLogEntry_RangedFire(caster, target, (equipmentSource != null) ? equipmentSource.def : null, this.Projectile, this.ShotsPerBurst > 1));
        }
        protected IntVec3 GetForcedMissTarget(float forcedMissRadius)
        {
            bool forcedMissEvenDispersal = this.verbProps.forcedMissEvenDispersal;
            if (forcedMissEvenDispersal)
            {
                bool flag = this.forcedMissTargetEvenDispersalCache.Count <= 0;
                if (flag)
                {
                    this.forcedMissTargetEvenDispersalCache.AddRange(Verb_LauncherProjectileSwitchFire.GenerateEvenDispersalForcedMissTargets(this.currentTarget.Cell, forcedMissRadius, this.burstShotsLeft));
                    this.forcedMissTargetEvenDispersalCache.SortByDescending((IntVec3 p) => p.DistanceToSquared(this.Caster.Position));
                }
                bool flag2 = this.forcedMissTargetEvenDispersalCache.Count > 0;
                if (flag2)
                {
                    return this.forcedMissTargetEvenDispersalCache.Pop<IntVec3>();
                }
            }
            int maxExclusive = GenRadial.NumCellsInRadius(forcedMissRadius);
            int num = Rand.Range(0, maxExclusive);
            return this.currentTarget.Cell + GenRadial.RadialPattern[num];
        }
        private static IEnumerable<IntVec3> GenerateEvenDispersalForcedMissTargets(IntVec3 root, float radius, int count)
        {
            float randomRotationOffset = Rand.Range(0f, 360f);
            float goldenRatio = (1f + Mathf.Pow(5f, 0.5f)) / 2f;
            int num3;
            for (int i = 0; i < count; i = num3 + 1)
            {
                float f = 6.2831855f * (float)i / goldenRatio;
                float f2 = Mathf.Acos(1f - 2f * ((float)i + 0.5f) / (float)count);
                int num = (int)(Mathf.Cos(f) * Mathf.Sin(f2) * radius);
                int num2 = (int)(Mathf.Cos(f2) * radius);
                Vector3 vect = new Vector3((float)num, 0f, (float)num2).RotatedBy(randomRotationOffset);
                yield return root + vect.ToIntVec3();
                vect = default(Vector3);
                num3 = i;
            }
            yield break;
        }
        protected override bool TryCastShot()
        {
            bool flag = this.currentTarget.HasThing && this.currentTarget.Thing.Map != this.caster.Map;
            bool result;
            if (flag)
            {
                result = false;
            }
            else
            {
                ThingDef projectile = this.Projectile;
                bool flag2 = projectile == null;
                if (flag2)
                {
                    result = false;
                }
                else
                {
                    ShootLine shootLine;
                    bool flag3 = base.TryFindShootLineFromTo(this.caster.Position, this.currentTarget, out shootLine, false);
                    bool flag4 = this.verbProps.stopBurstWithoutLos && !flag3;
                    if (flag4)
                    {
                        result = false;
                    }
                    else
                    {
                        bool flag5 = base.EquipmentSource != null;
                        if (flag5)
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
                        Thing equipment = base.EquipmentSource;
                        CompMannable compMannable = this.caster.TryGetComp<CompMannable>();
                        bool flag6 = ((compMannable != null) ? compMannable.ManningPawn : null) != null;
                        if (flag6)
                        {
                            thing = compMannable.ManningPawn;
                            equipment = this.caster;
                        }
                        Vector3 drawPos = this.caster.DrawPos;
                        Projectile projectile2 = (Projectile)GenSpawn.Spawn(projectile, shootLine.Source, this.caster.Map, WipeMode.Vanish);
                        bool flag7 = this.verbProps.ForcedMissRadius > 0.5f;
                        if (flag7)
                        {
                            float num = this.verbProps.ForcedMissRadius;
                            Pawn pawn = thing as Pawn;
                            bool flag8 = pawn != null;
                            if (flag8)
                            {
                                num *= this.verbProps.GetForceMissFactorFor(equipment, pawn);
                            }
                            float num2 = VerbUtility.CalculateAdjustedForcedMiss(num, this.currentTarget.Cell - this.caster.Position);
                            bool flag9 = num2 > 0.5f;
                            if (flag9)
                            {
                                IntVec3 forcedMissTarget = this.GetForcedMissTarget(num2);
                                bool flag10 = forcedMissTarget != this.currentTarget.Cell;
                                if (flag10)
                                {
                                    this.ThrowDebugText("ToRadius");
                                    this.ThrowDebugText("Rad\nDest", forcedMissTarget);
                                    ProjectileHitFlags projectileHitFlags = ProjectileHitFlags.NonTargetWorld;
                                    bool flag11 = Rand.Chance(0.5f);
                                    if (flag11)
                                    {
                                        projectileHitFlags = ProjectileHitFlags.All;
                                    }
                                    bool flag12 = !this.canHitNonTargetPawnsNow;
                                    if (flag12)
                                    {
                                        projectileHitFlags &= ~ProjectileHitFlags.NonTargetPawns;
                                    }
                                    projectile2.Launch(thing, drawPos, forcedMissTarget, this.currentTarget, projectileHitFlags, this.preventFriendlyFire, equipment, null);
                                    return true;
                                }
                            }
                        }
                        ShotReport shotReport = ShotReport.HitReportFor(this.caster, this, this.currentTarget);
                        Thing randomCoverToMissInto = shotReport.GetRandomCoverToMissInto();
                        ThingDef targetCoverDef = (randomCoverToMissInto != null) ? randomCoverToMissInto.def : null;
                        bool flag13 = this.verbProps.canGoWild && !Rand.Chance(shotReport.AimOnTargetChance_IgnoringPosture);
                        if (flag13)
                        {
                            bool flag14;
                            if (projectile2 == null)
                            {
                                flag14 = (null != null);
                            }
                            else
                            {
                                ThingDef def = projectile2.def;
                                flag14 = (((def != null) ? def.projectile : null) != null);
                            }
                            bool flyOverhead = flag14 && projectile2.def.projectile.flyOverhead;
                            this.ThrowDebugText("ToWild" + (this.canHitNonTargetPawnsNow ? "\nchntp" : ""));
                            this.ThrowDebugText("Wild\nDest", shootLine.Dest);
                            ProjectileHitFlags projectileHitFlags2 = ProjectileHitFlags.NonTargetWorld;
                            bool flag15 = Rand.Chance(0.5f) && this.canHitNonTargetPawnsNow;
                            if (flag15)
                            {
                                projectileHitFlags2 |= ProjectileHitFlags.NonTargetPawns;
                            }
                            projectile2.Launch(thing, drawPos, shootLine.Dest, this.currentTarget, projectileHitFlags2, this.preventFriendlyFire, equipment, targetCoverDef);
                            result = true;
                        }
                        else
                        {
                            bool flag16 = this.currentTarget.Thing != null && this.currentTarget.Thing.def.CanBenefitFromCover && !Rand.Chance(shotReport.PassCoverChance);
                            if (flag16)
                            {
                                this.ThrowDebugText("ToCover" + (this.canHitNonTargetPawnsNow ? "\nchntp" : ""));
                                this.ThrowDebugText("Cover\nDest", randomCoverToMissInto.Position);
                                ProjectileHitFlags projectileHitFlags3 = ProjectileHitFlags.NonTargetWorld;
                                bool canHitNonTargetPawnsNow = this.canHitNonTargetPawnsNow;
                                if (canHitNonTargetPawnsNow)
                                {
                                    projectileHitFlags3 |= ProjectileHitFlags.NonTargetPawns;
                                }
                                projectile2.Launch(thing, drawPos, randomCoverToMissInto, this.currentTarget, projectileHitFlags3, this.preventFriendlyFire, equipment, targetCoverDef);
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
                                bool flag17 = !this.currentTarget.HasThing || this.currentTarget.Thing.def.Fillage == FillCategory.Full;
                                if (flag17)
                                {
                                    projectileHitFlags4 |= ProjectileHitFlags.NonTargetWorld;
                                }
                                this.ThrowDebugText("ToHit" + (this.canHitNonTargetPawnsNow ? "\nchntp" : ""));
                                bool flag18 = this.currentTarget.Thing != null;
                                if (flag18)
                                {
                                    projectile2.Launch(thing, drawPos, this.currentTarget, this.currentTarget, projectileHitFlags4, this.preventFriendlyFire, equipment, targetCoverDef);
                                    this.ThrowDebugText("Hit\nDest", this.currentTarget.Cell);
                                }
                                else
                                {
                                    projectile2.Launch(thing, drawPos, shootLine.Dest, this.currentTarget, projectileHitFlags4, this.preventFriendlyFire, equipment, targetCoverDef);
                                    this.ThrowDebugText("Hit\nDest", shootLine.Dest);
                                }
                                result = true;
                            }
                        }
                    }
                }
            }
            return result;
        }

        // Token: 0x06000512 RID: 1298 RVA: 0x00028C90 File Offset: 0x00026E90
        protected bool Retarget()
        {
            bool flag = !this.doRetarget;
            bool result;
            if (flag)
            {
                result = true;
            }
            else
            {
                this.doRetarget = false;
                bool flag2 = this.currentTarget != this.lastTarget;
                if (flag2)
                {
                    this.lastTarget = this.currentTarget;
                    this.lastTargetPos = this.currentTarget.Cell;
                    Pawn pawn = this.currentTarget.Pawn;
                    this.shootingAtDowned = (pawn == null || pawn.Downed);
                    result = true;
                }
                else
                {
                    bool flag3 = this.shootingAtDowned;
                    if (flag3)
                    {
                        result = true;
                    }
                    else
                    {
                        IntVec3 cell;
                        bool flag4 = this.currentTarget.Pawn == null || this.currentTarget.Pawn.DeadOrDowned || !this.CanHitFromCellIgnoringRange(this.Caster.Position, this.currentTarget, out cell);
                        if (flag4)
                        {
                            Pawn pawn2 = null;
                            Thing caster = this.Caster;
                            foreach (Pawn pawn3 in this.Caster.Map.mapPawns.AllPawnsSpawned.ToList<Pawn>().FindAll((Pawn p) => p.Position.DistanceTo(this.lastTargetPos) <= 4.9f))
                            {
                                Faction faction = pawn3.Faction;
                                Pawn pawn4 = this.currentTarget.Pawn;
                                bool flag5 = faction == ((pawn4 != null) ? pawn4.Faction : null) && pawn3.Faction.HostileTo(caster.Faction) && !pawn3.Downed && this.CanHitFromCellIgnoringRange(this.Caster.Position, pawn3, out cell);
                                if (flag5)
                                {
                                    pawn2 = pawn3;
                                    break;
                                }
                            }
                            bool flag6 = pawn2 != null;
                            if (flag6)
                            {
                                this.currentTarget = new LocalTargetInfo(pawn2);
                                this.lastTarget = this.currentTarget;
                                this.lastTargetPos = this.currentTarget.Cell;
                                this.shootingAtDowned = false;
                                Building_TurretGun building_TurretGun = caster as Building_TurretGun;
                                bool flag7 = building_TurretGun != null;
                                if (flag7)
                                {
                                    cell = this.currentTarget.Cell;
                                    float curRotation = (cell.ToVector3Shifted() - building_TurretGun.DrawPos).AngleFlat();
                                    building_TurretGun.Top.CurRotation = curRotation;
                                }
                                result = true;
                            }
                            else
                            {
                                this.shootingAtDowned = true;
                                result = false;
                            }
                        }
                        else
                        {
                            result = true;
                        }
                    }
                }
            }
            return result;
        }
        private bool CanHitFromCellIgnoringRange(IntVec3 shotSource, LocalTargetInfo targ, out IntVec3 goodDest)
        {
            bool flag = targ.Thing != null && targ.Thing.Map != this.caster.Map;
            bool result;
            if (flag)
            {
                goodDest = IntVec3.Invalid;
                result = false;
            }
            else
            {
                ShootLine shootLine;
                bool flag2 = this.verbProps.requireLineOfSight && base.TryFindShootLineFromTo(shotSource, targ.Cell, out shootLine, false);
                if (flag2)
                {
                    goodDest = targ.Cell;
                    result = true;
                }
                else
                {
                    goodDest = IntVec3.Invalid;
                    result = false;
                }
            }
            return result;
        }
        private void ThrowDebugText(string text)
        {
            bool drawShooting = DebugViewSettings.drawShooting;
            if (drawShooting)
            {
                MoteMaker.ThrowText(this.caster.DrawPos, this.caster.Map, text, -1f);
            }
        }
        private void ThrowDebugText(string text, IntVec3 c)
        {
            bool drawShooting = DebugViewSettings.drawShooting;
            if (drawShooting)
            {
                MoteMaker.ThrowText(c.ToVector3Shifted(), this.caster.Map, text, -1f);
            }
        }

        // Token: 0x06000516 RID: 1302 RVA: 0x00028FF4 File Offset: 0x000271F4
        public override float HighlightFieldRadiusAroundTarget(out bool needLOSToCenter)
        {
            needLOSToCenter = true;
            ThingDef projectile = this.Projectile;
            bool flag = projectile == null;
            float result;
            if (flag)
            {
                result = 0f;
            }
            else
            {
                float num = projectile.projectile.explosionRadius + projectile.projectile.explosionRadiusDisplayPadding;
                float forcedMissRadius = this.verbProps.ForcedMissRadius;
                bool flag2 = forcedMissRadius > 0f && this.verbProps.burstShotCount > 1;
                if (flag2)
                {
                    num += forcedMissRadius;
                }
                result = num;
            }
            return result;
        }
        public override bool Available()
        {
            bool flag = !base.Available();
            bool result;
            if (flag)
            {
                result = false;
            }
            else
            {
                bool casterIsPawn = this.CasterIsPawn;
                if (casterIsPawn)
                {
                    Pawn casterPawn = this.CasterPawn;
                    bool flag2 = casterPawn.Faction != Faction.OfPlayer && !this.verbProps.ai_ProjectileLaunchingIgnoresMeleeThreats && casterPawn.mindState.MeleeThreatStillThreat && casterPawn.mindState.meleeThreat.Position.AdjacentTo8WayOrInside(casterPawn.Position);
                    if (flag2)
                    {
                        return false;
                    }
                }
                result = (this.Projectile != null);
            }
            return result;
        }
        public override void Reset()
        {
            base.Reset();
            this.forcedMissTargetEvenDispersalCache.Clear();
            this.resetRetarget();
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.shootingAtDowned, "shootingAtDowned", false, false);
            Scribe_TargetInfo.Look(ref this.lastTarget, "lastTarget");
            Scribe_Values.Look<IntVec3>(ref this.lastTargetPos, "lastTargetPos", IntVec3.Invalid, false);
        }

        private List<IntVec3> forcedMissTargetEvenDispersalCache = new List<IntVec3>();
        private bool shootingAtDowned = false;
        private LocalTargetInfo lastTarget = null;
        private IntVec3 lastTargetPos = IntVec3.Invalid;
        protected bool doRetarget = true;
    }
}
