using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TOT_DLL_test
{
    [StaticConstructorOnStartup]
    public class Comp_FloatingGunRework : ThingComp, IAttackTargetSearcher
    {
        public CompProperties_FloatGunRework Props
        {
            get
            {
                return (CompProperties_FloatGunRework)this.props;
            }
        }
        public Thing Thing
        {
            get
            {
                return this.PawnOwner;
            }
        }
        public Verb CurrentEffectiveVerb
        {
            get
            {
                return this.AttackVerb;
            }
        }
        public LocalTargetInfo LastAttackedTarget
        {
            get
            {
                return this.lastAttackedTarget;
            }
        }
        public int LastAttackTargetTick
        {
            get
            {
                return this.lastAttackTargetTick;
            }
        }
        public bool TempDestroyed()
        {
            return parent.HitPoints < parent.MaxHitPoints / 5;
        }
        public bool Active()
        {
            bool result = false;
            if(this.parent == null || PawnOwner == null)
            {
                result = false;
            }
            else
            {
                if(this.tickactive > ModifiedBatteryLifeTick / 10 && Released && !PawnOwner.DeadOrDowned && !PawnOwner.InBed())
                {
                    result = true;
                }
            }
            return result;
        }
        private bool ParentHeld()
        {
            if (PawnOwner != null && PawnOwner.ParentHolder != null)
            {
                return PawnOwner.ParentHolder is Map;
            }
            return true;
        }
        private bool WarmingUp
        {
            get
            {
                return this.burstWarmupTicksLeft > 0; 
            }
        }
        private void ResetCurrentTarget()
        {
            this.currentTarget = LocalTargetInfo.Invalid;
            this.burstWarmupTicksLeft = 0;
        }
        private static bool IsHashIntervalTick(Thing t, int interval)
        {
            return t.HashOffsetTicks() % interval == 0;
        }
        public override void CompTick()
        {
            bool Isdestroyed = TempDestroyed();
            base.CompTick();
            if(IsHashIntervalTick(this.parent, 300))
            {
                CheckRelease();
            }
            if (Active() && Released && !Isdestroyed && ParentHeld())
            {
                if (RenderRadius)
                {
                    float num = AttackVerb.EffectiveRange;
                    GenDraw.DrawCircleOutline(currentPosition, num, Props.RadiusColor);
                    GenDraw.DrawCircleOutline(currentPosition, num - 0.1f, Props.RadiusColor);
                    GenDraw.DrawCircleOutline(currentPosition, num - 0.2f, Props.RadiusColor);
                }
                UpdatePosition();
                this.tickactive--;
                if (this.fireAtWill)
                {
                    bool isValid = this.currentTarget.IsValid;
                    if (isValid)
                    {
                        this.curRotation = (this.currentTarget.Cell.ToVector3Shifted() - this.PawnOwner.DrawPos).AngleFlat();
                    }
                    this.AttackVerb.VerbTick();
                    if (this.AttackVerb.state != VerbState.Bursting)
                    {
                        bool warmingUp = this.WarmingUp;
                        if (warmingUp)
                        {
                            this.burstWarmupTicksLeft--;
                            if (this.burstWarmupTicksLeft == 0)
                            {
                                launching = true;
                                this.AttackVerb.TryStartCastOn(this.currentTarget, false, true, false, true);
                                this.lastAttackTargetTick = Find.TickManager.TicksGame;
                                this.lastAttackedTarget = this.currentTarget;
                                launching = false;
                            }
                        }
                        else
                        {
                            bool flag6 = this.burstCooldownTicksLeft > 0;
                            if (flag6)
                            {
                                this.burstCooldownTicksLeft--;
                            }
                            bool flag7 = this.burstCooldownTicksLeft <= 0 && this.PawnOwner.IsHashIntervalTick(10);
                            if (flag7)
                            {
                                this.currentTarget = (Thing)AttackTargetFinder.BestAttackTarget(this, TargetScanFlags.NeedThreat | TargetScanFlags.NeedAutoTargetable, null, 0f, 9999f, this.currentPosition.ToIntVec3());
                                bool isValid2 = this.currentTarget.IsValid;
                                if (isValid2)
                                {
                                    this.burstWarmupTicksLeft = 1;
                                }
                                else
                                {
                                    this.ResetCurrentTarget();
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                Released = false;
                if(IsHashIntervalTick(this.parent, 60) && !Isdestroyed)
                {
                    tickactive = Mathf.Min(tickactive + ModChargingSpeed, ModifiedBatteryLifeTick);
                }
                if(Isdestroyed)
                {
                    this.Released = false;
                }
            }
            tickactive = Mathf.Clamp(tickactive, 0, ModifiedBatteryLifeTick);
        }
        public int ModifiedBatteryLifeTick
        {
            get
            {
                if(ModifiedBatteryTickSaved <= 0)
                {
                    ModifiedBatteryTickSaved = (int)(this.Props.BatteryLifeTick);
                }
                return ModifiedBatteryTickSaved;
            }
        }
        private int ModChargingSpeed
        {
            get
            {
                if (ModifiedBatteryChargingTick <= 0)
                {
                    ModifiedBatteryChargingTick = (int)(this.Props.BatteryRecoverPerSec);
                }
                return ModifiedBatteryChargingTick;
            }
        }
        private void CheckRelease()
        {
            if(!ParentHeld())
            {
                return;
            }
            if(this.AutoRelease == true && this.parent != null && this.PawnOwner != null && !PawnOwner.Dead && tickactive > Props.BatteryLifeTick/2 && (PawnOwner.drafter.Drafted || PawnOwner.CurJob.def == JobDefOf.AttackStatic || PawnOwner.CurJob.def == JobDefOf.FleeAndCower) && !Released)
            {
                this.Released = true;
                this.currentPosition = PawnOwner.DrawPos;
            }
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref burstCooldownTicksLeft, "burstCooldownTicksLeft", 0);
            Scribe_Values.Look(ref burstWarmupTicksLeft, "burstWarmupTicksLeft", 0);
            Scribe_Values.Look(ref tickactive, "BatteryLifeLeft", 0);
            Scribe_TargetInfo.Look(ref currentTarget, "currentTarget");
            Scribe_Deep.Look<Thing>(ref this.gun, "gun", Array.Empty<object>());
            Scribe_Values.Look(ref fireAtWill, "fireAtWill", true);
            Scribe_Values.Look(ref Released, "UAVreleased", false);
            Scribe_Values.Look(ref RenderRadius, "RenderRadius", false);
            Scribe_Values.Look(ref AutoRelease, "autorelease", false);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (gun == null && PawnOwner != null)
                {
                    Log.Error("CompTurretGun: null gun after load. Recreating.");
                    MakeGun();
                }
                else
                {
                    UpdateGunVerbs();
                }
            }
        }
        public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
        {
            foreach (Gizmo item in base.CompGetWornGizmosExtra())
            {
                yield return item;
            }
            foreach (Gizmo gizmo in this.GetGizmos())
            {
                yield return gizmo;
            }
            yield break;
        }
        private IEnumerable<Gizmo> GetGizmos()
        {
            bool Isdestroyed = TempDestroyed();
            if (this.PawnOwner.IsPlayerControlled && this.PawnOwner.Drafted)
            {
                //BatteryPercentage
                if (Find.Selector.SingleSelectedThing == this.PawnOwner)
                {
                    if (gizmo_UAVPowerCell == null)
                    {
                        gizmo_UAVPowerCell = new Gizmo_UAVPowerCell(this);
                    }
                    yield return gizmo_UAVPowerCell;
                }
                //Release
                if(!Active() && !AutoRelease && !Isdestroyed)
                {
                    Command_Action command1 = new Command_Action
                    {
                        defaultLabel = "Command_CMC_ReleaseUAV".Translate(),
                        icon = ReleaseIcon.Texture,
                        action = delegate ()
                        {
                            Released = true;
                            this.currentPosition = PawnOwner.DrawPos;
                        }
                    };
                    yield return command1;
                }
                //Return
                if (Active())
                {
                    yield return new Command_Toggle
                    {
                        defaultLabel = "CommandToggleTurret".Translate(),
                        defaultDesc = "CommandToggleTurretDesc".Translate(),
                        isActive = (() => this.fireAtWill),
                        icon = ForceAttackIcon.Texture,
                        toggleAction = delegate ()
                        {
                            this.fireAtWill = !this.fireAtWill;
                        }
                    };
                    Command_Action command1 = new Command_Action
                    {
                        defaultLabel = "Command_CMC_CallBACKUAV".Translate(),
                        icon = CallIcon.Texture,
                        action = delegate ()
                        {
                            Released = false;
                        }
                    };
                    yield return command1; 
                }
                Command_Toggle command2 = new Command_Toggle
                {
                    //Toggle Auto Release
                    defaultLabel = "Command_CMC_AutoReleaseUAV".Translate(),
                    defaultDesc = "Command_CMC_AutoReleaseUAV_Desc".Translate(),
                    isActive = (() => AutoRelease),
                    icon = AutoReleaseIcon.Texture,
                    toggleAction = delegate ()
                    {
                        AutoRelease = !AutoRelease;
                    }
                };
                yield return command2;
                yield return new Command_Toggle
                {
                    defaultLabel = "Command_CMCUAVDrawRadius".Translate(),
                    defaultDesc = "Command_CMCUAVDrawRadiusDesc".Translate(),
                    isActive = (() => this.RenderRadius),
                    icon = RadiusIcon.Texture,
                    toggleAction = delegate ()
                    {
                        this.RenderRadius = !this.RenderRadius;
                    }
                };
            }
            yield break;
        }
        public override void Notify_Equipped(Pawn pawn)
        {
            base.PostPostMake();
            MakeGun();
            UpdateCurrentPos();
        }
        private void MakeGun()
        {
            this.gun = ThingMaker.MakeThing(this.Props.turretDef, null);
            this.UpdateGunVerbs();
        }
        private void UpdateCurrentPos()
        {
            this.currentPosition = this.PawnOwner.DrawPos;
        }
        private void UpdateGunVerbs()
        {
            List<Verb> allVerbs = this.gun.TryGetComp<CompEquippable>().AllVerbs;
            for (int i = 0; i < allVerbs.Count; i++)
            {
                Verb verb = allVerbs[i];
                verb.caster = this.PawnOwner;
                verb.castCompleteCallback = delegate ()
                {
                    this.burstCooldownTicksLeft = this.AttackVerb.verbProps.defaultCooldownTime.SecondsToTicks();
                };
            }
        }
        public Pawn PawnOwner
        {
            get
            {
                Apparel apparel;
                bool flag = (apparel = (this.parent as Apparel)) != null;
                Pawn result;
                if (flag)
                {
                    result = apparel.Wearer;
                }
                else
                {
                    Pawn pawn;
                    bool flag2 = (pawn = (this.parent as Pawn)) != null;
                    if (flag2)
                    {
                        result = pawn;
                    }
                    else
                    {
                        result = null;
                    }
                }
                return result;
            }
        }
        public Verb AttackVerb
        {
            get
            {
                return this.GunCompEq.PrimaryVerb;
            }
        }
        public CompEquippable GunCompEq
        {
            get
            {
                return this.gun.TryGetComp<CompEquippable>();
            }
        }
        public Vector3 UpdatePosition()
        {
            float deltaTime = Time.deltaTime;
            if (Time.time - lastUpdateTime > PositionChangeInterval)
            {
                targetOffset = GetRandomOffset();
                lastUpdateTime = Time.time;
            }
            targetPosition = PawnOwner.DrawPos + targetOffset;
            Vector3 desiredAcceleration = (targetPosition - currentPosition) * Acceleration;
            currentVelocity += desiredAcceleration * deltaTime;
            currentVelocity *= Mathf.Clamp01(1f - Damping * deltaTime);
            if (currentVelocity.magnitude > MaxSpeed)
            {
                currentVelocity = currentVelocity.normalized * MaxSpeed;
            }
            currentPosition += currentVelocity * deltaTime;
            return currentPosition;
        }
        private Vector3 GetRandomOffset()
        {
            float angle = Rand.Range(0f, 360f);
            float distance = Rand.Range(1.78f, 3.4f);
            return new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * distance,
                0,
                Mathf.Sin(angle * Mathf.Deg2Rad) * distance
            );
        }
        public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
            absorbed = false;
            if(dinfo.Def == DamageDefOf.EMP)
            {
                this.parent.HitPoints -= 80;
            }
            else if(Rand.Chance(0.25f))
            {
                this.parent.HitPoints -= 40; 
            }
        }
        public Vector3 targetPosition;
        public bool launching;
        public bool Released = false;
        private bool AutoRelease = false;
        public int tickactive = -1;
        public static readonly Vector3 PosDefault = Vector3.zero;
        private int burstCooldownTicksLeft;
        private int burstWarmupTicksLeft;
        public LocalTargetInfo currentTarget;
        public Thing gun;
        public bool fireAtWill;
        public Vector3 currentPosition;
        public Vector3 currentVelocity;
        private Vector3 targetOffset;
        private float lastUpdateTime;
        private const float PositionChangeInterval = 1.5f;
        private const float MaxSpeed = 12f;
        private const float Acceleration = 4.8f;
        private const float Damping = 4f;
        private static readonly CachedTexture ReleaseIcon = new CachedTexture("UI/UI_ReleaseUAV");
        private static readonly CachedTexture AutoReleaseIcon = new CachedTexture("UI/UI_ReleaseUAV_Auto");
        private static readonly CachedTexture CallIcon = new CachedTexture("UI/UI_Return");
        private static readonly CachedTexture ForceAttackIcon = new CachedTexture("UI/UI_ForceAttack");
        private static readonly CachedTexture RadiusIcon = new CachedTexture("UI/UI_TargetRange");
        private Gizmo_UAVPowerCell gizmo_UAVPowerCell;
        public float curRotation;
        private LocalTargetInfo lastAttackedTarget = LocalTargetInfo.Invalid;
        private int lastAttackTargetTick;
        public bool RenderRadius;
        private int ModifiedBatteryTickSaved = -1;
        private int ModifiedBatteryChargingTick = -1;
    }
}
