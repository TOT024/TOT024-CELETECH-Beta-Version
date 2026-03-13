using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace TOT_DLL_test
{
    public class AirstrikeConfig : IExposable
    {
        public string projectileDefName = "CMC_Missile_ASG";
        public float flightAltitude = 15f;
        public float speed = 50f;
        public float attackRange = 75f;
        public int burstCount = 12;
        public int burstInterval = 3;
        public float strafeLength = 25f;
        public float strafeSpread = 3f;
        public bool homing = false;
        public int projectilepershot = 1;
        public void ExposeData()
        {
            Scribe_Values.Look(ref projectileDefName, "projectileDefName", "CMC_Missile_ASG");
            Scribe_Values.Look(ref flightAltitude, "flightAltitude", 15f);
            Scribe_Values.Look(ref speed, "speed", 50f);
            Scribe_Values.Look(ref attackRange, "attackRange", 75f);
            Scribe_Values.Look(ref burstCount, "burstCount", 12);
            Scribe_Values.Look(ref burstInterval, "burstInterval", 3);
            Scribe_Values.Look(ref strafeLength, "strafeLength", 25f);
            Scribe_Values.Look(ref strafeSpread, "strafeSpread", 3f);
            Scribe_Values.Look(ref homing, "homing", false);
            Scribe_Values.Look(ref projectilepershot, "projectilepershot", 1);
        }
    }
    [StaticConstructorOnStartup]
    public class AirplaneAttackFlyer : Thing
        {
            public AirstrikeConfig config;

            public float FlightAltitude = 15f;
            public float Speed = 50f;
            public float AttackRange = 75f;
            public int BurstCount = 12;
            public int BurstInterval = 3;
            public float StrafeLength = 25f;
            public float StrafeSpread = 3f;
            public int Projectilepershot = 1;

            private Vector3 startPos;
            private Vector3 endPos;
            private Vector3 flightDir;
            private float totalDist;
            private float targetAlongDist;

            private IntVec3 targetCell;
            private Thing targetThing;

            private int ticksFlying;
            private int totalDurationTicks;
            private bool initialized;

            private int shotsFired;
            private int ticksUntilNextShot;

            private Vector3 drawPos;
            private float drawAngle;

            private float currentAltitude;
            private float altitudeVelocity;

            private Material cachedMat;
            private Sustainer sustainer;
            private Map cachedMap;

            private static readonly Material ShadowMat = MaterialPool.MatFrom("Things/Aerocraft/JX_Skyfall_Shadow", ShaderDatabase.Transparent, new Color(0f, 0f, 0f, 0.5f));
            private static readonly Material LightMat = MaterialPool.MatFrom("Things/Aerocraft/JX_Skyfall_Light", ShaderDatabase.TransparentPostLight, new Color(1f, 1f, 1f, 1f));

            private readonly Vector3 LeftWeaponOffset = new Vector3(-2f, 0f, 0.5f);
            private readonly Vector3 RightWeaponOffset = new Vector3(2f, 0f, 0.5f);
            private readonly Vector3 EngineAOffset = new Vector3(0.33f, 0f, -2.5f);
            private readonly Vector3 EngineBOffset = new Vector3(-0.33f, 0f, -2.5f);

            private static FleckDef engineGlowDef;

            private static readonly Vector3[] SampleDirs = new Vector3[]
            {
            new Vector3(1f,0f,0f).normalized,
            new Vector3(0.866f,0f,0.5f).normalized,
            new Vector3(0.5f,0f,0.866f).normalized,
            new Vector3(0f,0f,1f).normalized,
            new Vector3(-0.5f,0f,0.866f).normalized,
            new Vector3(-0.866f,0f,0.5f).normalized,
            new Vector3(-1f,0f,0f).normalized,
            new Vector3(-0.866f,0f,-0.5f).normalized,
            new Vector3(-0.5f,0f,-0.866f).normalized,
            new Vector3(0f,0f,-1f).normalized,
            new Vector3(0.5f,0f,-0.866f).normalized,
            new Vector3(0.866f,0f,-0.5f).normalized
            };

            private static readonly List<Thing> TmpHostiles = new List<Thing>(64);

            public void SetupAttackRun(IntVec3 target, Map map, AirstrikeConfig incomingConfig)
            {
                SetupAttackRun(new LocalTargetInfo(target), map, incomingConfig, false, Vector3.zero);
            }
            public void SetupAttackRun(LocalTargetInfo targetInfo, Map map, AirstrikeConfig incomingConfig)
            {
                SetupAttackRun(targetInfo, map, incomingConfig, false, Vector3.zero);
            }
            public void SetupAttackRun(LocalTargetInfo targetInfo, Map map, AirstrikeConfig incomingConfig, bool useManualDirection, Vector3 manualDirection)
            {
                this.targetCell = targetInfo.Cell;
                this.targetThing = targetInfo.Thing;
                this.cachedMap = map;
                this.config = incomingConfig ?? new AirstrikeConfig();

                ApplyConfig();

                this.ticksFlying = 0;
                this.shotsFired = 0;
                this.ticksUntilNextShot = 0;

                Vector3 targetPos = targetCell.ToVector3Shifted();
                targetPos.y = 0f;

                float mapDiagonal = Mathf.Sqrt(map.Size.x * map.Size.x + map.Size.z * map.Size.z);

                Vector3 axis = Vector3.zero;
                bool hasAxis = false;

                if (!this.config.homing && useManualDirection)
                {
                    axis = manualDirection;
                    axis.y = 0f;
                    if (axis.sqrMagnitude > 0.001f)
                    {
                        axis.Normalize();
                        hasAxis = true;
                    }
                }

                if (!hasAxis && !this.config.homing)
                {
                    hasAxis = TryPickWeightedDirection(map, targetCell, out axis);
                }

                if (hasAxis)
                {
                    Vector3 edgeA = GetMapEdgePointByDirection(targetPos, axis, map.Size.x, map.Size.z, 120f);
                    Vector3 edgeB = GetMapEdgePointByDirection(targetPos, -axis, map.Size.x, map.Size.z, 120f);

                    float dA = Vector3.Distance(targetPos, edgeA);
                    float dB = Vector3.Distance(targetPos, edgeB);

                    if (dA >= dB)
                    {
                        this.startPos = edgeA;
                        this.endPos = edgeB;
                    }
                    else
                    {
                        this.startPos = edgeB;
                        this.endPos = edgeA;
                    }

                    Vector3 fwd = (this.endPos - this.startPos).normalized;
                    if (!FirstShotTargetInBoundsForDirection(map, targetCell, fwd))
                    {
                        Vector3 tmp = this.startPos;
                        this.startPos = this.endPos;
                        this.endPos = tmp;

                        fwd = (this.endPos - this.startPos).normalized;
                        if (!FirstShotTargetInBoundsForDirection(map, targetCell, fwd))
                        {
                            this.startPos = GetFurthestMapEdgePoint(targetCell, map.Size.x, map.Size.z, 120f);
                            Vector3 dir = (targetPos - this.startPos).normalized;
                            this.endPos = targetPos + dir * (mapDiagonal + 120f);
                        }
                    }
                }
                else
                {
                    this.startPos = GetFurthestMapEdgePoint(targetCell, map.Size.x, map.Size.z, 120f);
                    Vector3 dir = (targetPos - this.startPos).normalized;
                    this.endPos = targetPos + dir * (mapDiagonal + 120f);
                }

                this.flightDir = (this.endPos - this.startPos).normalized;
                this.drawAngle = this.flightDir.AngleFlat();

                this.totalDist = Vector3.Distance(this.startPos, this.endPos);
                this.targetAlongDist = Vector3.Dot((targetPos - this.startPos), this.flightDir);

                this.totalDurationTicks = Mathf.Max(1, Mathf.CeilToInt((this.totalDist / Mathf.Max(1f, this.Speed)) * 60f));
                this.drawPos = this.startPos;
                this.currentAltitude = this.FlightAltitude;
                this.altitudeVelocity = 0f;

                this.cachedMat = this.Graphic.MatAt(this.Rotation);
                this.initialized = true;
            }

            private void ApplyConfig()
            {
                if (config == null) config = new AirstrikeConfig();
                this.FlightAltitude = config.flightAltitude;
                this.Speed = config.speed;
                this.AttackRange = config.attackRange;
                this.BurstCount = config.burstCount;
                this.BurstInterval = config.burstInterval;
                this.StrafeLength = config.strafeLength;
                this.StrafeSpread = config.strafeSpread;
                this.Projectilepershot = config.projectilepershot;
            }

            private bool TryPickWeightedDirection(Map map, IntVec3 centerCell, out Vector3 bestDir)
            {
                bestDir = Vector3.zero;
                if (map == null || map.attackTargetsCache == null || Faction.OfPlayer == null) return false;

                TmpHostiles.Clear();

                HashSet<IAttackTarget> hostileSet = map.attackTargetsCache.TargetsHostileToFaction(Faction.OfPlayer);
                if (hostileSet != null)
                {
                    float scanRadius = Mathf.Max(this.AttackRange + this.StrafeLength + 8f, 30f);
                    foreach (IAttackTarget at in hostileSet)
                    {
                        Thing th = at.Thing;
                        if (th == null || th.Destroyed || !th.Spawned) continue;
                        if (!th.Position.InBounds(map)) continue;
                        if (!th.Position.InHorDistOf(centerCell, scanRadius)) continue;
                        TmpHostiles.Add(th);
                        if (TmpHostiles.Count >= 48) break;
                    }
                }

                Vector3 center = centerCell.ToVector3Shifted();
                center.y = 0f;

                float halfLen = Mathf.Max(4f, this.StrafeLength * 0.5f + 2f);
                float halfWid = Mathf.Max(1.5f, this.StrafeSpread + 1f);

                float bestScore = -1f;
                bool found = false;

                for (int i = 0; i < SampleDirs.Length; i++)
                {
                    Vector3 fwd = SampleDirs[i];
                    Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;

                    if (!FirstShotTargetInBoundsForDirection(map, centerCell, fwd)) continue;

                    float score = 0f;
                    if (TmpHostiles.Count == 0)
                    {
                        score = 0.1f;
                    }
                    else
                    {
                        for (int j = 0; j < TmpHostiles.Count; j++)
                        {
                            Vector3 d = TmpHostiles[j].DrawPos;
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
                    for (int i = 0; i < SampleDirs.Length; i++)
                    {
                        if (FirstShotTargetInBoundsForDirection(map, centerCell, SampleDirs[i]))
                        {
                            bestDir = SampleDirs[i];
                            return true;
                        }
                    }
                    return false;
                }

                return true;
            }

            private bool FirstShotTargetInBoundsForDirection(Map map, IntVec3 centerCell, Vector3 forward)
            {
                if (map == null) return false;
                float progress = (this.BurstCount > 1) ? -0.5f : 0f;
                Vector3 target = centerCell.ToVector3Shifted();
                Vector3 firstImpact = target + forward.normalized * (progress * this.StrafeLength);
                IntVec3 c = firstImpact.ToIntVec3();
                return c.InBounds(map);
            }

            private Vector3 GetMapEdgePointByDirection(Vector3 origin, Vector3 dir, float mapWidth, float mapHeight, float buffer)
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
                if (float.IsInfinity(t) || float.IsNaN(t))
                {
                    t = Mathf.Max(mapWidth, mapHeight) + buffer;
                }

                return origin + dir * t;
            }

            private Vector3 GetFurthestMapEdgePoint(IntVec3 target, float mapWidth, float mapHeight, float buffer)
            {
                float distToTop = mapHeight - target.z;
                float distToBottom = target.z;
                float distToRight = mapWidth - target.x;
                float distToLeft = target.x;

                float maxDist = Mathf.Max(distToTop, Mathf.Max(distToBottom, Mathf.Max(distToRight, distToLeft)));
                List<int> validSides = new List<int>();

                if (Mathf.Abs(maxDist - distToTop) < 1f) validSides.Add(0);
                if (Mathf.Abs(maxDist - distToRight) < 1f) validSides.Add(1);
                if (Mathf.Abs(maxDist - distToBottom) < 1f) validSides.Add(2);
                if (Mathf.Abs(maxDist - distToLeft) < 1f) validSides.Add(3);

                int side = validSides.RandomElement();
                float x = 0f;
                float z = 0f;

                switch (side)
                {
                    case 0: x = Rand.Range(0f, mapWidth); z = mapHeight + buffer; break;
                    case 1: x = mapWidth + buffer; z = Rand.Range(0f, mapHeight); break;
                    case 2: x = Rand.Range(0f, mapWidth); z = -buffer; break;
                    case 3: x = -buffer; z = Rand.Range(0f, mapHeight); break;
                }

                return new Vector3(x, 0f, z);
            }

            protected override void Tick()
            {
                if (!initialized) return;
                if (this.Map != null) this.cachedMap = this.Map;
                if (this.cachedMap == null) return;

                ticksFlying++;
                float tRaw = (float)ticksFlying / (float)totalDurationTicks;
                if (tRaw >= 1f)
                {
                    SafeEndSustainer();
                    this.Destroy();
                    return;
                }

                float t = Mathf.SmoothStep(0f, 1f, tRaw);
                UpdatePosition(t);
                UpdateFlightAltitudeProfile();

                IntVec3 currentCell = this.drawPos.ToIntVec3();
                if (currentCell.InBounds(this.cachedMap) && currentCell != this.Position)
                {
                    this.Position = currentCell;
                }

                MaintainSustainer();
                SpawnEffects();
                TryFireWeapon();
            }

            private void UpdatePosition(float t)
            {
                this.drawPos = Vector3.Lerp(startPos, endPos, t);
            }

            private void UpdateFlightAltitudeProfile()
            {
                Vector3 p = this.drawPos;
                p.y = 0f;

                Vector3 target = this.targetCell.ToVector3Shifted();
                target.y = 0f;

                float along = Vector3.Dot((p - this.startPos), this.flightDir);
                float distToTarget = Vector3.Distance(p, target);

                float desired = this.FlightAltitude;

                if (along <= targetAlongDist)
                {
                    float nearT = Mathf.InverseLerp(this.AttackRange + this.StrafeLength, Mathf.Max(5f, this.AttackRange * 0.35f), distToTarget);
                    desired = Mathf.Lerp(this.FlightAltitude, this.FlightAltitude * 0.65f, nearT);
                }
                else
                {
                    float pullT = Mathf.InverseLerp(this.targetAlongDist, Mathf.Max(this.targetAlongDist + 1f, this.totalDist), along);
                    desired = Mathf.Lerp(this.FlightAltitude * 0.72f, this.FlightAltitude * 1.15f, pullT);
                }

                this.currentAltitude = Mathf.SmoothDamp(this.currentAltitude, desired, ref this.altitudeVelocity, 0.18f);
            }

            private void TryFireWeapon()
            {
                if (shotsFired >= BurstCount) return;

                if (ticksUntilNextShot > 0)
                {
                    ticksUntilNextShot--;
                    return;
                }

                float dist = Vector3.Distance(new Vector3(drawPos.x, 0f, drawPos.z), targetCell.ToVector3Shifted());
                if (dist <= AttackRange + (StrafeLength / 2f))
                {
                    for (int i = 0; i < Projectilepershot; i++)
                    {
                        DropBomb();
                    }

                    shotsFired++;
                    ticksUntilNextShot = BurstInterval;
                }
            }

            private void DropBomb()
            {
                if (this.cachedMap == null) return;

                Quaternion quat = Quaternion.AngleAxis(this.drawAngle, Vector3.up);
                Vector3 forwardDir = quat * Vector3.forward;
                Vector3 rightDir = quat * Vector3.right;
                Vector3 weaponOffset = (shotsFired % 2 == 0) ? LeftWeaponOffset : RightWeaponOffset;

                Vector3 dropStartPos = this.drawPos + (quat * weaponOffset);
                dropStartPos.z += currentAltitude;

                ThingDef projectileDef = DefDatabase<ThingDef>.GetNamed(config.projectileDefName, false) ?? DefDatabase<ThingDef>.GetNamed("CMC_Missile_ASG");
                if (projectileDef == null) return;

                LocalTargetInfo launchTarget;
                IntVec3 spawnCell;

                if (config.homing && targetThing != null && !targetThing.Destroyed)
                {
                    launchTarget = new LocalTargetInfo(targetThing);
                    spawnCell = targetThing.Position;
                }
                else
                {
                    float progress = BurstCount > 1 ? ((float)shotsFired / (float)(BurstCount - 1)) - 0.5f : 0f;
                    Vector3 longitudinalOffset = forwardDir * (progress * StrafeLength);
                    float lateralRand = (shotsFired == 0) ? 0f : Rand.Range(-StrafeSpread, StrafeSpread);
                    Vector3 lateralOffset = rightDir * lateralRand;

                    Vector3 targetVec = targetCell.ToVector3Shifted() + longitudinalOffset + lateralOffset;
                    spawnCell = targetVec.ToIntVec3();
                    launchTarget = new LocalTargetInfo(spawnCell);
                }

                if (!spawnCell.InBounds(this.cachedMap)) return;

                IntVec3 projectileSpawnCell = this.Position;
                if (!projectileSpawnCell.InBounds(this.cachedMap))
                {
                    projectileSpawnCell = spawnCell;
                }
                if (!projectileSpawnCell.InBounds(this.cachedMap)) return;

                Projectile projectile = (Projectile)GenSpawn.Spawn(projectileDef, projectileSpawnCell, this.cachedMap);

                Vector3 dir = (this.endPos - this.startPos).normalized;
                Projectile_PoiMissile_ASG customMissile = projectile as Projectile_PoiMissile_ASG;
                if (customMissile != null)
                {
                    customMissile.planeDirection = dir;
                }

                projectile.Launch(this, dropStartPos, launchTarget, launchTarget, ProjectileHitFlags.All);

                SoundDef soundDef = DefDatabase<SoundDef>.GetNamed("Sound_ReleaseRocket", false);
                if (soundDef != null)
                {
                    soundDef.PlayOneShot(SoundInfo.InMap(this));
                }
            }

            private void SpawnEffects()
            {
                if (this.cachedMap == null || !this.Position.InBounds(this.cachedMap)) return;
                if (engineGlowDef == null) engineGlowDef = FleckDefOf.LightningGlow;

                Quaternion quat = Quaternion.AngleAxis(this.drawAngle, Vector3.up);
                Vector3 worldOffsetA = quat * EngineAOffset;
                Vector3 worldOffsetB = quat * EngineBOffset;

                Vector3 spawnPosA = this.drawPos + worldOffsetA;
                spawnPosA.y = AltitudeLayer.Skyfaller.AltitudeFor() - 0.05f;
                spawnPosA.z += currentAltitude;

                Vector3 spawnPosB = this.drawPos + worldOffsetB;
                spawnPosB.y = AltitudeLayer.Skyfaller.AltitudeFor() - 0.05f;
                spawnPosB.z += currentAltitude;

                FleckCreationData dataA = FleckMaker.GetDataStatic(spawnPosA, cachedMap, engineGlowDef, 1.5f);
                dataA.velocityAngle = Rand.Range(-360f, 360f);
                dataA.velocitySpeed = Rand.Range(0.12f, 0.24f);
                cachedMap.flecks.CreateFleck(dataA);

                FleckCreationData dataB = FleckMaker.GetDataStatic(spawnPosB, cachedMap, engineGlowDef, 1.5f);
                dataB.velocityAngle = Rand.Range(-360f, 360f);
                dataB.velocitySpeed = Rand.Range(0.12f, 0.24f);
                cachedMap.flecks.CreateFleck(dataB);

                FleckCreationData flashA = FleckMaker.GetDataStatic(spawnPosA, cachedMap, FleckDefOf.FlashHollow, 1.05f);
                flashA.rotationRate = Rand.RangeInclusive(-360, 360);
                flashA.velocityAngle = Rand.Range(0f, 360f);
                flashA.velocitySpeed = Rand.Range(0.8f, 1.2f);
                flashA.instanceColor = new Color(0.3f, 0.5f, 1f, 0.7f);
                cachedMap.flecks.CreateFleck(flashA);

                FleckCreationData flashB = FleckMaker.GetDataStatic(spawnPosB, cachedMap, FleckDefOf.FlashHollow, 1.05f);
                flashB.rotationRate = Rand.RangeInclusive(-360, 360);
                flashB.velocityAngle = Rand.Range(0f, 360f);
                flashB.velocitySpeed = Rand.Range(0.8f, 1.2f);
                flashB.instanceColor = new Color(0.3f, 0.5f, 1f, 0.7f);
                cachedMap.flecks.CreateFleck(flashB);
            }

            private void MaintainSustainer()
            {
                if (this.DestroyedOrNull() || !this.Spawned)
                {
                    SafeEndSustainer();
                    return;
                }

                if (this.sustainer != null && this.sustainer.Ended)
                {
                    this.sustainer = null;
                }

                if (this.sustainer == null)
                {
                    SoundDef def = DefDatabase<SoundDef>.GetNamed("CMC_EngineSustainSound_Fighter", false);
                    if (def != null)
                    {
                        SoundInfo info = SoundInfo.InMap(this, MaintenanceType.PerTick);
                        this.sustainer = def.TrySpawnSustainer(info);
                    }
                }

                if (this.sustainer != null)
                {
                    this.sustainer.Maintain();
                }
            }

            private void SafeEndSustainer()
            {
                if (this.sustainer != null && !this.sustainer.Ended)
                {
                    this.sustainer.End();
                }
                this.sustainer = null;
            }

            public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
            {
                SafeEndSustainer();
                base.DeSpawn(mode);
            }

            public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
            {
                SafeEndSustainer();
                base.Destroy(mode);
            }

            protected override void DrawAt(Vector3 loc, bool flip = false)
            {
                ForceDrawAt(this.drawPos);
            }

            private void ForceDrawAt(Vector3 pos)
            {
                Vector3 planePos = pos;
                planePos.y = AltitudeLayer.Skyfaller.AltitudeFor();
                planePos.z += currentAltitude;

                Quaternion quat = Quaternion.AngleAxis(this.drawAngle, Vector3.up);
                Matrix4x4 matrix = Matrix4x4.TRS(planePos, quat, this.Graphic.drawSize.ToVector3());

                if (this.cachedMat == null)
                {
                    this.cachedMat = this.Graphic.MatAt(this.Rotation);
                }

                Graphics.DrawMesh(MeshPool.plane10, matrix, this.cachedMat, 0);
                Graphics.DrawMesh(MeshPool.plane10, matrix, LightMat, 0);

                Vector3 shadowPos = pos;
                shadowPos.y = AltitudeLayer.Shadows.AltitudeFor();
                Matrix4x4 shadowMatrix = Matrix4x4.TRS(shadowPos, quat, this.Graphic.drawSize.ToVector3());
                Graphics.DrawMesh(MeshPool.plane10, shadowMatrix, ShadowMat, 0);
            }

            public override void ExposeData()
            {
                base.ExposeData();

                Scribe_Values.Look(ref startPos, "startPos");
                Scribe_Values.Look(ref endPos, "endPos");
                Scribe_Values.Look(ref flightDir, "flightDir");
                Scribe_Values.Look(ref totalDist, "totalDist", 0f);
                Scribe_Values.Look(ref targetAlongDist, "targetAlongDist", 0f);

                Scribe_Values.Look(ref targetCell, "targetCell");
                Scribe_References.Look(ref targetThing, "targetThing");

                Scribe_Values.Look(ref ticksFlying, "ticksFlying");
                Scribe_Values.Look(ref totalDurationTicks, "totalDurationTicks");
                Scribe_Values.Look(ref shotsFired, "shotsFired");
                Scribe_Values.Look(ref ticksUntilNextShot, "ticksUntilNextShot");

                Scribe_Values.Look(ref drawPos, "drawPos");
                Scribe_Values.Look(ref drawAngle, "drawAngle");
                Scribe_Values.Look(ref initialized, "initialized", false);

                Scribe_Values.Look(ref currentAltitude, "currentAltitude", 15f);
                Scribe_Values.Look(ref altitudeVelocity, "altitudeVelocity", 0f);

                Scribe_Deep.Look(ref config, "config");

                if (Scribe.mode == LoadSaveMode.PostLoadInit)
                {
                    ApplyConfig();
                    if (currentAltitude <= 0f) currentAltitude = FlightAltitude;
                    this.cachedMat = this.Graphic.MatAt(this.Rotation);
                }
            }
        }
}