using RimWorld;
using UnityEngine;
using Verse;

namespace TOT_DLL_test
{
    public class Projectile_Fire : Projectile_Explosive
    {
        public Vector3 IPPos(float t)
        {
            t = Mathf.Clamp01(t);
            Vector3 result = this.origin + (this.destination - this.origin).Yto0() * t;
            result = result + 4 * t * (1 - t) * new Vector3(0f, 0f, 1f);
            return result;
        }
        protected override void DrawAt(Vector3 position, bool flip = false)
        {
            Vector3 position2 = IPPos(this.DistanceCoveredFraction - 0.01f);
            position = IPPos(this.DistanceCoveredFraction);
            Quaternion rotation = Quaternion.LookRotation((position - position2));
            if (this.DistanceCoveredFraction > 0.02f)
            {
                Vector3 drawloc = position;
                drawloc.y = AltitudeLayer.Projectile.AltitudeFor();
                Graphics.DrawMesh(MeshPool.GridPlane(this.def.graphicData.drawSize), drawloc, rotation, this.DrawMat, 0);
                base.Comps_PostDraw();
            }
        }
        protected override void Tick()
        {
            if (this.intendedTarget.Thing != null)
            {
                this.destination = this.intendedTarget.Thing.DrawPos;
            }
            this.Fleck_MakeFleckTick++;
            bool flag = this.Fleck_MakeFleckTick >= this.Fleck_MakeFleckTickMax;
            if (flag && this.DistanceCoveredFraction > 0.04f)
            {
                this.Fleck_MakeFleckTick = 0;
                Map map = this.Map;
                int randomInRange = this.Fleck_MakeFleckNum.RandomInRange;
                Vector3 position = IPPos(this.DistanceCoveredFraction);
                Vector3 position2 = IPPos(this.DistanceCoveredFraction - 0.01f);
                for (int i = 0; i < randomInRange; i++)
                {
                    float num = (position - this.intendedTarget.CenterVector3).AngleFlat();
                    float velocityAngle = this.Fleck_Angle.RandomInRange + num;
                    float randomInRange2 = this.Fleck_Scale.RandomInRange;
                    float randomInRange22 = this.Fleck_Scale2.RandomInRange;
                    float randomInRange3 = this.Fleck_Speed.RandomInRange;
                    float randomInRange4 = this.Fleck_Speed2.RandomInRange;
                    FleckCreationData dataStatic = FleckMaker.GetDataStatic(position, map, this.FleckDef, randomInRange2);
                    FleckCreationData dataStatic2 = FleckMaker.GetDataStatic(position, map, this.FleckDef2, randomInRange22);
                    dataStatic.rotation = ((position - position2)).AngleFlat();
                    dataStatic2.rotation = ((position - position2)).AngleFlat();
                    dataStatic.rotationRate = this.Fleck_Rotation.RandomInRange;
                    dataStatic.velocityAngle = velocityAngle;
                    dataStatic.velocitySpeed = randomInRange3;
                    dataStatic2.rotationRate = this.Fleck_Rotation.RandomInRange;
                    dataStatic2.velocityAngle = velocityAngle;
                    dataStatic2.velocitySpeed = randomInRange4;
                    map.flecks.CreateFleck(dataStatic2);
                    map.flecks.CreateFleck(dataStatic);
                }
            }
            base.Tick();
        }

        public FleckDef FleckDef = DefDatabase<FleckDef>.GetNamed("CMC_FireGlow", true);
        public FleckDef FleckDef2 = DefDatabase<FleckDef>.GetNamed("CMC_Fleck_ProjectileSmoke_LongLastingGrow", true);
        public int Fleck_MakeFleckTickMax = 1;
        public IntRange Fleck_MakeFleckNum = new IntRange(2, 2);
        public FloatRange Fleck_Angle = new FloatRange(-180f, 180f);
        public FloatRange Fleck_Scale = new FloatRange(1.3f, 1.5f);
        public FloatRange Fleck_Scale2 = new FloatRange(1.1f, 1.2f);
        public FloatRange Fleck_Speed = new FloatRange(0.1f, 0.2f);
        public FloatRange Fleck_Speed2 = new FloatRange(0.1f, 0.2f);
        public FloatRange Fleck_Rotation = new FloatRange(-180f, 180f);
        public int Fleck_MakeFleckTick;
    }
}
