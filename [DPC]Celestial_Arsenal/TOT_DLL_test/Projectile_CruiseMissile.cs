using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Verse;

namespace TOT_DLL_test
{
    public class Projectile_CruiseMissile : Projectile_Explosive
    {
        private void RandFactor()
        {
            FloatRange xrange = new FloatRange(-7f, 7f);
            FloatRange zrange = new FloatRange(-7f, 7f);
            Randdd.x = xrange.RandomInRange;
            Randdd.y = 20f;
            Randdd.z = zrange.RandomInRange;
            flag2 = true;
        }
        public Vector3 BPos(float t)
        {
            bool flag = !this.flag2;
            if (flag)
            {
                this.RandFactor();
            }
            Vector3 origin = this.origin;
            Vector3 a = this.origin + new Vector3(0f, 0f, 16f);
            Vector3 a2 = (this.destination + this.origin) / 2f;
            a2.z += 10f;
            a += this.Randdd;
            a2 += this.Randdd;
            Vector3 destination = this.destination;
            return (1f - t) * (1f - t) * (1f - t) * origin + 3f * t * (1f - t) * (1f - t) * a + 3f * t * t * (1f - t) * a2 + t * t * t * destination;
        }
        private void FindNextTarget(Vector3 d)
        {
            IntVec3 intloc = IntVec3.FromVector3(d);
            IEnumerable<IntVec3> celllist = GenRadial.RadialCellsAround(intloc, 7f, true);
            foreach (IntVec3 cell in celllist)
            {
                Pawn suspawn = cell.GetFirstPawn(this.Map);
                if (suspawn != null)
                {
                    if ((suspawn.Faction.HostileTo(this.launcher.Faction) || this.launcher is null) && !suspawn.Downed && !suspawn.Dead)
                    {
                        this.intendedTarget = (LocalTargetInfo)suspawn;
                        return;
                    }
                }
            }
            CellRect cellRect = CellRect.CenteredOn(intloc, 7);
            this.intendedTarget = (LocalTargetInfo)cellRect.RandomCell;
            return;
        }
        protected override void DrawAt(Vector3 position, bool flip = false)
        {
            position2 = BPos(this.DistanceCoveredFraction - 0.01f);
            position = BPos(this.DistanceCoveredFraction);
            this.ExPos = position;
            Quaternion rotation = Quaternion.LookRotation(position - position2);
            Vector3 drawloc = position;
            drawloc.y = AltitudeLayer.Projectile.AltitudeFor();
            Graphics.DrawMesh(MeshPool.GridPlane(this.def.graphicData.drawSize), drawloc, rotation, this.DrawMat, 0);
            base.Comps_PostDraw();
        }
        protected override void Tick()
        {
            if (this.intendedTarget.Thing != null)
            {
                this.HitLoc = this.intendedTarget.Thing.DrawPos;
                if (this.intendedTarget.Thing is Pawn)
                {
                    Pawn pawn = (Pawn)intendedTarget.Thing;
                    if (pawn.Dead || pawn.Downed)
                    {
                        if (this.DistanceCoveredFraction < 0.6)
                        {
                            FindNextTarget(this.HitLoc);
                        }
                    }
                }
                if (this.intendedTarget.Thing != null)
                {
                    this.destination = this.intendedTarget.Thing.DrawPos;
                }
                else
                {
                    this.destination = this.intendedTarget.CenterVector3;
                }
            }
            this.Fleck_MakeFleckTick++;
            bool flag = this.Fleck_MakeFleckTick >= this.Fleck_MakeFleckTickMax;
            if (flag)
            {
                this.Fleck_MakeFleckTick = 0;
                Map map = this.Map;
                int randomInRange = this.Fleck_MakeFleckNum.RandomInRange;
                Vector3 position = BPos(this.DistanceCoveredFraction);
                position2 = BPos(this.DistanceCoveredFraction - 0.01f);
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
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map map1 = this.Map;
            base.Impact(hitThing, blockedByShield);
            IntVec3 position = this.Position;
            Map map2 = map1;
            double explosionRadius = (double)this.def.projectile.explosionRadius;
            DamageDef bomb = DamageDefOf.Bomb;
            Thing launcher = this.launcher;
            int damageAmount = this.DamageAmount;
            double armorPenetration = (double)this.ArmorPenetration;
            ThingDef equipmentDef = this.equipmentDef;
            ThingDef def = this.def;
            Thing thing = this.intendedTarget.Thing;
            GasType? postExplosionGasType = new GasType?();
            float? direction = new float?();
            FloatRange? affectedAngle = new FloatRange?();
            GenExplosion.DoExplosion(position, map2, (float)explosionRadius, bomb, launcher, damageAmount, (float)armorPenetration, weapon: equipmentDef, projectile: def, intendedTarget: thing, postExplosionGasType: postExplosionGasType, direction: direction, affectedAngle: affectedAngle);
            CellRect cellRect = CellRect.CenteredOn(this.Position, 6);
            cellRect.ClipInsideMap(map1);
            for (int index = 0; index < 5; ++index)
                this.DomultiExplosion(cellRect.RandomCell, map1, 1.9f);
        }
        protected void DomultiExplosion(IntVec3 pos, Map map, float radius) => GenExplosion.DoExplosion(pos, map, radius, DamageDefOf.Bomb, this.launcher, 30, this.ArmorPenetration, weapon: this.equipmentDef, projectile: this.def, intendedTarget: this.intendedTarget.Thing);

        public FleckDef FleckDef = DefDatabase<FleckDef>.GetNamed("CMC_SparkFlash_Blue", true);
        public FleckDef FleckDef2 = DefDatabase<FleckDef>.GetNamed("CMC_Fleck_ProjectileSmoke_LongLasting", true);
        public int Fleck_MakeFleckTickMax = 1;
        public IntRange Fleck_MakeFleckNum = new IntRange(2, 2);
        public FloatRange Fleck_Angle = new FloatRange(-180f, 180f);
        public FloatRange Fleck_Scale = new FloatRange(1.3f, 1.5f);
        public FloatRange Fleck_Scale2 = new FloatRange(2.1f, 2.3f);
        public FloatRange Fleck_Speed = new FloatRange(0.1f, 0.2f);
        public FloatRange Fleck_Speed2 = new FloatRange(0.1f, 0.2f);
        public FloatRange Fleck_Rotation = new FloatRange(-180f, 180f);
        public int Fleck_MakeFleckTick;
        private bool flag2 = false;
        private Vector3 Randdd;
        private Vector3 HitLoc;
        private Vector3 position2;
        public Vector3 ExPos;
    }
}
