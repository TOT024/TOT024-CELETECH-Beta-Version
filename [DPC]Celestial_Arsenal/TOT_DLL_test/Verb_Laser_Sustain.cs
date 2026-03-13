using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using Verse.AI;

namespace TOT_DLL_test
{
    public class Verb_Laser_Sustain : Verb
    {
        private Comp_LaserData_Sustain cachedComp;

        public CompProperties_LaserData_Sustain Props
        {
            get
            {
                if (this.cachedComp == null)
                {
                    this.cachedComp = this.EquipmentSource.GetComp<Comp_LaserData_Sustain>();
                }
                return this.cachedComp.Props;
            }
        }

        public Comp_LaserData_Sustain comp_Laser
        {
            get
            {
                if (this.cachedComp == null)
                {
                    this.cachedComp = this.EquipmentSource.GetComp<Comp_LaserData_Sustain>();
                }
                return this.cachedComp;
            }
        }

        private MoteDualAttached mote, mote2, moteCore, moteCore2;
        private Sustainer activeSustainer;
        private int scatterTickCounter = 0;
        private bool isRetargeting;
        private int retargetShotsTotal;
        private int retargetShotsDone;
        private Vector3 retargetFromPos;
        private Vector3 retargetToPos;
        private Vector3 lastBeamEndPos;
        private CompLaserHeat cachedHeatComp;
        private CompLaserHeat HeatComp
        {
            get
            {
                if (cachedHeatComp == null && this.EquipmentSource != null)
                {
                    cachedHeatComp = this.EquipmentSource.GetComp<CompLaserHeat>();
                }
                return cachedHeatComp;
            }
        }
        protected override int ShotsPerBurst => this.verbProps.burstShotCount;
        public Vector3 TargetPosition_Vector3 => BeamEndPosition;
        private Vector3 BeamEndPosition
        {
            get
            {
                if (isRetargeting)
                {
                    float t = retargetShotsTotal <= 0 ? 1f : (float)retargetShotsDone / retargetShotsTotal;
                    return Vector3.Lerp(retargetFromPos, retargetToPos, Mathf.Clamp01(t));
                }

                if (base.CurrentTarget.IsValid)
                    return base.CurrentTarget.CenterVector3;

                return lastBeamEndPos;
            }
        }
        public override float? AimAngleOverride
        {
            get
            {
                if (this.state == VerbState.Bursting)
                {
                    return (this.TargetPosition_Vector3 - this.caster.DrawPos).AngleFlat();
                }
                return null;
            }
        }
        private void ForceOverheatStopBurst()
        {
            if (this.state != VerbState.Bursting) return;

            burstShotsLeft = 0;
            ticksToNextBurstShot = 0;
            state = VerbState.Idle;

            if (activeSustainer != null)
            {
                activeSustainer.End();
                activeSustainer = null;
            }

            if (this.CasterIsPawn && HeatComp != null)
            {
                int backswing = Mathf.Max(1, HeatComp.Props.overheatBackswingTicks);
                this.CasterPawn.stances.SetStance(new Stance_Cooldown(backswing, this.currentTarget, this));
            }
        }
        public override void WarmupComplete()
        {
            base.WarmupComplete();
            lastBeamEndPos = base.CurrentTarget.IsValid ? base.CurrentTarget.CenterVector3 : this.caster.DrawPos;

            if (Props.SoundDef != null)
            {
                activeSustainer = Props.SoundDef.TrySpawnSustainer(SoundInfo.InMap(this.caster, MaintenanceType.PerTick));
            }
            if (Props.LaserLine_MoteDef != null)
            {
                Color laserColor = new Color(Props.Color_Red / 255f, Props.Color_Green / 255f, Props.Color_Blue / 255f, Props.Color_Alpha * 0.5f);

                mote = MoteMaker.MakeInteractionOverlay(Props.LaserLine_MoteDef, this.caster, new TargetInfo(this.TargetPosition_Vector3.ToIntVec3(), this.caster.Map));
                mote2 = MoteMaker.MakeInteractionOverlay(Props.LaserLine_MoteDef, new TargetInfo(this.TargetPosition_Vector3.ToIntVec3(), this.caster.Map), this.caster);

                mote.instanceColor = laserColor;
                mote2.instanceColor = laserColor;
            }
            if (Props.LaserLine_MoteDef_Core != null)
            {
                Color coreColor = new Color(1f, 1f, 1f, Props.Color_Alpha);
                moteCore = MoteMaker.MakeInteractionOverlay(Props.LaserLine_MoteDef_Core, this.caster, new TargetInfo(this.TargetPosition_Vector3.ToIntVec3(), this.caster.Map));
                moteCore2 = MoteMaker.MakeInteractionOverlay(Props.LaserLine_MoteDef_Core, new TargetInfo(this.TargetPosition_Vector3.ToIntVec3(), this.caster.Map), this.caster);
                moteCore.instanceColor = coreColor;
                moteCore2.instanceColor = coreColor;
            }
        }
        private bool overheatAbortPending;
        public override void BurstingTick()
        {
            if (overheatAbortPending)
            {
                overheatAbortPending = false;
                ForceOverheatStopBurst();
                return;
            }
            TryRetargetIfNeeded();
            base.BurstingTick();
            Vector3 casterDrawPos = this.Caster.DrawPos;
            Vector3 targetCenter = this.TargetPosition_Vector3;
            lastBeamEndPos = targetCenter;                     
            if (isRetargeting && this.currentTarget.IsValid && this.currentTarget.Thing != null && !this.currentTarget.Thing.Destroyed)
            {
                retargetToPos = this.currentTarget.Thing.DrawPos;
            }

            if (Props.StartPositionOffset_Range > 0f)
            {
                float angle = (casterDrawPos - targetCenter).AngleFlat();
                casterDrawPos = MYDE_ModFront.GetVector3_By_AngleFlat(casterDrawPos, Props.StartPositionOffset_Range, angle);
            }

            if (mote != null && mote2 != null)
            {
                Vector3 casterOffset = casterDrawPos - this.Caster.Position.ToVector3Shifted();
                Vector3 targetOffset = targetCenter - targetCenter.ToIntVec3().ToVector3Shifted();

                mote.UpdateTargets(new TargetInfo(this.Caster.Position, this.caster.Map), new TargetInfo(targetCenter.ToIntVec3(), this.caster.Map), casterOffset, targetOffset);
                mote2.UpdateTargets(new TargetInfo(targetCenter.ToIntVec3(), this.caster.Map), new TargetInfo(this.Caster.Position, this.caster.Map), targetOffset, casterOffset);

                mote.Maintain();
                mote2.Maintain();
            }
            if (moteCore != null && moteCore2 != null)
            {
                Vector3 casterOffset = casterDrawPos - this.Caster.Position.ToVector3Shifted();
                Vector3 targetOffset = targetCenter - targetCenter.ToIntVec3().ToVector3Shifted();
                moteCore.UpdateTargets(new TargetInfo(this.Caster.Position, this.caster.Map), new TargetInfo(targetCenter.ToIntVec3(), this.caster.Map), casterOffset, targetOffset);
                moteCore2.UpdateTargets(new TargetInfo(targetCenter.ToIntVec3(), this.caster.Map), new TargetInfo(this.Caster.Position, this.caster.Map), targetOffset, casterOffset);
                moteCore.Maintain();
                moteCore2.Maintain();
            }

            if (Props.ifPeriodicEffect && Props.periodicEffectFleck != null)
            {
                if (Rand.Chance(Props.periodicEffectChance))
                {
                    Vector3 spawnPos = Vector3.Lerp(casterDrawPos, targetCenter, Rand.Value);
                    float scale = Rand.Range(Props.periodicEffectScaleMin, Props.periodicEffectScaleMax);
                    FleckMaker.Static(spawnPos, this.caster.Map, Props.periodicEffectFleck, scale);
                }
            }
            if (Props.ifPeriodicLine && Props.periodicLineFleck != null)
            {
                if (Rand.Chance(Props.periodicLineChance))
                {
                    FleckMaker.ConnectingLine(casterDrawPos, targetCenter, Props.periodicLineFleck, this.caster.Map, 3.25f);
                }
            }

            if (burstShotsLeft > 0)
            {
                if (activeSustainer != null)
                {
                    if (activeSustainer.Ended)
                    {
                        activeSustainer = null;
                    }
                    else
                    {
                        activeSustainer.Maintain();
                    }
                }
            }
            else
            {
                if (activeSustainer != null)
                {
                    activeSustainer.End();
                    activeSustainer = null;
                }
            }

            if (Props.LaserFleck_End != null)
            {
                float currentScale = Props.LaserFleck_End_Scale_Base * Rand.Range(0.6f, 1.1f);
                FleckMaker.Static(targetCenter, this.caster.Map, Props.LaserFleck_End, currentScale);
            }

            if (Props.IfCanScatter)
            {
                scatterTickCounter++;
                if (scatterTickCounter >= Props.ScatterTickMax)
                {
                    scatterTickCounter = 0;
                    for (int i = 0; i < Props.ScatterNum; i++)
                    {
                        CellRect scatterArea = CellRect.CenteredOn(base.CurrentTarget.Cell, Props.ScatterRadius).ClipInsideMap(this.caster.Map);
                        IntVec3 randomCell = scatterArea.RandomCell;

                        GenExplosion.DoExplosion(
                            center: randomCell,
                            map: this.caster.Map,
                            radius: Props.ScatterExplosionRadius,
                            damType: Props.ScatterExplosionDef,
                            instigator: this.caster,
                            damAmount: Props.ScatterExplosionDamage,
                            armorPenetration: Props.ScatterExplosionArmorPenetration,
                            explosionSound: null,
                            weapon: base.EquipmentSource?.def,
                            projectile: null,
                            intendedTarget: null,
                            postExplosionSpawnThingDef: null,
                            postExplosionSpawnChance: 0f,
                            postExplosionSpawnThingCount: 0,
                            postExplosionGasType: null,
                            applyDamageToExplosionCellsNeighbors: false,
                            preExplosionSpawnThingDef: null,
                            preExplosionSpawnChance: 0f,
                            preExplosionSpawnThingCount: 0,
                            chanceToStartFire: 0f,
                            damageFalloff: false,
                            direction: null,
                            ignoredThings: null,
                            doVisualEffects: true,
                            propagationSpeed: 1f,
                            screenShakeFactor: 0f,
                            doSoundEffects: true
                        );

                        if (Props.LaserFleck_ScatterLaser != null)
                        {
                            FleckMaker.ConnectingLine(targetCenter, randomCell.ToVector3Shifted(), Props.LaserFleck_ScatterLaser, this.caster.Map, 1f);
                        }
                    }
                }
            }
        }

