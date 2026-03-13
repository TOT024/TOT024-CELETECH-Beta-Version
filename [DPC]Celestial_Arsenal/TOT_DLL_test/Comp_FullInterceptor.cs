using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TOT_DLL_test
{
    [StaticConstructorOnStartup]
    public class CompFullProjectileInterceptor : ThingComp
    {
        public CompProperties_FullProjectileInterceptor Props
        {
            get
            {
                return (CompProperties_FullProjectileInterceptor)this.props;
            }
        }
        public bool Active
        {
            get
            {
                return this.ShieldState == ShieldState.Active;
            }
        }
        public bool BombardmentCanStartFireAt(Bombardment bombardment, IntVec3 cell)
        {
            return !this.Active || ((bombardment.instigator == null || !bombardment.instigator.HostileTo(this.parent)) && !this.debugInterceptNonHostileProjectiles && !this.Props.interceptNonHostileProjectiles) || !cell.InHorDistOf(this.parent.Position, this.radius);
        }
        public bool CheckBombardmentIntercept(Bombardment bombardment, Bombardment.BombardmentProjectile projectile)
        {
            if (!this.Active)
            {
                return false;
            }
            if (!projectile.targetCell.InHorDistOf(this.parent.Position, this.radius))
            {
                return false;
            }
            if ((bombardment.instigator == null || !bombardment.instigator.HostileTo(this.parent)) && !this.debugInterceptNonHostileProjectiles && !this.Props.interceptNonHostileProjectiles)
            {
                return false;
            }
            this.lastInterceptTicks = Find.TickManager.TicksGame;
            this.drawInterceptCone = false;
            this.TriggerEffecter(projectile.targetCell);
            return true;
        }
        private void TriggerEffecter(IntVec3 pos)
        {
            Effecter effecter = new Effecter(this.Props.interceptEffect ?? RimWorld.EffecterDefOf.Interceptor_BlockedProjectile);
            effecter.Trigger(new TargetInfo(pos, this.parent.Map, false), TargetInfo.Invalid, -1);
            effecter.Cleanup();
        }
        public bool CheckIntercept(Projectile projectile, Vector3 lastExactPos, Vector3 newExactPos)
        {
            Vector3 vector = this.parent.Position.ToVector3Shifted();
            float num = this.radius + projectile.def.projectile.SpeedTilesPerTick + 0.1f;
            if ((newExactPos.x - vector.x) * (newExactPos.x - vector.x) + (newExactPos.z - vector.z) * (newExactPos.z - vector.z) > num * num)
            {
                return false;
            }
            if (!this.Active || this.ShieldState == ShieldState.Resetting)
            {
                return false;
            }
            if ((projectile.Launcher == null || !projectile.Launcher.HostileTo(this.parent)) && !this.debugInterceptNonHostileProjectiles && !this.Props.interceptNonHostileProjectiles)
            {
                return false;
            }
            if (!GenGeo.IntersectLineCircleOutline(new Vector2(vector.x, vector.z), this.radius, new Vector2(lastExactPos.x, lastExactPos.z), new Vector2(newExactPos.x, newExactPos.z)))
            {
                return false;
            }
            DamageInfo dinfo = new DamageInfo(projectile.def.projectile.damageDef, (float)projectile.DamageAmount, 0f, -1f, null, null, null, DamageInfo.SourceCategory.ThingOrUnknown, null, true, true);
            this.lastInterceptAngle = lastExactPos.AngleToFlat(this.parent.TrueCenter());
            this.lastInterceptTicks = Find.TickManager.TicksGame;
            Effecter effecter = new Effecter(this.Props.interceptEffect ?? RimWorld.EffecterDefOf.Interceptor_BlockedProjectile);
            effecter.Trigger(new TargetInfo(newExactPos.ToIntVec3(), this.parent.Map, false), TargetInfo.Invalid, -1);
            effecter.Cleanup();
            this.energy -= dinfo.Amount * this.EnergyLossPerDamage;
            if (this.energy < 0f)
            {
                this.Break();
            }
            return true;
        }
        public override void CompTick()
        {
            base.CompTick();
            if (this.power.PowerOn)
            {
                this.energy += this.EnergyGainPerTick;
                if (this.energy > this.EnergyMax)
                {
                    this.energy = this.EnergyMax;
                }
            }
            else
            {
                this.energy = 0f;
            }
            if (this.ShieldState == ShieldState.Resetting)
            {
                this.ticksToReset--;
                if (this.energy >= this.EnergyMax)
                {
                    this.Reset();
                    return;
                }
            }
        }
        public void Break()
        {
            SoundDefOf.MechSelfShutdown.PlayOneShot(new TargetInfo(this.parent.Position, this.parent.Map, false));
            FleckMaker.Static(this.parent.TrueCenter(), this.parent.Map, FleckDefOf.ExplosionFlash, 52f);
            for (int i = 0; i < 6; i++)
            {
                FleckMaker.ThrowDustPuff(this.parent.TrueCenter() + Vector3Utility.HorizontalVectorFromAngle((float)Rand.Range(0, 360)) * Rand.Range(0.3f, 0.6f), this.parent.Map, Rand.Range(0.8f, 1.2f));
            }
            this.energy = 0f;
            this.ticksToReset = this.Props.startingTicksToReset;
        }
        public ShieldState ShieldState
        {
            get
            {
                if (this.ticksToReset > 0 || !this.flick.SwitchIsOn || !this.power.PowerOn)
                {
                    return ShieldState.Resetting;
                }
                return ShieldState.Active;
            }
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<float>(ref this.energy, "energy", 0f, false);
        }
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            this.flick = this.parent.GetComp<CompFlickable>();
            this.power = this.parent.GetComp<CompPowerTrader>();
        }
        private void Reset()
        {
            if (this.parent.Spawned)
            {
                SoundDefOf.EnergyShield_Reset.PlayOneShot(new TargetInfo(this.parent.Position, this.parent.Map, false));
            }
            this.ticksToReset = -1;
        }
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (Find.Selector.SingleSelectedThing == this.parent)
            {
                yield return new Gizmo_PIStatus
                {
                    shield = this
                };
            }
            if (Prefs.DevMode)
            {
                if (this.ShieldState == ShieldState.Resetting)
                {
                    yield return new Command_Action
                    {
                        defaultLabel = "Dev: Reset cooldown ",
                        action = delegate ()
                        {
                            this.lastInterceptTicks = Find.TickManager.TicksGame - this.Props.cooldownTicks;
                        }
                    };
                }
                yield return new Command_Toggle
                {
                    defaultLabel = "Dev: Intercept non-hostile",
                    isActive = (() => this.debugInterceptNonHostileProjectiles),
                    toggleAction = delegate ()
                    {
                        this.debugInterceptNonHostileProjectiles = !this.debugInterceptNonHostileProjectiles;
                    }
                };
            }
            yield break;
        }
        private float GetCurrentAlpha()
        {
            float result;
            result = Mathf.Max(Mathf.Max(Mathf.Max(Mathf.Max(this.GetCurrentAlpha_Idle(), this.GetCurrentAlpha_RecentlyIntercepted()), this.GetCurrentAlpha_RecentlyActivated()), 0.11f));
            if(this.parent.Map.dangerWatcher.DangerRating == StoryDanger.High)
            {
                result = Mathf.Clamp01(result * 1.5f);
            }
            return result;
        }
        private float GetCurrentAlpha_Idle()
        {
            float idlePulseSpeed = 0.7f;
            float minIdleAlpha = -1.7f;
            if (!this.Active)
            {
                return 0f;
            }
            if (Find.Selector.IsSelected(this.parent))
            {
                return 0f;
            }
            return Mathf.Lerp(minIdleAlpha, 0.11f, (Mathf.Sin((float)(Gen.HashCombineInt(this.parent.thingIDNumber, 96804938) % 100) + Time.realtimeSinceStartup * idlePulseSpeed) + 1f) / 2f);
        }
        private float GetCurrentAlpha_RecentlyActivated()
        {
            if (!this.Active)
            {
                return 0f;
            }
            int num = Find.TickManager.TicksGame - (this.lastInterceptTicks + this.Props.cooldownTicks);
            return Mathf.Clamp01(1f - (float)num / 50f) * 0.09f;
        }
        private float GetCurrentAlpha_RecentlyIntercepted()
        {
            int num = Find.TickManager.TicksGame - this.lastInterceptTicks;
            return Mathf.Clamp01(1f - (float)num / 40f) * 0.09f;
        }

        public override void PostDraw()
        {
            base.PostDraw();
            Vector3 drawPos = this.parent.DrawPos;
            drawPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
            if(this.Active && (this.parent.Map.attackTargetsCache.TargetsHostileToColony.Count > 0 || Find.TickManager.TicksGame - this.lastInterceptTicks < 15))
            {
                Color value;
                value = Color.cyan;
                value.a = GetCurrentAlpha();
                CompFullProjectileInterceptor.MatPropertyBlock.SetColor(ShaderPropertyIDs.Color, value);
                Matrix4x4 matrix = default;
                matrix.SetTRS(drawPos, Quaternion.identity, new Vector3(this.radius * 2f * 1.1601562f, 1f, this.radius * 2f * 1.1601562f));
                Graphics.DrawMesh(MeshPool.plane10, matrix, CompFullProjectileInterceptor.ShieldMat, 0, null, 0, CompFullProjectileInterceptor.MatPropertyBlock);
            }
        }
        public override void PostDrawExtraSelectionOverlays()
        {
            base.PostDrawExtraSelectionOverlays();
            Vector3 drawPos = this.parent.DrawPos;
            drawPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
            if (this.Active && (this.parent.Map.attackTargetsCache.TargetsHostileToColony.Count > 0 || Find.TickManager.TicksGame - this.lastInterceptTicks < 15))
            {
                Color value;
                value = Color.cyan;
                value.a = GetCurrentAlpha();
                CompFullProjectileInterceptor.MatPropertyBlock.SetColor(ShaderPropertyIDs.Color, value);
                Matrix4x4 matrix = default;
                matrix.SetTRS(drawPos, Quaternion.identity, new Vector3(this.radius * 2f * 1.1601562f, 1f, this.radius * 2f * 1.1601562f));
                Graphics.DrawMesh(MeshPool.plane10, matrix, CompFullProjectileInterceptor.ShieldMat, 0, null, 0, CompFullProjectileInterceptor.MatPropertyBlock);
            }
                if (radius < 54)
                {
                    GenDraw.DrawRadiusRing(this.parent.DrawPos.ToIntVec3(), this.radius, Color.white);
                }
        }
        public bool OnCooldown
        {
            get
            {
                return Find.TickManager.TicksGame < this.lastInterceptTicks + this.Props.cooldownTicks;
            }
        }
        public bool Charging
        {
            get
            {
                return this.nextChargeTick >= 0 && Find.TickManager.TicksGame > this.nextChargeTick;
            }
        }
        public float energy;
        protected int ticksToReset = -1;
        private CompFlickable flick;
        private CompPowerTrader power;
        public bool debugInterceptNonHostileProjectiles;
        private float lastInterceptAngle;
        private int lastInterceptTicks = -999999;
        private float EnergyLossPerDamage = 1f;
        private int nextChargeTick = -1;
        private static readonly MaterialPropertyBlock MatPropertyBlock = new MaterialPropertyBlock();
        private static readonly Material ShieldMat = MaterialPool.MatFrom("Things/FRShield_CMC", ShaderDatabase.MoteGlow);
        private bool drawInterceptCone;
        private float EnergyGainPerTick = 9f;
        public int EnergyMax = 1000;
        public static float RadiusMax;
        public float radius = 20f;
    }
}
