using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace TOT_DLL_test
{
    [StaticConstructorOnStartup]
    public class Comp_SpawnFleck_Projectile : ThingComp
    {
        public CompProperties_SpawnFleck_Projectile Props
        {
            get
            {
                return this.props as CompProperties_SpawnFleck_Projectile;
            }
        }
        private Projectile Projectile
        {
            get
            {
                return this.parent as Projectile;
            }
        }
        public override void CompTick()
        {
            base.CompTick();
            this.Tick_SpawnFleck();
        }

        public void Tick_SpawnFleck()
        {
            this.Fleck_MakeFleckTick++;
            bool flag = this.Fleck_MakeFleckTick >= this.Props.Fleck_MakeFleckTickMax;
            if (flag)
            {
                this.Fleck_MakeFleckTick = 0;
                Map map = this.Projectile.Map;
                int randomInRange = this.Props.Fleck_MakeFleckNum.RandomInRange;
                Vector3 position = this.Projectile.DrawPos;
                for (int i = 0; i < randomInRange; i++)
                {
                    float num = (position - this.Projectile.intendedTarget.CenterVector3).AngleFlat();
                    float velocityAngle = this.Props.Fleck_Angle.RandomInRange + num;
                    float randomInRange2 = this.Props.Fleck_Scale.RandomInRange;
                    float randomInRange3 = this.Props.Fleck_Speed.RandomInRange;
                    FleckCreationData dataStatic = FleckMaker.GetDataStatic(position, map, this.Props.FleckDef, randomInRange2);
                    dataStatic.rotationRate = this.Props.Fleck_Rotation.RandomInRange;
                    dataStatic.velocityAngle = velocityAngle;
                    dataStatic.velocitySpeed = randomInRange3;
                    map.flecks.CreateFleck(dataStatic);
                }
            }
        }

        public int Fleck_MakeFleckTick;
    }
}
