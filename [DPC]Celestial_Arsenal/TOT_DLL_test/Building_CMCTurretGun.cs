using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace TOT_DLL_test
{
    [StaticConstructorOnStartup]
    public class Building_CMCTurretGun : Building_Turret
    {
        public CompTurretLifter cachedLifter;
        public Comp_DrawTurretBarrel cachedBarrel;
        public Sustainer wickSustainer;
        readonly bool canBeginBurstImmediately = false;
        public CMCTurretTop turrettop;
        public float rotationVelocity;
        public int burstCooldownTicksLeft;
        public int burstWarmupTicksLeft = 6;
        public LocalTargetInfo currentTargetInt = LocalTargetInfo.Invalid;
        public bool holdFire;
        public bool burstActivated;
        public Thing gun;
        public CompPowerTrader powerComp;
        public CompCanBeDormant dormantComp;
        public CompInitiatable initiatableComp;
        public CompMannable mannableComp;
        public CompInteractable interactableComp;
        public CompRefuelable refuelableComp;
        public Effecter progressBarEffecter;
        public CompMechPowerCell powerCellComp;
        public static Material ForcedTargetLineMat = MaterialPool.MatFrom(GenDraw.LineTexPath, ShaderDatabase.Transparent, new Color(1f, 0.5f, 0.5f));
        public static Material RangeMat = MaterialPool.MatFrom(GenDraw.LineTexPath, ShaderDatabase.MoteGlow);
        public bool SleepMode = false;
        public Building_CMCTurretGun()
        {
            this.turrettop = new CMCTurretTop(this);
        }
        public virtual bool IsTargrtingWorld
        {
            get
            {
                return false;
            }
        }
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            TurretExtension_CMC modExtension = this.def.GetModExtension<TurretExtension_CMC>();
            if (modExtension != null)
            {
                this.rotationVelocity = modExtension.rotationSpeed;
            }
            this.dormantComp = base.GetComp<CompCanBeDormant>();
            this.initiatableComp = base.GetComp<CompInitiatable>();
            this.powerComp = base.GetComp<CompPowerTrader>();
            this.mannableComp = base.GetComp<CompMannable>();
            this.interactableComp = base.GetComp<CompInteractable>();
            this.refuelableComp = base.GetComp<CompRefuelable>();
            this.powerCellComp = base.GetComp<CompMechPowerCell>();
            cachedLifter = GetComp<CompTurretLifter>();
            cachedBarrel = GetComp<Comp_DrawTurretBarrel>();
            //this.accessoryHolder = this.GetComp<CompAccessoryHolder>();

            if (!respawningAfterLoad)
            {
                this.turrettop.SetRotationFromOrientation();
                this.SleepMode = false;
            }
            if (respawningAfterLoad && this.gun == null)
            {
                Log.Error("Turret had null gun after loading. Recreating.");
                this.MakeGun();
            }
            else if (this.gun != null)
            {
                this.UpdateGunVerbs();
            }
        }
        public void StartWickSustainer()
        {
            SoundInfo info = SoundInfo.InMap(this, MaintenanceType.PerTick);
            this.wickSustainer = SoundDef.Named("CMC_TurretRotate").TrySpawnSustainer(info);
        }
        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            Vector3 zero = Vector3.zero;
            if (cachedLifter == null || (cachedLifter != null && cachedLifter.IsFullyDeployed))
            {
                this.turrettop.DrawTurret(drawLoc, zero);
                this.TryGetComp<Comp_DrawTurretBarrel>()?.CompPostDrawBarrel(this);
            }
            base.DrawAt(drawLoc, flip);
        }
        public static void GunRecoil(ThingDef weaponDef, Verb_LaunchProjectile shootVerb, out Vector3 drawOffset, float aimAngle)
        {
            drawOffset = Vector3.zero;
            bool flag = weaponDef.recoilPower > 0f && shootVerb != null;
            if (flag)
            {
                Rand.PushState(shootVerb.LastShotTick);
                try
                {
                    int num = Find.TickManager.TicksGame - shootVerb.LastShotTick;
                    bool flag2 = (float)num < 180f;
                    if (flag2)
                    {
                        drawOffset = new Vector3(0f, 0f, -Mathf.Sin((float)num / 180f * 3.14159f) * Mathf.Sin((float)num / 180f * 3.14159f) * 1.6f);
                        drawOffset = drawOffset.RotatedBy(aimAngle);
                    }
                }
                finally
                {
                    Rand.PopState();
                }
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
                Verb attackVerb = this.AttackVerb;
                VerbProperties verbProps = attackVerb?.verbProps;
                if (attackVerb != null && verbProps != null && !(attackVerb is Verb_Laser_Sustain))
                {
                    ThingDef projectileDef = verbProps.defaultProjectile;
                    if (projectileDef != null)
                    {
                        string typeLabelKey = "TOT_ProjType_Standard";
                        string typeDescKey = "TOT_ProjType_Standard_Desc";
                        if (projectileDef.thingClass == typeof(Projectile_Piercing) || projectileDef.thingClass == typeof(Projectile_XM_Re))
                        {
                            typeLabelKey = "TOT_ProjType_Piercing";
                            typeDescKey = "TOT_ProjType_Piercing_Desc";
                        }
                        else if (projectileDef.thingClass == typeof(Projectile_FakeBulletLaser))
                        {
                            typeLabelKey = "TOT_ProjType_PL";
                            typeDescKey = "TOT_ProjType_PL_Desc";
                        }
                        else if (this.AttackVerb is Verb_Laser_Sustain)
                        {
                            typeLabelKey = "TOT_ProjType_Laser";
                            typeDescKey = "TOT_ProjType_Laser_Desc";
                        }
                        yield return new StatDrawEntry(
                            StatCategoryDefOf.Weapon,
                            "TOT_ProjType_Label".Translate(),
                            typeLabelKey.Translate(),         
                            typeDescKey.Translate(),        
                            3020
                        );
                    }
                    if (projectileDef != null && projectileDef.projectile != null)
                    {

                        float damagePerShot = projectileDef.projectile.GetDamageAmount(null);
                        if (projectileDef.projectile.extraDamages != null)
                        {
                            foreach (ExtraDamage extra in projectileDef.projectile.extraDamages)
                            {
                                damagePerShot += extra.amount;
                            }
                        }
                        int burstCount = verbProps.burstShotCount;
                        float totalBurstDamage = damagePerShot * burstCount;
                        float warmup = verbProps.warmupTime;
                        float cooldown = this.BurstCooldownTime();
                        float burstInterval = verbProps.ticksBetweenBurstShots / 60f;
                        float totalTime = warmup + cooldown + ((burstCount - 1) * burstInterval);
                        if (totalTime < 0.001f) totalTime = 1f;
                        float dps = totalBurstDamage / totalTime;
                        string penStr = "";
                        var piercingExt = projectileDef.GetModExtension<PiercingAmmo_Extension>();
                        if (piercingExt != null)
                        {
                            penStr = $"\nPenetration: {piercingExt.penetratingPower + GameComponent_CeleTech.Instance.ExtraPenetration()}";
                        }
                        yield return new StatDrawEntry(
                            StatCategoryDefOf.Weapon,
                            "TOT_DPS_Solid_Label".Translate(),
                            dps.ToString("F1"),
                            "TOT_DPS_Solid_Desc".Translate(damagePerShot, burstCount, warmup.ToString("F2"), cooldown.ToString("F2")) + penStr,
                            4010
                        );
                    }
                }
                if (this.AttackVerb is Verb_Laser_Sustain && this.gun.TryGetComp<Comp_LaserData_Sustain>()!= null)
                {
                    Comp_LaserData_Sustain comp_LaserData_Sustain = this.gun.TryGetComp<Comp_LaserData_Sustain>();
                    int damageTickInterval = 20;
                    ThingDef weaponDef = gun.def as ThingDef;

                    if (weaponDef != null && weaponDef.Verbs != null && weaponDef.Verbs.Count > 0)
                    {
                        damageTickInterval = weaponDef.Verbs[0].ticksBetweenBurstShots;
                    }
                    if (damageTickInterval <= 0)
                    {
                        damageTickInterval = 1;
                    }
                    float attacksPerSecond = 60f / (float)damageTickInterval;
                    float dps = (float)comp_LaserData_Sustain.Props.DamageNum * attacksPerSecond;
                    yield return new StatDrawEntry(StatCategoryDefOf.Weapon,
                        "TOT_Sustain_DPSLabel".Translate(),
                        dps.ToString("0.0"),
                        "TOT_Sustain_DPSDesc".Translate(comp_LaserData_Sustain.Props.DamageNum, damageTickInterval),
                        4010);

                    yield return new StatDrawEntry(StatCategoryDefOf.Weapon,
                        "TOT_Sustain_DamageLabel".Translate(),
                        comp_LaserData_Sustain.Props.DamageNum.ToString(),
                        "TOT_Sustain_DamageDesc".Translate(),
                        4000);

                    yield return new StatDrawEntry(StatCategoryDefOf.Weapon,
                        "TOT_Sustain_APLabel".Translate(),
                        comp_LaserData_Sustain.Props.DamageArmorPenetration.ToStringPercent(),
                        "TOT_Sustain_APDesc".Translate(),
                        3990);

                    float damageIntervalInSeconds = (float)damageTickInterval / 60f;
                    yield return new StatDrawEntry(StatCategoryDefOf.Weapon,
                        "TOT_Sustain_IntervalLabel".Translate(),
                        damageIntervalInSeconds.ToString("0.00") + " s",
                        "TOT_Sustain_IntervalDesc".Translate(damageTickInterval, damageIntervalInSeconds.ToString("0.00")),
                        3985);
                    if (comp_LaserData_Sustain.Props.IfSecondDamage)
                    {
                        yield return new StatDrawEntry(StatCategoryDefOf.Weapon,
                            "TOT_Sustain_SecDamageLabel".Translate(),
                            "TOT_Sustain_ValueWithLabel".Translate(comp_LaserData_Sustain.Props.DamageNum_B, comp_LaserData_Sustain.Props.DamageDef_B.label),
                            "TOT_Sustain_SecDamageDesc".Translate(),
                            3980);

                        yield return new StatDrawEntry(StatCategoryDefOf.Weapon,
                            "TOT_Sustain_SecAPLabel".Translate(),
                            comp_LaserData_Sustain.Props.DamageArmorPenetration_B.ToStringPercent(),
                            "TOT_Sustain_SecAPDesc".Translate(),
                            3970);
                    }
                    if (comp_LaserData_Sustain.Props.IfCanScatter)
                    {
                        yield return new StatDrawEntry(StatCategoryDefOf.Weapon,
                            "TOT_Sustain_ScatterLabel".Translate(),
                            "TOT_Sustain_Yes".Translate(),
                            "TOT_Sustain_ScatterDesc".Translate(),
                            3960);

                        yield return new StatDrawEntry(StatCategoryDefOf.Weapon,
                            "TOT_Sustain_ScatterCountLabel".Translate(),
                            comp_LaserData_Sustain.Props.ScatterNum.ToString(),
                            "TOT_Sustain_ScatterCountDesc".Translate(),
                            3950);

                        yield return new StatDrawEntry(StatCategoryDefOf.Weapon,
                            "TOT_Sustain_ScatterExplosionLabel".Translate(),
                            "TOT_Sustain_ValueWithLabel".Translate(comp_LaserData_Sustain.Props.ScatterExplosionDamage, comp_LaserData_Sustain.Props.ScatterExplosionDef.label),
                            "TOT_Sustain_ScatterExplosionDesc".Translate(),
                            3940);

                        yield return new StatDrawEntry(StatCategoryDefOf.Weapon,
                            "TOT_Sustain_ScatterRadiusLabel".Translate(),
                            comp_LaserData_Sustain.Props.ScatterExplosionRadius.ToString("0.0"),
                            "TOT_Sustain_ScatterRadiusDesc".Translate(),
                            3930);
                    }
                }
            }
        }
        public virtual CMCTurretTop TurretTop
        {
            get
            {
                return this.turrettop;
            }
        }
        public void MakeGun()
        {
            if (this.def.building.turretGunDef == null)
            {
                Log.Error($"Building {this.def.defName} has null turretGunDef.");
                return;
            }
            this.gun = ThingMaker.MakeThing(this.def.building.turretGunDef, null);
            this.UpdateGunVerbs();
        }
        protected virtual void BurstComplete()
        {
            this.burstCooldownTicksLeft = this.BurstCooldownTime().SecondsToTicks();
        }
        public float BurstCooldownTime()
        {
            bool flag = this.def.building.turretBurstCooldownTime >= 0f;
            float result;
            if (flag)
            {
                result = this.def.building.turretBurstCooldownTime;
            }
            else if (this.AttackVerb?.verbProps != null)
            {
                result = this.AttackVerb.verbProps.defaultCooldownTime;
            }
            else
            {
                Log.WarningOnce($"Could not get defaultCooldownTime for {this.LabelCap}, AttackVerb or verbProps is null.", this.thingIDNumber ^ 741122);
                result = 1f;
            }
            return result;
        }
        public override AcceptanceReport ClaimableBy(Faction by)
        {
            AcceptanceReport result = base.ClaimableBy(by);
            if (!result.Accepted) return result;
            if (this.mannableComp != null && this.mannableComp.ManningPawn != null) return false;
            if (this.Active && this.mannableComp == null) return false;
            if (((this.dormantComp != null && !this.dormantComp.Awake) || (this.initiatableComp != null && !this.initiatableComp.Initiated)) && (this.powerComp == null || this.powerComp.PowerOn)) return false;
            return true;
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            base.DeSpawn(mode);
            this.ResetCurrentTarget();
            this.progressBarEffecter?.Cleanup();
        }
        public override void DrawExtraSelectionOverlays()
        {
            base.DrawExtraSelectionOverlays();
            if (!this.IsTargrtingWorld)
            {
                float num = this.AttackVerb.verbProps.EffectiveMinRange(true);
                float numMax = this.AttackVerb.verbProps.range;
                Material material = RangeMat;
                if(!this.Active || this.holdFire)
                {
                    material.color = Color.red;
                }
                else
                {
                    material.color = Color.white;
                }
                if (num > 0.1f)
                {
                    GenDraw.DrawCircleOutline(DrawPos, num, material);
                    GenDraw.DrawCircleOutline(DrawPos, num - 0.1f, material);
                    GenDraw.DrawCircleOutline(DrawPos, num - 0.2f, material);
                }
                if (numMax > 0.1f)
                {
                    GenDraw.DrawCircleOutline(DrawPos, numMax, material);
                    GenDraw.DrawCircleOutline(DrawPos, numMax - 0.1f, material);
                    GenDraw.DrawCircleOutline(DrawPos, numMax - 0.2f, material);
                }
                if (this.forcedTarget.IsValid && (!this.forcedTarget.HasThing || this.forcedTarget.Thing.Spawned))
                {
                    bool hasThing = this.forcedTarget.HasThing;
                    Vector3 vector;
                    if (hasThing)
                    {
                        vector = this.forcedTarget.Thing.TrueCenter();
                    }
                    else
                    {
                        vector = this.forcedTarget.Cell.ToVector3Shifted();
                    }
                    Vector3 a = this.TrueCenter();
                    vector.y = AltitudeLayer.MetaOverlays.AltitudeFor();
                    a.y = vector.y;
                    GenDraw.DrawLineBetween(a, vector, Building_TurretGun.ForcedTargetLineMat, 0.2f);
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.rotationVelocity, "rotationVelocity", 1f, false);
            Scribe_Values.Look(ref this.burstCooldownTicksLeft, "burstCooldownTicksLeft", 0, false);
            Scribe_Values.Look(ref this.burstWarmupTicksLeft, "burstWarmupTicksLeft", 0, false);
            Scribe_TargetInfo.Look(ref this.currentTargetInt, "currentTarget");
            Scribe_Values.Look(ref this.holdFire, "holdFire", false, false);
            Scribe_Values.Look(ref this.SleepMode, "sleepMode", false, false);
            Scribe_Values.Look(ref this.burstActivated, "burstActivated", false, false);
            Scribe_Deep.Look(ref this.gun, "gun");
            Scribe_Values.Look<float>(ref this.turrettop.curRotationInt, "curRotationInt", 0f, false);
            Scribe_Values.Look<float>(ref this.turrettop.destRotationInt, "destRotationInt", 0f, false);
            BackCompatibility.PostExposeData(this);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                bool flag2 = this.gun == null;
                if (flag2)
                {
                    Log.Error("Turret had null gun after loading. Recreating.");
                    this.MakeGun();
                }
                else
                {
                    this.UpdateGunVerbs();
                }
            }
        }
        public void ExtractShell()
        {
            CompChangeableProjectile comp = this.gun?.TryGetComp<CompChangeableProjectile>();
            if (comp != null)
            {
                GenPlace.TryPlaceThing(comp.RemoveShell(), base.Position, base.Map, ThingPlaceMode.Near);
            }
        }
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos()) yield return gizmo;
            CompChangeableProjectile compChangeableProjectile = this.gun?.TryGetComp<CompChangeableProjectile>();
            if (this.CanExtractShell && compChangeableProjectile != null)
            {
                yield return new Command_Action
                {
                    defaultLabel = "CommandExtractShell".Translate(),
                    defaultDesc = "CommandExtractShellDesc".Translate(),
                    icon = compChangeableProjectile.LoadedShell.uiIcon,
                    iconAngle = compChangeableProjectile.LoadedShell.uiIconAngle,
                    iconOffset = compChangeableProjectile.LoadedShell.uiIconOffset,
                    iconDrawScale = GenUI.IconDrawScale(compChangeableProjectile.LoadedShell),
                    action = delegate { this.ExtractShell(); }
                };
            }
            if (compChangeableProjectile != null)
            {
                StorageSettings storeSettings = compChangeableProjectile.GetStoreSettings();
                foreach (Gizmo gizmo2 in StorageSettingsClipboard.CopyPasteGizmosFor(storeSettings)) yield return gizmo2;
            }
            if (!this.HideForceTargetGizmo)
            {
                if (this.CanSetForcedTarget)
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
                    if (base.Spawned && this.IsMortarOrProjectileFliesOverhead && base.Position.Roofed(base.Map))
                    {
                        command_VerbTarget.Disable("CannotFire".Translate() + ": " + "Roofed".Translate().CapitalizeFirst());
                    }
                    yield return command_VerbTarget;
                }
                if (this.forcedTarget.IsValid)
                {
                    Command_Action command_Action = new Command_Action
                    {
                        defaultLabel = "CommandStopForceAttack".Translate(),
                        defaultDesc = "CommandStopForceAttackDesc".Translate(),
                        icon = ContentFinder<Texture2D>.Get("UI/Commands/Halt", true),
                        action = delegate
                        {
                            this.ResetForcedTarget();
                            SoundDefOf.Tick_Low.PlayOneShotOnCamera(null);
                        }
                    };
                    if (!this.forcedTarget.IsValid) command_Action.Disable("CommandStopAttackFailNotForceAttacking".Translate());
                    command_Action.hotKey = KeyBindingDefOf.Misc5;
                    yield return command_Action;
                }
            }
            if (this.CanToggleHoldFire)
            {
                yield return new Command_Toggle
                {
                    defaultLabel = "CommandHoldFire".Translate(),
                    defaultDesc = "CommandHoldFireDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/HoldFire", true),
                    hotKey = KeyBindingDefOf.Misc6,
                    toggleAction = delegate
                    {
                        this.holdFire = !this.holdFire;
                        if (this.holdFire) this.ResetForcedTarget();
                    },
                    isActive = (() => this.holdFire)
                };
            }
            yield return new Command_Toggle
            {
                defaultLabel = "CommandSleepMode".Translate(),
                defaultDesc = "CommandSleepModeDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/UI_ShutDown", true),
                hotKey = KeyBindingDefOf.Misc7,
                toggleAction = delegate
                {
                    if (!this.SleepMode)
                        this.ToSleepMode();
                    else
                        this.WakeUp();
                    this.ResetForcedTarget();
                },
                isActive = (() => this.SleepMode)
            };
        }
        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            string inspectString = base.GetInspectString();
            if (!inspectString.NullOrEmpty()) stringBuilder.AppendLine(inspectString);

            Verb attackVerb = this.AttackVerb;
            VerbProperties props = attackVerb?.verbProps;

            if (props != null && props.minRange > 0f)
            {
                stringBuilder.AppendLine("MinimumRange".Translate() + ": " + props.minRange.ToString("F0"));
            }
            if (base.Spawned && this.IsMortarOrProjectileFliesOverhead && base.Position.Roofed(base.Map))
            {
                stringBuilder.AppendLine("CannotFire".Translate() + ": " + "Roofed".Translate().CapitalizeFirst());
            }
            else if (base.Spawned && this.burstCooldownTicksLeft > 0 && this.BurstCooldownTime() > 5f)
            {
                stringBuilder.AppendLine("CanFireIn".Translate() + ": " + this.burstCooldownTicksLeft.ToStringSecondsFromTicks());
            }

            CompChangeableProjectile compChangeableProjectile = this.gun?.TryGetComp<CompChangeableProjectile>();
            if (compChangeableProjectile != null)
            {
                stringBuilder.AppendLine(compChangeableProjectile.Loaded ? "ShellLoaded".Translate(compChangeableProjectile.LoadedShell.LabelCap, compChangeableProjectile.LoadedShell) : "ShellNotLoaded".Translate());
            }

            CompAccessoryHolder holder = this.TryGetComp<CompAccessoryHolder>();
            if (holder != null && holder.InstalledAccessories.Any())
            {
                stringBuilder.AppendLine("CMC_InstalledAccessory".Translate(holder.InstalledAccessories.Count, holder.Props.maxAccessories));
                foreach (Thing acc in holder.InstalledAccessories)
                {
                    stringBuilder.Append('[' + acc.LabelCap + ']');
                }
                stringBuilder.AppendLine();
            }
            return stringBuilder.ToString().TrimEndNewlines();
        }
        public virtual bool IsValidTarget(Thing t)
        {
            Pawn pawn = t as Pawn;
            if (pawn != null)
            {
                if (base.Faction == Faction.OfPlayer && pawn.IsPrisoner) return false;
                if (this.AttackVerb.ProjectileFliesOverhead())
                {
                    RoofDef roofDef = base.Map.roofGrid.RoofAt(t.Position);
                    if (roofDef != null && roofDef.isThickRoof) return false;
                }
                if (this.mannableComp == null) return !GenAI.MachinesLike(base.Faction, pawn);
                if (pawn.RaceProps.Animal && pawn.Faction == Faction.OfPlayer) return false;
            }
            return true;
        }
        public override void OrderAttack(LocalTargetInfo targ)
        {
            if (!targ.IsValid)
            {
                if (this.forcedTarget.IsValid) this.ResetForcedTarget();
                return;
            }
            Verb attackVerb = this.AttackVerb;
            if (attackVerb?.verbProps == null) return;

            if ((targ.Cell - base.Position).LengthHorizontal < attackVerb.verbProps.EffectiveMinRange(targ, this))
            {
                Messages.Message("MessageTargetBelowMinimumRange".Translate(), this, MessageTypeDefOf.RejectInput, false);
            }
            else if ((targ.Cell - base.Position).LengthHorizontal > attackVerb.verbProps.range)
            {
                Messages.Message("MessageTargetBeyondMaximumRange".Translate(), this, MessageTypeDefOf.RejectInput, false);
            }
            else
            {
                if (this.forcedTarget != targ)
                {
                    this.forcedTarget = targ;
                    if (this.burstCooldownTicksLeft <= 0) this.TryStartShootSomething(false);
                }
                if (this.holdFire)
                {
                    Messages.Message("MessageTurretWontFireBecauseHoldFire".Translate(this.def.label), this, MessageTypeDefOf.RejectInput, false);
                }
            }
        }
        public override void PostMake()
        {
            base.PostMake();
            this.MakeGun();
            if (this.def.building.turretInitialCooldownTime > 0f)
            {
                this.burstCooldownTicksLeft = this.def.building.turretInitialCooldownTime.SecondsToTicks();
            }
        }
        public void ResetCurrentTarget()
        {
            this.currentTargetInt = LocalTargetInfo.Invalid;
            this.burstWarmupTicksLeft = 0;
        }
        public void ResetForcedTarget()
        {
            this.forcedTarget = LocalTargetInfo.Invalid;
            this.currentTargetInt = LocalTargetInfo.Invalid;
            this.burstWarmupTicksLeft = 0;
            if (this.burstCooldownTicksLeft <= 0)
            {
                this.TryStartShootSomething(false);
            }
        }
        public IAttackTargetSearcher TargSearcher()
        {
            return (this.mannableComp != null && this.mannableComp.MannedNow) ? (IAttackTargetSearcher)this.mannableComp.ManningPawn : this;
        }
        public int ScanInterval
        {
            get
            {
                return 60;
            }
        }
        protected override void Tick()
        {
            bool doScanThisTick = this.IsHashIntervalTick(this.ScanInterval);
            base.Tick();
            if (this.CanExtractShell && this.MannedByColonist)
            {
                CompChangeableProjectile compChangeableProjectile = this.gun?.TryGetComp<CompChangeableProjectile>();
                if (compChangeableProjectile != null &&
                    !compChangeableProjectile.allowedShellsSettings.AllowedToAccept(compChangeableProjectile.LoadedShell))
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

            if (this.forcedTarget.ThingDestroyed && doScanThisTick)
            {
                this.ResetForcedTarget();
            }

            if (this.Active && (this.mannableComp == null || this.mannableComp.MannedNow) && !this.IsStunned && this.Spawned)
            {
                CompEquippable eq = this.GunCompEq;
                Verb attackVerb = this.AttackVerb;
                if (eq == null || attackVerb == null)
                {
                    if (doScanThisTick) this.ResetCurrentTarget();
                    return;
                }

                eq.verbTracker.VerbsTick();
                if (attackVerb.state == VerbState.Bursting && attackVerb.CurrentTarget.IsValid)
                {
                    this.currentTargetInt = attackVerb.CurrentTarget;
                }

                if (attackVerb.state != VerbState.Bursting)
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
                        }

                        if (this.burstCooldownTicksLeft <= 0 && doScanThisTick)
                        {
                            this.TryStartShootSomething(true);
                        }
                    }
                }
                this.turrettop.TurretTopTick();
            }
            else
            {
                if (doScanThisTick)
                {
                    this.ResetCurrentTarget();
                }
            }
        }
        public void TryActivateBurst()
        {
            this.burstActivated = true;
            this.TryStartShootSomething(true);
        }
        public virtual LocalTargetInfo TryFindNewTarget()
        {
            IAttackTargetSearcher searcher = this.TargSearcher();
            Verb verb = this.AttackVerb;
            if (searcher == null || verb?.verbProps == null || this.Map == null)
                return LocalTargetInfo.Invalid;

            bool needLOS = NeedLineOfSightForTargeting(verb);

            List<IAttackTarget> candidates = this.Map.attackTargetsCache.GetPotentialTargetsFor(searcher);
            Thing bestThing = null;
            float bestScore = float.MinValue;

            for (int i = 0; i < candidates.Count; i++)
            {
                IAttackTarget at = candidates[i];
                if (at == null || at == searcher) continue;
                if (at.ThreatDisabled(searcher)) continue;
                if (!AttackTargetFinder.IsAutoTargetable(at)) continue;

                Thing t = at.Thing;
                if (!CanTargetNow(t, verb, needLOS)) continue;

                float score = ScoreTarget(at, t, verb);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestThing = t;
                }
            }

            if (bestThing == null)
                return LocalTargetInfo.Invalid;
            if (this.currentTargetInt.IsValid && this.currentTargetInt.HasThing)
            {
                Thing cur = this.currentTargetInt.Thing;
                if (cur != null && !cur.DestroyedOrNull())
                {
                    IAttackTarget curAt = cur as IAttackTarget;
                    if (curAt != null && CanTargetNow(cur, verb, needLOS))
                    {
                        float curScore = ScoreTarget(curAt, cur, verb);
                        if (bestScore < curScore + SwitchThreshold)
                            return this.currentTargetInt;
                    }
                }
            }

            return new LocalTargetInfo(bestThing);
        }
        public virtual void TryStartShootSomething(bool canBeginBurstImmediately)
        {
            this.progressBarEffecter?.Cleanup();
            this.progressBarEffecter = null;
            Verb attackVerb = this.AttackVerb;
            if (!base.Spawned ||
                (this.holdFire && this.CanToggleHoldFire) ||
                (attackVerb.ProjectileFliesOverhead() && base.Map.roofGrid.Roofed(base.Position)) ||
                !attackVerb.Available())
            {
                this.ResetCurrentTarget();
                return;
            }
            if (this.forcedTarget.IsValid)
            {
                this.currentTargetInt = this.forcedTarget;
            }
            else if (IsTargetStillValid(this.currentTargetInt))
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
        public void UpdateGunVerbs()
        {
            if (this.gun == null) return;
            CompEquippable comp = this.gun.TryGetComp<CompEquippable>();
            if (comp == null) return;

            List<Verb> allVerbs = comp.AllVerbs;
            for (int i = 0; i < allVerbs.Count; i++)
            {
                Verb verb = allVerbs[i];
                verb.caster = this;
                verb.castCompleteCallback = this.BurstComplete;
            }
        }
        public virtual bool CanToggleHoldFire => this.PlayerControlled;
        public bool PlayerControlled => (base.Faction == Faction.OfPlayer || this.MannedByColonist) && !this.MannedByNonColonist && !this.IsActivable;
        public bool MannedByColonist => this.mannableComp != null && this.mannableComp.ManningPawn != null && this.mannableComp.ManningPawn.Faction == Faction.OfPlayer;
        public bool MannedByNonColonist => this.mannableComp != null && this.mannableComp.ManningPawn != null && this.mannableComp.ManningPawn.Faction != Faction.OfPlayer;
        public bool IsActivable => this.interactableComp != null;
        public override Verb AttackVerb
        {
            get
            {
                return this.gun?.TryGetComp<CompEquippable>()?.PrimaryVerb;
            }
        }
        public CompEquippable GunCompEq
        {
            get
            {
                return this.gun?.TryGetComp<CompEquippable>();
            }
        }
        public bool IsMortar => this.def.building.IsMortar;
        protected virtual bool HideForceTargetGizmo => false;
        public override LocalTargetInfo CurrentTarget => this.currentTargetInt;
        public virtual bool CanSetForcedTarget
        {
            get
            {
                if (base.Faction == Faction.OfPlayer)
                {
                    CompAffectedByFacilities facilitiesComp = this.TryGetComp<CompAffectedByFacilities>();
                    if (facilitiesComp != null)
                    {
                        List<Thing> linked = facilitiesComp.LinkedFacilitiesListForReading;
                        for (int i = 0; i < linked.Count; i++)
                        {
                            if (linked[i].TryGetComp<Comp_FCradar>() != null) return true;
                        }
                    }
                }
                return false;
            }
        }
        public bool WarmingUp => this.burstWarmupTicksLeft > 0;
        private bool IsTargetStillValid(LocalTargetInfo t)
        {
            if (!t.IsValid) return false;

            if (t.HasThing)
            {
                Thing thing = t.Thing;
                if (thing.DestroyedOrNull()) return false;
                if (!thing.Spawned || thing.Map != this.Map) return false;
                if (thing is Pawn p && (p.Dead || p.Downed)) return false;
            }

            Verb verb = this.AttackVerb;
            if (verb?.verbProps == null) return false;

            float range = verb.verbProps.range;
            float distSq = (t.Cell - this.Position).LengthHorizontalSquared;
            if (distSq > range * range) return false;

            float minRange = verb.verbProps.EffectiveMinRange(t, this);
            if (distSq < minRange * minRange) return false;
            if (NeedLineOfSightForTargeting(verb))
            {
                if (!GenSight.LineOfSight(this.Position, t.Cell, this.Map, true))
                    return false;
            }

            return true;
        }
        public bool CanExtractShell
        {
            get
            {
                if (!this.PlayerControlled) return false;
                CompChangeableProjectile comp = this.gun?.TryGetComp<CompChangeableProjectile>();
                return comp != null && comp.Loaded;
            }
        }
        public void ToSleepMode()
        {
            if(this.SleepMode)
            {
                return;
            }
            else
            {
                this.BurstComplete();
                this.SleepMode = true;
                if(this.powerComp == null)
                    this.powerComp.SetUpPowerVars();
            }
        }
        public void WakeUp()
        {
            if (!this.SleepMode)
            {
                return;
            }
            else
            {
                this.BurstComplete();
                this.SleepMode = false;
                if (this.powerComp == null)
                    this.powerComp.ResetPowerVars();
            }
        }
        public virtual bool Active => !SleepMode && (this.powerComp == null || this.powerComp.PowerOn) && (this.initiatableComp == null || this.initiatableComp.Initiated) && (this.interactableComp == null || this.burstActivated) && (this.powerCellComp == null || !this.powerCellComp.depleted) && (this.refuelableComp == null || this.refuelableComp.HasFuel);
        public bool IsMortarOrProjectileFliesOverhead => this.AttackVerb.ProjectileFliesOverhead() || this.IsMortar;
        protected virtual void BeginBurst()
        {
            Verb attackVerb = this.AttackVerb;
            if (attackVerb == null || !this.CurrentTarget.IsValid)
                return;
            if (!this.IsTargetStillValid(this.CurrentTarget))
            {
                this.ResetCurrentTarget();
                return;
            }
            if (!this.TestForTarget(1.5f))
            {
                return;
            }
            bool started = attackVerb.TryStartCastOn(this.CurrentTarget, false, true);
            if (started)
            {
                base.OnAttackedTarget(this.CurrentTarget);
            }
        }
        private bool TestForTarget(float angleTolerance = 1.5f)
        {
            if (!this.CurrentTarget.IsValid) return false;

            float targetAngle = (this.CurrentTarget.Cell.ToVector3Shifted() - this.DrawPos).AngleFlat();
            float delta = Mathf.Abs(Mathf.DeltaAngle(this.turrettop.CurRotation, targetAngle));
            return delta <= angleTolerance;
        }
        private bool CanTargetNow(Thing t, Verb verb, bool needLOS)
        {
            if (t == null || t.DestroyedOrNull() || !t.Spawned || t.Map != this.Map) return false;
            if (!this.HostileTo(t)) return false;
            if (!this.IsValidTarget(t)) return false;

            float distSq = (t.Position - this.Position).LengthHorizontalSquared;
            float maxRange = verb.verbProps.range;
            float minRange = verb.verbProps.EffectiveMinRange(t, this);

            if (distSq > maxRange * maxRange) return false;
            if (distSq < minRange * minRange) return false;
            if (needLOS && !GenSight.LineOfSight(this.Position, t.Position, this.Map, true))
                return false;

            return true;
        }
        private float ScoreTarget(IAttackTarget at, Thing t, Verb verb)
        {
            float dist = (t.Position - this.Position).LengthHorizontal;
            float score = 60f - Mathf.Min(dist, 40f);

            if (at.TargetCurrentlyAimingAt == this) score += 10f;

            if (this.LastAttackedTarget.IsValid &&
                this.LastAttackedTarget.Thing == t &&
                Find.TickManager.TicksGame - this.LastAttackTargetTick <= 300)
            {
                score += 40f;
            }
            score -= CoverUtility.CalculateOverallBlockChance(t.Position, this.Position, this.Map) * 10f;
            if (t is Pawn p)
            {
                if (!p.IsCombatant()) score -= 50f;
                else if (p.DevelopmentalStage.Juvenile()) score -= 25f;
                if (p.Downed) score -= 50f;

                if (verb.verbProps.ai_TargetHasRangedAttackScoreOffset != 0f &&
                    p.CurrentEffectiveVerb != null &&
                    p.CurrentEffectiveVerb.verbProps.Ranged)
                {
                    score += verb.verbProps.ai_TargetHasRangedAttackScoreOffset;
                }
            }
            float targetAngle = (t.TrueCenter() - this.DrawPos).AngleFlat();
            float delta = Mathf.Abs(Mathf.DeltaAngle(this.turrettop.CurRotation, targetAngle));
            score -= delta * AnglePenaltyPerDegree;
            score *= at.TargetPriorityFactor;
            if (this.currentTargetInt.IsValid && this.currentTargetInt.Thing == t)
                score += KeepCurrentTargetBonus;

            return score;
        }
        private bool NeedLineOfSightForTargeting(Verb verb)
        {
            return verb?.verbProps?.requireLineOfSight ?? true;
        }
        private const float AnglePenaltyPerDegree = 0.30f;   
        private const float KeepCurrentTargetBonus = 10f;
        private const float SwitchThreshold = 12f;
        public static readonly AccessTools.FieldRef<Projectile, Vector3> GetProjectileDestination = AccessTools.FieldRefAccess<Projectile, Vector3>("destination");
    }
}