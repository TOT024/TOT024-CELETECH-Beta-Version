using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;

namespace TOT_DLL_test
{
    public class CompProperties_CMCShield : CompProperties
    {
        public CompProperties_CMCShield()
        {
            this.compClass = typeof(Comp_CMCShield);
        }
        public int startingTicksToReset = 1200;
        public float minDrawSize = 1.8f;
        public float maxDrawSize = 1.85f;
        public float energyLossPerDamage = 0.067f;
        public float energyOnReset = 0.2f;
        public bool blocksRangedWeapons = false;
        public string baseBubbleTexPath = "Things/CMC_ShieldBubble_Base";
        public string shieldMainTexPath = "Things/CMC_ShieldBubble";
        public Color colorA = new Color(0.1f, 0.95f, 0.95f, 0.25f);
        public Color colorB = new Color(0.75f, 0.77f, 0.73f, 0.33f);

        public float intensity = 1.75f;
        public float currentWidth = 0.12f;
        public float currentDensity = 2.8f;
        public float noiseScale = 3.2f;
        public float cutoff = 0.24f;
        public float softness = 0.045f;
        public float baseTexStrength = 0.33f;
        public float baseAlpha = 0.24f;
        public float glowIntensity = 1.1f;
        public float glowRange = 0.12f;
        public bool addBreakHediffOnApparel = true;
        public string breakHediffDefName = "CMC_DMGAbsorb";
        public int breakHediffDurationTicks = 180;
    }

    [StaticConstructorOnStartup]
    public class Comp_CMCShield : ThingComp
    {
        protected float energy;
        protected int ticksToReset = -1;
        protected int lastKeepDisplayTick = -9999;

        private Vector3 impactAngleVect;
        private int lastAbsorbDamageTick = -9999;

        private const int KeepDisplayingTicks = 1000;
        private const float ApparelScorePerEnergyMax = 0.25f;

        private Material cachedShieldMaterial;
        private Material cachedBaseBubbleMaterial;

        private static readonly int MainTexID = Shader.PropertyToID("_MainTex");
        private static readonly int ColorAID = Shader.PropertyToID("_ColorA");
        private static readonly int ColorBID = Shader.PropertyToID("_ColorB");
        private static readonly int IntensityID = Shader.PropertyToID("_Intensity");
        private static readonly int CurrentWidthID = Shader.PropertyToID("_CurrentWidth");
        private static readonly int CurrentDensityID = Shader.PropertyToID("_CurrentDensity");
        private static readonly int NoiseScaleID = Shader.PropertyToID("_NoiseScale");
        private static readonly int CutoffID = Shader.PropertyToID("_Cutoff");
        private static readonly int SoftnessID = Shader.PropertyToID("_Softness");
        private static readonly int BaseTexStrengthID = Shader.PropertyToID("_BaseTexStrength");
        private static readonly int BaseAlphaID = Shader.PropertyToID("_BaseAlpha");
        private static readonly int GlowIntensityID = Shader.PropertyToID("_GlowIntensity");
        private static readonly int GlowRangeID = Shader.PropertyToID("_GlowRange");

        public CompProperties_CMCShield Props
        {
            get { return (CompProperties_CMCShield)this.props; }
        }

        private float EnergyMax
        {
            get { return this.parent.GetStatValue(StatDefOf.EnergyShieldEnergyMax, true, -1); }
        }

        private float EnergyGainPerTick
        {
            get { return this.parent.GetStatValue(StatDefOf.EnergyShieldRechargeRate, true, -1) / 60f; }
        }

        public float Energy
        {
            get { return this.energy; }
        }

        public ShieldState ShieldState
        {
            get
            {
                // 原版逻辑：pawn 处于充电/自关机时禁用
                Pawn p = this.parent as Pawn;
                if (p != null && (p.IsCharging() || p.IsSelfShutdown()))
                {
                    return ShieldState.Disabled;
                }

                // 原版逻辑：Dormant 时禁用
                CompCanBeDormant compDormant = this.parent.GetComp<CompCanBeDormant>();
                if (compDormant != null && !compDormant.Awake)
                {
                    return ShieldState.Disabled;
                }

                if (this.ticksToReset <= 0)
                {
                    return ShieldState.Active;
                }
                return ShieldState.Resetting;
            }
        }

