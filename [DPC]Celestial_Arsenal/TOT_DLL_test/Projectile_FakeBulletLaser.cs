using RimWorld;
using UnityEngine;
using Verse;

namespace TOT_DLL_test
{
    public class Projectile_FakeBulletLaser : Projectile
    {
        private const float DefaultSpreadRadius = 0.29f;

        protected override void Tick()
        {
            LocalTargetInfo finalTarget = this.intendedTarget;
            if (this.intendedTarget != null && this.Map != null)
            {
                finalTarget = GetScatterTarget(this.intendedTarget);
            }
            if (finalTarget.HasThing)
            {
                this.Impact(finalTarget.Thing, false);
            }
            else
            {
                this.Impact(null, false, finalTarget);
            }
        }
        protected void Impact(Thing hitThing, bool blockedByShield, LocalTargetInfo actualHitTarget)
        {
            Map map = base.Map;
            if (actualHitTarget.HasThing)
            {
                hitThing = actualHitTarget.Thing;
            }
            if (hitThing != null)
            {
                BattleLogEntry_RangedImpact battleLogEntry_RangedImpact = new BattleLogEntry_RangedImpact(this.launcher, hitThing, this.intendedTarget.Thing, this.equipmentDef, this.def, this.targetCoverDef);
                Find.BattleLog.Add(battleLogEntry_RangedImpact);
            }

            if (EquipmentSource != null)
            {
                for (int i = 0; i < EquipmentSource.AllComps.Count; i++)
                {
                    if (EquipmentSource.AllComps[i] is Comp_LaserData_Instant comp_LaserData_Instant)
                    {
                        comp_LaserData_Instant.TakeDamageToTarget(actualHitTarget, this.launcher, EquipmentVerbs, 0, 0);
                    }
                }
            }
            this.Destroy(DestroyMode.Vanish);
        }
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            LocalTargetInfo t = (hitThing != null) ? new LocalTargetInfo(hitThing) : new LocalTargetInfo(this.Position);
            this.Impact(hitThing, blockedByShield, t);
        }
        private LocalTargetInfo GetScatterTarget(LocalTargetInfo originalTarget)
        {
            if (this.launcher is Pawn pawn)
            {
                if (pawn.health.hediffSet.HasHediff(CMC_Def.TOT_PrecisionFocusBuff))
                {
                    return originalTarget;
                }
            }
            float baseRadius = DefaultSpreadRadius;
            float accuracyFactor = GetShooterAccuracy();
            float finalRadius = baseRadius / Mathf.Max(0.1f, accuracyFactor);
            Vector3 center = originalTarget.CenterVector3;
            float angle = Rand.Range(0f, 360f);

            float dist = Mathf.Abs(Rand.Gaussian(0f, finalRadius));
            dist = Mathf.Clamp(dist, 0f, finalRadius * 3f);

            Vector3 offset = Vector3Utility.FromAngleFlat(angle) * dist;
            IntVec3 finalCell = (center + offset).ToIntVec3();

            if (!finalCell.InBounds(base.Map))
            {
                finalCell = ClampToMap(finalCell, base.Map);
            }
            Thing potentialVictim = finalCell.GetFirstPawn(base.Map);
            if (potentialVictim == null) potentialVictim = finalCell.GetFirstBuilding(base.Map);

            if (potentialVictim != null)
            {
                return new LocalTargetInfo(potentialVictim);
            }

            return new LocalTargetInfo(finalCell);
        }
        private float GetShooterAccuracy()
        {
            float accuracy = 1f;
            if (this.launcher == null) return accuracy;
            if (this.launcher is Pawn pawn)
            {
                accuracy = pawn.GetStatValue(StatDefOf.ShootingAccuracyPawn, true);
                accuracy = Mathf.Pow(accuracy, 20f);
            }
            else
            {
                accuracy = 1.0f;
            }
            return accuracy;
        }
        private IntVec3 ClampToMap(IntVec3 cell, Map map)
        {
            int x = Mathf.Clamp(cell.x, 0, map.Size.x - 1);
            int z = Mathf.Clamp(cell.z, 0, map.Size.z - 1);
            return new IntVec3(x, cell.y, z);
        }
        public ThingWithComps EquipmentSource
        {
            get
            {
                ThingWithComps result = null;
                if (launcher is Pawn pawn)
                {
                    result = pawn.equipment.Primary;
                }
                else if (launcher is CMC_Drone _Drone)
                {
                    CompDroneMovement comp = _Drone.TryGetComp<CompDroneMovement>();
                    if (comp != null && comp.gun != null)
                    {
                        result = comp.gun as ThingWithComps;
                    }
                }
                else
                {
                    Building_TurretGun Turret = launcher as Building_TurretGun;
                    if (Turret != null && Turret.gun != null)
                    {
                        result = Turret.gun as ThingWithComps;
                    }
                }
                return result;
            }
        }
        public Verb EquipmentVerbs
        {
            get
            {
                return EquipmentSource?.GetComp<CompEquippable>()?.PrimaryVerb;
            }
        }
    }
}