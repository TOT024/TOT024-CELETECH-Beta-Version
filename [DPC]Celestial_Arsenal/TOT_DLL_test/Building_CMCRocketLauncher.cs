using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace TOT_DLL_test
{
    [StaticConstructorOnStartup]
    public class Building_CMCRocketLauncher : Building_CMCTurretGun
    {
        public override bool CanSetForcedTarget
        {
            get
            {
                return true;
            }
        }
        protected override void Tick()
        {
            base.Tick();
            bool flag = this.forcedTarget.IsValid && !this.CanSetForcedTarget;
            if (flag)
            {
                base.ResetForcedTarget();
            }
            bool flag2 = !base.CanToggleHoldFire;
            if (flag2)
            {
                this.holdFire = false;
            }
            bool thingDestroyed = this.forcedTarget.ThingDestroyed;
            if (thingDestroyed)
            {
                base.ResetForcedTarget();
            }
            bool flag3 = base.Active && (this.mannableComp == null || this.mannableComp.MannedNow) && !base.IsStunned && base.Spawned;
            if (flag3)
            {
                base.GunCompEq.verbTracker.VerbsTick();
                bool flag4 = this.AttackVerb.state != VerbState.Bursting;
                if (flag4)
                {
                    this.burstActivated = false;
                    bool flag5 = base.WarmingUp && this.turrettop.CurRotation == this.turrettop.DestRotation;
                    if (flag5)
                    {
                        this.burstWarmupTicksLeft--;
                        bool flag6 = this.burstWarmupTicksLeft == 0;
                        if (flag6)
                        {
                            this.BeginBurst();
                        }
                    }
                    else
                    {
                        bool flag7 = this.burstCooldownTicksLeft > 0;
                        if (flag7)
                        {
                            this.burstCooldownTicksLeft--;
                        }
                        bool flag8 = this.burstCooldownTicksLeft <= 0 && this.IsHashIntervalTick(10);
                        if (flag8)
                        {
                            base.TryStartShootSomething(true);
                        }
                    }
                    this.turrettop.TurretTopTick();
                }
            }
            else
            {
                base.ResetCurrentTarget();
            }
        }
    }
}