        protected bool ShouldDisplay
        {
            get
            {
                Pawn pawnOwner = this.PawnOwner;
                if (pawnOwner == null)
                {
                    return false;
                }
                if (!pawnOwner.Spawned || pawnOwner.Dead || pawnOwner.Downed)
                {
                    return false;
                }
                if (pawnOwner.InAggroMentalState)
                {
                    return true;
                }
                if (pawnOwner.Drafted)
                {
                    return true;
                }
                if (pawnOwner.Faction != null && pawnOwner.Faction.HostileTo(Faction.OfPlayer) && !pawnOwner.IsPrisoner)
                {
                    return true;
                }
                if (Find.TickManager.TicksGame < this.lastKeepDisplayTick + KeepDisplayingTicks)
                {
                    return true;
                }
                if (ModsConfig.BiotechActive && pawnOwner.IsColonyMech && Find.Selector.SingleSelectedThing == pawnOwner)
                {
                    return true;
                }
                return false;
            }
        }

        protected Pawn PawnOwner
        {
            get
            {
                Apparel apparel = this.parent as Apparel;
                if (apparel != null)
                {
                    return apparel.Wearer;
                }
                Pawn pawn = this.parent as Pawn;
                if (pawn != null)
                {
                    return pawn;
                }
                return null;
            }
        }

        public bool IsApparel
        {
            get { return this.parent is Apparel; }
        }

        private bool IsBuiltIn
        {
            get { return !this.IsApparel; }
        }

        private Material BaseBubbleMat
        {
            get
            {
                if (this.cachedBaseBubbleMaterial == null)
                {
                    this.cachedBaseBubbleMaterial = MaterialPool.MatFrom(this.Props.baseBubbleTexPath, ShaderDatabase.TransparentPostLight);
                }
                return this.cachedBaseBubbleMaterial;
            }
        }

        public Material GetMaterial_Shield
        {
            get
            {
                if (this.cachedShieldMaterial != null)
                {
                    return this.cachedShieldMaterial;
                }
                if (ShaderLoader.PulseEffectShader == null)
                {
                    return null;
                }

                Material mat = new Material(ShaderLoader.PulseEffectShader);
                mat.SetTexture(MainTexID, ContentFinder<Texture2D>.Get(this.Props.shieldMainTexPath, true));
                mat.mainTextureScale = Vector2.one;

                mat.SetColor(ColorAID, this.Props.colorA);
                mat.SetColor(ColorBID, this.Props.colorB);

                mat.SetFloat(IntensityID, this.Props.intensity);
                mat.SetFloat(CurrentWidthID, this.Props.currentWidth);
                mat.SetFloat(CurrentDensityID, this.Props.currentDensity);
                mat.SetFloat(NoiseScaleID, this.Props.noiseScale);
                mat.SetFloat(CutoffID, this.Props.cutoff);
                mat.SetFloat(SoftnessID, this.Props.softness);
                mat.SetFloat(BaseTexStrengthID, this.Props.baseTexStrength);
                mat.SetFloat(BaseAlphaID, this.Props.baseAlpha);
                mat.SetFloat(GlowIntensityID, this.Props.glowIntensity);
                mat.SetFloat(GlowRangeID, this.Props.glowRange);

                this.cachedShieldMaterial = mat;
                return this.cachedShieldMaterial;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref this.energy, "energy", 0f);
            Scribe_Values.Look(ref this.ticksToReset, "ticksToReset", -1);
            Scribe_Values.Look(ref this.lastKeepDisplayTick, "lastKeepDisplayTick", -9999);
        }

        public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetWornGizmosExtra())
            {
                yield return gizmo;
            }

