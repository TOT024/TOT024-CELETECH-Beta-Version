using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace TOT_DLL_test
{
    [StaticConstructorOnStartup]
    public class WorldObject_EMLShell : WorldObject
    {
        private float TravelSpeedInt = 0f;
        private bool arrived;
        private float traveledPct;
        public PlanetTile destinationTile = PlanetTile.Invalid;
        private PlanetTile initialTile = PlanetTile.Invalid;
        private int trailSampleTick;
        public IntVec3 destinationCell = IntVec3.Invalid;
        public ThingDef Projectile; 
        public Thing railgun;
        public int spread = 1;

        private PlanetTile StartTile
        {
            get
            {
                if (initialTile.Valid) return initialTile;
                if (Tile.Valid) return Tile;
                return PlanetTile.Invalid;
            }
        }

        private PlanetTile EndTile
        {
            get
            {
                if (destinationTile.Valid) return destinationTile;
                return PlanetTile.Invalid;
            }
        }
        private readonly List<Vector3> trail = new List<Vector3>();
        private static readonly Material TrailMat = MaterialPool.MatFrom(
            GenDraw.LineTexPath,
            ShaderDatabase.WorldOverlayTransparent,
            new Color(0.67f, 0.98f, 0.98f, 1f),
            3590
        );
        public override void Draw()
        {
            float w = TrailWidthByZoom();
            float size = 0.22f * Tile.Layer.AverageTileSize * w;
            float alt = DrawAltitude + def.drawAltitudeOffset;
            Material mat = Material;
            if (mat != null)
            {
                WorldRendererUtility.DrawQuadTangentialToPlanet(DrawPos, size, alt, mat);
            }
            for (int i = 1; i < trail.Count; i++)
            {
                GenDraw.DrawWorldLineBetween(trail[i - 1], trail[i], TrailMat, w);
            }
        }
        private float TraveledPctStepPerTick
        {
            get
            {
                Vector3 start = Start;
                Vector3 end = End;
                if (start == end) return 1f;

                float dist = GenMath.SphericalDistance(start.normalized, end.normalized);
                if (dist <= 0f) return 1f;

                return TravelSpeed / dist;
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref destinationTile, "destinationTile", PlanetTile.Invalid);
            Scribe_Values.Look(ref destinationCell, "destinationCell", IntVec3.Invalid);
            Scribe_Values.Look(ref arrived, "arrived", false);
            Scribe_Values.Look(ref initialTile, "initialTile", PlanetTile.Invalid);
            Scribe_Values.Look(ref traveledPct, "traveledPct", 0f);
            Scribe_Defs.Look(ref Projectile, "Projectile");
            Scribe_References.Look(ref railgun, "railgun");
            Scribe_Values.Look(ref spread, "spread", 1);
            Scribe_Values.Look(ref arcfacInt, "arcfacInt", 0f);
            Scribe_Values.Look(ref TravelSpeedInt, "travelspeed", 0f);
        }
        public override void PostAdd()
        {
            base.PostAdd();
            initialTile = Tile;
        }

        protected override void Tick()
        {
            base.Tick();
            if (!StartTile.Valid || !EndTile.Valid)
            {
                SafeRemoveSelf();
                return;
            }
            traveledPct += TraveledPctStepPerTick;
            if (traveledPct >= 1f)
            {
                traveledPct = 1f;
                Arrived();
            }
            trailSampleTick++;
            if (trailSampleTick >= 4)
            {
                trailSampleTick = 0;
                float minDist = TrailMinDistByZoom();
                Vector3 p = DrawPos;
                if (trail.Count == 0 || (trail[trail.Count - 1] - p).sqrMagnitude > minDist * minDist)
                {
                    trail.Add(p);
                    //if (trail.Count > TrailMaxPoints) trail.RemoveAt(0);
                }
            }
        }
        private void Arrived()
        {
            if (arrived) return;
            arrived = true;

            Map map = ResolveTargetMap();
            if (map == null)
            {
                SafeRemoveSelf();
                return;
            }

            if (Projectile == null)
            {
                Log.Error("[EMLShell] Projectile is null.");
                SafeRemoveSelf();
                return;
            }

            IntVec3 spawnCell = CellRect.WholeMap(map).CenterCell;

            IntVec3 targetCell = destinationCell;
            if (!targetCell.IsValid || !targetCell.InBounds(map))
            {
                targetCell = spawnCell;
            }

            if (spread > 0)
            {
                if (!CellFinder.TryFindRandomCellNear(targetCell, map, spread, null, out IntVec3 c, -1))
                {
                    c = targetCell;
                }
                targetCell = c;
            }

            Thing spawned = GenSpawn.Spawn(Projectile, spawnCell, map, WipeMode.Vanish);
            Projectile projectile = spawned as Projectile;
            if (projectile == null)
            {
                Log.Error($"[EMLShell] {Projectile.defName} is not a Projectile.");
                spawned.Destroy();
                SafeRemoveSelf();
                return;
            }

            projectile.Launch(railgun ?? projectile, targetCell, targetCell, ProjectileHitFlags.IntendedTarget, false, null);
            SafeRemoveSelf();
        }
        private Vector3 PositionAt(float t)
        {
            Vector3 s = Start;
            Vector3 e = End;

            if (!StartTile.Valid || !EndTile.Valid) return s;
            if (StartTile.Layer == EndTile.Layer)
            {
                Vector3 origin = StartTile.Layer.Origin;
                Vector3 ls = (s - origin).normalized;
                Vector3 le = (e - origin).normalized;
                Vector3 local = Vector3.Slerp(ls, le, t);
                float arc = Mathf.Sin(t * Mathf.PI) * ArcFactor;
                return origin + local * (StartTile.Layer.Radius * (1f + arc));
            }
            return Vector3.Lerp(s, e, t);
        }
        public override Vector3 DrawPos => PositionAt(traveledPct);
        private Vector3 Start => StartTile.Valid ? WorldPos(StartTile) : Vector3.zero;
        private Vector3 End => EndTile.Valid ? WorldPos(EndTile) : Start;
        private static Vector3 WorldPos(PlanetTile t)
        {
            return t.Layer.Origin + Find.WorldGrid.GetTileCenter(t);
        }
        private Map ResolveTargetMap()
        {
            if (!destinationTile.Valid) return null;
            return Find.Maps.FirstOrDefault(m => m.Tile == destinationTile);
        }
        private float TrailMinDistByZoom()
        {
            float z = Find.WorldCameraDriver.AltitudePercent;
            return Mathf.Lerp(0.01f, 0.25f, z); 
        }
        private float TrailWidthByZoom()
        {
            float z = Find.WorldCameraDriver.AltitudePercent; 
            return Mathf.Lerp(1.2f, 16f, z); 
        }
        private void SafeRemoveSelf()
        {
            if (Spawned)
            {
                Find.WorldObjects.Remove(this);
            }
        }
        private float arcfacInt = 0f;
        private float ArcFactor
        {
            get
            {
                if(arcfacInt <= 0f)
                {
                    arcfacInt = Rand.Range(0.16f, 0.24f);
                }
                return arcfacInt;
            }
        }
        private float TravelSpeed
        {
            get
            {
                if (TravelSpeedInt <= 0f)
                {
                    TravelSpeedInt = Rand.Range(0.00052f, 0.00057f);
                }
                return TravelSpeedInt;
            }
        }
    }
}