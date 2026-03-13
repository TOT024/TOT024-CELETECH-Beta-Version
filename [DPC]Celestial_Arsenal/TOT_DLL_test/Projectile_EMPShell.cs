using Verse;
using RimWorld;
using UnityEngine;
using Verse.Sound;

namespace TOT_DLL_test
{
    public class CMC_Projectile_EMPshell : Projectile
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
            if (this.DistanceCoveredFraction > 0.04f)
            {
                Vector3 drawloc = position;
                drawloc.y = AltitudeLayer.Projectile.AltitudeFor();
                Graphics.DrawMesh(MeshPool.GridPlane(this.def.graphicData.drawSize), drawloc, rotation, this.DrawMat, 0);
                base.Comps_PostDraw();
            }
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
            CellRect cellRect = CellRect.CenteredOn(this.Position, 3);
            cellRect.ClipInsideMap(map1);
            for (int index = 0; index < 2; ++index)
                this.DomultiEMPExplosion(cellRect.RandomCell, map1, 1.1f);
        }
        protected override void Tick()
        {
            if (this.landed)
            {
                return;
            }
            Vector3 exactPosition = this.ExactPosition;
            this.ticksToImpact--;
            if (!this.ExactPosition.InBounds(base.Map))
            {
                this.ticksToImpact++;
                base.Position = this.ExactPosition.ToIntVec3();
                this.Destroy(DestroyMode.Vanish);
                return;
            }
            Vector3 exactPosition2 = this.ExactPosition;
            base.Position = this.ExactPosition.ToIntVec3();
            if (this.ticksToImpact == 60 && Find.TickManager.CurTimeSpeed == TimeSpeed.Normal && this.def.projectile.soundImpactAnticipate != null)
            {
                this.def.projectile.soundImpactAnticipate.PlayOneShot(this);
            }
            if (this.ticksToImpact <= 0)
            {
                if (this.DestinationCell.InBounds(base.Map))
                {
                    base.Position = this.DestinationCell;
                }
                this.ImpactSomething();
                return;
            }
        }
        protected void DomultiEMPExplosion(IntVec3 pos, Map map, float radius) => GenExplosion.DoExplosion(pos, map, radius, DamageDefOf.EMP, this.launcher, 30, this.ArmorPenetration, weapon: this.equipmentDef, projectile: this.def, intendedTarget: this.intendedTarget.Thing);
    }
}