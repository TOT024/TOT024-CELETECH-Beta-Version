using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TOT_DLL_test
{
    [StaticConstructorOnStartup]
    public class Comp_UAV : ThingComp, IAttackTargetSearcher
    {
        public CompProperties_UAV Props
        {
            get
            {
                return (CompProperties_UAV)this.props;
            }
        }
        public bool IsApparel
        {
            get
            {
                return this.parent is Apparel;
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

        //weapon system
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
        public CompEquippable GunCompEq
        {
            get
            {
                return this.gun.TryGetComp<CompEquippable>();
            }
        }
        public Verb AttackVerb
        {
            get
            {
                return this.GunCompEq.PrimaryVerb;
            }
        }
        private bool WarmingUp
        {
            get
            {
                return this.burstWarmupTicksLeft > 0;
            }
        }
        public bool CanShoot
        {
            get
            {
                if(PawnOwner == null)
                {
                    return false;
                }
                else
                {
                    if(!this.fireAtWill)
                    {
                        return false;
                    }
                    if(this.CompApparelReloadable!=null && this.CompApparelReloadable.RemainingCharges<1)
                    {
                        return false;
                    }
                    if ((PawnOwner.Faction.IsPlayer && PawnOwner.Drafted) || (!PawnOwner.Faction.IsPlayer && !PawnOwner.DeadOrDowned))
                    {
                        return true;
                    }
                    return false;
                }
            }
        }
        public bool AutoAttack
        {
            get
            {
                return this.Props.autoAttack;
            }
        }
        public override void Notify_Equipped(Pawn pawn)
        {
            base.PostPostMake();
            bool isApparel = this.IsApparel;
            if (isApparel)
            {
                this.MakeGun();
            }
            CompApparelReloadable = this.parent.TryGetComp<CompApparelReloadable>();
        }
        public override void PostPostMake()
        {
            base.PostPostMake();
        }

        private void MakeGun()
        {
            this.gun = ThingMaker.MakeThing(this.Props.turretDef, null);
            this.UpdateGunVerbs();
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
        public override void CompTick()
        {
            base.CompTick();
            PosX = Mathf.PingPong(((float)Find.TickManager.TicksGame + rand) * this.Props.BobSpeed, this.Props.BobDistance) * this.Props.Xoffset;
            PosY = Mathf.Sin(((float)Find.TickManager.TicksGame + rand) * (0.006f)) * this.Props.Yoffset;
            if (this.CanShoot)
            {
                bool isValid = this.currentTarget.IsValid;
                if (isValid)
                {
                    this.curRotation = (this.currentTarget.Cell.ToVector3Shifted() - this.PawnOwner.DrawPos).AngleFlat() + this.Props.angleOffset;
                }
                this.AttackVerb.VerbTick();
                if (this.AttackVerb.state != VerbState.Bursting)
                {
                    bool warmingUp = this.WarmingUp;
                    if (warmingUp)
                    {
                        this.burstWarmupTicksLeft--;
                        bool flag5 = this.burstWarmupTicksLeft == 0;
                        if (flag5)
                        {
                            bool launched;
                            launched = this.AttackVerb.TryStartCastOn(this.currentTarget, false, true, false, true);
                            this.lastAttackTargetTick = Find.TickManager.TicksGame;
                            this.lastAttackedTarget = this.currentTarget;
                            if(this.CompApparelReloadable!=null && launched == true)
                            {
                                this.CompApparelReloadable.UsedOnce();
                            }
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
                            this.currentTarget = (Thing)AttackTargetFinder.BestShootTargetFromCurrentPosition(this, TargetScanFlags.NeedThreat | TargetScanFlags.NeedAutoTargetable, null, 0f, 9999f);
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
        private void ResetCurrentTarget()
        {
            this.currentTarget = LocalTargetInfo.Invalid;
            this.burstWarmupTicksLeft = 0;
        }
        public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
        {
            foreach (Gizmo item in base.CompGetWornGizmosExtra())
            {
                yield return item;
            }
            bool isApparel = this.IsApparel;
            if (isApparel)
            {
                foreach (Gizmo gizmo in this.GetGizmos())
                {
                    yield return gizmo;
                }
            }
            yield break;
        }
        private IEnumerable<Gizmo> GetGizmos()
        {
            bool flag = this.PawnOwner.Faction == Faction.OfPlayer && (this.PawnOwner.Drafted);
            if (flag)
            {
                Command_Toggle command_Toggle = new Command_Toggle();
                command_Toggle.defaultLabel = "CommandToggleTurret".Translate();
                command_Toggle.defaultDesc = "CommandToggleTurretDesc".Translate();
                command_Toggle.isActive = (() => this.fireAtWill);
                command_Toggle.icon = Comp_UAV.ToggleTurretIcon.Texture;
                command_Toggle.toggleAction = delegate ()
                {
                    this.fireAtWill = !this.fireAtWill;
                    this.PawnOwner.Drawer.renderer.renderTree.SetDirty();
                };
                yield return command_Toggle;
                command_Toggle = null;
            }
            yield break;
        }
        public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
        {
            if (this.Props.turretDef != null)
            {
                yield return new StatDrawEntry(StatCategoryDefOf.PawnCombat, "Turret".Translate(), this.Props.turretDef.LabelCap, "Stat_Thing_TurretDesc".Translate(), 5600, null, Gen.YieldSingle<Dialog_InfoCard.Hyperlink>(new Dialog_InfoCard.Hyperlink(this.Props.turretDef, -1)), false, false);
            }
            yield break;
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<int>(ref this.burstCooldownTicksLeft, "burstCooldownTicksLeft", 0, false);
            Scribe_Values.Look<int>(ref this.burstWarmupTicksLeft, "burstWarmupTicksLeft", 0, false);
            Scribe_TargetInfo.Look(ref this.currentTarget, "currentTarget");
            Scribe_Deep.Look<Thing>(ref this.gun, "gun", Array.Empty<object>());
            Scribe_Values.Look<bool>(ref this.fireAtWill, "fireAtWill", true, false);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (this.gun == null)
                {
                    Log.Error("CompTurrentGun had null gun after loading. Recreating.");
                    this.MakeGun();
                    return;
                }
                this.UpdateGunVerbs();
            }
        }
        //ammo
        public Thing ReloadableThing
        {
            get
            {
                return this.parent;
            }
        }
        public int BaseReloadTicks
        {
            get
            {
                return 10;
            }
        }
        private const int StartShootIntervalTicks = 10;
        private static readonly CachedTexture ToggleTurretIcon = new CachedTexture("UI/UI_TargetRange");
        public Thing gun;
        public float curRotation;
        protected int burstCooldownTicksLeft;
        protected int burstWarmupTicksLeft;
        public LocalTargetInfo currentTarget = LocalTargetInfo.Invalid;
        private bool fireAtWill = true;
        private LocalTargetInfo lastAttackedTarget = LocalTargetInfo.Invalid;
        private int lastAttackTargetTick;
        public Material Mat;
        public Material Mat2;
        public int LastAttackTick = 0;
        public GraphicData graphicData;
        public float PosX;
        public float PosY;
        public float rand = Rand.Range(0f, 200f);
        public CompApparelReloadable CompApparelReloadable;
    }
}
