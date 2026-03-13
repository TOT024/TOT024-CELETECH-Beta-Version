using RimWorld;
using UnityEngine;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace TOT_DLL_test
{
    [StaticConstructorOnStartup]
    public class CMC_Drone : Building, IAttackTarget
    {
        private readonly MaterialPropertyBlock muzzleMPB = new MaterialPropertyBlock();
        public Mote mote = null;
        private List<Vector3> trailHistory = new List<Vector3>();
        private const int MaxTrailLength = 100;
        private const float TrailWidthStart = 0.3f;
        private const float TrailWidthEnd = 0.01f;
        private static readonly Material TrailMat = MaterialPool.MatFrom(
            GenDraw.LineTexPath,
            ShaderDatabase.MoteGlow,
            new Color(0.1f, 1f, 1f, 1f)
        );
        private static readonly Material ThrusterMat = MaterialPool.MatFrom(
            "Things/Others/UAV_Tail",
            ShaderDatabase.MoteGlow,
            Color.white
        );
        private static readonly Mesh ThrusterMesh = MeshPool.plane10;
        private readonly MaterialPropertyBlock thrusterMPB = new MaterialPropertyBlock();
        private Vector3 lastGroundPos;
        private float speedNow;
        private float speedPrev;
        private Vector3 moveDir = Vector3.forward;

        private int rcsPulseTicksLeft;
        private Vector3 rcsPulseDir;
        public LocalTargetInfo TargetCurrentlyAimingAt
        {
            get { return this.compMove.currentAttackTarget; }
        }
        public float TargetPriorityFactor
        {
            get { return 1.08f; }
        }
        public override void DrawExtraSelectionOverlays()
        {
            base.DrawExtraSelectionOverlays();
            Verb attackVerb = compMove.AttackVerb;
            if (attackVerb?.verbProps == null) return;
            float range = attackVerb.verbProps.range;
            if (range < 90)
            {
                GenDraw.DrawRadiusRing(base.Position, range);
            }
            float num = attackVerb.verbProps.EffectiveMinRange(true);
            if (num < 90f && num > 0.1f)
            {
                GenDraw.DrawRadiusRing(base.Position, num);
            }
            if (compMove.currentAttackTarget.IsValid && (!compMove.currentAttackTarget.HasThing || compMove.currentAttackTarget.Thing.Spawned))
            {
                Vector3 vector = compMove.currentAttackTarget.HasThing ? compMove.currentAttackTarget.Thing.DrawPos : compMove.currentAttackTarget.Cell.ToVector3Shifted();
                Vector3 a = flyingDrawPos;
                vector.y = AltitudeLayer.MetaOverlays.AltitudeFor();
                a.y = vector.y;
                GenDraw.DrawLineBetween(a, vector, CMC_Drone.ForcedTargetLineMat, 0.3f);
            }
        }
        public static Material ForcedTargetLineMat = MaterialPool.MatFrom(GenDraw.LineTexPath, ShaderDatabase.Transparent, new Color(1f, 0.1f, 0.1f));
        public bool ThreatDisabled(IAttackTargetSearcher disabledFor)
        {
            return false;
        }
        public Thing Thing
        {
            get { return this as Thing; }
        }
        private CompDroneMovement compMove;
        private Mesh mesh;
        Material baseMat;
        private Graphic turretGraphic;
        private Graphic LightGraphic;
        private Mesh turretMesh;
        private Material turretMat;
        private Mesh glowMesh;
        private Material glowMat;
        private readonly MaterialPropertyBlock turretMPB = new MaterialPropertyBlock();
        private readonly MaterialPropertyBlock glowMPB = new MaterialPropertyBlock();
        public const float FlyingAltitude = 0.8f;
        public Vector3 flyingDrawPos;
        public bool isTargetable = true;
        public bool IsFlying => compMove?.IsFlying ?? false;
        public override Vector3 DrawPos => compMove?.ExactDrawPos ?? base.DrawPos;
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            this.compMove = GetComp<CompDroneMovement>();
            LongEventHandler.ExecuteWhenFinished(() =>
            {
                if (compMove?.Props.shadowTexPath != null)
                {
                    var props = compMove.Props;
                    if (compMove.Props.graphicDataTurret != null)
                    {
                        turretGraphic = compMove.Props.graphicDataTurret.Graphic;
                        turretMesh = turretGraphic.MeshAt(Rot4.East);
                        turretMat = turretGraphic.MatAt(Rot4.East, null);
                    }

                    if (compMove.Props.graphicDataTurretOverlay != null)
                    {
                        LightGraphic = compMove.Props.graphicDataTurretOverlay.Graphic;
                        glowMesh = LightGraphic.MeshAt(Rot4.East);
                        glowMat = LightGraphic.MatAt(Rot4.East, null);
                    }
                }
            });
            lastGroundPos = compMove != null ? compMove.ExactGroundPos : this.Position.ToVector3Shifted();
            speedNow = 0f;
            speedPrev = 0f;
            moveDir = Vector3.forward;
            rcsPulseTicksLeft = 0;
        }
        protected override void Tick()
        {
            base.Tick();
            UpdateMotionState();
            CheckSmokeEffect();
            MaintainMote();
        }
        private void MaintainMote()
        {
            if (!compMove.currentAttackTarget.IsValid || compMove.currentAttackTarget.IsValid && compMove.currentAttackTarget.Thing.DestroyedOrNull())
                return;
            if (this.mote.DestroyedOrNull())
            {
                ThingDef cmc_Mote_SWTargetLocked = CMC_Def.CMC_Mote_SWTargetLocked;
                Vector3 offset = new Vector3(0f, 0f, 0f)
                {
                    y = AltitudeLayer.PawnRope.AltitudeFor()
                };
                this.mote = MoteMaker.MakeAttachedOverlay(compMove.currentAttackTarget.Thing, cmc_Mote_SWTargetLocked, offset, 1f, 1f);
                this.mote.exactRotation = 45f;
            }
            else
            {
                this.mote.Maintain();
            }
        }
        private void UpdateMotionState()
        {
            if (compMove == null || !IsFlying || Find.TickManager.Paused) return;
            Vector3 cur = compMove.ExactGroundPos;
            Vector3 delta = cur - lastGroundPos;
            delta.y = 0f;
            speedNow = delta.magnitude;
            if (speedNow > 0.0001f) moveDir = delta.normalized;
            float accel = speedNow - speedPrev;
            TryTriggerRCSPulse(accel);
            speedPrev = speedNow;
            lastGroundPos = cur;
            if (rcsPulseTicksLeft > 0) rcsPulseTicksLeft--;
        }
        private void TryTriggerRCSPulse(float accel)
        {
            if (compMove == null) return;
            float th = Mathf.Max(0.0001f, compMove.Props.rcsAccelThreshold);
            if (Mathf.Abs(accel) < th) return;
            if (moveDir.sqrMagnitude < 0.0001f) return;
            Vector3 dir = accel >= 0f ? moveDir : -moveDir;
            float sx = dir.x >= 0f ? 1f : -1f;
            float sz = dir.z >= 0f ? 1f : -1f;
            rcsPulseDir = new Vector3(sx, 0f, sz).normalized;
            rcsPulseTicksLeft = Mathf.Max(1, compMove.Props.rcsPulseTicks);
        }
        private void DrawMuzzleFlashIfNeeded(Vector3 basePos, float turretAngle, bool flipUv)
        {
            if (compMove == null) return;
            if (!compMove.MuzzleFlashActive) return;
            Material mat = compMove.MuzzleFlashMat;
            if (mat == null) return;
            Vector2 size = compMove.MuzzleFlashDrawSize;
            if (size == Vector2.zero) size = Vector2.one;
            Mesh m = flipUv ? MeshPool.plane10Flip : MeshPool.plane10;
            Vector3 muzzleOffset = new Vector3(0f, 0f, 1.5f).RotatedBy(turretAngle);
            Vector3 pos = basePos + muzzleOffset + new Vector3(0f, -0.02f, 0f);
            float alpha = Mathf.Clamp01(compMove.MuzzleFlashTicksLeft / 10f);
            muzzleMPB.Clear();
            Color c = mat.color;
            c.a *= alpha;
            muzzleMPB.SetColor(ShaderPropertyIDs.Color, c);
            Matrix4x4 mx = Matrix4x4.TRS(
                pos,
                Quaternion.AngleAxis(turretAngle, Vector3.up),
                new Vector3(size.x, 1f, size.y)
            );
            Graphics.DrawMesh(m, mx, mat, 0, null, 0, muzzleMPB);
        }
        public override void ExposeData()
        {
            base.ExposeData();
        }
        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (compMove == null)
            {
                base.DrawAt(drawLoc, flip);
                return;
            }

            if (IsFlying)
            {
                Vector3 p = compMove.ExactGroundPos;
                if (compMove.currentEscortTarget.Thing != null && compMove.currentEscortTarget.Thing.Rotation == Rot4.North)
                    flyingDrawPos = new Vector3(p.x, AltitudeLayer.Pawn.AltitudeFor() + 0.5f, p.z);
                else
                    flyingDrawPos = new Vector3(p.x, AltitudeLayer.Pawn.AltitudeFor() - 0.5f, p.z);

                base.Graphic.Draw(flyingDrawPos, Rot4.North, this, 0f);

                float turretAngle = compMove.CurRotation;
                bool flipUv = CompDroneMovement.ShouldFlipDockUV(compMove.FollowDockSlot, compMove.MasterRotation);
                if (turretMesh != null && turretMat != null)
                {
                    Mesh m = flipUv ? MeshPool.GridPlaneFlip(turretMesh) : turretMesh;

                    turretMPB.Clear();
                    Color tint = compMove.DockTintColor;
                    turretMPB.SetColor(ShaderPropertyIDs.Color, tint * turretMat.color);

                    Matrix4x4 mx = Matrix4x4.TRS(
                        flyingDrawPos + new Vector3(0f, 0.01f, 0f),
                        Quaternion.AngleAxis(turretAngle, Vector3.up),
                        Vector3.one
                    );

                    Graphics.DrawMesh(m, mx, turretMat, 0, null, 0, turretMPB);
                }
                if (glowMesh != null && glowMat != null)
                {
                    Mesh gm = flipUv ? MeshPool.GridPlaneFlip(glowMesh) : glowMesh;

                    glowMPB.Clear();
                    glowMPB.SetColor(ShaderPropertyIDs.Color, glowMat.color);

                    Matrix4x4 gmx = Matrix4x4.TRS(
                        flyingDrawPos + new Vector3(0f, 0.015f, 0f),
                        Quaternion.AngleAxis(turretAngle, Vector3.up),
                        Vector3.one
                    );
                    Graphics.DrawMesh(gm, gmx, glowMat, 0, null, 0, glowMPB);
                }
                DrawMainFlameIfNeeded();
                DrawRCSPulseIfNeeded();
                UpdateAndDrawTrail();
                DrawMuzzleFlashIfNeeded(flyingDrawPos, turretAngle, flipUv);
            }
            else
            {
                if (trailHistory.Count > 0) trailHistory.Clear();
                base.DrawAt(drawLoc, flip);
                if (turretMesh != null && turretMat != null)
                {
                    bool flipUv = CompDroneMovement.ShouldFlipDockUV(compMove.FollowDockSlot, compMove.MasterRotation);
                    Mesh m = flipUv ? MeshPool.GridPlaneFlip(turretMesh) : turretMesh;

                    turretMPB.Clear();
                    turretMPB.SetColor(ShaderPropertyIDs.Color, compMove.DockTintColor * turretMat.color);

                    Matrix4x4 mx = Matrix4x4.TRS(
                        drawLoc + new Vector3(0f, 0.01f, 0f),
                        Quaternion.AngleAxis(compMove.CurRotation, Vector3.up),
                        Vector3.one
                    );

                    Graphics.DrawMesh(m, mx, turretMat, 0, null, 0, turretMPB);
                }

                if (glowMesh != null && glowMat != null)
                {
                    bool flipUv = CompDroneMovement.ShouldFlipDockUV(compMove.FollowDockSlot, compMove.MasterRotation);
                    Mesh gm = flipUv ? MeshPool.GridPlaneFlip(glowMesh) : glowMesh;

                    glowMPB.Clear();
                    glowMPB.SetColor(ShaderPropertyIDs.Color, glowMat.color);

                    Matrix4x4 gmx = Matrix4x4.TRS(
                        drawLoc + new Vector3(0f, 0.015f, 0f),
                        Quaternion.AngleAxis(compMove.CurRotation, Vector3.up),
                        Vector3.one
                    );
                    Graphics.DrawMesh(gm, gmx, glowMat, 0, null, 0, glowMPB);
                }
            }
        }
        private void DrawMainFlameIfNeeded()
        {
            if (compMove == null || moveDir.sqrMagnitude < 0.0001f) return;

            float maxPerTick = Mathf.Max(0.0001f, compMove.Props.moveSpeed / 60f);
            float ratio = speedNow / maxPerTick;
            if (ratio < compMove.Props.mainFlameMinSpeedRatio) return;

            Vector3 pos = flyingDrawPos - moveDir * compMove.Props.mainFlameBackOffset;
            pos.y -= 0.01f;

            float t = Mathf.InverseLerp(compMove.Props.mainFlameMinSpeedRatio, 1f, Mathf.Clamp01(ratio));
            float size = compMove.Props.mainFlameSize * Mathf.Lerp(0.9f, 1.6f, t);
            float angle = moveDir.AngleFlat() + 180f;
            DrawThrusterQuad(pos, angle, size, 1f);
        }
        private void DrawRCSPulseIfNeeded()
        {
            if (compMove == null || rcsPulseTicksLeft <= 0) return;
            float k = (float)rcsPulseTicksLeft / Mathf.Max(1, compMove.Props.rcsPulseTicks);
            float alpha = Mathf.Clamp01(k);
            float size = compMove.Props.rcsSize * Mathf.Lerp(0.8f, 1.2f, k);
            Vector3 pos = flyingDrawPos + rcsPulseDir * compMove.Props.rcsOffset;
            pos.y -= 0.012f;
            float angle = rcsPulseDir.AngleFlat() + 180f;
            DrawThrusterQuad(pos, angle, size, alpha);
        }
        private void DrawThrusterQuad(Vector3 pos, float angle, float size, float alpha)
        {
            thrusterMPB.Clear();
            thrusterMPB.SetColor("_Color", new Color(1f, 1f, 1f, alpha));
            Matrix4x4 m = Matrix4x4.TRS(
                pos,
                Quaternion.AngleAxis(angle, Vector3.up),
                new Vector3(size, 1f, size)
            );
            Graphics.DrawMesh(ThrusterMesh, m, ThrusterMat, 0, null, 0, thrusterMPB);
        }
        private void UpdateAndDrawTrail()
        {
            float maxPerTick = Mathf.Max(0.0001f, compMove.Props.moveSpeed / 60f);
            float speedRatio = speedNow / maxPerTick;

            if (speedRatio < compMove.Props.trailMinSpeedRatio)
            {
                if (trailHistory.Count > 0) trailHistory.Clear();
                return;
            }

            Vector3 enginePos = flyingDrawPos;
            enginePos.y -= 0.1f;

            if (float.IsNaN(enginePos.x) || float.IsNaN(enginePos.z))
            {
                trailHistory.Clear();
                return;
            }

            if (trailHistory.Count > 0)
            {
                float maxJump = maxPerTick * 12f + 1.0f;
                if ((trailHistory[0] - enginePos).sqrMagnitude > maxJump * maxJump)
                    trailHistory.Clear();
            }

            if (!Find.TickManager.Paused)
            {
                trailHistory.Insert(0, enginePos);
                if (trailHistory.Count > MaxTrailLength)
                    trailHistory.RemoveAt(trailHistory.Count - 1);
            }

            if (trailHistory.Count < 2) return;

            for (int i = 0; i < trailHistory.Count - 1; i++)
            {
                Vector3 start = trailHistory[i];
                Vector3 end = trailHistory[i + 1];
                float pct = (float)i / trailHistory.Count;
                float width = Mathf.Lerp(TrailWidthStart, TrailWidthEnd, pct);
                GenDraw.DrawLineBetween(start, end, TrailMat, width);
            }
        }
        private int lastSmokeTick = 0;
        private const int SMOKE_INTERVAL = 60;
        private const float SMOKE_THRESHOLD = 0.3f;
        private void CheckSmokeEffect()
        {
            if (this.HitPoints <= 0 || this.Destroyed) return;
            float healthPercent = (float)this.HitPoints / this.MaxHitPoints;
            if (healthPercent < SMOKE_THRESHOLD)
            {
                if (this.IsHashIntervalTick(2))
                {
                    EmitSmoke();
                    lastSmokeTick = Find.TickManager.TicksGame;
                    if (healthPercent < 0.33f)
                    {
                        EmitSmoke();
                    }
                }
            }
        }
        private void EmitSmoke()
        {
            if (!this.Spawned || this.Map == null) return;
            Vector3 smokePos;
            if (IsFlying)
            {
                smokePos = flyingDrawPos + new Vector3(
                    Rand.Range(-0.03f, 0.03f),
                    Rand.Range(0f, 0.02f),
                    Rand.Range(-0.03f, 0.03f)
                );
            }
            else
            {
                smokePos = this.DrawPos + new Vector3(
                    Rand.Range(-0.02f, 0.02f),
                    0.3f,
                    Rand.Range(-0.02f, 0.02f)
                );
            }
            FleckCreationData smokeData = FleckMaker.GetDataStatic(
                smokePos,
                this.Map,
                FleckDefOf.Smoke,
                Rand.Range(0.45f, 0.62f)
            );
            smokeData.rotationRate = Rand.Range(-30f, 30f);
            smokeData.velocityAngle = Rand.Range(0, 360);
            smokeData.velocitySpeed = Rand.Range(0.07f, 0.12f);
            this.Map.flecks.CreateFleck(smokeData);
        }
        public void CreateExplosionEffect()
        {
            if (!this.Spawned || this.Map == null) return;
            GenExplosion.DoExplosion(
                this.Position,
                this.Map,
                radius: 1.5f,
                DamageDefOf.Bomb,
                instigator: null,
                damAmount: 20,
                armorPenetration: -1f,
                explosionSound: null,
                weapon: null,
                projectile: null,
                intendedTarget: null,
                postExplosionSpawnThingDef: ThingDefOf.Filth_BlastMark,
                postExplosionSpawnChance: 0.4f,
                postExplosionSpawnThingCount: 2,
                applyDamageToExplosionCellsNeighbors: false,
                preExplosionSpawnThingDef: null,
                preExplosionSpawnChance: 0f,
                preExplosionSpawnThingCount: 0,
                chanceToStartFire: 0.3f,
                damageFalloff: true
            );
            for (int i = 0; i < 3; i++)
            {
                Vector3 explosionPos = this.DrawPos + new Vector3(
                    Rand.Range(-0.8f, 0.8f),
                    Rand.Range(0f, 0.5f),
                    Rand.Range(-0.8f, 0.8f)
                );

                FleckCreationData explosionData = FleckMaker.GetDataStatic(
                    explosionPos,
                    this.Map,
                    FleckDefOf.ExplosionFlash,
                    Rand.Range(1f, 2f)
                );
                explosionData.rotationRate = Rand.Range(-90f, 90f);
                explosionData.velocityAngle = Rand.Range(0, 360);
                explosionData.velocitySpeed = Rand.Range(0.5f, 1.5f);
                this.Map.flecks.CreateFleck(explosionData);
            }
        }
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if (mode == DestroyMode.KillFinalize || mode == DestroyMode.Deconstruct)
            {
                CreateExplosionEffect();
            }
            base.Destroy(mode);
        }
    }
}