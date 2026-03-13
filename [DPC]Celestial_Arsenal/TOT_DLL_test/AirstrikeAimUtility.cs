using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TOT_DLL_test
{
    public static class AirstrikeAimUtility
    {
        private static readonly Vector3[] Dir8 = new Vector3[]
        {
            new Vector3(1f,0f,0f).normalized,
            new Vector3(1f,0f,1f).normalized,
            new Vector3(0f,0f,1f).normalized,
            new Vector3(-1f,0f,1f).normalized,
            new Vector3(-1f,0f,0f).normalized,
            new Vector3(-1f,0f,-1f).normalized,
            new Vector3(0f,0f,-1f).normalized,
            new Vector3(1f,0f,-1f).normalized
        };

        private static readonly List<Thing> tmpHostiles = new List<Thing>(64);
        private static IntVec3 cachedCell = IntVec3.Invalid;
        private static int cachedTick = -9999;
        private static Vector3 cachedDir = Vector3.forward;
        private static bool cachedValid = false;
        public static bool TryPickDirection(Map map, IntVec3 targetCell, AirstrikeConfig cfg, out Vector3 bestDir)
        {
            bestDir = Vector3.zero;
            if (map == null || cfg == null) return false;
            tmpHostiles.Clear();
            HashSet<IAttackTarget> hostileSet = map.attackTargetsCache.TargetsHostileToFaction(Faction.OfPlayer);
            if (hostileSet != null)
            {
                float scanRadius = Mathf.Max(cfg.attackRange + cfg.strafeLength + 8f, 30f);
                foreach (IAttackTarget at in hostileSet)
                {
                    Thing th = at.Thing;
                    if (th == null || th.Destroyed || !th.Spawned) continue;
                    if (!th.Position.InBounds(map)) continue;
                    if (!th.Position.InHorDistOf(targetCell, scanRadius)) continue;

                    tmpHostiles.Add(th);
                    if (tmpHostiles.Count >= 48) break;
                }
            }

            Vector3 center = targetCell.ToVector3Shifted();
            center.y = 0f;

            float halfLen = Mathf.Max(4f, cfg.strafeLength * 0.5f + 2f);
            float halfWid = Mathf.Max(1.5f, cfg.strafeSpread + 1f);

            float bestScore = -1f;
            bool found = false;

            for (int i = 0; i < Dir8.Length; i++)
            {
                Vector3 fwd = Dir8[i];
                Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;
                if (!FirstShotTargetInBounds(map, targetCell, cfg, fwd))
                    continue;

                float score = 0f;
                if (tmpHostiles.Count == 0)
                {
                    score = 0.1f;
                }
                else
                {
                    for (int j = 0; j < tmpHostiles.Count; j++)
                    {
                        Vector3 d = tmpHostiles[j].DrawPos;
                        d.y = 0f;
                        d -= center;

                        float along = Mathf.Abs(Vector3.Dot(d, fwd));
                        float lateral = Mathf.Abs(Vector3.Dot(d, right));
                        if (along <= halfLen && lateral <= halfWid)
                        {
                            float w1 = 1f - (lateral / halfWid) * 0.6f;
                            float w2 = 1f - (along / halfLen) * 0.25f;
                            score += Mathf.Max(0.1f, w1 * w2);
                        }
                    }
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestDir = fwd;
                    found = true;
                }
            }

            if (!found)
            {
                for (int i = 0; i < Dir8.Length; i++)
                {
                    if (FirstShotTargetInBounds(map, targetCell, cfg, Dir8[i]))
                    {
                        bestDir = Dir8[i];
                        return true;
                    }
                }
                return false;
            }

            return true;
        }

        private static bool FirstShotTargetInBounds(Map map, IntVec3 targetCell, AirstrikeConfig cfg, Vector3 forward)
        {
            float progress = (cfg.burstCount > 1) ? (-0.5f) : 0f;
            Vector3 target = targetCell.ToVector3Shifted();
            Vector3 firstImpact = target + forward.normalized * (progress * cfg.strafeLength);

            IntVec3 firstCell = firstImpact.ToIntVec3();
            return firstCell.InBounds(map);
        }

        public static Vector3 GetMapEdgePointByDirection(Vector3 origin, Vector3 dir, float mapWidth, float mapHeight, float buffer)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector3.forward;
            dir.Normalize();

            float tx = float.PositiveInfinity;
            float tz = float.PositiveInfinity;

            if (Mathf.Abs(dir.x) > 0.0001f)
            {
                float t1 = (-buffer - origin.x) / dir.x;
                float t2 = (mapWidth + buffer - origin.x) / dir.x;
                if (t1 > 0f) tx = Mathf.Min(tx, t1);
                if (t2 > 0f) tx = Mathf.Min(tx, t2);
            }

            if (Mathf.Abs(dir.z) > 0.0001f)
            {
                float t3 = (-buffer - origin.z) / dir.z;
                float t4 = (mapHeight + buffer - origin.z) / dir.z;
                if (t3 > 0f) tz = Mathf.Min(tz, t3);
                if (t4 > 0f) tz = Mathf.Min(tz, t4);
            }

            float t = Mathf.Min(tx, tz);
            if (float.IsInfinity(t) || float.IsNaN(t)) t = Mathf.Max(mapWidth, mapHeight) + buffer;

            return origin + dir * t;
        }
        public static void DrawStrikePreview(LocalTargetInfo t, Map map, AirstrikeConfig cfg)
        {
            if (!t.IsValid || map == null || cfg == null) return;
            if (!t.Cell.InBounds(map)) return;

            IntVec3 cell = t.Cell;
            float y = AltitudeLayer.MetaOverlays.AltitudeFor();
            Vector3 center = cell.ToVector3Shifted();
            center.y = y;
            bool singleShot = (cfg.burstCount <= 1) || (cfg.strafeLength <= 0.1f);
            if (singleShot)
            {
                float radius = Mathf.Max(1f, cfg.attackRange);
                DrawCircleWhiteThick(center, radius, 48, 0.38f);
                return;
            }
            int now = Find.TickManager.TicksGame;
            Vector3 fwd;

            if (cell == cachedCell && now - cachedTick <= 8 && cachedValid)
            {
                fwd = cachedDir;
            }
            else
            {
                cachedValid = TryPickDirection(map, cell, cfg, out fwd);
                if (!cachedValid) fwd = Vector3.forward;
                cachedCell = cell;
                cachedTick = now;
                cachedDir = fwd;
            }
            fwd.y = 0f;
            fwd.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;

            float halfLen = Mathf.Max(2f, cfg.strafeLength * 0.5f);
            float halfWid = Mathf.Max(1f, cfg.strafeSpread);

            Vector3 p1 = center + fwd * halfLen + right * halfWid;
            Vector3 p2 = center + fwd * halfLen - right * halfWid;
            Vector3 p3 = center - fwd * halfLen - right * halfWid;
            Vector3 p4 = center - fwd * halfLen + right * halfWid;

            p1.y = y; p2.y = y; p3.y = y; p4.y = y;

            DrawRectWhiteThick(p1, p2, p3, p4, 0.38f);
        }
        public static void DrawDirectionPickPreviewWithAnchor(IntVec3 fixedCell, LocalTargetInfo dirTarget, Map map, AirstrikeConfig cfg)
        {
            if (map == null || cfg == null) return;
            if (!fixedCell.InBounds(map)) return;

            float y = AltitudeLayer.MetaOverlays.AltitudeFor();
            Vector3 center = fixedCell.ToVector3Shifted();
            center.y = y;

            DrawAnchorCrossWhite(center, 1.15f, 0.42f);

            bool singleShot = (cfg.burstCount <= 1) || (cfg.strafeLength <= 0.1f);
            if (singleShot)
            {
                float radius = Mathf.Max(1f, cfg.attackRange);
                DrawCircleWhiteThick(center, radius, 48, 0.38f);
                return;
            }

            Vector3 dir = Vector3.forward;
            if (dirTarget.IsValid)
            {
                dir = dirTarget.Cell.ToVector3Shifted() - fixedCell.ToVector3Shifted();
                dir.y = 0f;
                if (dir.sqrMagnitude < 0.001f) dir = Vector3.forward;
                dir.Normalize();
            }

            Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;

            float halfLen = Mathf.Max(2f, cfg.strafeLength * 0.5f);
            float halfWid = Mathf.Max(1f, cfg.strafeSpread);

            Vector3 p1 = center + dir * halfLen + right * halfWid;
            Vector3 p2 = center + dir * halfLen - right * halfWid;
            Vector3 p3 = center - dir * halfLen - right * halfWid;
            Vector3 p4 = center - dir * halfLen + right * halfWid;

            p1.y = y; p2.y = y; p3.y = y; p4.y = y;

            GenDraw.DrawLineBetween(p1, p2, SimpleColor.White, 0.38f);
            GenDraw.DrawLineBetween(p2, p3, SimpleColor.White, 0.38f);
            GenDraw.DrawLineBetween(p3, p4, SimpleColor.White, 0.38f);
            GenDraw.DrawLineBetween(p4, p1, SimpleColor.White, 0.38f);
        }

        private static void DrawAnchorCrossWhite(Vector3 center, float halfSize, float width)
        {
            Vector3 a1 = new Vector3(center.x - halfSize, center.y, center.z);
            Vector3 a2 = new Vector3(center.x + halfSize, center.y, center.z);
            Vector3 b1 = new Vector3(center.x, center.y, center.z - halfSize);
            Vector3 b2 = new Vector3(center.x, center.y, center.z + halfSize);

            GenDraw.DrawLineBetween(a1, a2, SimpleColor.White, width);
            GenDraw.DrawLineBetween(b1, b2, SimpleColor.White, width);
        }
        public static void DrawDirectionPickPreview(IntVec3 centerCell, LocalTargetInfo dirTarget, Map map, AirstrikeConfig cfg)
        {
            if (map == null || cfg == null || !centerCell.InBounds(map)) return;
            if (cfg.burstCount <= 1 || cfg.strafeLength <= 0.1f)
            {
                DrawCircleWhiteThick(centerCell.ToVector3Shifted(), Mathf.Max(1f, cfg.attackRange), 48, 0.38f);
                return;
            }

            Vector3 center = centerCell.ToVector3Shifted();
            center.y = AltitudeLayer.MetaOverlays.AltitudeFor();

            Vector3 dir = Vector3.forward;
            if (dirTarget.IsValid)
            {
                dir = (dirTarget.Cell.ToVector3Shifted() - centerCell.ToVector3Shifted());
                dir.y = 0f;
                if (dir.sqrMagnitude < 0.001f) dir = Vector3.forward;
                dir.Normalize();
            }

            Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;
            float halfLen = Mathf.Max(2f, cfg.strafeLength * 0.5f);
            float halfWid = Mathf.Max(1f, cfg.strafeSpread);

            Vector3 p1 = center + dir * halfLen + right * halfWid;
            Vector3 p2 = center + dir * halfLen - right * halfWid;
            Vector3 p3 = center - dir * halfLen - right * halfWid;
            Vector3 p4 = center - dir * halfLen + right * halfWid;
            p1.y = p2.y = p3.y = p4.y = AltitudeLayer.MetaOverlays.AltitudeFor();

            GenDraw.DrawLineBetween(p1, p2, SimpleColor.White, 0.38f);
            GenDraw.DrawLineBetween(p2, p3, SimpleColor.White, 0.38f);
            GenDraw.DrawLineBetween(p3, p4, SimpleColor.White, 0.38f);
            GenDraw.DrawLineBetween(p4, p1, SimpleColor.White, 0.38f);
        }
        private static void DrawRectWhiteThick(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, float width)
        {
            GenDraw.DrawLineBetween(p1, p2, SimpleColor.White, width);
            GenDraw.DrawLineBetween(p2, p3, SimpleColor.White, width);
            GenDraw.DrawLineBetween(p3, p4, SimpleColor.White, width);
            GenDraw.DrawLineBetween(p4, p1, SimpleColor.White, width);
        }
        private static void DrawCircleWhiteThick(Vector3 center, float radius, int segments, float width)
        {
            if (segments < 12) segments = 12;
            float step = 360f / segments;

            Vector3 prev = center + new Vector3(radius, 0f, 0f);
            for (int i = 1; i <= segments; i++)
            {
                float a = step * i * Mathf.Deg2Rad;
                Vector3 cur = center + new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
                GenDraw.DrawLineBetween(prev, cur, SimpleColor.White, width);
                prev = cur;
            }
        }
    }
}