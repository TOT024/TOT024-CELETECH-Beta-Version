using UnityEngine;
using RimWorld;
using Verse;

namespace TOT_DLL_test
{
    public class Projectile_ExplosiveNormal : Projectile_Explosive
    {
        public override bool AnimalsFleeImpact => true;
        private Vector3 CurretPos(float t)
        {
            Vector3 result;
            result = this.origin + (this.destination - this.origin) * t;
            return result;
        }
        protected override void DrawAt(Vector3 position, bool flip = false)
        {
            Vector3 position2 = CurretPos(this.DistanceCoveredFraction - 0.01f);
            position = CurretPos(this.DistanceCoveredFraction);
            Quaternion rotation = Quaternion.LookRotation((position - position2));
            if (this.tickcount >= 2)
            {
                Vector3 drawloc = position;
                drawloc.y = AltitudeLayer.Projectile.AltitudeFor();
                Graphics.DrawMesh(MeshPool.GridPlane(this.def.graphicData.drawSize), drawloc, rotation, this.DrawMat, 0);
                base.Comps_PostDraw();
            }
        }
        protected override void Tick()
        {
            tickcount++;
            this.Fleck_MakeFleckTick++;
            bool flag = this.Fleck_MakeFleckTick >= this.Fleck_MakeFleckTickMax;
            if (flag && this.tickcount >= 8)
            {
                this.Fleck_MakeFleckTick = 0;
                Map map = this.Map;
                int randomInRange = this.Fleck_MakeFleckNum.RandomInRange;
                Vector3 position = CurretPos(this.DistanceCoveredFraction - 0.01f);
                Vector3 position2 = CurretPos(this.DistanceCoveredFraction - 0.02f);
                for (int i = 0; i < randomInRange; i++)
                {
                    float num = (position - this.intendedTarget.CenterVector3).AngleFlat();
                    float velocityAngle = this.Fleck_Angle.RandomInRange + num;
                    float randomInRange2 = this.Fleck_Scale.RandomInRange;
                    float randomInRange3 = this.Fleck_Speed.RandomInRange;
                    float randomInRange4 = this.Fleck_Speed2.RandomInRange;
                    FleckCreationData dataStatic = FleckMaker.GetDataStatic(position, map, this.FleckDef, randomInRange2);
                    FleckCreationData dataStatic2 = FleckMaker.GetDataStatic(position2, map, this.FleckDef2, randomInRange2);
                    dataStatic.rotation = ((position - position2)).AngleFlat();
                    dataStatic.velocityAngle = velocityAngle;
                    dataStatic.velocitySpeed = randomInRange3;
                    dataStatic2.rotation = ((position - position2)).AngleFlat();
                    dataStatic2.velocityAngle = velocityAngle;
                    dataStatic2.velocitySpeed = randomInRange4;
                    map.flecks.CreateFleck(dataStatic2);
                    map.flecks.CreateFleck(dataStatic);
                }
            }
            base.Tick();
        }

        private int tickcount;
        public FleckDef FleckDef = DefDatabase<FleckDef>.GetNamed("CMC_SparkFlash_Blue_Small", true);
        public FleckDef FleckDef2 = DefDatabase<FleckDef>.GetNamed("CMC_SparkFlash_Blue_LongLasting_Small", true);
        public int Fleck_MakeFleckTickMax = 1;
        public IntRange Fleck_MakeFleckNum = new IntRange(2, 2);
        public FloatRange Fleck_Angle = new FloatRange(-180f, 180f);
        public FloatRange Fleck_Scale = new FloatRange(2.2f, 2.3f);
        public FloatRange Fleck_Speed = new FloatRange(5f, 7f);
        public FloatRange Fleck_Speed2 = new FloatRange(0.1f, 0.2f);
        public FloatRange Fleck_Rotation = new FloatRange(-180f, 180f);
        public int Fleck_MakeFleckTick;
    }
}
