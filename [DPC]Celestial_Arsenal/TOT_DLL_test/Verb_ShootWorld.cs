using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TOT_DLL_test
{
    public class Verb_ShootWorld : Verb_Shoot
    {
        public LocalTargetInfo TryRandomTarget()
        {
            MapParent mapParent = GameComponent_CeleTech.Instance?.ASEA_observedMap;
            Map map = mapParent?.Map;
            if (map == null) return LocalTargetInfo.Invalid;

            var targets = map.attackTargetsCache.TargetsHostileToFaction(caster.Faction);
            if (targets != null && targets.Count > 0)
            {
                IAttackTarget t = targets.RandomElement();
                if (t?.Thing != null) return t.Thing;
            }
            return map.AllCells.RandomElement();
        }

        public bool TryCastFireMission()
        {
            MapParent mapParent = GameComponent_CeleTech.Instance?.ASEA_observedMap;
            Map targetMap = mapParent?.Map;
            Building_CMCTurretGun_MainBattery building = caster as Building_CMCTurretGun_MainBattery;

            if (building == null || building.Map == null) return false;
            if (mapParent == null || targetMap == null) return false;

            Vector3 a = Vector3.forward.RotatedBy(building.turrettop.DestRotation);
            IntVec3 c = (building.DrawPos + a * 500f).ToIntVec3();

            LocalTargetInfo randomTarget = TryRandomTarget();
            IntVec3 destCell = randomTarget.IsValid ? randomTarget.Cell : CellRect.WholeMap(targetMap).CenterCell;

            WorldObject_EMLShell worldObject = (WorldObject_EMLShell)WorldObjectMaker.MakeWorldObject(
                DefDatabase<WorldObjectDef>.GetNamed("CMC_EMLShell", true));

            worldObject.railgun = building;
            worldObject.Tile = building.Map.Tile;
            worldObject.destinationTile = mapParent.Tile; 
            worldObject.destinationCell = destCell;
            worldObject.spread = 2;
            worldObject.Projectile = this.Projectile;

            Find.WorldObjects.Add(worldObject);
            Find.CameraDriver.shaker.SetMinShake(0.1f);
            Projectile muzzleProj = GenSpawn.Spawn(this.Projectile, building.Position, caster.Map, WipeMode.Vanish) as Projectile;
            if (muzzleProj != null)
            {
                muzzleProj.Launch(building, building.DrawPos, c, LocalTargetInfo.Invalid, ProjectileHitFlags.None, false, EquipmentSource, null);
            }

            ThingWithComps equipmentSource = EquipmentSource;
            CompChangeableProjectile comp = equipmentSource?.GetComp<CompChangeableProjectile>();
            comp?.Notify_ProjectileLaunched();

            return true;
        }
    }
}