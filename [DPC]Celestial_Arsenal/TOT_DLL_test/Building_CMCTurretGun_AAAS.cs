using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TOT_DLL_test
{
    [StaticConstructorOnStartup]
    public class Building_CMCTurretGun_AAAS : Building_CMCTurretGun
    {
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            map.GetComponent<MissileDefenseManager>()?.RegisterTurret(this);
        }
        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            Map?.GetComponent<MissileDefenseManager>()?.UnregisterTurret(this);
            base.DeSpawn(mode);
        }
        public bool CanEngageTarget(Thing target)
        {
            if (target == null) return false;
            return CanEngageTarget(new LocalTargetInfo(target));
        }
        public bool CanEngageTarget(LocalTargetInfo target)
        {
            if (!this.Spawned || this.Destroyed || this.holdFire)
                return false;
            var powerComp = this.GetComp<CompPowerTrader>();
            if (powerComp != null && !powerComp.PowerOn)
                return false;
            if (!this.Active)
                return false;
            Verb attackVerb = this.AttackVerb;
            if (attackVerb?.verbProps == null) return false;
            float distance = (target.Cell - this.Position).LengthHorizontal;
            if (distance > attackVerb.verbProps.range)
                return false;
            if (!attackVerb.CanHitTarget(target))
                return false;
            if (AttackVerb.state != VerbState.Idle || burstCooldownTicksLeft > 0)
                return false;
            return true;
        }
        protected override void BurstComplete()
        {
            base.BurstComplete();
        }
        protected override void BeginBurst()
        {
            Verb attackVerb = this.AttackVerb;
            if (attackVerb == null || !this.CurrentTarget.IsValid)
                return;
            if (!this.CurrentTarget.IsValid)
            {
                this.ResetCurrentTarget();
                return;
            }
            if (!this.TestForTarget(5f))
            {
                return;
            }
            bool started = attackVerb.TryStartCastOn(this.CurrentTarget, false, true);
            if (started)
            {
                base.OnAttackedTarget(this.CurrentTarget);
            }
        }
        public override void OrderAttack(LocalTargetInfo targ)
        {
            if (!targ.IsValid)
            {
                if (this.forcedTarget.IsValid)
                {
                    this.ResetForcedTarget();
                }
                return;
            }
            Verb attackVerb = this.AttackVerb;
            if (attackVerb?.verbProps == null) return;
            if (this.forcedTarget != targ)
            {
                this.forcedTarget = targ;
            }
            if (this.burstCooldownTicksLeft <= 0)
            {
                this.TryStartShootSomething(true);
            }
        }
        public override void TryStartShootSomething(bool canBeginBurstImmediately)
        {
            if (!base.Spawned || (this.holdFire && this.CanToggleHoldFire) || (this.AttackVerb.ProjectileFliesOverhead() && base.Map.roofGrid.Roofed(base.Position)) || !this.AttackVerb.Available())
            {
                this.ResetCurrentTarget();
                return;
            }
            bool isValid = this.currentTargetInt.IsValid;
            if (this.forcedTarget.IsValid)
            {
                this.currentTargetInt = this.forcedTarget;
            }
            if (!isValid && this.currentTargetInt.IsValid && this.def.building.playTargetAcquiredSound)
            {
                SoundDefOf.TurretAcquireTarget.PlayOneShot(new TargetInfo(base.Position, base.Map, false));
            }
            if (!this.currentTargetInt.IsValid)
            {
                this.ResetCurrentTarget();
                return;
            }
            if (canBeginBurstImmediately)
            {
                this.BeginBurst();
                return;
            }
        }
        private bool TestForTarget(float angleTolerance)
        {
            if (!this.CurrentTarget.IsValid) return false;
            float targetAngle = (this.CurrentTarget.Cell.ToVector3Shifted() - this.DrawPos).AngleFlat();
            float delta = Mathf.Abs(Mathf.DeltaAngle(this.turrettop.CurRotation, targetAngle));
            return delta <= angleTolerance;
        }
        protected override void Tick()
        {
            if (!this.forcedTarget.IsValid)
            {
                this.ResetForcedTarget();
            }
            bool flag6 = !this.CanToggleHoldFire;
            if (flag6)
            {
                this.holdFire = false;
            }
            bool thingDestroyed = this.forcedTarget.ThingDestroyed;
            if (thingDestroyed)
            {
                this.ResetForcedTarget();
            }
            if (this.Active && (this.mannableComp == null || this.mannableComp.MannedNow) && !base.IsStunned && base.Spawned)
            {
                this.GunCompEq.verbTracker.VerbsTick();
                if (this.AttackVerb.state != VerbState.Bursting)
                {
                    bool warmingUp = this.WarmingUp;
                    if (warmingUp)
                    {
                        this.burstWarmupTicksLeft--;
                        if (this.burstWarmupTicksLeft <= 0)
                        {
                            this.BeginBurst();
                        }
                    }
                    else
                    {
                        if (this.burstCooldownTicksLeft > 0)
                        {
                            this.burstCooldownTicksLeft--;
                        }
                        if (this.burstCooldownTicksLeft <= 0)
                        {
                            this.TryStartShootSomething(true);
                        }
                    }
                }
                this.turrettop.TurretTopTick();
            }
        }
    }
}