        public override void Reset()
        {
            base.Reset();
            if (activeSustainer != null)
            {
                activeSustainer.End();
                activeSustainer = null;
            }
            isRetargeting = false;
            retargetShotsDone = 0;
            retargetShotsTotal = 0;
        }

        protected override bool TryCastShot()
        {
            TryRetargetIfNeeded();
            CompLaserHeat heat = this.HeatComp;
            if (heat != null && heat.IsOverheated)
            {
                overheatAbortPending = true;
                return false;               
            }
            if (this.CasterIsPawn)
            {
                this.CasterPawn.records.Increment(RecordDefOf.ShotsFired);
            }
            Thing targetThing = this.currentTarget.Thing;
            bool isTargetOnDifferentMap = targetThing != null && targetThing.Map != this.caster.Map;
            if (isTargetOnDifferentMap)
            {
                return false;
            }
            if (this.verbProps.requireLineOfSight)
            {
                bool hasLineOfSight = base.TryFindShootLineFromTo(this.caster.Position, this.currentTarget, out _);
                if (this.verbProps.stopBurstWithoutLos && !hasLineOfSight)
                {
                    return false;
                }
            }
            base.EquipmentSource?.GetComp<CompChangeableProjectile>()?.Notify_ProjectileLaunched();
            this.lastShotTick = Find.TickManager.TicksGame;
            if (isRetargeting)
            {
                retargetShotsDone++;
                if (retargetShotsDone < retargetShotsTotal)
                {
                    return true;
                }

                isRetargeting = false;
            }
            this.lastShotTick = Find.TickManager.TicksGame;
            if (HeatComp != null)
            {
                HeatComp.AddHeatPerShot();
            }
            ApplyDamage(targetThing);
            return true;
        }

