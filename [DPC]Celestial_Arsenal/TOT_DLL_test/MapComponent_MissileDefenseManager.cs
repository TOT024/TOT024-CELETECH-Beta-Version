using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace TOT_DLL_test
{
    public class MissileDefenseManager : MapComponent
    {
        private static readonly AccessTools.FieldRef<Projectile, int> GetTicksToImpact =
            AccessTools.FieldRefAccess<Projectile, int>("ticksToImpact");

        private class TrackedMissile
        {
            public Projectile missile;
            public int incomingInterceptors;

            public bool IsValid =>
                missile != null &&
                !missile.Destroyed &&
                missile.Spawned;
        }

        private readonly List<TrackedMissile> incomingMissiles = new List<TrackedMissile>();
        private readonly HashSet<int> trackedMissileIds = new HashSet<int>();

        private readonly List<Building_CMCTurretGun_AAAS> turrets = new List<Building_CMCTurretGun_AAAS>();

        private int radarCount = 0;
        public bool HasActiveRadar => radarCount > 0;

        public MissileDefenseManager(Map map) : base(map) { }

        public void RegisterRadar(bool isAdd)
        {
            radarCount += isAdd ? 1 : -1;
            radarCount = Mathf.Max(0, radarCount);
        }

        public void RegisterTurret(Building_CMCTurretGun_AAAS turret)
        {
            if (turret == null) return;
            if (!turrets.Contains(turret)) turrets.Add(turret);
        }
        public void UnregisterTurret(Building_CMCTurretGun_AAAS turret)
        {
            if (turret == null) return;
            turrets.Remove(turret);
        }
        public void RegisterIncomingMissile(Projectile missile)
        {
            if (missile == null || missile.Destroyed || !missile.Spawned) return;

            int id = missile.thingIDNumber;
            if (!trackedMissileIds.Add(id)) return;

            incomingMissiles.Add(new TrackedMissile
            {
                missile = missile,
                incomingInterceptors = 0
            });
        }
        public override void MapComponentTick()
        {
            base.MapComponentTick();
            if (Find.TickManager.TicksGame % 4 != 0) return;
            if (!HasActiveRadar) return;
            if (incomingMissiles.Count == 0) return;
            for (int i = incomingMissiles.Count - 1; i >= 0; i--)
            {
                var tr = incomingMissiles[i];
                if (!tr.IsValid)
                {
                    if (tr.missile != null)
                        trackedMissileIds.Remove(tr.missile.thingIDNumber);
                    incomingMissiles.RemoveAt(i);
                }
            }
            if (incomingMissiles.Count == 0) return;
            turrets.RemoveAll(t => t == null || t.DestroyedOrNull() || !t.Spawned);

            var availableTurrets = turrets
            .Where(t => t != null
                     && t.Spawned
                     && !t.Destroyed
                     && t.Active
                     && t.AttackVerb != null
                     && t.AttackVerb.Available())
            .ToList();
            if (availableTurrets.Count == 0) return;
            incomingMissiles.Sort((a, b) => GetEtaSafe(a.missile).CompareTo(GetEtaSafe(b.missile)));
            AssignTurretsToMissiles(availableTurrets, 0);
            if (availableTurrets.Count > 0 && availableTurrets.Count > incomingMissiles.Count)
            {
                AssignTurretsToMissiles(availableTurrets, 1);
            }
        }
        private void AssignTurretsToMissiles(List<Building_CMCTurretGun_AAAS> availableTurrets, int targetInterceptorCount)
        {
            Faction playerFaction = Faction.OfPlayer;
            var missilesSnapshot = incomingMissiles.ToList();
            foreach (var tracker in missilesSnapshot)
            {
                if (availableTurrets.Count == 0) break;
                if (!tracker.IsValid) continue;
                Projectile missile = tracker.missile;
                int eta = GetEtaSafe(missile);
                //if (eta < 3) continue;
                Faction missileFaction = missile.Faction ?? missile.Launcher?.Faction;
                if (missileFaction == playerFaction) continue;
                if (missileFaction != null && !missileFaction.HostileTo(playerFaction)) continue;
                if (tracker.incomingInterceptors != targetInterceptorCount) continue;

                Building_CMCTurretGun_AAAS bestTurret = null;
                float bestScore = float.MaxValue;
                for (int i = availableTurrets.Count - 1; i >= 0; i--)
                {
                    var turret = availableTurrets[i];
                    if (!turret.CanEngageTarget(tracker.missile)) continue;
                    float interceptTicks = EstimateInterceptTicks_NoRotation(turret, missile);
                    if (interceptTicks > eta) continue;
                    if (interceptTicks < bestScore)
                    {
                        bestScore = interceptTicks;
                        bestTurret = turret;
                    }
                }
                if (bestTurret != null)
                {
                    Log.Message(bestTurret.ToString() + tracker.missile.ToString());
                    bestTurret.OrderAttack(tracker.missile);
                    tracker.incomingInterceptors++;
                    availableTurrets.Remove(bestTurret);
                }
            }
        }
        private int GetEtaSafe(Projectile missile)
        {
            if (missile == null || missile.Destroyed || !missile.Spawned) return int.MaxValue;

            try
            {
                int tti = GetTicksToImpact(missile);
                return tti > 0 ? tti : int.MaxValue;
            }
            catch
            {
                return int.MaxValue;
            }
        }
        private float EstimateInterceptTicks_NoRotation(Building_CMCTurretGun_AAAS turret, Projectile missile)
        {
            if (turret?.AttackVerb == null) return 9999f;

            ThingDef projDef = turret.AttackVerb.GetProjectile();
            if (projDef?.projectile == null) return 9999f;

            float shotSpeed = projDef.projectile.SpeedTilesPerTick;
            if (shotSpeed <= 0f) return 9999f;

            float dist = (turret.DrawPos - missile.ExactPosition).magnitude;
            return dist / shotSpeed + 5f;
        }
    }
}