using System;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;

namespace TOT_DLL_test
{
    [StaticConstructorOnStartup]
    public class AirplaneDeliveryFlyer : Thing
    {
        private const float FlightAltitude = 15f;
        private const float Speed = 30f;
        private Vector3 p0;
        private Vector3 p1;
        private Vector3 p2;
        private int ticksFlying;
        private int totalDurationTicks;
        private bool hasDropped;
        private Vector3 drawPos;
        private float drawAngle;
        public ActiveTransporterInfo cargoInfo;
        private Sustainer sustainer;
        private readonly Vector3 WingTipOffset = new Vector3(3.5f, 0, -0.5f);
        private readonly Vector3 EngineOffset = new Vector3(1.0f, 0, -2.5f);
        private static FleckDef wingTrailDef;
        private static FleckDef engineGlowDef;
        private static readonly Material ShadowMat = MaterialPool.MatFrom("Things/Aerocraft/Y50_Shadow", ShaderDatabase.Transparent, new Color(0f, 0f, 0f, 0.5f));

        public void SetupFlight(IntVec3 target, Map map, ActiveTransporterInfo cargo)
        {
            this.cargoInfo = cargo;
            this.hasDropped = false;
            this.ticksFlying = 0;

            if (wingTrailDef == null) wingTrailDef = FleckDefOf.Smoke;
            if (engineGlowDef == null) engineGlowDef = FleckDefOf.LightningGlow;

            Vector3 targetVec = target.ToVector3Shifted();
            targetVec.y = 0;

            float mapWidth = map.Size.x;
            float mapHeight = map.Size.z;
            float buffer = 30f;

            this.p0 = RandomMapEdgePoint(mapWidth, mapHeight, buffer);

            do
            {
                this.p2 = RandomMapEdgePoint(mapWidth, mapHeight, buffer);
            } while (Vector3.Distance(p0, p2) < (Mathf.Max(mapWidth, mapHeight) / 2f));

            this.p1 = (2f * targetVec) - (0.5f * p0) - (0.5f * p2);

            float dist1 = Vector3.Distance(p0, targetVec);
            float dist2 = Vector3.Distance(targetVec, p2);
            float totalDist = dist1 + dist2;

            this.totalDurationTicks = Mathf.CeilToInt((totalDist / Speed) * 60f);

            UpdatePositionAndRotation(0f);
        }

        private Vector3 RandomMapEdgePoint(float width, float height, float buffer)
        {
            int side = Rand.RangeInclusive(0, 3);
            float x = 0, z = 0;

            switch (side)
            {
                case 0: x = Rand.Range(0f, width); z = height + buffer; break;
                case 1: x = width + buffer; z = Rand.Range(0f, height); break;
                case 2: x = Rand.Range(0f, width); z = -buffer; break;
                case 3: x = -buffer; z = Rand.Range(0f, height); break;
            }
            return new Vector3(x, 0, z);
        }
        private void SpawnTickEffects()
        {
            if (!this.Position.InBounds(Map)) return;
            Quaternion quat = Quaternion.AngleAxis(this.drawAngle, Vector3.up);

            // --- 1. 生成翼尖尾迹 (左右各一条) ---
            if (wingTrailDef != null)
            {
                SpawnFleckAtOffset(WingTipOffset, quat, wingTrailDef, 0.6f); // 左翼
                SpawnFleckAtOffset(new Vector3(-WingTipOffset.x, WingTipOffset.y, WingTipOffset.z), quat, wingTrailDef, 0.6f); // 右翼
            }

            // --- 2. 生成引擎尾焰 (左右各一条) ---
            if (engineGlowDef != null)
            {
                SpawnFleckAtOffset(EngineOffset, quat, engineGlowDef, 1.0f); // 左引擎
                SpawnFleckAtOffset(new Vector3(-EngineOffset.x, EngineOffset.y, EngineOffset.z), quat, engineGlowDef, 1.0f); // 右引擎
            }
        }