        private void ApplyDamage(Thing target)
        {
            if (target == null) return;

            Map map = this.caster.Map;
            Vector3 targetCenter = this.TargetPosition_Vector3;
            float angle = (this.currentTarget.Cell - this.caster.Position).AngleFlat;

            var log = new BattleLogEntry_RangedImpact(this.caster, target, this.currentTarget.Thing, base.EquipmentSource.def, null, null);
            if (Props.DamageDef != null && Props.DamageNum > 0)
            {
                var primaryDinfo = new DamageInfo(Props.DamageDef, Props.DamageNum * comp_Laser.QualityNum, Props.DamageArmorPenetration, angle, this.caster, null, base.EquipmentSource.def, DamageInfo.SourceCategory.ThingOrUnknown, this.currentTarget.Thing);
                target.TakeDamage(primaryDinfo).AssociateWithLog(log);
            }
            if (Props.IfSecondDamage && Props.DamageDef_B != null && Props.DamageNum_B > 0)
            {
                var secondaryDinfo = new DamageInfo(Props.DamageDef_B, Props.DamageNum_B * comp_Laser.QualityNum, Props.DamageArmorPenetration_B, angle, this.caster, null, base.EquipmentSource.def, DamageInfo.SourceCategory.ThingOrUnknown, this.currentTarget.Thing);
                target.TakeDamage(secondaryDinfo).AssociateWithLog(log);
            }
        }
        private bool IsDeadOrInvalidTarget(LocalTargetInfo targ)
        {
            if (!targ.IsValid) return true;

            Thing t = targ.Thing;
            if (t == null) return false;

            if (t.Destroyed || !t.Spawned || t.Map != this.caster.Map) return true;
            if (t is Pawn p && p.Dead) return true;

            return false;
        }
        private TraverseParms RetargetTraverseParms()
        {
            if (this.CasterIsPawn)
                return TraverseParms.For(this.CasterPawn, Danger.Deadly);

            return TraverseParms.For(TraverseMode.NoPassClosedDoors);
        }

