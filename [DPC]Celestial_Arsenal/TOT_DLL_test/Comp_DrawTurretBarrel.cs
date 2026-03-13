using RimWorld;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using Verse;
using Verse.Noise;
using Verse.Sound;

namespace TOT_DLL_test
{
    public class CompProperties_DrawTurretBarrel : CompProperties
    {
        public GraphicData BarrelTexture;
        public GraphicData OverHeatTexture;
        public Vector3 TurretOffset = new Vector3(0,0,0);
        public float maxRecoil = 0.25f;
        public int rapidPhase = 5;
        public int holdPhase = 5;
        public int totalTicks = 30;
        public Vector3 MuzzleOffset = new Vector3(0, 0, 0);
        public FleckDef smokeFleck;
        public float smokeChance = 0.7f; 
        public float smokeSize = 1.5f; 
        public int smokeDuration = 25; 
        public int baseSmokeDuration = 25; 
        public int minSmokeDuration = 10;
        public float maxOverHeatAlpha = 1f; 
        public int overHeatDelay = 0;
        public int overHeatDuration = 30;
        public string SoundReloading;
        public bool SideSmokde = true;
        public CompProperties_DrawTurretBarrel()
        {
            compClass = typeof(Comp_DrawTurretBarrel);
        }
    }
    public class Comp_DrawTurretBarrel : ThingComp
    {
        private int lastProcessedTick = -1;
        private CompProperties_DrawTurretBarrel Props => (CompProperties_DrawTurretBarrel)props;
        public void CompPostDrawBarrel(Building_CMCTurretGun building_CMCTurretGun)
        {
            Vector3 b = new Vector3(building_CMCTurretGun.def.building.turretTopOffset.x, 0f, building_CMCTurretGun.def.building.turretTopOffset.y).RotatedBy(building_CMCTurretGun.turrettop.CurRotation);
            float turretTopDrawSize = building_CMCTurretGun.def.building.turretTopDrawSize;
            Verb currentEffectiveVerb = building_CMCTurretGun.CurrentEffectiveVerb;
            float num = ((currentEffectiveVerb != null) ? currentEffectiveVerb.AimAngleOverride : null) ?? building_CMCTurretGun.turrettop.CurRotation;
            Vector3 vector = building_CMCTurretGun.TrueCenter() + Altitudes.AltIncVect + b;
            vector.y = AltitudeLayer.BuildingOnTop.AltitudeFor() - 0.13f;
            Quaternion q = ((float)TurretTop.ArtworkRotation + num).ToQuat();
            Vector3 s = new Vector3(turretTopDrawSize, 1f, turretTopDrawSize);
            Vector3 b2 = new Vector3(0f, 0f, this.Props.TurretOffset.z - CalculateRecoil(building_CMCTurretGun)).RotatedBy(building_CMCTurretGun.turrettop.CurRotation);
            Vector3 pos = vector + b2;
            Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(pos, q, s), Props.BarrelTexture.Graphic.MatSingle, 0);
            DrawOverHeatTexture(building_CMCTurretGun, pos, q, s);
            TrySpawnSmokeFleck(building_CMCTurretGun, vector, building_CMCTurretGun.turrettop.CurRotation);
        }