            if (this.IsApparel)
            {
                foreach (Gizmo gizmo2 in this.GetGizmos())
                {
                    yield return gizmo2;
                }
            }

            if (DebugSettings.ShowDevGizmos)
            {
                yield return new Command_Action
                {
                    defaultLabel = "DEV: Break",
                    action = new Action(this.Break)
                };

                if (this.ticksToReset > 0)
                {
                    yield return new Command_Action
                    {
                        defaultLabel = "DEV: Clear reset",
                        action = delegate { this.ticksToReset = 0; }
                    };
                }
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }

            if (this.IsBuiltIn)
            {
                foreach (Gizmo gizmo2 in this.GetGizmos())
                {
                    yield return gizmo2;
                }
            }
        }

        private IEnumerable<Gizmo> GetGizmos()
        {
            Pawn pawnOwner = this.PawnOwner;
            if (pawnOwner == null)
            {
                yield break;
            }

            Pawn pawn = this.parent as Pawn;
            if ((pawnOwner.Faction == Faction.OfPlayer || (pawn != null && pawn.RaceProps.IsMechanoid))
                && Find.Selector.SingleSelectedThing == pawnOwner)
            {
                yield return new Gizmo_EnergyShieldStatus
                {
                    shield = this
                };
            }
        }

        public override float CompGetSpecialApparelScoreOffset()
        {
            return this.EnergyMax * ApparelScorePerEnergyMax;
        }

        public override void CompTick()
        {
            base.CompTick();

            if (this.PawnOwner == null)
            {
                this.energy = 0f;
                return;
            }

            if (this.ShieldState == ShieldState.Resetting)
            {
                this.ticksToReset--;
                if (this.ticksToReset <= 0)
                {
                    this.Reset();
                    return;
                }
            }
            else if (this.ShieldState == ShieldState.Active)
            {
                this.energy += this.EnergyGainPerTick;
                if (this.energy > this.EnergyMax)
                {
                    this.energy = this.EnergyMax;
                }
            }
        }

        public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
            absorbed = false;

            if (this.ShieldState != ShieldState.Active || this.PawnOwner == null)
            {
                return;
            }

            // 原版逻辑
            if (dinfo.Def == DamageDefOf.EMP)
            {
                this.energy = 0f;
                this.Break();
                return;
            }

            if (!dinfo.Def.ignoreShields && (dinfo.Def.isRanged || dinfo.Def.isExplosive))
            {
                this.energy -= dinfo.Amount * this.Props.energyLossPerDamage;
                if (this.energy < 0f)
                {
                    this.Break();
                }
                else
                {
                    this.AbsorbedDamage(dinfo);
                }
                absorbed = true;
            }
        }

        public void KeepDisplaying()
        {
            this.lastKeepDisplayTick = Find.TickManager.TicksGame;
        }

        private void AbsorbedDamage(DamageInfo dinfo)
        {
            Pawn pawnOwner = this.PawnOwner;
            if (pawnOwner == null || !pawnOwner.Spawned)
            {
                return;
            }

            SoundDefOf.EnergyShield_AbsorbDamage.PlayOneShot(new TargetInfo(pawnOwner.Position, pawnOwner.Map, false));
            this.impactAngleVect = Vector3Utility.HorizontalVectorFromAngle(dinfo.Angle);

            Vector3 loc = pawnOwner.TrueCenter() + this.impactAngleVect.RotatedBy(180f) * 0.5f;
            float num = Mathf.Min(10f, 2f + dinfo.Amount / 10f);
            FleckMaker.Static(loc, pawnOwner.Map, FleckDefOf.ExplosionFlash, num);

            int num2 = (int)num;
            for (int i = 0; i < num2; i++)
            {
                FleckMaker.ThrowDustPuff(loc, pawnOwner.Map, Rand.Range(0.8f, 1.2f));
            }

            this.lastAbsorbDamageTick = Find.TickManager.TicksGame;
            this.KeepDisplaying();
        }
        private void Break()
        {
            Pawn pawnOwner = this.PawnOwner;

            if (this.parent.Spawned && pawnOwner != null)
            {
                float scale = Mathf.Lerp(this.Props.minDrawSize, this.Props.maxDrawSize, this.energy);
                EffecterDefOf.Shield_Break.SpawnAttached(this.parent, this.parent.MapHeld, scale);
                FleckMaker.Static(pawnOwner.TrueCenter(), pawnOwner.Map, FleckDefOf.ExplosionFlash, 12f);
                for (int i = 0; i < 6; i++)
                {
                    FleckMaker.ThrowDustPuff(
                        pawnOwner.TrueCenter() + Vector3Utility.HorizontalVectorFromAngle((float)Rand.Range(0, 360)) * Rand.Range(0.3f, 0.6f),
                        pawnOwner.Map,
                        Rand.Range(0.8f, 1.2f));
                }
            }
            this.energy = 0f;
            if (this.IsApparel && this.Props.addBreakHediffOnApparel)
            {
                Apparel thing = (Apparel)this.parent;
                Pawn wearer = thing.Wearer;
                if (wearer != null && !string.IsNullOrEmpty(this.Props.breakHediffDefName))
                {
                    HediffDef hDef = DefDatabase<HediffDef>.GetNamed(this.Props.breakHediffDefName, false);
                    if (hDef != null)
                    {
                        Hediff hediff = HediffMaker.MakeHediff(hDef, wearer, null);
                        HediffComp_Disappears disappears = hediff.TryGetComp<HediffComp_Disappears>();
                        if (disappears != null)
                        {
                            disappears.ticksToDisappear = this.Props.breakHediffDurationTicks;
                        }
                        wearer.health.AddHediff(hediff, null, null, null);
                    }
                }
            }

            this.ticksToReset = this.Props.startingTicksToReset;
        }
        private void Reset()
        {
            Pawn pawnOwner = this.PawnOwner;
            if (pawnOwner != null && pawnOwner.Spawned)
            {
                SoundDefOf.EnergyShield_Reset.PlayOneShot(new TargetInfo(pawnOwner.Position, pawnOwner.Map, false));
                FleckMaker.ThrowLightningGlow(pawnOwner.TrueCenter(), pawnOwner.Map, 3f);
            }

            this.ticksToReset = -1;
            this.energy = this.Props.energyOnReset;
        }

        public override void CompDrawWornExtras()
        {
            base.CompDrawWornExtras();
            if (this.IsApparel)
            {
                this.Draw();
            }
        }

        public override void PostDraw()
        {
            base.PostDraw();
            if (this.IsBuiltIn)
            {
                this.Draw();
            }
        }

        private void Draw()
        {
            if (this.ShieldState != ShieldState.Active || !this.ShouldDisplay)
            {
                return;
            }

            Pawn pawnOwner = this.PawnOwner;
            if (pawnOwner == null)
            {
                return;
            }

            float num = Mathf.Lerp(this.Props.minDrawSize, this.Props.maxDrawSize, this.energy);
            Vector3 vector = pawnOwner.Drawer.DrawPos;
            vector.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            int num2 = Find.TickManager.TicksGame - this.lastAbsorbDamageTick;
            if (num2 < 8)
            {
                float num3 = (float)(8 - num2) / 8f * 0.05f;
                vector += this.impactAngleVect * num3;
                num -= num3;
            }

            Vector3 s = new Vector3(num, 1f, num);
            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(vector, Quaternion.identity, s);

            Material baseMat = this.BaseBubbleMat;
            if (baseMat != null)
            {
                Graphics.DrawMesh(MeshPool.plane10, matrix, baseMat, 0);
            }

            Material overlayMat = this.GetMaterial_Shield;
            if (overlayMat != null)
            {
                Graphics.DrawMesh(MeshPool.plane10, matrix, overlayMat, 0);
            }
        }

        public override bool CompAllowVerbCast(Verb verb)
        {
            if (this.Props.blocksRangedWeapons)
            {
                return !(verb is Verb_LaunchProjectile);
            }
            return true;
        }
    }
}