        private Thing FindRetargetCandidate(IntVec3 rootCell, float radius)
        {
            return GenClosest.ClosestThingReachable(
                root: rootCell,
                map: this.caster.Map,
                thingReq: ThingRequest.ForGroup(ThingRequestGroup.AttackTarget),
                peMode: PathEndMode.Touch,
                traverseParams: RetargetTraverseParms(),
                maxDistance: radius,
                validator: (Thing x) =>
                {
                    if (x == null || x == this.caster) return false;
                    if (x.Destroyed || !x.Spawned || x.Map != this.caster.Map) return false;
                    if (x is Pawn p && p.Dead) return false;
                    if (!this.caster.HostileTo(x)) return false;

                    LocalTargetInfo lti = new LocalTargetInfo(x);

                    if (!this.CanHitTarget(lti)) return false;

                    if (this.verbProps.requireLineOfSight &&
                        !this.TryFindShootLineFromTo(this.caster.Position, lti, out _))
                        return false;

                    return true;
                });
        }
        private void TryRetargetIfNeeded()
        {
            if (burstShotsLeft <= 0) return;
            if (!IsDeadOrInvalidTarget(this.currentTarget)) return;

            Vector3 fromPos = BeamEndPosition;
            IntVec3 root = fromPos.ToIntVec3();

            Thing next = FindRetargetCandidate(root, Props.DefaultRetargetRadius);
            if (next == null) return;
            this.currentTarget = new LocalTargetInfo(next);

            isRetargeting = true;
            retargetFromPos = fromPos;
            retargetToPos = next.DrawPos;
            retargetShotsDone = 0;
            retargetShotsTotal = Mathf.Clamp(Props.DefaultRetargetTransitionShots, 1, Mathf.Max(1, burstShotsLeft));
        }
    }
}