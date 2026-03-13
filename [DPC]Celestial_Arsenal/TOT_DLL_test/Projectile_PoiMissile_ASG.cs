using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace TOT_DLL_test
{
    [StaticConstructorOnStartup]
    public class Projectile_PoiMissile_ASG : Projectile_Explosive
    {
        private bool targetinit = false;
        public Mote_ScaleAndRotate mote;
        public Vector3 position1;
        public Vector3 position2;
        private bool positionCalculated = false;
        public Quaternion rotation;
        public float DCFExport;
        public FloatRange Fleck_Angle = new FloatRange(-180f, 180f);
        public FloatRange Fleck_Speed = new FloatRange(0.1f, 0.2f);
        public FloatRange Fleck_Speed2 = new FloatRange(0.1f, 0.2f);
        public FloatRange Fleck_Rotation = new FloatRange(-180f, 180f);
        public FleckDef FleckDef2 = DefDatabase<FleckDef>.GetNamed("CMC_Fleck_ProjectileSmoke_LongLasting", true);
        private bool spawnedsmoke = false;
        public float startingAltitude = 15f;
        public Vector3 planeDirection = Vector3.zero;
        private readonly Color smokeColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        public int CountToDestroy = 10;
        private List<Vector3> recentPositions = new List<Vector3>();
        private const int POSITION_HISTORY_COUNT = 5;
        private Vector3 previousPosition;

        public override Vector3 ExactPosition
        {
            get
            {
                if (positionCalculated)
                    return this.position2;
                else
                    return base.ExactPosition;
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref planeDirection, "planeDirection");
            Scribe_Values.Look(ref startingAltitude, "startingAltitude", 15f);
            Scribe_Values.Look(ref targetinit, "targetinit");
            Scribe_Values.Look(ref spawnedsmoke, "spawnedsmoke");
        }
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            this.position1 = base.ExactPosition;
            this.position2 = base.ExactPosition;

            recentPositions = new List<Vector3>();
            for (int i = 0; i < POSITION_HISTORY_COUNT; i++)
            {
                recentPositions.Add(this.DrawPos);
            }
            this.previousPosition = this.DrawPos;
        }
        public void ThrowAirPuffUp(Vector3 loc, Map map)
        {
            if (!loc.ToIntVec3().ShouldSpawnMotesAt(map, true))
            {
                return;
            }
            Vector3 jitter = new Vector3(Rand.Range(-0.15f, 0.15f), 0f, Rand.Range(-0.15f, 0.15f));
            FleckCreationData dataStatic = FleckMaker.GetDataStatic(loc + jitter, map, FleckDefOf.AirPuff, 1.5f);
            dataStatic.rotationRate = (float)Rand.RangeInclusive(-240, 240);
            dataStatic.velocityAngle = (float)Rand.Range(-180, 180);
            dataStatic.velocitySpeed = Rand.Range(0.5f, 1.5f);
            dataStatic.instanceColor = smokeColor;
            map.flecks.CreateFleck(dataStatic);
        }
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if(this.CountToDestroy <= 0)
                base.Destroy(mode);
            else
                CountToDestroy--;
        }
        protected override void Tick()
        {
            if (this.DestroyedOrNull()) return;
            this.previousPosition = this.DrawPos;
            if (!this.spawnedsmoke)
            {
                for (int i = 0; i < 5; i++)
                {
                    Vector3 vec = new Vector3(Rand.Range(-0.4f, 0.5f), 0f, Rand.Range(-0.4f, 0.5f));
                    ThrowAirPuffUp(this.DrawPos + vec, Map);
                }
                FleckMaker.ThrowHeatGlow(DrawPos.ToIntVec3(), Map, 1.5f);
                spawnedsmoke = true;
            }
            position1 = this.InterceptMissilePosition(base.DistanceCoveredFraction);
            position2 = this.InterceptMissilePosition(base.DistanceCoveredFraction - 0.01f);
            positionCalculated = true;
            if ((position1 - position2).sqrMagnitude > 0.0001f)
            {
                rotation = Quaternion.LookRotation(position1 - this.position2);
            }
            DCFExport = this.DistanceCoveredFraction;
            if (this.intendedTarget != null && this.intendedTarget.Thing != null)
                this.destination = this.intendedTarget.Thing.DrawPos;
            int smokeCount = GenerateSmokeFleckCount(DistanceCoveredFraction, DrawPos, previousPosition);
            for (int i = 0; i < smokeCount; i++)
            {
                Vector3 smokePos = this.DrawPos;
                smokePos += new Vector3(Rand.Range(-0.25f, 0.25f), 0f, Rand.Range(-0.25f, 0.25f));

                float flightAngle = (this.DrawPos - this.previousPosition).AngleFlat();
                float velocityAngle = flightAngle + 180f + this.Fleck_Angle.RandomInRange + Rand.Range(-15f, 15f);

                float sizeMultiplier = Mathf.Lerp(1.2f, 0.6f, DistanceCoveredFraction);
                float randomSizeVar = Rand.Range(0.5f, 1.5f);
                float scale = (this.def.graphicData.drawSize.x / 1.92f * randomSizeVar) * sizeMultiplier;
                float randomSpeedVar = Rand.Range(0.7f, 1.4f);
                float currentSpeed = this.Fleck_Speed2.RandomInRange * randomSpeedVar;

                FleckCreationData dataStatic = FleckMaker.GetDataStatic(smokePos, Map, this.FleckDef2, scale);
                dataStatic.rotationRate = this.Fleck_Rotation.RandomInRange * Rand.Range(0.8f, 1.2f);
                dataStatic.velocityAngle = velocityAngle;
                dataStatic.velocitySpeed = currentSpeed;
                dataStatic.rotation = (float)Rand.Range(0, 360);
                dataStatic.instanceColor = smokeColor;
                base.Map.flecks.CreateFleck(dataStatic);
            }
            base.Tick();
        }
        public Vector3 InterceptMissilePosition(float t)
        {
            t = Mathf.Clamp01(t);

            Vector3 rawOrigin = this.origin;
            Vector3 targetPos = this.destination;
            float startGroundZ = rawOrigin.z - this.startingAltitude;
            float currentX = Mathf.Lerp(rawOrigin.x, targetPos.x, t);
            float currentMapZ = Mathf.Lerp(startGroundZ, targetPos.z, t);
            float currentAltitude = this.startingAltitude * (1f - (t * t));
            Vector3 currentPos = new Vector3(currentX, 0, currentMapZ + currentAltitude);
            currentPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            return currentPos;
        }
        protected override void DrawAt(Vector3 position, bool flip = false)
        {
            if ((position1 - position2).sqrMagnitude > 0.0001f)
            {
                Quaternion rotation = Quaternion.LookRotation(position1 - position2);
                Graphics.DrawMesh(MeshPool.GridPlane(this.def.graphicData.drawSize), position1, rotation, this.DrawMat, 0);
            }
            base.Comps_PostDraw();
        }
        public int GenerateSmokeFleckCount(float t, Vector3 currentPosition, Vector3 previousPosition)
        {
            float baseSmoke = Mathf.Lerp(8f, 0f, t);

            float speed = (currentPosition - previousPosition).magnitude / Time.deltaTime;
            float speedFactor = Mathf.Clamp(speed / 30f, 1f, 2f);

            int smokeCount = Mathf.RoundToInt(baseSmoke * speedFactor);
            return Mathf.Clamp(smokeCount, 0, 8);
        }
        protected override void Explode()
        {
            CountToDestroy = 0;
            base.Explode();
        }
    }
}