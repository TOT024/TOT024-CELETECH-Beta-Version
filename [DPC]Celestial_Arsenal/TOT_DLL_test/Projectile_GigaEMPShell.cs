using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse.Sound;
using Verse;
using RimWorld;

namespace TOT_DLL_test
{
    public class Projectile_GigaEMPShell : CMC_Projectile_EMPshell
    {
        protected override void Tick()
        {
            this.tickcount++;
            if (this.landed)
            {
                return;
            }
            if(this.tickcount < 4)
            {
                FleckMaker.ThrowFireGlow(DrawPos, Map, 1.5f);
                FleckMaker.ThrowMicroSparks(DrawPos, Map);
                FleckMaker.ThrowDustPuff(Position, Map, 2f);
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
            for (int index = 0; index < 2; ++index)
                this.DomultiEMPExplosion(cellRect.RandomCell, map1, 4.3f);
        }
        private int tickcount = 0;
    }
}