        public float CalculateRecoil(Building_CMCTurretGun building_CMCTurretGun)
        {
            int currentTick = building_CMCTurretGun.BurstCooldownTime().SecondsToTicks() - building_CMCTurretGun.burstCooldownTicksLeft;
            if (currentTick != lastProcessedTick)
            {
                lastProcessedTick = currentTick;
                if (currentTick > 0 && currentTick <= Props.holdPhase)
                {
                    if (currentTick % 2 == 0 && Rand.Value < Props.smokeChance)
                    {
                    }
                }
            }
            if (currentTick > Props.totalTicks)
            {
                return 0f;
            }

            if (currentTick <= this.Props.rapidPhase)
            {
                float t = currentTick / (float)this.Props.rapidPhase;
                return this.Props.maxRecoil * t * t;
            }
            if (currentTick <= this.Props.holdPhase)
            {
                return this.Props.maxRecoil;
            }
            {
                float t = (currentTick - this.Props.holdPhase) / (float)(this.Props.totalTicks - this.Props.holdPhase);
                float easeOut = t * t;
                float easeIn = 1 - (1 - t) * (1 - t);
                float blend = easeOut * 0.4f + easeIn * 0.6f;

                return this.Props.maxRecoil * (1 - blend);
            }
        }
        private void DrawOverHeatTexture(Building_CMCTurretGun building_CMCTurretGun, Vector3 barrelPos, Quaternion rotation, Vector3 scale)
        {
            if (Props.OverHeatTexture?.Graphic?.MatSingle != null)
            {
                float alpha = CalculateOverHeatAlpha(building_CMCTurretGun);

                if (alpha > 0f)
                {
                    Color color = new Color(1f, 1f, 1f, alpha);
                    Material overHeatMat = Props.OverHeatTexture.Graphic.MatSingle;
                    Material tempMat = new Material(overHeatMat);
                    tempMat.color = color;
                    Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(barrelPos, rotation, scale), tempMat, 0);
                }
            }
        }
        private float CalculateOverHeatAlpha(Building_CMCTurretGun building_CMCTurretGun)
        {
            int currentTick = building_CMCTurretGun.BurstCooldownTime().SecondsToTicks() - building_CMCTurretGun.burstCooldownTicksLeft;

            if (currentTick > Props.totalTicks)
            {
                return 0f;
            }
            if (currentTick <= Props.rapidPhase)
            {
                return currentTick / (float)Props.rapidPhase;
            }
            else if (currentTick <= Props.holdPhase)
            {
                return 1f;
            }
            else
            {
                float t = (currentTick - Props.holdPhase) / (float)(Props.totalTicks - Props.holdPhase);
                return 1f - t;
            }
        }
        private void TrySpawnSmokeFleck(Building_CMCTurretGun building_CMCTurretGun, Vector3 turretTopPos, float rotation)
        {
            int currentTick = building_CMCTurretGun.BurstCooldownTime().SecondsToTicks() - building_CMCTurretGun.burstCooldownTicksLeft;
            if (Props.smokeFleck != null && currentTick > 0 && currentTick > Props.rapidPhase && currentTick < Props.totalTicks && !Find.TickManager.Paused)
            {
                if (currentTick % 2 == 0 && Rand.Value < Props.smokeChance)
                {
                    if(Props.SideSmokde)
                    {
                        Vector3 muzzleOffsetRotated = Props.MuzzleOffset.RotatedBy(rotation);
                        Vector3 muzzlePos = turretTopPos + muzzleOffsetRotated;
                        Vector3 right = muzzleOffsetRotated.RotatedBy(90);
                        Vector3 left = -right;
                        Vector3 smokeDirection = Rand.Value < 0.5f ? left : right;
                        Vector3 velocity = smokeDirection * Rand.Range(0.320f, 0.40f);
                        float initialSize = Props.smokeSize * Rand.Range(0.8f, 1.2f);
                        float progress = currentTick / (float)Props.holdPhase;

                        int smokeDuration = Mathf.RoundToInt(
                            Props.baseSmokeDuration -
                            (Props.baseSmokeDuration - Props.minSmokeDuration) * progress
                        );
                        FleckCreationData data = FleckMaker.GetDataStatic(
                            muzzlePos,
                            building_CMCTurretGun.Map,
                            Props.smokeFleck,
                            initialSize
                        );
                        data.rotation = Rand.Range(0f, 360f);
                        data.velocity = velocity;
                        data.rotationRate = Rand.Range(-60f, 60f);
                        data.solidTimeOverride = smokeDuration * 0.1f;
                        data.airTimeLeft = smokeDuration * 0.9f;

                        building_CMCTurretGun.Map.flecks.CreateFleck(data);
                    }
                    else
                    {
                        Vector3 muzzleOffsetRotated = Props.MuzzleOffset.RotatedBy(rotation);
                        Vector3 muzzlePos = turretTopPos + muzzleOffsetRotated;
                        Vector3 smokeDirection = muzzleOffsetRotated;
                        Vector3 velocity = smokeDirection * Rand.Range(0.320f, 0.40f);
                        float initialSize = Props.smokeSize * Rand.Range(0.8f, 1.2f);
                        float progress = currentTick / (float)Props.holdPhase;

                        int smokeDuration = Mathf.RoundToInt(
                            Props.baseSmokeDuration -
                            (Props.baseSmokeDuration - Props.minSmokeDuration) * progress
                        );
                        FleckCreationData data = FleckMaker.GetDataStatic(
                            muzzlePos,
                            building_CMCTurretGun.Map,
                            Props.smokeFleck,
                            initialSize
                        );
                        data.rotation = Rand.Range(0f, 360f);
                        data.velocity = velocity;
                        data.rotationRate = Rand.Range(-60f, 60f);
                        data.solidTimeOverride = smokeDuration * 0.1f;
                        data.airTimeLeft = smokeDuration * 0.9f;

                        building_CMCTurretGun.Map.flecks.CreateFleck(data);
                    }
                }
            }
            else if (currentTick == Props.totalTicks && building_CMCTurretGun.AttackVerb.BurstShotCount <2)
            {
                SoundDef soundDef = DefDatabase<SoundDef>.GetNamed(Props.SoundReloading, false);
                soundDef?.PlayOneShot(new TargetInfo(this.parent.Position, this.parent.Map));
            }
        }
    }
}
