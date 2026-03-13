using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace TOT_DLL_test
{
    public class PiercingAmmo_Extension : DefModExtension
    {
        public int penetratingPower = 255;
        public bool reachMaxRangeAlways;
        public float? rangeOverride = null;
        public float minDistanceToAffectAlly = 4.9f;
        public float minDistanceToAffectAny = 1.9f;
        public int penetratingPowerCostByShield = 120;
        public bool alwaysHitStandingEnemy = false;
        public bool HasTail = false;
        public float TailR = 1f;
        public float TailG = 1f;
        public float TailB = 1f;
    }
    public class Projectile_Piercing : Projectile
    {
        PiercingAmmo_Extension piercingProjectileDefInt;
        public PiercingAmmo_Extension piercingProjectileDef
        {
            get
            {
                bool flag = this.piercingProjectileDefInt == null;
                if (flag)
                {
                    this.piercingProjectileDefInt = this.def.GetModExtension<PiercingAmmo_Extension>();
                }
                return this.piercingProjectileDefInt;
            }
        }
        public int PenetratingPowerLeft
        {
            get
            {
                return this.penetratingPowerLeft;
            }
        }
        public override bool AnimalsFleeImpact
        {
            get
            {
                return true;
            }
        }
        public override void Launch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, bool preventFriendlyFire = false, Thing equipment = null, ThingDef targetCoverDef = null)
        {
            base.Launch(launcher, origin, usedTarget, intendedTarget, hitFlags, preventFriendlyFire, equipment, targetCoverDef);
            this.Init(launcher, origin, usedTarget, intendedTarget, hitFlags, preventFriendlyFire, equipment, targetCoverDef);
        }
        private void Init(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, bool preventFriendlyFire = false, Thing equipment = null, ThingDef targetCoverDef = null)
        {
            bool flag = this.piercingProjectileDef == null;
            if (flag && !this.Destroyed)
            {
                this.Destroy(DestroyMode.Vanish);
            }
            else
            {
                this.penetratingPowerLeft = this.piercingProjectileDef.penetratingPower + GameComponent_CeleTech.Instance.ExtraPenetration();
                this.prevPosition = new Vector3(origin.x, 0f, origin.z);
                this.startPosition = new Vector3(origin.x, 0f, origin.z);
                bool flag2 = this.piercingProjectileDef.rangeOverride != null;
                if (flag2)
                {
                    this.SetRangeTo(this.piercingProjectileDef.rangeOverride.Value);
                }
                else
                {
                    bool flag3 = this.piercingProjectileDef.reachMaxRangeAlways && equipment != null && (intendedTarget.Thing == null || intendedTarget.Thing.def.Fillage == FillCategory.Full || this.piercingProjectileDef.penetratingPower > this.piercingProjectileDef.penetratingPowerCostByShield * GameComponent_CeleTech.Instance.ShieldCost() || intendedTarget.Cell.DistanceToSquared(origin.ToIntVec3()) > 16);
                    if (flag3)
                    {
                        this.SetDestinationToMax(equipment, launcher);
                    }
                }
            }
        }
        public void SetRangeTo(float range)
        {
            Vector3 normalized = (this.destination - this.origin).normalized;
            Vector3 vect = normalized * range + this.origin;
            ShootLine shootLine = new ShootLine(this.origin.ToIntVec3(), vect.ToIntVec3());
            List<IntVec3> list = shootLine.Points().ToList<IntVec3>();
            while (list.Count > 0)
            {
                IntVec3 c = list.Pop<IntVec3>();
                bool flag = c.InBounds(base.Map);
                if (flag)
                {
                    this.destination = c.ToVector3();
                    this.destination.x += Rand.Value;
                    this.destination.z += Rand.Value;
                    break;
                }
            }
            this.ticksToImpact = Mathf.CeilToInt(base.StartingTicksToImpact);
        }
        public void SetDestinationToMax(Thing equipment, Thing launcher)
        {
            this.SetRangeTo(Mathf.Min((float)Mathf.Max(base.Map.Size.x, base.Map.Size.z), this.GetEquipmentRange(equipment)));
        }
        private float GetEquipmentRange(Thing equipment)
        {
            CompEquippable compEquippable = equipment.TryGetComp<CompEquippable>();
            bool flag = compEquippable != null;
            if (flag)
            {
                return compEquippable.PrimaryVerb.verbProps.range;
            }
            throw new Exception("Couldn'hitThing determine max range for " + this.Label);
        }
        public bool TryHitThing(Thing t, out bool needToBeDestroy, bool blockedByShield = false)
        {
            needToBeDestroy = false;
            bool result = false;
            bool flag = !this.hitHashSet.Contains(t);
            if (flag)
            {
                bool flag2 = this.IsDamagable(t, blockedByShield);
                if (flag2)
                {
                    bool flag3 = !this.CanPiercing(t);
                    if (flag3)
                    {
                        needToBeDestroy = true;
                    }
                    this.HitThing(t, blockedByShield);
                    result = true;
                }
                else
                {
                    this.MissThing(t);
                }
            }
            bool flag4 = this.penetratingPowerLeft <= 0;
            if (flag4)
            {
                needToBeDestroy = true;
            }
            return result;
        }
        protected override void Tick()
        {
            base.Tick();
            if (this.Destroyed) return;

            Vector3 currentPos = this.DrawPos;
            float moveDistSqr = (currentPos - this.prevPosition).sqrMagnitude;
            if (moveDistSqr > 1.0f)
            {
                this.CheckCollisionBetween(this.prevPosition, currentPos);
            }
            else
            {
                this.CheckCell(currentPos.ToIntVec3());
            }

            this.prevPosition = currentPos;
        }
        private void CheckCollisionBetween(Vector3 start, Vector3 end)
        {
            IntVec3 startCell = start.ToIntVec3();
            IntVec3 endCell = end.ToIntVec3();
            if (startCell == endCell)
            {
                CheckCell(endCell);
                return;
            }
            foreach (IntVec3 cell in GenSight.PointsOnLineOfSight(startCell, endCell))
            {
                if (CheckCell(cell))
                {
                    return;
                }
            }
        }
        private bool IsForcedTarget(Thing t)
        {
            return t != null && this.intendedTarget.IsValid && this.intendedTarget.HasThing && this.intendedTarget.Thing == t;
        }
        private bool IsHostilePawn(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.Downed) return false;
            if (this.launcher == null || this.launcher.Faction == null) return false;
            if (pawn.Faction == null) return true; 
            return pawn.Faction.HostileTo(this.launcher.Faction);
        }
        private bool CheckCell(IntVec3 cell)
        {
            if (!cell.InBounds(base.Map)) return false;

            List<Thing> thingList = base.Map.thingGrid.ThingsListAt(cell);
            for (int i = thingList.Count - 1; i >= 0; i--)
            {
                Thing t = thingList[i];
                if (t == null || t == this || t == this.launcher) continue;
                if (t.def.category == ThingCategory.Mote || t.def.category == ThingCategory.Filth) continue;

                bool destroyProjectile = false;
                if (IsForcedTarget(t))
                {
                    if (this.TryHitThing(t, out destroyProjectile))
                    {
                        this.OnHitResolved(t, destroyProjectile, false);
                        if (this.Destroyed) return true;
                    }
                    continue;
                }
                Pawn pawn = t as Pawn;
                if (pawn == null) continue;
                if (!IsHostilePawn(pawn)) continue;

                if (this.TryHitThing(pawn, out destroyProjectile))
                {
                    this.OnHitResolved(pawn, destroyProjectile, false);
                    if (this.Destroyed) return true;
                }
            }
            return false;
        }
        private void OnHitResolved(Thing hitThing, bool destroyProjectile, bool blockedByShield = false)
        {
            this.SpawnFleck();

            GenClamor.DoClamor(this, 12f, ClamorDefOf.Impact);

            if (!blockedByShield && this.def.projectile.landedEffecter != null)
            {
                this.def.projectile.landedEffecter.Spawn(base.Position, base.Map, 1f).Cleanup();
            }

            if ((destroyProjectile || this.penetratingPowerLeft <= 0) && !this.Destroyed)
            {
                this.Destroy(DestroyMode.Vanish);
            }
        }
        protected bool IsDamagable(Thing thing, bool blockedByShield = false)
        {
            if (blockedByShield) return true;
            if (thing == null) return false;
            if (IsForcedTarget(thing)) return true;

            if (Projectile_Piercing.altitudeLayersBlackList.Contains(thing.def.altitudeLayer))
                return false;

            float minDistSqr = this.piercingProjectileDef.minDistanceToAffectAny * this.piercingProjectileDef.minDistanceToAffectAny;
            if ((float)thing.Position.DistanceToSquared(this.startPosition.ToIntVec3()) < minDistSqr)
                return false;
            if (!(thing is Pawn pawn)) return false;

            return IsHostilePawn(pawn);
        }
        private void HitThing(Thing hitThing, bool blockedByShield = false)
        {
            if (blockedByShield)
            {
                this.penetratingPowerLeft -= (int)(this.piercingProjectileDef.penetratingPowerCostByShield * GameComponent_CeleTech.Instance.ShieldCost());
            }
            else
            {
                this.penetratingPowerLeft--;
            }
            bool flag = hitThing == null;
            if (!flag)
            {
                this.hitHashSet.Add(hitThing);
                BattleLogEntry_RangedImpact battleLogEntry_RangedImpact = (this.equipmentDef != null) ? new BattleLogEntry_RangedImpact(this.launcher, hitThing, this.intendedTarget.Thing, this.equipmentDef, this.def, this.targetCoverDef) : new BattleLogEntry_RangedImpact(this.launcher, hitThing, this.intendedTarget.Thing, ThingDef.Named("Gun_Autopistol"), this.def, this.targetCoverDef);
                Find.BattleLog.Add(battleLogEntry_RangedImpact);
                DamageInfo dinfo = new DamageInfo(this.def.projectile.damageDef, (float)this.DamageAmount, base.ArmorPenetration, this.ExactRotation.eulerAngles.y, this.launcher, null, this.equipmentDef, DamageInfo.SourceCategory.ThingOrUnknown, this.intendedTarget.Thing, true, true, QualityCategory.Normal, true, false);
                hitThing.TakeDamage(dinfo).AssociateWithLog(battleLogEntry_RangedImpact);
                bool flag2 = hitThing != null && hitThing is Pawn && (hitThing as Pawn).stances != null;
                if (flag2)
                {
                    Pawn pawn = (Pawn)hitThing;
                    bool flag3 = pawn.BodySize <= this.def.projectile.stoppingPower + 0.001f;
                    if (flag3)
                    {
                        pawn.stances.stagger.StaggerFor(95, 0.17f);
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
                            DamageInfo dinfo2 = new DamageInfo(extraDamage.def, extraDamage.amount, extraDamage.AdjustedArmorPenetration(), this.ExactRotation.eulerAngles.y, this.launcher, null, this.equipmentDef, DamageInfo.SourceCategory.ThingOrUnknown, this.intendedTarget.Thing, true, true, QualityCategory.Normal, true, false);
                            hitThing.TakeDamage(dinfo2).AssociateWithLog(battleLogEntry_RangedImpact);
                        }
                    }
                }
            }
        }
        private void MissThing(Thing t)
        {
            bool flag = t == null;
            if (!flag)
            {
                this.hitHashSet.Add(t);
            }
        }
        public virtual bool CanPiercing(Thing thing)
        {
            bool flag = thing == null;
            bool result;
            if (flag)
            {
                result = true;
            }
            else
            {
                bool flag2 = thing.def.Fillage == FillCategory.Full && !(thing is Building_Door);
                result = !flag2;
            }
            return result;
        }
        private void SpawnFleck()
        {
            Map map = base.Map;
            if (map == null)
            {
                return;
            }
            Vector3 drawPos = this.DrawPos;

            FleckCreationData fleckData = new FleckCreationData
            {
                def = DefDatabase<FleckDef>.GetNamed("CMC_Fleck_HitFlash", true),
                spawnPosition = drawPos,
                scale = 0.18f * this.DrawSize.x,
                rotation = (float)Rand.Range(0, 360),
                velocitySpeed = 0f,
                rotationRate = 0f,
                orbitSpeed = 0f,
                ageTicksOverride = -1
            };
            map.flecks.CreateFleck(fleckData);

            for (int i = 0; i <= 7; i++)
            {
                FleckCreationData fleckData2 = new FleckCreationData
                {
                    def = DefDatabase<FleckDef>.GetNamed("SparkFlash", true),
                    spawnPosition = drawPos,
                    scale = 0.64f,
                    velocitySpeed = 0f,
                    rotationRate = 0f,
                    rotation = (float)Rand.Range(0, 360),
                    ageTicksOverride = -1
                };
                map.flecks.CreateFleck(fleckData2);
            }
            FleckMaker.ThrowAirPuffUp(drawPos, map);
        }
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            bool destroyProjectile;
            bool hit = this.TryHitThing(hitThing, out destroyProjectile, blockedByShield);
            if (!hit) return;

            this.OnHitResolved(hitThing, destroyProjectile, blockedByShield);
        }
        protected override void ImpactSomething()
        {
            if (this.Destroyed) return;
            if (this.intendedTarget.IsValid && this.intendedTarget.HasThing)
            {
                Thing forced = this.intendedTarget.Thing;
                if (forced != null && !forced.Destroyed && forced.Spawned && forced.Map == base.Map)
                {
                    bool destroyProjectile;
                    if (this.TryHitThing(forced, out destroyProjectile))
                    {
                        this.OnHitResolved(forced, destroyProjectile, false);
                        if (this.Destroyed) return;
                    }
                }
            }
            this.CheckCell(base.Position);

            if (!this.Destroyed)
                this.Destroy(DestroyMode.Vanish);
        }
        private bool IsTerminalCell(IntVec3 cell)
        {
            if (cell == this.DestinationCell) return true;
            if (this.intendedTarget.IsValid && cell == this.intendedTarget.Cell) return true;
            return false;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.penetratingPowerLeft, "penetratingPowerLeft", 1, false);
            Scribe_Values.Look<Vector3>(ref this.prevPosition, "prevPosition", default(Vector3), false);
            Scribe_Values.Look<Vector3>(ref this.startPosition, "startPosition", default(Vector3), false);
            Scribe_Collections.Look<Thing>(ref this.hitHashSet, "hitHashSet", LookMode.Reference);
        }

        private int penetratingPowerLeft;
        private Vector3 prevPosition;
        private HashSet<Thing> hitHashSet = new HashSet<Thing>();
        private Vector3 startPosition;
        public static readonly HashSet<AltitudeLayer> altitudeLayersBlackList = new HashSet<AltitudeLayer>
        {
            AltitudeLayer.Item,
            AltitudeLayer.ItemImportant,
            AltitudeLayer.Conduits,
            AltitudeLayer.Floor,
            AltitudeLayer.FloorEmplacement
        };
    }
}