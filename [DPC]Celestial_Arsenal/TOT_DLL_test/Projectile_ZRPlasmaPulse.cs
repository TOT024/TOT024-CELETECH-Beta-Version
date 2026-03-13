using Verse;
using RimWorld;

namespace TOT_DLL_test
{
    public class CMC_Projectile_ZRPlasmaPulse : Projectile_PlasmaShell
    {
        public override bool AnimalsFleeImpact => true;
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
            CellRect cellRect = CellRect.CenteredOn(this.Position, 5);
            cellRect.ClipInsideMap(map1);
            for (int index = 0; index < 3; ++index)
            {

                for (int indexj = 0; indexj < 2; ++indexj)
                {
                    this.DomultiEMPExplosion(cellRect.RandomCell, map1, 1.0f);
                }
                this.DomultiFlameExplosion(cellRect.RandomCell, map1, 1.0f);
                this.DomultiBombExplosion(cellRect.RandomCell, map1, 1.5f);
                
            }
                
        }

        protected void DomultiBombExplosion(IntVec3 pos, Map map, float radius) => GenExplosion.DoExplosion(pos, map, radius, DamageDefOf.Bomb, this.launcher, this.DamageAmount, this.ArmorPenetration, weapon: this.equipmentDef, projectile: this.def, intendedTarget: this.intendedTarget.Thing);
        protected void DomultiFlameExplosion(IntVec3 pos, Map map, float radius) => GenExplosion.DoExplosion(pos, map, radius, DamageDefOf.Flame, this.launcher, this.DamageAmount, this.ArmorPenetration, weapon: this.equipmentDef, projectile: this.def, intendedTarget: this.intendedTarget.Thing);
        protected void DomultiEMPExplosion(IntVec3 pos, Map map, float radius) => GenExplosion.DoExplosion(pos, map, radius, DamageDefOf.EMP, this.launcher, this.DamageAmount, this.ArmorPenetration, weapon: this.equipmentDef, projectile: this.def, intendedTarget: this.intendedTarget.Thing);
    }
}