        private void SpawnFleckAtOffset(Vector3 localOffset, Quaternion rotation, FleckDef fleckDef, float scale)
        {
            Vector3 worldOffset = rotation * localOffset;
            Vector3 spawnPos = this.drawPos + worldOffset;
            spawnPos.z += FlightAltitude;
            spawnPos.y = AltitudeLayer.Skyfaller.AltitudeFor() - 0.05f;
            FleckCreationData data = FleckMaker.GetDataStatic(spawnPos, this.Map, fleckDef, scale);
            data.velocityAngle = this.drawAngle + 180f; // 向后
            data.velocitySpeed = 2f;
            this.Map.flecks.CreateFleck(data);
        }
        protected override void Tick()
        {
            if (this.Map == null) return;

            ticksFlying++;
            float t = (float)ticksFlying / (float)totalDurationTicks;

            UpdatePositionAndRotation(t);

            IntVec3 currentCell = this.drawPos.ToIntVec3();
            if (currentCell.InBounds(Map) && currentCell != this.Position)
            {
                this.Position = currentCell;
            }

            if (this.sustainer == null)
            {
                SoundDef def = DefDatabase<SoundDef>.GetNamed("CMC_EngineSustainSound", false);
                if (def != null)
                {
                    this.sustainer = def.TrySpawnSustainer(SoundInfo.InMap(this, MaintenanceType.PerTick));
                }
            }

            if (this.sustainer != null)
            {
                this.sustainer.Maintain();
            }
            SpawnTickEffects();
            if (!hasDropped && t >= 0.5f)
            {
                DoDrop();
            }

            if (t >= 1.0f)
            {
                this.Destroy();
            }
        }
        private void UpdatePositionAndRotation(float t)
        {
            t = Mathf.Clamp01(t);
            float oneMinusT = 1f - t;

            this.drawPos = (oneMinusT * oneMinusT * p0) +
                           (2f * oneMinusT * t * p1) +
                           (t * t * p2);

            Vector3 tangent = (2f * oneMinusT * (p1 - p0)) + (2f * t * (p2 - p1));

            if (tangent.magnitude > 0.001f)
            {
                this.drawAngle = tangent.AngleFlat();
            }
        }

        private void DoDrop()
        {
            hasDropped = true;
            if (this.Map == null || this.cargoInfo == null) return;

            IntVec3 dropCell = ((0.25f * p0) + (0.5f * p1) + (0.25f * p2)).ToIntVec3();
            if (!dropCell.InBounds(Map)) dropCell = this.Position;

            ActiveTransporter activePod = (ActiveTransporter)ThingMaker.MakeThing(ThingDefOf.ActiveDropPod);
            activePod.Contents = this.cargoInfo;

            Skyfaller skyfaller = SkyfallerMaker.MakeSkyfaller(CMC_Def.CMC_CargoCrateIncoming, activePod);
            skyfaller.ticksToImpact = 15;

            GenSpawn.Spawn(skyfaller, dropCell, Map);
        }
        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            ForceDrawAt(this.drawPos);
        }
        protected void ForceDrawAt(Vector3 drawLoc, bool flip = false)
        {
            Vector3 planePos = this.drawPos;
            planePos.y = AltitudeLayer.Skyfaller.AltitudeFor();
            planePos.z += FlightAltitude;

            Quaternion quat = Quaternion.AngleAxis(this.drawAngle, Vector3.up);
            Matrix4x4 matrix = Matrix4x4.TRS(planePos, quat, this.Graphic.drawSize.ToVector3());

            Graphics.DrawMesh(MeshPool.plane10, matrix, this.Graphic.MatAt(this.Rotation), 0);

            Vector3 shadowPos = this.drawPos;
            shadowPos.y = AltitudeLayer.Shadows.AltitudeFor();

            Matrix4x4 shadowMatrix = Matrix4x4.TRS(shadowPos, quat, this.Graphic.drawSize.ToVector3());
            Graphics.DrawMesh(MeshPool.plane10, shadowMatrix, ShadowMat, 0);
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if (this.sustainer != null && !this.sustainer.Ended)
            {
                this.sustainer.End();
            }
            base.Destroy(mode);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref p0, "p0");
            Scribe_Values.Look(ref p1, "p1");
            Scribe_Values.Look(ref p2, "p2");
            Scribe_Values.Look(ref ticksFlying, "ticksFlying");
            Scribe_Values.Look(ref totalDurationTicks, "totalDurationTicks");
            Scribe_Values.Look(ref hasDropped, "hasDropped");
            Scribe_Values.Look(ref drawPos, "drawPos");
            Scribe_Values.Look(ref drawAngle, "drawAngle");
            Scribe_Deep.Look(ref cargoInfo, "cargoInfo");
        }
    }

    public static class VectorExtensions
    {
        public static Vector3 ToVector3(this Vector2 v)
        {
            return new Vector3(v.x, 1f, v.y);
        }
    }
}