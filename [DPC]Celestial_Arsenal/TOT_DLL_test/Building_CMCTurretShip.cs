using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TOT_DLL_test
{
    [StaticConstructorOnStartup]
    public class Building_CMCTurretShip : Building_CMCTurretGun
    {
        public override bool CanSetForcedTarget
        {
            get
            {
                if (base.Faction == Faction.OfPlayer)
                {
                    return true;
                }
                return false;
            }
        }
        public override bool IsValidTarget(Thing t)
        {
            if (t is Pawn pawn)
            {
                if (base.Faction == Faction.OfPlayer && pawn.IsPrisoner) return false;
                if (this.mannableComp == null) return !GenAI.MachinesLike(base.Faction, pawn);
                if (pawn.RaceProps.Animal && pawn.Faction == Faction.OfPlayer) return false;
            }
            return true;
        }

        public override LocalTargetInfo TryFindNewTarget()
        {
            IAttackTargetSearcher attackTargetSearcher = this.TargSearcher();
            Verb attackVerb = this.AttackVerb;
            if (attackVerb?.verbProps == null) return LocalTargetInfo.Invalid;

            TargetScanFlags targetScanFlags = TargetScanFlags.NeedThreat | TargetScanFlags.NeedAutoTargetable;

            if (attackVerb.IsIncendiary_Ranged())
            {
                targetScanFlags |= TargetScanFlags.NeedNonBurning;
            }

            return (Thing)AttackTargetFinder.BestShootTargetFromCurrentPosition(attackTargetSearcher, targetScanFlags, this.IsValidTarget, 0f, 9999f);
        }

        protected override void Tick()
        {
            bool active = true;
            if (!this.IsHashIntervalTick(this.ScanInterval))
            {
                active = false;
            }

            base.Tick();

            if (this.CanExtractShell && this.MannedByColonist)
            {
                CompChangeableProjectile compChangeableProjectile = this.gun.TryGetComp<CompChangeableProjectile>();
                if (!compChangeableProjectile.allowedShellsSettings.AllowedToAccept(compChangeableProjectile.LoadedShell))
                {
                    this.ExtractShell();
                }
            }

            if (this.forcedTarget.IsValid && !this.CanSetForcedTarget)
            {
                this.ResetForcedTarget();
            }

            if (!this.CanToggleHoldFire)
            {
                this.holdFire = false;
            }

            if (this.forcedTarget.ThingDestroyed)
            {
                if (active)
                {
                    this.ResetForcedTarget();
                }
            }

            if (this.Active && (this.mannableComp == null || this.mannableComp.MannedNow) && !base.IsStunned && base.Spawned)
            {
                this.GunCompEq.verbTracker.VerbsTick();
                if (this.AttackVerb.state != VerbState.Bursting)
                {
                    this.burstActivated = false;
                    if (this.WarmingUp)
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
                            if (this.IsMortar)
                            {
                                this.progressBarEffecter.EffectTick(this, TargetInfo.Invalid);
                                MoteProgressBar mote = ((SubEffecter_ProgressBar)this.progressBarEffecter.children[0]).mote;
                                mote.progress = 1f - (float)Math.Max(this.burstCooldownTicksLeft, 0) / (float)this.BurstCooldownTime().SecondsToTicks();
                                mote.offsetZ = -0.8f;
                            }
                        }
                        if (this.burstCooldownTicksLeft <= 0)
                        {
                            if (active)
                            {
                                this.TryStartShootSomethingUnrestricted(true);
                            }
                        }
                    }
                    this.turrettop.TurretTopTick();
                }
            }
            else
            {
                if (active)
                {
                    this.ResetCurrentTarget();
                }
            }
        }

        public void TryStartShootSomethingUnrestricted(bool canBeginBurstImmediately)
        {
            this.progressBarEffecter?.Cleanup();
            this.progressBarEffecter = null;
            Verb attackVerb = this.AttackVerb;

            if (!base.Spawned || (this.holdFire && this.CanToggleHoldFire) || !attackVerb.Available())
            {
                this.ResetCurrentTarget();
                return;
            }

            if (this.forcedTarget.IsValid)
            {
                this.currentTargetInt = this.forcedTarget;
            }
            else if (IsTargetStillValidUnrestricted(this.currentTargetInt))
            {
            }
            else
            {
                this.currentTargetInt = this.TryFindNewTarget();
            }

            if (!this.currentTargetInt.IsValid)
            {
                this.ResetCurrentTarget();
            }
            else
            {
                if (canBeginBurstImmediately)
                {
                    this.BeginBurst();
                }
                else
                {
                    if (this.burstWarmupTicksLeft == 0)
                    {
                        this.burstWarmupTicksLeft = 1;
                    }
                }
            }
        }

        private bool IsTargetStillValidUnrestricted(LocalTargetInfo t)
        {
            if (!t.IsValid) return false;
            if (t.HasThing)
            {
                Thing thing = t.Thing;
                if (thing.DestroyedOrNull()) return false;
                if (!thing.Spawned || thing.Map != base.Map) return false;
                if (thing is Pawn p && (p.Dead || p.Downed)) return false;
            }
            Verb attackVerb = this.AttackVerb;
            if (attackVerb?.verbProps == null) return false;
            float range = attackVerb.verbProps.range;
            float distSq = (t.Cell - base.Position).LengthHorizontalSquared;
            if (distSq > range * range) return false;
            float minRange = attackVerb.verbProps.EffectiveMinRange(t, this);
            if (distSq < minRange * minRange) return false;

            return true;
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                if (gizmo is Command_VerbTarget cmd && cmd.verb == this.AttackVerb)
                {
                    continue;
                }
                yield return gizmo;
            }

            if (!this.HideForceTargetGizmo && this.CanSetForcedTarget)
            {
                Command_VerbTarget command_VerbTarget = new Command_VerbTarget
                {
                    defaultLabel = "CommandSetForceAttackTarget".Translate(),
                    defaultDesc = "CommandSetForceAttackTargetDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/Attack", true),
                    verb = this.AttackVerb,
                    hotKey = KeyBindingDefOf.Misc4,
                    drawRadius = false,
                    requiresAvailableVerb = false
                };
                yield return command_VerbTarget;
            }
        }
    }
}
