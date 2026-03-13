using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace TOT_DLL_test
{
    [StaticConstructorOnStartup]
    public class Building_CMCTurretGun_MainBattery : Building_CMCTurretGun
    {
        public override bool IsTargrtingWorld
        {
            get
            {
                return this.IsTargrtingWorldInt;
            }
        }
        public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
        {
            IEnumerable<StatDrawEntry> enumerable = base.SpecialDisplayStats();
            if (enumerable != null)
            {
                foreach (StatDrawEntry statDrawEntry in enumerable)
                {
                    yield return statDrawEntry;
                }
            }
            List<Verb> allVerbs = this.gun.TryGetComp<CompEquippable>().AllVerbs;
            for (int i = 0; i < allVerbs.Count; i++)
            {
                Verb verb = allVerbs[i];
                if(verb is Verb_ShootMultiTarget)
                {
                    Verb_ShootMultiTarget verb_ShootMultiTarget = (Verb_ShootMultiTarget)verb;
                    yield return new StatDrawEntry(StatCategoryDefOf.Weapon_Ranged, "StatShootNum_Label".Translate(), "StatShootNum_Desc".Translate(verb_ShootMultiTarget.ShootNum * verb_ShootMultiTarget.verbProps.burstShotCount), "StatShootNum_Text".Translate(verb_ShootMultiTarget.ShootNum * verb_ShootMultiTarget.verbProps.burstShotCount), 25, null, null, false, false);
                }
            }
            yield break;
        }
        public float CalculateRecoil()
        {
            const float maxRecoil = 1.5f;
            const int rapidPhase = 20;    
            const int holdPhase = 80;     
            const int totalTicks = 600;

            int currentTick = this.BurstCooldownTime().SecondsToTicks() - burstCooldownTicksLeft;
            if (currentTick > 600)
            {
                return 0f;
            }

            if (currentTick <= rapidPhase)
            {
                float t = currentTick / (float)rapidPhase;
                return maxRecoil * t * t;
            }
            if (currentTick <= holdPhase)
            {
                return maxRecoil;
            }
            {
                float t = (currentTick - holdPhase) / (float)(totalTicks - holdPhase);
                float easeOut = t * t;
                float easeIn = 1 - (1 - t) * (1 - t);
                float blend = easeOut * 0.4f + easeIn * 0.6f;

                return maxRecoil * (1 - blend);
            }
        }
        public override AcceptanceReport ClaimableBy(Faction by)
        {
            AcceptanceReport result = base.ClaimableBy(by);
            if (!result.Accepted)
            {
                return result;
            }
            if (this.mannableComp != null && this.mannableComp.ManningPawn != null)
            {
                return false;
            }
            if (this.Active && this.mannableComp == null)
            {
                return false;
            }
            if (((this.dormantComp != null && !this.dormantComp.Awake) || (this.initiatableComp != null && !this.initiatableComp.Initiated)) && (this.powerComp == null || this.powerComp.PowerOn))
            {
                return false;
            }
            return true;
        }
        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            base.DeSpawn(mode);
            this.ResetCurrentTarget();
            Effecter effecter = this.progressBarEffecter;
            bool flag = effecter == null;
            if (!flag)
            {
                effecter.Cleanup();
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.IsTargrtingWorldInt, "IsTargrtingWorld", false, false);
        }
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
            if(IsTargrtingWorld)
            {
                Command_Action command_Action = new Command_Action
                {
                    defaultLabel = "CMC_CommandTargetMapFromWorld".Translate(),
                    defaultDesc = "CMC_CommandTarggetMapFromWorldDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/UI_TargetMap", true),
                    action = delegate ()
                    {
                        this.IsTargrtingWorldInt = false;
                        this.powerComp.powerOutputInt = -10000;
                        SoundDefOf.Tick_Low.PlayOneShotOnCamera(null);
                    }
                };
                yield return command_Action;
            }
            else
            {
                Command_Action command_Action = new Command_Action
                {
                    defaultLabel = "CMC_CommandTargetWorldFromMap".Translate(),
                    defaultDesc = "CMC_CommandTarggetWorldFromMapDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/UI_TargetWorld", true),
                    action = delegate ()
                    {
                        this.IsTargrtingWorldInt = true;
                        this.powerComp.powerOutputInt = -48000;
                        SoundDefOf.Tick_Low.PlayOneShotOnCamera(null);
                    }
                };
                yield return command_Action;
            }
                yield break;
        }
        public override void OrderAttack(LocalTargetInfo targ)
        {
            bool flag = !targ.IsValid;
            if (flag)
            {
                bool isValid = this.forcedTarget.IsValid;
                if (isValid)
                {
                    this.ResetForcedTarget();
                }
            }
            else
            {
                bool flag2 = (targ.Cell - base.Position).LengthHorizontal < this.AttackVerb.verbProps.EffectiveMinRange(targ, this);
                if (flag2)
                {
                    Messages.Message("MessageTargetBelowMinimumRange".Translate(), this, MessageTypeDefOf.RejectInput, false);
                }
                else
                {
                    bool flag3 = (targ.Cell - base.Position).LengthHorizontal > this.AttackVerb.verbProps.range;
                    if (flag3)
                    {
                        Messages.Message("MessageTargetBeyondMaximumRange".Translate(), this, MessageTypeDefOf.RejectInput, false);
                    }
                    else
                    {
                        bool flag4 = this.forcedTarget != targ;
                        if (flag4)
                        {
                            this.forcedTarget = targ;
                            bool flag5 = this.burstCooldownTicksLeft <= 0;
                            if (flag5)
                            {
                                this.TryStartShootSomething(false);
                            }
                        }
                        bool flag6 = this.holdFire;
                        if (flag6)
                        {
                            Messages.Message("MessageTurretWontFireBecauseHoldFire".Translate(this.def.label), this, MessageTypeDefOf.RejectInput, false);
                        }
                    }
                }
            }
        }
        public override void PostMake()
        {
            base.PostMake();
            this.burstCooldownTicksLeft = this.def.building.turretInitialCooldownTime.SecondsToTicks();
            this.MakeGun();
        }
        protected override void Tick()
        {
            base.Tick();
            if(!this.IsTargrtingWorld)
            {
                if (this.forcedTarget.IsValid && !this.CanSetForcedTarget)
                {
                    this.ResetForcedTarget();
                }
                bool thingDestroyed = this.forcedTarget.ThingDestroyed;
                if (thingDestroyed)
                {
                    this.ResetForcedTarget();
                }
                if (this.Active && (this.mannableComp == null || this.mannableComp.MannedNow) && !base.IsStunned && base.Spawned)
                {
                    this.turrettop.TurretTopTick();
                    this.GunCompEq.verbTracker.VerbsTick();
                    if (this.AttackVerb.state != VerbState.Bursting)
                    {
                        this.burstActivated = false;
                        bool flag7 = this.WarmingUp && this.turrettop.CurRotation == this.turrettop.DestRotation;
                        if (flag7)
                        {
                            this.burstWarmupTicksLeft--;
                            bool flag8 = this.burstWarmupTicksLeft == 0;
                            if (flag8)
                            {
                                this.BeginBurst();
                            }
                        }
                        else
                        {
                            bool flag9 = this.burstCooldownTicksLeft > 0;
                            if (flag9)
                            {
                                this.burstCooldownTicksLeft--;
                            }
                            bool flag10 = this.burstCooldownTicksLeft <= 0 && this.IsHashIntervalTick(10);
                            if (flag10)
                            {
                                this.TryStartShootSomething(true);
                            }
                        }
                    }
                }
                else
                {
                    this.ResetCurrentTarget();
                }
            }
            else
            {
                if (this.Active && (this.mannableComp == null || this.mannableComp.MannedNow) && !base.IsStunned && base.Spawned && GameComponent_CeleTech.Instance.ASEA_observedMap != null)
                {
                    this.turrettop.TurretTopTick();
                    if (this.burstCooldownTicksLeft <= 0 && this.turrettop.CurRotation == this.turrettop.DestRotation)
                    {
                        Map map = GameComponent_CeleTech.Instance.ASEA_observedMap.Map;
                        if(map == null)
                        {
                            return;
                        }
                        if (map.attackTargetsCache.TargetsHostileToFaction(this.Faction).Count > 0)
                        {
                            TryStartShootSomething_WorldTarget(canBeginBurstImmediately: true);
                        }
                    }
                    else
                    {
                        this.burstCooldownTicksLeft--;
                    }
                }
            }
            if (this.CanExtractShell)
            {
                CompChangeableProjectile compChangeableProjectile = this.gun.TryGetComp<CompChangeableProjectile>();
                if (!compChangeableProjectile.allowedShellsSettings.AllowedToAccept(compChangeableProjectile.LoadedShell))
                {
                    this.ExtractShell();
                }
            }
            if (!this.CanToggleHoldFire)
            {
                this.holdFire = false;
            }
        }
        public override LocalTargetInfo TryFindNewTarget()
        {
            IAttackTargetSearcher attackTargetSearcher = this.TargSearcher();
            Faction faction = attackTargetSearcher.Thing.Faction;
            float range = this.AttackVerb.verbProps.range;
            Building t;
            if (Rand.Value < 0.5f && this.AttackVerb.ProjectileFliesOverhead() && faction.HostileTo(Faction.OfPlayer) && base.Map.listerBuildings.allBuildingsColonist.Where(delegate (Building x)
            {
                float num = this.AttackVerb.verbProps.EffectiveMinRange(x, this);
                float num2 = (float)x.Position.DistanceToSquared(this.Position);
                return num2 > num * num && num2 < range * range;
            }).TryRandomElement(out t))
            {
                return t;
            }
            TargetScanFlags targetScanFlags = TargetScanFlags.NeedThreat | TargetScanFlags.NeedAutoTargetable;
            bool flag2 = !this.AttackVerb.ProjectileFliesOverhead();
            if (flag2)
            {
                targetScanFlags |= TargetScanFlags.NeedLOSToAll;
                targetScanFlags |= TargetScanFlags.LOSBlockableByGas;
            }
            bool isMortar = this.IsMortar;
            if (isMortar)
            {
                targetScanFlags |= TargetScanFlags.NeedNotUnderThickRoof;
            }
            return (Thing)AttackTargetFinder.BestShootTargetFromCurrentPosition(attackTargetSearcher, targetScanFlags, new Predicate<Thing>(this.IsValidTarget), 0f, 9999f);
        }
        public void TryStartShootSomething_WorldTarget(bool canBeginBurstImmediately)
        {
            if (GameComponent_CeleTech.Instance.ASEA_observedMap.Map == null)
            {
                return;
            }
            if (!base.Spawned || (this.holdFire && this.CanToggleHoldFire) || (this.AttackVerb.ProjectileFliesOverhead() && base.Map.roofGrid.Roofed(base.Position)) || !this.AttackVerb.Available())
            {
            }
            else
            {
                Verb_ShootWorld verb = this.AttackVerb as Verb_ShootWorld;
                verb.TryCastFireMission();
                this.BurstComplete();
                this.refuelableComp.ConsumeFuel(1);
            }
        }
        public override LocalTargetInfo CurrentTarget
        {
            get
            {
                return this.currentTargetInt;
            }
        }
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
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            TurretExtension_CMC modExtension = this.def.GetModExtension<TurretExtension_CMC>();
            this.rotationVelocity = modExtension.rotationSpeed;
            this.IsTargrtingWorldInt = false;
            this.dormantComp = base.GetComp<CompCanBeDormant>();
            this.initiatableComp = base.GetComp<CompInitiatable>();
            this.powerComp = base.GetComp<CompPowerTrader>();
            this.mannableComp = base.GetComp<CompMannable>();
            this.interactableComp = base.GetComp<CompInteractable>();
            this.refuelableComp = base.GetComp<CompRefuelable>();
            this.powerCellComp = base.GetComp<CompMechPowerCell>();
            bool flag = !respawningAfterLoad;
            if (flag)
            {
                this.turrettop.SetRotationFromOrientation();
            }
        }
        public const int highpowerCost = 3;
        public bool IsTargrtingWorldInt;
    }
}
