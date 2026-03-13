using UnityEngine;
using RimWorld;
using Verse;
using Verse.Sound;
using System.Collections.Generic;
using Verse.Noise;
using RimWorld.Planet;

namespace TOT_DLL_test
{
    public class Projectile_ChargedThunderer : Projectile_ChargedNormal
    {
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Thing thing = hitThing as Thing;
            if(hitThing!=null && (hitThing.Faction == null || hitThing.Faction!=null && hitThing.Faction.HostileTo(this.launcher.Faction)))
            {
                if (Rand.Chance(0.1f))
                {
                    Map.weatherManager.eventHandler.AddEvent(new WeatherEvent_LightningStrike(Map, Position));
                    GenExplosion.DoExplosion(Position, Map, 2.5f, DamageDefOf.EMP, this.launcher, 30, 99f, null, null, null, null, null, 0f, 1, null, null, 255, false, null, 0f, 1, 0f, false, null, null, null, true, 1f, 0f, true, null, 1f, null, null);
                }
            }
            base.Impact(hitThing, blockedByShield);
        }
        public static void DoStrike(IntVec3 strikeLoc, Map map, ref Mesh boltMesh)
        {
            SoundDefOf.Thunder_OffMap.PlayOneShotOnCamera(map);
            if (!strikeLoc.IsValid)
            {
                strikeLoc = CellFinderLoose.RandomCellWith((IntVec3 sq) => sq.Standable(map) && !map.roofGrid.Roofed(sq), map, 1000);
            }
            boltMesh = LightningBoltMeshPool.RandomBoltMesh;
            if (!strikeLoc.Fogged(map))
            {
                GenExplosion.DoExplosion(strikeLoc, map, 1.9f, DamageDefOf.Flame, null, -1, -1f, null, null, null, null, null, 0f, 1, null, null, 255, false, null, 0f, 1, 0f, false, null, null, null, true, 1f, 0f, true, null, 1f, null, null);
                Vector3 loc = strikeLoc.ToVector3Shifted();
                for (int i = 0; i < 4; i++)
                {
                    FleckMaker.ThrowSmoke(loc, map, 1.5f);
                    FleckMaker.ThrowMicroSparks(loc, map);
                    FleckMaker.ThrowLightningGlow(loc, map, 1.5f);
                }
            }
            SoundInfo info = SoundInfo.InMap(new TargetInfo(strikeLoc, map, false), MaintenanceType.None);
            SoundDefOf.Thunder_OnMap.PlayOneShot(info);
        }
        private void NotifyImpact(Thing hitThing, Map map, IntVec3 position)
        {
            BulletImpactData impactData = new BulletImpactData
            {
                bullet = this,
                hitThing = hitThing,
                impactPosition = position
            };
            if (hitThing != null)
            {
                hitThing.Notify_BulletImpactNearby(impactData);
            }
            int num = 9;
            for (int i = 0; i < num; i++)
            {
                IntVec3 c = position + GenRadial.RadialPattern[i];
                if (c.InBounds(map))
                {
                    List<Thing> thingList = c.GetThingList(map);
                    for (int j = 0; j < thingList.Count; j++)
                    {
                        if (thingList[j] != hitThing)
                        {
                            thingList[j].Notify_BulletImpactNearby(impactData);
                        }
                    }
                }
            }
        }
    }
}
