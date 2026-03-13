using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using static UnityEngine.ParticleSystem;

namespace TOT_DLL_test
{
    public enum DroneAttackMoveStyle { Legacy = 0, CurveDash = 1 }
    public class CompProperties_DroneMovement : CompProperties
    {
        public float hoverRadiusMin = 1.5f;
        public float hoverRadiusMax = 1.8f;
        public float moveSpeed = 25f;
        public float acceleration = 0.05f;
        public float turnRate = 0.15f;
        public string shadowTexPath;
        public float shadowSize = 1.0f;
        public bool CanManualControl = true;
        public ThingDef turretDef;
        public GraphicData graphicDataTurret;
        public GraphicData graphicDataTurretOverlay;
        public float rotationVelocity = 16f;
        public int unifiedTargetSearchRange = 70;
        public int lostTargetStickTicks = 90;
        // Attack move style
        public bool enableCurveAttack = true;
        public bool randomAttackMoveStyle = true;
        public DroneAttackMoveStyle attackMoveStyle = DroneAttackMoveStyle.Legacy;
        // Choose style when we decide to (re)roll:
        public float curveDashChance = 0.5f;
        // Far distance: force CurveDash (NOT first engage)
        public float forceCurveFarDistanceFactor = 1.15f;

        // Style switching triggers
        public int attackStyleSwitchIntervalTicks = 240;  // periodic check
        public bool attackStyleSwitchAfterBurst = true;   // check after burst ends
        public float attackStyleSwitchChance =
            0.35f;  // chance to attempt reroll on a trigger
        public int attackStyleLockTicksMin = 240;
        public int attackStyleLockTicksMax = 420;

        // CurveDash movement
        public int attackDashTicks = 36;  // fallback if curveDashTicks <= 0
        public int curveDashTicks = 54;  // make curve dash longer/slower by default
        public int attackHoldTicks = 64;
        public float attackCurveSideOffset = 1.2f;
        public float attackRangeFactorMin = 0.12f;
        public float attackRangeFactorMax = 0.95f;

        // Legacy in-range orbit (bezier segment) + hold
        public float legacyTurnBoost = 2.0f;
        public int legacyAttackHoldTicks = 10;
        // Reposition planning (when cannot shoot)
        public int firePosRecalcCooldownTicks = 60;
        public int firePosMinNoHitTicks = 20;
        public float firePosEnemyMoveRecalcDist = 2.5f;
        public int MaxTargetSearchRange = 999;
        public float curveAttackChance = 0.5f;
        public float trailMinSpeedRatio = 0.22f;
        public float mainFlameMinSpeedRatio = 0.35f;
        public float mainFlameBackOffset = 0f;
        public float mainFlameSize = 2.02f;
        public float rcsAccelThreshold = 0.012f;
        public int rcsPulseTicks = 12;
        public float rcsOffset = 0.01f;
        public float rcsSize = 0.7f;

        public CompProperties_DroneMovement()
        {
            compClass = typeof(CompDroneMovement);
        }
    }
    [StaticConstructorOnStartup]
    public class CompDroneMovement : ThingComp, IAttackTargetSearcher
    {
        public bool __AssaultMode = false;

        private CompProperties_DroneMovement propsCache;
        private Map mapCache;

        private Vector3 exactPosition;
        private Vector3 moveTargetPos;

        public float curRotationInt;
        public float destRotationInt;
        private int lastAttackTargetId = -1;

        private bool arcActive;
        private int arcTick;
        private int arcTotalTicks;
        private Vector3 arcA, arcB, arcC;
        private int arcHoldTicksLeft;
        private int arcSegIndex;
        private float maxSpeedPerTick;
        private float currentSpeedVal;
        private Vector3 curMoveDir;

        private LocalTargetInfo lastAttackedTarget = LocalTargetInfo.Invalid;
        private int lastAttackTargetTick;

        private int burstCooldownTicksLeft;
        private int burstWarmupTicksLeft;
        private bool prevBursting;
        private Vector3 curveSmoothedDir;
        public enum DroneMoveMode { Landed, Idle, MovingTo, Following, Attacking }
        private enum AttackMoveStyle { Legacy = 0, CurveDash = 1 }

        public DroneMoveMode mode = DroneMoveMode.Landed;
        public LocalTargetInfo currentAttackTarget;
        public LocalTargetInfo currentEscortTarget;
        public Thing currentForceEscortTarget;

        public Thing gun;
        public CompEquippable GunCompEq => gun?.TryGetComp<CompEquippable>();
        public Verb AttackVerb =>
            (gun == null || GunCompEq == null) ? null : GunCompEq.PrimaryVerb;

        private float desiredFollowDistance;
        private float followAngle;
        private int ticksToNextFollowAngleChange;
        private const int FollowAngleChangeIntervalMin = 300;
        private const int FollowAngleChangeIntervalMax = 900;
        private int followDockSlot = -1;
        private CompFunnelHauler cachedHaulerComp;
        private int nextHaulerRefreshTick;

        private bool forcedAttackActive;
        private bool manualForcedAttack;
        private int lostTargetStickTicksLeft;
        private Vector3 lastKnownEnemyPos;
        private bool lastKnownEnemyPosValid;

        private AttackMoveStyle currentAttackStyle = AttackMoveStyle.Legacy;
        private int attackStyleLockTicksLeft;
        private int attackStyleIntervalTicksLeft;

        private Vector3 plannedFirePos;
        private Vector3 plannedEnemyAnchor;
        private bool plannedFirePosValid;
        private int firePosCooldownTicksLeft;
        private int noHitTicks;

        private bool attackDashActive;
        private int attackDashTick;
        private int attackHoldTicksLeft;
        private int attackRepositionIndex;
        private Vector3 attackDashStart;
        private Vector3 attackDashControl;
        private Vector3 attackDashEnd;
        private int unseenTicks;
        private int closeInRecalcTicksLeft;
        private Vector3 closeInPos;
        private bool closeInPosValid;
        private Vector3 bezA, bezB, bezC;
        private int bezTicksLeft;
        private int bezTotalTicks;

        private int orbitSign;
        private float orbitRadiusMul;
        private int legacyHoldCooldownTicks;

        private const float RotationSnapEpsilon = 0.8f;
        private const float AimAlignedEpsilon = 2f;

        private Command_Action gizmoTakeOff;
        private Command_Action gizmoLand;
        private Command_Action gizmoMoveTo;
        private Command_Action gizmoFollow;
        private Command_Action gizmoStop;
        private Command_Action gizmoAttack;
        private int muzzleFlashTicksLeft;
        private Material muzzleFlashMat;
        private Vector2 muzzleFlashDrawSize = Vector2.one;
        public bool MuzzleFlashActive
        {
            get { return muzzleFlashTicksLeft > 0 && muzzleFlashMat != null; }
        }
        public int MuzzleFlashTicksLeft
        {
            get { return muzzleFlashTicksLeft; }
        }
        public Material MuzzleFlashMat
        {
            get { return muzzleFlashMat; }
        }
        public Vector2 MuzzleFlashDrawSize
        {
            get { return muzzleFlashDrawSize; }
        }
        public bool AssaultMode
        {
            get => __AssaultMode;
            set => __AssaultMode = value;
        }
        public bool IsFlying => mode != DroneMoveMode.Landed;
        public CompProperties_DroneMovement Props
        {
            get
            {
                if (propsCache == null)
                    propsCache = (CompProperties_DroneMovement)props;
                return propsCache;
            }
        }
        public Vector3 ExactGroundPos => exactPosition;
        public Vector3 ExactDrawPos =>
            new Vector3(exactPosition.x, 0f, exactPosition.z);
        public float CurRotation
        {
            get => curRotationInt;
            set => curRotationInt = Mathf.Repeat(value, 360f);
        }
        public float DestRotation
        {
            get => destRotationInt;
            set => destRotationInt = Mathf.Repeat(value, 360f);
        }
        private Color dockTintColor = Color.white;
        public Color DockTintColor
        {
            get { return dockTintColor; }
        }
        public int FollowDockSlot
        {
            get { return followDockSlot; }
        }
        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            cachedHaulerComp?.NotifyDroneGoneFromSlot(FollowDockSlot, parent,
                                                      destroyed: true);
            base.PostDestroy(mode, previousMap);
        }
        public override void PostDeSpawn(Map map,
                                         DestroyMode mode = DestroyMode.Vanish)
        {
            // bool treatAsDestroyed = mode != DestroyMode.Vanish;
            // cachedHaulerComp?.NotifyDroneGoneFromSlot(FollowDockSlot, parent,
            // destroyed: treatAsDestroyed);
            base.PostDeSpawn(map, mode);
        }
        public Rot4 MasterRotation
        {
            get
            {
                Pawn p = currentForceEscortTarget as Pawn;
                return (p != null) ? p.Rotation : Rot4.South;
            }
        }
        public static bool ShouldFlipDockUV(int slotIndex, Rot4 masterRot)
        {
            if (masterRot == Rot4.North || masterRot == Rot4.South)
            {
                return slotIndex >= 3;
            }
            return false;
        }
        public Verb CurrentEffectiveVerb => AttackVerb;
        public LocalTargetInfo LastAttackedTarget => lastAttackedTarget;
        public int LastAttackTargetTick => lastAttackTargetTick;
        public Thing Thing => parent;
        private bool WarmingUp => burstWarmupTicksLeft > 0;
        private int DashTicksCfg => Mathf.Max(6, Props.attackDashTicks);
        private int HoldTicksCfg => Mathf.Max(1, Props.attackHoldTicks);
        private float CurveSideCfg => Mathf.Max(0.1f, Props.attackCurveSideOffset);
        private float RangeFactorMinCfg =>
            Mathf.Clamp(Props.attackRangeFactorMin, 0.2f, 1.2f);
        private float RangeFactorMaxCfg =>
            Mathf.Clamp(Props.attackRangeFactorMax, RangeFactorMinCfg, 1.5f);
        private int UnifiedSearchRange
        {
            get
            {
                int raw =
                    Props.unifiedTargetSearchRange >
                    0 ? Props.unifiedTargetSearchRange : Props.MaxTargetSearchRange;
                return Mathf.Max(1, raw);
            }
        }
        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            CacheGizmos();
        }
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

            mapCache = parent.Map;
            maxSpeedPerTick = Props.moveSpeed / 60f;
            desiredFollowDistance =
                (Props.hoverRadiusMin + Props.hoverRadiusMax) * 0.5f;
            MakeGun();

            if (!respawningAfterLoad)
            {
                exactPosition = parent.Position.ToVector3Shifted();
                followAngle = Rand.Range(0f, 360f);
                ResetFollowAngleTimer();
                curMoveDir = Vector3.forward;
            }

            currentAttackTarget = LocalTargetInfo.Invalid;
        }
        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Values.Look(ref mode, "mode", DroneMoveMode.Landed);
            Scribe_Values.Look(ref __AssaultMode, "AssaultMode", false);

            Scribe_TargetInfo.Look(ref currentEscortTarget, "currentEscortTarget");
            Scribe_References.Look(ref currentForceEscortTarget,
                                   "currentForceEscortTarget");
            Scribe_TargetInfo.Look(ref currentAttackTarget, "currentAttackTarget");

            Scribe_Values.Look(ref exactPosition, "exactPosition");
            Scribe_Values.Look(ref moveTargetPos, "moveTargetPos");
            Scribe_Values.Look(ref currentSpeedVal, "currentSpeedVal", 0f);
            Scribe_Values.Look(ref curMoveDir, "curMoveDir", Vector3.forward);

            Scribe_Values.Look(ref followAngle, "followAngle", 0f);
            Scribe_Values.Look(ref ticksToNextFollowAngleChange,
                               "ticksToNextFollowAngleChange", 0);
            Scribe_Values.Look(ref followDockSlot, "followDockSlot", -1);

            Scribe_Values.Look(ref burstCooldownTicksLeft, "burstCooldownTicksLeft",
                               0);
            Scribe_Values.Look(ref burstWarmupTicksLeft, "burstWarmupTicksLeft", 0);
            Scribe_Values.Look(ref prevBursting, "prevBursting", false);
            Scribe_Deep.Look(ref gun, "gun", Array.Empty<object>());

            Scribe_Values.Look(ref forcedAttackActive, "forcedAttackActive", false);
            Scribe_Values.Look(ref manualForcedAttack, "manualForcedAttack", false);
            Scribe_Values.Look(ref currentAttackStyle, "currentAttackStyle",
                               AttackMoveStyle.Legacy);
            Scribe_Values.Look(ref attackStyleLockTicksLeft,
                               "attackStyleLockTicksLeft", 0);
            Scribe_Values.Look(ref attackStyleIntervalTicksLeft,
                               "attackStyleIntervalTicksLeft", 0);

            Scribe_Values.Look(ref lostTargetStickTicksLeft,
                               "lostTargetStickTicksLeft", 0);
            Scribe_Values.Look(ref lastKnownEnemyPos, "lastKnownEnemyPos");
            Scribe_Values.Look(ref lastKnownEnemyPosValid, "lastKnownEnemyPosValid",
                               false);

            Scribe_Values.Look(ref plannedFirePos, "plannedFirePos");
            Scribe_Values.Look(ref plannedEnemyAnchor, "plannedEnemyAnchor");
            Scribe_Values.Look(ref plannedFirePosValid, "plannedFirePosValid", false);
            Scribe_Values.Look(ref firePosCooldownTicksLeft,
                               "firePosCooldownTicksLeft", 0);
            Scribe_Values.Look(ref noHitTicks, "noHitTicks", 0);

            Scribe_Values.Look(ref attackDashActive, "attackDashActive", false);
            Scribe_Values.Look(ref attackDashTick, "attackDashTick", 0);
            Scribe_Values.Look(ref attackHoldTicksLeft, "attackHoldTicksLeft", 0);
            Scribe_Values.Look(ref attackRepositionIndex, "attackRepositionIndex", 0);
            Scribe_Values.Look(ref attackDashStart, "attackDashStart");
            Scribe_Values.Look(ref attackDashControl, "attackDashControl");
            Scribe_Values.Look(ref attackDashEnd, "attackDashEnd");

            Scribe_Values.Look(ref bezA, "bezA");
            Scribe_Values.Look(ref bezB, "bezB");
            Scribe_Values.Look(ref bezC, "bezC");
            Scribe_Values.Look(ref bezTicksLeft, "bezTicksLeft", 0);
            Scribe_Values.Look(ref bezTotalTicks, "bezTotalTicks", 0);
            Scribe_Values.Look(ref orbitSign, "orbitSign", 0);
            Scribe_Values.Look(ref orbitRadiusMul, "orbitRadiusMul", 0f);
            Scribe_Values.Look(ref legacyHoldCooldownTicks, "legacyHoldCooldownTicks",
                               0);
            Scribe_Values.Look(ref dockTintColor, "dockTintColor", Color.white);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                cachedHaulerComp = null;
                nextHaulerRefreshTick = 0;

                if (Props.turretDef != null)
                {
                    if (gun == null && parent != null)
                        MakeGun();
                    else
                        UpdateGunVerbs();
                }
            }
        }
        public void SetMaster(Thing thing)
        {
            if (thing != null)
                currentForceEscortTarget = thing;
        }
        public void SetFollowDockSlot(int slotIndex)
        {
            followDockSlot = slotIndex;
            cachedHaulerComp = null;
            nextHaulerRefreshTick = 0;
        }
        public void AssignFollowAngle(float angle) => followAngle = angle;

        public void ResetCurrentTarget()
        {
            currentAttackTarget = LocalTargetInfo.Invalid;
            burstWarmupTicksLeft = 0;
        }
        public void SetDockTintColor(Color c)
        {
            dockTintColor = c;
        }
        public void SnapTo(Vector3 worldPos)
        {
            worldPos.y = 0f;

            exactPosition = worldPos;
            moveTargetPos = worldPos;

            currentSpeedVal = 0f;
            curMoveDir = Vector3.forward;
            if (parent != null)
            {
                parent.Position = worldPos.ToIntVec3();
            }
        }
        private static int HashInt(int x)
        {
            unchecked
            {
                x ^= x >> 16;
                x *= 0x7feb352d;
                x ^= x >> 15;
                x *= (int)0x846ca68b;
                x ^= x >> 16;
                return x;
            }
        }

        private float Seed01(int salt)
        {
            int h = HashInt(parent.thingIDNumber * 1103515245 + salt);
            uint u = (uint)h;
            return (u & 0x00FFFFFF) / 16777216f;
        }
        private float SeedSigned(int salt) => Seed01(salt) * 2f - 1f;

        private bool HasValidAttackTarget()
        {
            return currentAttackTarget.IsValid && currentAttackTarget.Thing != null &&
                   currentAttackTarget.Thing.Spawned &&
                   !currentAttackTarget.Thing.Destroyed;
        }
        public override void CompTick()
        {
            base.CompTick();
            if (!parent.Spawned || mode == DroneMoveMode.Landed)
            {
                currentSpeedVal = 0f;
                prevBursting = false;
                return;
            }

            if (currentForceEscortTarget != null &&
                currentForceEscortTarget.DestroyedOrNull())
                currentForceEscortTarget = null;

            bool recalling = cachedHaulerComp != null && cachedHaulerComp.isRecalling;

            bool hasValidTarget = false;
            bool activeAttackIntent = false;

            if (!recalling)
            {
                hasValidTarget = HasValidAttackTarget();
                activeAttackIntent = (AssaultMode || forcedAttackActive);

                if (hasValidTarget)
                {
                    lostTargetStickTicksLeft = Props.lostTargetStickTicks;
                    lastKnownEnemyPos = currentAttackTarget.Thing.DrawPos;
                    lastKnownEnemyPos.y = 0f;
                    lastKnownEnemyPosValid = true;
                }
                else if (lostTargetStickTicksLeft > 0)
                {
                    lostTargetStickTicksLeft--;
                }

                bool guardIntent =
                    (mode == DroneMoveMode.Following && AttackVerb != null);
                if ((activeAttackIntent || guardIntent) && !hasValidTarget &&
                    parent.IsHashIntervalTick(30))
                {
                    float scanRange =
                        activeAttackIntent ? UnifiedSearchRange : AttackVerb.EffectiveRange;
                    scanRange = Mathf.Max(1f, scanRange);

                    IAttackTarget at = AttackTargetFinder.BestAttackTarget(
                        this, TargetScanFlags.NeedThreat | TargetScanFlags.NeedLOSToAll,
                        null, 0f, scanRange, parent.Position, scanRange, false, true, true,
                        true);

                    Thing found = at?.Thing;
                    if (found != null)
                    {
                        currentAttackTarget = new LocalTargetInfo(found);
                        hasValidTarget = true;
                    }
                }

                if (currentForceEscortTarget != null)
                {
                    if (hasValidTarget && activeAttackIntent)
                    {
                        if (mode != DroneMoveMode.Attacking)
                            StartAttacking(currentAttackTarget.Thing, forcedAttackActive,
                                           manualForcedAttack);
                    }
                    else
                    {
                        bool keepAttackLinger =
                            (mode == DroneMoveMode.Attacking && activeAttackIntent &&
                             lostTargetStickTicksLeft > 0);
                        if (!keepAttackLinger && mode != DroneMoveMode.Following)
                            StartFollowing(currentForceEscortTarget);
                    }
                }
                else
                {
                    if (hasValidTarget && activeAttackIntent)
                    {
                        if (mode != DroneMoveMode.Attacking)
                            StartAttacking(currentAttackTarget.Thing, forcedAttackActive,
                                           manualForcedAttack);
                    }
                    else if (mode == DroneMoveMode.Attacking &&
                             !(activeAttackIntent && lostTargetStickTicksLeft > 0))
                    {
                        SetMode(DroneMoveMode.Idle);
                    }
                }
            }
            else
            {
                // 召回中：不索敌，不攻击；其余移动/跟随逻辑照常
                forcedAttackActive = false;
                manualForcedAttack = false;
                ResetCurrentTarget();

                if (mode == DroneMoveMode.Attacking)
                {
                    if (currentForceEscortTarget != null) StartFollowing(currentForceEscortTarget);
                    else SetMode(DroneMoveMode.Idle);
                }
            }

            switch (mode)
            {
                case DroneMoveMode.Idle:
                    Decelerate();
                    break;
                case DroneMoveMode.MovingTo:
                    HandleMoveTo();
                    break;
                case DroneMoveMode.Following:
                    HandleFollowing();
                    break;
                case DroneMoveMode.Attacking:
                    HandleAttacking();
                    break;
            }

            bool burstingNow = false;
            if (AttackVerb != null)
            {
                AttackVerb.VerbTick();
                burstingNow = (AttackVerb.state == VerbState.Bursting);

                if (!recalling && mode != DroneMoveMode.Landed)
                {
                    if (AttackVerb.state != VerbState.Bursting)
                    {
                        if (WarmingUp)
                        {
                            burstWarmupTicksLeft--;
                            if (HasValidAttackTarget())
                            {
                                if (burstWarmupTicksLeft == 0)
                                {
                                    bool casted = AttackVerb.TryStartCastOn(
                                        currentAttackTarget, false, true, false, true);
                                    if (casted)
                                    {
                                        TriggerMuzzleFlash();
                                    }
                                    lastAttackTargetTick = Find.TickManager.TicksGame;
                                    lastAttackedTarget = currentAttackTarget;
                                }
                            }
                            else
                            {
                                ResetCurrentTarget();
                            }
                        }
                        else
                        {
                            if (burstCooldownTicksLeft > 0)
                                burstCooldownTicksLeft--;

                            if (burstCooldownTicksLeft <= 0 &&
                                parent.IsHashIntervalTick(45))
                            {
                                if (HasValidAttackTarget())
                                {
                                    if (AttackVerb.CanHitTarget(currentAttackTarget) &&
                                        IsAimAligned())
                                        burstWarmupTicksLeft = 1;
                                }
                            }
                        }
                    }
                }
                else if (recalling)
                {
                    // 召回中禁用新的开火预热
                    burstWarmupTicksLeft = 0;
                }
            }

            if (mode == DroneMoveMode.Attacking && Props.enableCurveAttack &&
                prevBursting && !burstingNow)
            {
                Thing t =
                    currentAttackTarget.IsValid ? currentAttackTarget.Thing : null;
                if (t != null && t.Spawned && !t.Destroyed)
                    TrySwitchAttackStyle(t, true);
            }

            prevBursting = burstingNow;
            if (muzzleFlashTicksLeft > 0)
                muzzleFlashTicksLeft--;

            TurretTick();
        }
        private bool IsAimAligned()
        {
            return Mathf.Abs(Mathf.DeltaAngle(CurRotation, DestRotation)) <=
                   AimAlignedEpsilon;
        }

        private void Decelerate()
        {
            currentSpeedVal =
                Mathf.MoveTowards(currentSpeedVal, 0f, Props.acceleration * 1.5f);
            if (currentSpeedVal > 0.01f)
            {
                Vector3 newExactPos = exactPosition + curMoveDir * currentSpeedVal;
                UpdatePosition(newExactPos);
            }
        }

        private void HandleMoveTo()
        {
            ExecuteMoveRaw(moveTargetPos, 1f);
            if (Vector3.Distance(exactPosition, moveTargetPos) < 0.5f)
                SetMode(DroneMoveMode.Idle);
        }

        private void ExecuteMoveRaw(Vector3 targetPos, float targetSpeedFactor)
        {
            moveTargetPos = targetPos;
            Vector3 desiredDir = targetPos - exactPosition;
            float dist = desiredDir.magnitude;
            if (dist < 0.05f)
            {
                currentSpeedVal = Mathf.Lerp(currentSpeedVal, 0f, 0.2f);
                return;
            }
            desiredDir.Normalize();

            float targetSpeed = maxSpeedPerTick * Mathf.Clamp01(targetSpeedFactor);
            if (targetSpeedFactor > 0f)
                currentSpeedVal =
                    Mathf.Lerp(currentSpeedVal, targetSpeed, Props.acceleration);
            else
                currentSpeedVal =
                    Mathf.MoveTowards(currentSpeedVal, 0f, Props.acceleration * 2f);

            if (currentSpeedVal <= 0.001f)
                return;

            float turnRateDynamic = Props.turnRate;
            if (mode == DroneMoveMode.Attacking &&
                currentAttackStyle == AttackMoveStyle.Legacy)
                turnRateDynamic *= Mathf.Max(1f, Props.legacyTurnBoost);
            if (dist < 2f)
                turnRateDynamic *= 3f;

            if (curMoveDir == Vector3.zero)
                curMoveDir = desiredDir;
            curMoveDir = Vector3.Slerp(curMoveDir, desiredDir, turnRateDynamic);

            Vector3 moveVec = curMoveDir * currentSpeedVal;
            if (moveVec.magnitude > dist)
                moveVec = desiredDir * dist;

            UpdatePosition(exactPosition + moveVec);
        }
        private void UpdatePosition(Vector3 newExactPos)
        {
            exactPosition = newExactPos;

            IntVec3 oldPos = parent.Position;
            IntVec3 newGridPos = newExactPos.ToIntVec3();

            if (newGridPos != oldPos)
            {
                if (!newGridPos.InBounds(mapCache))
                {
                    SetMode(DroneMoveMode.Idle);
                    exactPosition.x =
                        Mathf.Clamp(exactPosition.x, 0, mapCache.Size.x - 1);
                    exactPosition.z =
                        Mathf.Clamp(exactPosition.z, 0, mapCache.Size.z - 1);
                    parent.Position = exactPosition.ToIntVec3();
                    currentSpeedVal = 0f;
                }
                else
                {
                    parent.Position = newGridPos;
                }
            }
        }
        private void HandleFollowing()
        {
            if (!currentEscortTarget.HasThing ||
                currentEscortTarget.Thing.Destroyed ||
                !currentEscortTarget.Thing.Spawned)
            {
                SetMode(DroneMoveMode.Idle);
                return;
            }

            if (ticksToNextFollowAngleChange > 0)
                ticksToNextFollowAngleChange--;
            if (ticksToNextFollowAngleChange <= 0)
            {
                followAngle = Rand.Range(0f, 360f);
                ResetFollowAngleTimer();
            }

            if (!TryGetFollowAnchorAndAngle(out Vector3 anchorPos,
                                            out float fixedAngle))
            {
                SetMode(DroneMoveMode.Idle);
                return;
            }

            float dist = (anchorPos - exactPosition).MagnitudeHorizontal();
            float speedFactor =
                dist > 2f ? 1f : (dist > 0.8f ? 0.6f : (dist > 0.22f ? 0.25f : 0f));
            ExecuteMoveRaw(anchorPos, speedFactor);

            if (dist <= 0.15f)
            {
                currentSpeedVal =
                    Mathf.MoveTowards(currentSpeedVal, 0f, Props.acceleration * 2.8f);
                if (currentSpeedVal < 0.01f)
                    currentSpeedVal = 0f;
            }
        }
        private void HandleAttacking()
        {
            if (AttackVerb == null)
            {
                forcedAttackActive = false;
                manualForcedAttack = false;
                if (currentForceEscortTarget != null)
                    StartFollowing(currentForceEscortTarget);
                else
                    SetMode(DroneMoveMode.Idle);
                return;
            }

            Thing t = currentAttackTarget.IsValid ? currentAttackTarget.Thing : null;
            bool valid = t != null && t.Spawned && !t.Destroyed;

            if (!valid)
            {
                forcedAttackActive = false;
                manualForcedAttack = false;
                if (currentForceEscortTarget != null)
                    StartFollowing(currentForceEscortTarget);
                else
                    SetMode(DroneMoveMode.Idle);
                return;
            }
            if (attackStyleIntervalTicksLeft > 0)
                attackStyleIntervalTicksLeft--;
            if (attackStyleIntervalTicksLeft <= 0)
            {
                attackStyleIntervalTicksLeft = 240 + (parent.thingIDNumber % 61);
                TrySwitchAttackStyle(t, false);
            }

            bool canShootHere = CanHitFromPos(exactPosition, t);
            if (!canShootHere)
                noHitTicks++;
            else
                noHitTicks = 0;
            if (!canShootHere)
            {
                if (WarmingUp || AttackVerb.state == VerbState.Bursting)
                {
                    currentSpeedVal =
                        Mathf.MoveTowards(currentSpeedVal, 0f, Props.acceleration * 3f);
                    return;
                }
                if (EnsurePlannedFirePos(t))
                {
                    if (UseCurveLikeAuto())
                    {
                        if (!attackDashActive)
                            BeginAttackDashTo(plannedFirePos, true);
                        TickAttackDash(true);
                    }
                    else
                    {
                        bezTicksLeft = 0;
                        ExecuteMoveRaw(plannedFirePos, 0.95f);
                    }
                }
                else
                {
                    Vector3 enemy = t.DrawPos;
                    enemy.y = 0f;

                    Vector3 fromEnemy = exactPosition - enemy;
                    fromEnemy.y = 0f;
                    if (fromEnemy.sqrMagnitude < 1e-4f)
                        fromEnemy = Vector3.forward;

                    float effRange = Mathf.Max(3f, AttackVerb.EffectiveRange);
                    float desiredDist =
                        Mathf.Clamp(effRange * 0.55f, 2.2f, effRange * 0.92f);

                    float curDist = fromEnemy.magnitude;
                    Vector3 radial = fromEnemy / Mathf.Max(0.001f, curDist);

                    float sign = ((HashInt(parent.thingIDNumber) & 1) == 0) ? 1f : -1f;
                    Vector3 tangent = new Vector3(-radial.z, 0f, radial.x) * sign;
                    float rangeError = curDist - desiredDist;
                    Vector3 seekDir =
                        tangent * 0.95f +
                        (-radial * Mathf.Clamp(rangeError * 0.55f, -1.1f, 1.1f));

                    if (seekDir.sqrMagnitude < 1e-6f)
                        seekDir = -radial;
                    seekDir.Normalize();

                    float step = 4.2f + Seed01(8801) * 2.2f;
                    Vector3 approachPoint = exactPosition + seekDir * step;

                    if (mapCache != null)
                    {
                        approachPoint.x =
                            Mathf.Clamp(approachPoint.x, 1f, mapCache.Size.x - 2f);
                        approachPoint.z =
                            Mathf.Clamp(approachPoint.z, 1f, mapCache.Size.z - 2f);
                    }
                    approachPoint.y = 0f;

                    ExecuteMoveRaw(approachPoint, 1f);
                }
                int giveUpTicks = manualForcedAttack ? 600 : 360;
                if (noHitTicks >= giveUpTicks)
                {
                    forcedAttackActive = false;
                    manualForcedAttack = false;
                    plannedFirePosValid = false;
                    firePosCooldownTicksLeft = 0;
                    ResetCurrentTarget();

                    if (currentForceEscortTarget != null)
                        StartFollowing(currentForceEscortTarget);
                    else
                        SetMode(DroneMoveMode.Idle);
                }

                return;
            }
            if (UseCurveLikeAuto())
            {
                HandleAttackingCurveSmooth(t);
                return;
            }

            if (currentAttackStyle == AttackMoveStyle.Legacy)
                HandleAttackingLegacyInRange(t);
            else
                HandleAttackingCurveSmooth(t);
        }
        private bool UseCurveLikeAuto()
        {
            return manualForcedAttack ||
                   currentAttackStyle == AttackMoveStyle.CurveDash;
        }

        private void TrySwitchAttackStyle(Thing target, bool burstEndedTrigger)
        {
            if (manualForcedAttack)
            {
                SetAttackStyle(AttackMoveStyle.CurveDash);
                return;
            }

            if (!Props.enableCurveAttack)
            {
                SetAttackStyle(AttackMoveStyle.Legacy);
                return;
            }

            if (attackStyleLockTicksLeft > 0)
            {
                attackStyleLockTicksLeft--;
                return;
            }

            if (attackDashActive || WarmingUp ||
                AttackVerb.state == VerbState.Bursting)
                return;

            float dist = (target.DrawPos - exactPosition).MagnitudeHorizontal();
            float farThreshold = AttackVerb.EffectiveRange *
                                 Mathf.Max(1f, Props.forceCurveFarDistanceFactor);

            if (dist >= farThreshold)
            {
                SetAttackStyle(AttackMoveStyle.CurveDash);
                attackStyleLockTicksLeft = 240 + (parent.thingIDNumber % 181);
                return;
            }

            if (!Props.randomAttackMoveStyle)
            {
                SetAttackStyle(Props.attackMoveStyle == DroneAttackMoveStyle.CurveDash
                                   ? AttackMoveStyle.CurveDash
                                   : AttackMoveStyle.Legacy);
                attackStyleLockTicksLeft = 240 + (parent.thingIDNumber % 181);
                return;
            }

            float switchChance = burstEndedTrigger ? 0.55f : 0.35f;
            switchChance = Mathf.Clamp01(switchChance + SeedSigned(9107) * 0.08f);
            if (Rand.Value > switchChance)
                return;

            float chance = Props.curveDashChance;
            if (Mathf.Approximately(chance, 0f) && Props.curveAttackChance > 0f)
                chance = Props.curveAttackChance;
            chance = Mathf.Clamp01(chance + SeedSigned(9109) * 0.10f);

            AttackMoveStyle rolled = Rand.Value < chance ? AttackMoveStyle.CurveDash
                                                         : AttackMoveStyle.Legacy;
            SetAttackStyle(rolled);

            attackStyleLockTicksLeft = 240 + (parent.thingIDNumber % 181);
        }

        private void SetAttackStyle(AttackMoveStyle style)
        {
            if (currentAttackStyle == style)
                return;
            currentAttackStyle = style;
            ResetAttackPatternState();
        }

        private void ResetAttackPatternState()
        {
            attackDashActive = false;
            attackDashTick = 0;
            attackHoldTicksLeft = 0;
            attackRepositionIndex = 0;

            plannedFirePosValid = false;
            firePosCooldownTicksLeft = 0;
            noHitTicks = 0;
            curveSmoothedDir = Vector3.zero;
            bezTicksLeft = 0;
            legacyHoldCooldownTicks = 0;

            unseenTicks = 0;
            closeInRecalcTicksLeft = 0;
            closeInPosValid = false;
        }
        private bool EnsurePlannedFirePos(Thing target)
        {
            if (firePosCooldownTicksLeft > 0)
                firePosCooldownTicksLeft--;

            Vector3 enemy = target.DrawPos;
            enemy.y = 0f;

            float enemyMoveRecalcDist = 2.4f + Seed01(2001) * 1.6f;
            int minNoHitTicks = 18 + (parent.thingIDNumber % 17);
            int recalcCooldown = 45 + (parent.thingIDNumber % 61);

            if (plannedFirePosValid)
            {
                if (CanHitFromPos(plannedFirePos, target))
                    return true;

                bool enemyMovedFar = (enemy - plannedEnemyAnchor).sqrMagnitude >
                                     enemyMoveRecalcDist * enemyMoveRecalcDist;
                if (!enemyMovedFar && noHitTicks < minNoHitTicks)
                    return true;
                if (firePosCooldownTicksLeft > 0)
                    return true;
            }

            if (firePosCooldownTicksLeft > 0 && !plannedFirePosValid)
                return false;

            if (TryFindNextAttackPos(target, out Vector3 bestPos))
            {
                plannedFirePos = bestPos;
                plannedEnemyAnchor = enemy;
                plannedFirePosValid = true;
                firePosCooldownTicksLeft = recalcCooldown;
                return true;
            }

            plannedFirePosValid = false;
            return false;
        }

        private bool CanHitFromPos(Vector3 pos, Thing target)
        {
            if (AttackVerb == null || target == null || !target.Spawned ||
                target.Destroyed)
                return false;

            IntVec3 from = pos.ToIntVec3();
            if (!from.InBounds(mapCache))
                return false;
            return AttackVerb.CanHitTargetFrom(from, target);
        }

        private bool TryFindNextAttackPos(Thing target, out Vector3 bestPos)
        {
            bestPos = exactPosition;
            if (AttackVerb == null || target == null || mapCache == null)
                return false;

            Vector3 enemy = target.DrawPos;
            enemy.y = 0f;

            float desiredRange = AttackVerb.verbProps.range *
                                 Mathf.Lerp(RangeFactorMinCfg, RangeFactorMaxCfg,
                                            0.55f + SeedSigned(3001) * 0.10f);
            desiredRange *= Mathf.Lerp(0.96f, 1.04f, Seed01(3003));

            int samples = 18 + (parent.thingIDNumber % 7);
            float baseAng =
                (parent.thingIDNumber * 17 + (Find.TickManager.TicksGame / 30) * 23) %
                360f;
            baseAng += SeedSigned(3007) * 35f;

            float bestScore = float.MaxValue;
            bool found = false;
            for (int i = 0; i < samples; i++)
            {
                float ang = (baseAng + i * (360f / samples)) * Mathf.Deg2Rad;
                Vector3 p = enemy + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) *
                                        desiredRange;

                p.x = Mathf.Clamp(p.x, 1f, mapCache.Size.x - 2f);
                p.z = Mathf.Clamp(p.z, 1f, mapCache.Size.z - 2f);
                IntVec3 cell = p.ToIntVec3();
                if (!cell.InBounds(mapCache) || !cell.Walkable(mapCache))
                    continue;
                if (!CanHitFromPos(p, target))
                    continue;

                float moveDist = (p - exactPosition).MagnitudeHorizontal();
                if (moveDist < 1.2f)
                    continue;

                float score = moveDist;
                if (score < bestScore)
                {
                    bestScore = score;
                    bestPos = p;
                    found = true;
                }
            }

            return found;
        }

        private void HandleAttackingLegacyInRange(Thing target)
        {
            if (WarmingUp || AttackVerb.state == VerbState.Bursting)
            {
                currentSpeedVal =
                    Mathf.MoveTowards(currentSpeedVal, 0f, Props.acceleration * 3f);
                return;
            }

            if (legacyHoldCooldownTicks > 0)
                legacyHoldCooldownTicks--;
            if (attackHoldTicksLeft > 0)
            {
                attackHoldTicksLeft--;
                currentSpeedVal =
                    Mathf.MoveTowards(currentSpeedVal, 0f, Props.acceleration * 3f);
                return;
            }

            int seed = parent.thingIDNumber;
            float rangeVariation = 1.1f + (Mathf.Abs(seed % 25) / 100f);
            float desiredRange = AttackVerb.verbProps.range * rangeVariation;

            Vector3 enemyPos = target.DrawPos;
            enemyPos.y = 0f;

            Vector3 vecToEnemy = enemyPos - exactPosition;
            vecToEnemy.y = 0f;

            float currentDist = vecToEnemy.magnitude;

            InitOrbitParamsIfNeeded();
            float orbitRadius = Mathf.Max(2f, desiredRange * orbitRadiusMul);

            Vector3 radial = exactPosition - enemyPos;
            radial.y = 0f;
            if (radial.sqrMagnitude < 0.01f)
                radial = -vecToEnemy.normalized;
            else
                radial.Normalize();

            float minChord = 2.4f + Seed01(4011) * 1.0f;

            if (bezTicksLeft <= 0)
            {
                if (!StartBezierOrbitSegment(enemyPos, orbitRadius, radial, minChord))
                {
                    LegacyTangentDrift(enemyPos, orbitRadius, currentDist);
                }
            }
            if (bezTicksLeft > 0)
            {
                float t = 1f - (bezTicksLeft / (float)bezTotalTicks);
                float lookAhead = 0.20f + Seed01(4021) * 0.10f;
                float tLook = Mathf.Clamp01(t + lookAhead);

                Vector3 targetPoint = Bezier2(bezA, bezB, bezC, tLook);
                float speedFactor =
                    (Mathf.Abs(currentDist - orbitRadius) > 4f) ? 0.9f : 0.62f;

                ExecuteMoveRaw(targetPoint, speedFactor);
                bezTicksLeft--;
            }

            bool canHit = AttackVerb.CanHitTarget(currentAttackTarget);
            if (canHit && IsAimAligned() && currentDist <= desiredRange + 1.2f &&
                legacyHoldCooldownTicks <= 0)
            {
                attackHoldTicksLeft = Mathf.Max(1, Props.legacyAttackHoldTicks);
                legacyHoldCooldownTicks = Props.legacyAttackHoldTicks + 10;
            }
        }
        private void LegacyTangentDrift(Vector3 enemyPos, float orbitRadius,
                                        float currentDist)
        {
            Vector3 fromEnemy = exactPosition - enemyPos;
            fromEnemy.y = 0f;
            if (fromEnemy.sqrMagnitude < 0.001f)
                fromEnemy = new Vector3(1f, 0f, 0f);

            Vector3 radial = fromEnemy.normalized;
            float tangentSign = ((parent.thingIDNumber & 1) == 0) ? 1f : -1f;
            Vector3 tangent = new Vector3(-radial.z, 0f, radial.x) * tangentSign;

            float rangeError = currentDist - orbitRadius;
            Vector3 dir =
                tangent + (-radial * Mathf.Clamp(rangeError * 0.75f, -0.85f, 0.85f));
            if (dir.sqrMagnitude < 1e-6f)
                dir = tangent;
            dir.Normalize();

            float step = 3.0f + Seed01(4201) * 2.0f;
            Vector3 targetPoint = exactPosition + dir * step;
            targetPoint.x = Mathf.Clamp(targetPoint.x, 1f, mapCache.Size.x - 2f);
            targetPoint.z = Mathf.Clamp(targetPoint.z, 1f, mapCache.Size.z - 2f);

            ExecuteMoveRaw(targetPoint, 0.58f);
        }

        private void HandleAttackingCurveSmooth(Thing target)
        {
            if (AttackVerb == null || target == null || target.DestroyedOrNull() ||
                !target.Spawned)
            {
                SetMode(DroneMoveMode.Idle);
                return;
            }

            bool canShootHere = CanHitFromPos(exactPosition, target);

            if (WarmingUp || AttackVerb.state == VerbState.Bursting)
            {
                if (canShootHere)
                    CurveInRangeDrift(target, 0.42f);
                else
                    currentSpeedVal =
                        Mathf.MoveTowards(currentSpeedVal, 0f, Props.acceleration * 2.8f);
                if (currentSpeedVal < 0.01f)
                    currentSpeedVal = 0f;
                return;
            }

            if (attackHoldTicksLeft > 0)
            {
                attackHoldTicksLeft--;
                if (canShootHere)
                    CurveInRangeDrift(target, 0.46f);
                else
                    currentSpeedVal =
                        Mathf.MoveTowards(currentSpeedVal, 0f, Props.acceleration * 2.8f);
                if (currentSpeedVal < 0.01f)
                    currentSpeedVal = 0f;
                return;
            }

            if (attackDashActive)
            {
                TickAttackDash(true);
                return;
            }

            if (canShootHere)
            {
                CurveInRangeDrift(target, 0.55f);
                return;
            }

            if (EnsurePlannedFirePos(target))
            {
                if (BeginAttackDashTo(plannedFirePos, true))
                    TickAttackDash(true);
                else
                    ExecuteMoveRaw(plannedFirePos, 0.9f);
            }
            else
            {
                currentSpeedVal =
                    Mathf.MoveTowards(currentSpeedVal, 0f, Props.acceleration * 2.8f);
                if (currentSpeedVal < 0.01f)
                    currentSpeedVal = 0f;
            }
        }
        private void CurveInRangeDrift(Thing target, float speedFactor)
        {
            Vector3 enemy = target.DrawPos;
            enemy.y = 0f;

            Vector3 fromEnemy = exactPosition - enemy;
            if (fromEnemy.sqrMagnitude < 0.001f)
                fromEnemy = new Vector3(1f, 0f, 0f);

            float desiredRange = AttackVerb.verbProps.range *
                                 Mathf.Lerp(RangeFactorMinCfg, RangeFactorMaxCfg,
                                            0.60f + SeedSigned(5101) * 0.08f);
            float curRange = fromEnemy.magnitude;

            Vector3 radial = fromEnemy.normalized;
            float tangentSign = ((HashInt(parent.thingIDNumber) & 1) == 0) ? 1f : -1f;
            Vector3 tangent = new Vector3(-radial.z, 0f, radial.x) * tangentSign;

            float rangeError = curRange - desiredRange;
            Vector3 dir =
                tangent + (-radial * Mathf.Clamp(rangeError * 0.85f, -0.95f, 0.95f));
            if (dir.sqrMagnitude < 1e-6f)
                dir = tangent;
            dir.Normalize();

            float step = 3.2f + Seed01(5107) * 2.4f;
            Vector3 targetPoint = exactPosition + dir * step;
            targetPoint.x = Mathf.Clamp(targetPoint.x, 1f, mapCache.Size.x - 2f);
            targetPoint.z = Mathf.Clamp(targetPoint.z, 1f, mapCache.Size.z - 2f);

            ExecuteMoveRaw(targetPoint, speedFactor);
        }

        private bool BeginAttackDashTo(Vector3 end, bool curveMode)
        {
            attackDashStart = exactPosition;
            attackDashEnd = end;
            moveTargetPos = end;

            Vector3 chord = attackDashEnd - attackDashStart;
            chord.y = 0f;

            float chordLen = chord.magnitude;
            float minLen = 3.2f + Seed01(6005) * 2.2f;
            if (chordLen < minLen)
            {
                attackDashActive = false;
                return false;
            }

            float baseSign = (attackRepositionIndex % 2 == 0) ? 1f : -1f;
            float idSign = ((HashInt(parent.thingIDNumber) & 1) == 0) ? 1f : -1f;
            float sign = baseSign * idSign;

            float sideMul = curveMode ? (1.00f + SeedSigned(6001) * 0.35f)
                                      : (0.90f + SeedSigned(6003) * 0.25f);
            float sideLen = CurveSideCfg * sideMul;

            Vector3 side =
                new Vector3(-chord.z, 0f, chord.x).normalized * (sideLen * sign);
            attackDashControl = (attackDashStart + attackDashEnd) * 0.5f + side;

            attackDashTick = 0;
            attackDashActive = true;
            attackRepositionIndex++;
            return true;
        }

        private void TickAttackDash(bool curveMode)
        {
            int baseTicks = DashTicksCfg;
            int total =
                curveMode
                    ? Mathf.RoundToInt(baseTicks * (1.45f + SeedSigned(6101) * 0.20f))
                    : baseTicks;
            total = Mathf.Max(6, total);

            float t = (attackDashTick + 1f) / total;
            float eased = SmoothStep(t);

            Vector3 prev = exactPosition;
            Vector3 next =
                Bezier2(attackDashStart, attackDashControl, attackDashEnd, eased);
            Vector3 delta = next - prev;

            if (delta.sqrMagnitude > 1e-6f)
                curMoveDir = delta.normalized;
            currentSpeedVal = delta.magnitude;
            UpdatePosition(next);

            attackDashTick++;
            if (attackDashTick >= total ||
                (attackDashEnd - exactPosition).sqrMagnitude < 0.04f)
            {
                attackDashActive = false;
                attackHoldTicksLeft = Mathf.Max(6, HoldTicksCfg / 2);
                currentSpeedVal = 0f;
            }
        }

        private void InitOrbitParamsIfNeeded()
        {
            if (orbitSign != 0)
                return;

            int seed = parent.thingIDNumber;
            orbitSign = ((HashInt(seed) & 1) == 0) ? 1 : -1;
            orbitRadiusMul = 0.90f + Seed01(7001) * 0.45f;
        }

        private bool StartBezierOrbitSegment(Vector3 enemyPos, float radius,
                                             Vector3 radial, float minChord)
        {
            float stepDeg = Mathf.Lerp(28f, 68f, Seed01(7011));
            stepDeg *= Mathf.Lerp(0.92f, 1.10f, Seed01(7013));
            float chord = 2f * radius * Mathf.Sin(stepDeg * Mathf.Deg2Rad * 0.5f);
            if (chord < minChord)
                return false;
            Vector3 endRadial =
                Quaternion.AngleAxis(stepDeg * orbitSign, Vector3.up) * radial;
            Vector3 midRadial =
                Quaternion.AngleAxis(stepDeg * 0.5f * orbitSign, Vector3.up) * radial;
            Vector3 tangentMid =
                new Vector3(midRadial.z, 0f, -midRadial.x) * orbitSign;

            bezA = exactPosition;
            bezC = enemyPos + endRadial * radius;

            float curve = radius * Mathf.Lerp(0.22f, 0.55f, Seed01(7021));
            float tangentPush = Mathf.Lerp(0.75f, 1.35f, Seed01(7023));
            bezB = enemyPos + midRadial * radius + tangentMid * (curve * tangentPush);

            bezTotalTicks = Mathf.RoundToInt(Mathf.Lerp(18f, 34f, Seed01(7031)));
            bezTicksLeft = bezTotalTicks;
            return true;
        }
        private bool TryGetFollowAnchorAndAngle(out Vector3 anchorPos,
                                                out float fixedAngle)
        {
            anchorPos = exactPosition;
            fixedAngle = CurRotation;

            Thing escort = currentEscortTarget.HasThing ? currentEscortTarget.Thing
                                                        : currentForceEscortTarget;
            Pawn pawn = escort as Pawn;
            if (pawn == null || pawn.DestroyedOrNull() || !pawn.Spawned)
                return false;

            if (cachedHaulerComp == null ||
                Find.TickManager.TicksGame >= nextHaulerRefreshTick ||
                cachedHaulerComp.parent.DestroyedOrNull())
            {
                cachedHaulerComp = null;
                if (pawn.apparel != null)
                {
                    List<Apparel> worn = pawn.apparel.WornApparel;
                    for (int i = 0; i < worn.Count; i++)
                    {
                        var comp = worn[i].TryGetComp<CompFunnelHauler>();
                        if (comp != null)
                        {
                            cachedHaulerComp = comp;
                            break;
                        }
                    }
                }
                nextHaulerRefreshTick = Find.TickManager.TicksGame + 120;
            }
            int ticknow = GenTicks.TicksGame;
            if (cachedHaulerComp != null && followDockSlot >= 0)
            {
                Vector3 localOffset;
                float ang;
                int t = GenTicks.TicksGame;
                if (cachedHaulerComp.TryGetDockTransformWithBob(
                        followDockSlot, pawn.Rotation, t, out localOffset, out ang))
                {
                    anchorPos = pawn.DrawPos + localOffset;
                    anchorPos.y = 0f;
                    fixedAngle = ang;
                    return true;
                }
            }
            float r = desiredFollowDistance;
            float rad = followAngle * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(
                0f, 0f, cachedHaulerComp.DockBobOffsetZ(followDockSlot, ticknow));
            anchorPos = pawn.DrawPos + offset;
            anchorPos.y = 0f;
            fixedAngle = (pawn.DrawPos - anchorPos).AngleFlat();
            return true;
        }
        public void TurretTick()
        {
            float speedRatio =
                (maxSpeedPerTick > 1e-6f) ? (currentSpeedVal / maxSpeedPerTick) : 0f;
            bool fastMoving = speedRatio >= 0.45f;

            Thing tgt =
                (currentAttackTarget.IsValid ? currentAttackTarget.Thing : null);
            bool hasTgt = tgt != null && tgt.Spawned && !tgt.Destroyed;

            float targetAngle;
            if (hasTgt)
            {
                targetAngle = (tgt.DrawPos - parent.DrawPos).AngleFlat();
            }
            else if (fastMoving)
            {
                Vector3 dir = (moveTargetPos - exactPosition);
                if (dir.sqrMagnitude < 1e-6f)
                    dir = curMoveDir;
                targetAngle = dir.AngleFlat();
            }
            else if (mode == DroneMoveMode.Following &&
                       TryGetFollowAnchorAndAngle(out _, out float fixedAngle))
            {
                targetAngle = fixedAngle;
            }
            else
            {
                return;
            }
            DestRotation = targetAngle;
            RotateToward(DestRotation);
        }
        private void RotateToward(float targetAngle)
        {
            float delta = Mathf.DeltaAngle(CurRotation, targetAngle);
            float abs = Mathf.Abs(delta);
            if (abs <= RotationSnapEpsilon)
                return;

            float step = Props.rotationVelocity;
            if (mode == DroneMoveMode.Attacking &&
                currentAttackStyle == AttackMoveStyle.Legacy)
                step *= Mathf.Max(1f, Props.legacyTurnBoost);

            float dynamicStep = Mathf.Lerp(step * 0.35f, step * 1.4f,
                                           Mathf.InverseLerp(0f, 90f, abs));

            if (abs <= dynamicStep)
                CurRotation = targetAngle;
            else
                CurRotation += Mathf.Sign(delta) * dynamicStep;
        }
        private static float SmoothStep(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        private static Vector3 Bezier2(Vector3 a, Vector3 b, Vector3 c, float t)
        {
            float u = 1f - t;
            return u * u * a + 2f * u * t * b + t * t * c;
        }
        private void MakeGun()
        {
            if (Props.turretDef == null)
                return;
            gun = ThingMaker.MakeThing(Props.turretDef, null);
            UpdateGunVerbs();
        }
        private void UpdateGunVerbs()
        {
            if (gun == null)
                return;
            var eq = gun.TryGetComp<CompEquippable>();
            if (eq == null)
                return;

            List<Verb> allVerbs = eq.AllVerbs;
            for (int i = 0; i < allVerbs.Count; i++)
            {
                Verb verb = allVerbs[i];
                verb.caster = parent;
                verb.castCompleteCallback = delegate {
                    if (AttackVerb != null)
                        burstCooldownTicksLeft =
                            AttackVerb.verbProps.defaultCooldownTime.SecondsToTicks();
                };
            }
        }
        private bool EnsureMuzzleFlashMat()
        {
            if (muzzleFlashMat != null)
                return true;

            if (Props == null || Props.graphicDataTurret == null)
                return false;
            string basePath = Props.graphicDataTurret.texPath;
            if (basePath.NullOrEmpty())
                return false;

            string flashPath = basePath + "_MuzzleFlash";
            Texture2D tex =
                ContentFinder<Texture2D>.Get(flashPath, reportFailure: false);
            if (tex == null)
                return false;
            muzzleFlashMat =
                MaterialPool.MatFrom(tex, ShaderDatabase.MoteGlow, Color.white);
            Vector2 baseSize = Props.graphicDataTurret.drawSize;
            if (baseSize == Vector2.zero)
                baseSize = Vector2.one;
            muzzleFlashDrawSize = baseSize * 3.66f;

            return true;
        }
        public void TriggerMuzzleFlash()
        {
            if (EnsureMuzzleFlashMat())
            {
                muzzleFlashTicksLeft = 18;
            }
        }
        private void ResetFollowAngleTimer()
        {
            ticksToNextFollowAngleChange = Rand.Range(FollowAngleChangeIntervalMin,
                                                      FollowAngleChangeIntervalMax);
        }

        public void SetMode(DroneMoveMode newMode)
        {
            DroneMoveMode oldMode = mode;
            mode = newMode;

            if (oldMode == DroneMoveMode.Attacking &&
                newMode != DroneMoveMode.Attacking)
            {
                ResetAttackPatternState();
                manualForcedAttack = false;
                lostTargetStickTicksLeft = 0;
            }

            if (newMode == DroneMoveMode.Idle || newMode == DroneMoveMode.Landed)
            {
                ResetCurrentTarget();
                forcedAttackActive = false;
                manualForcedAttack = false;
            }

            if (newMode == DroneMoveMode.Landed)
            {
                parent.Position = exactPosition.ToIntVec3();
                exactPosition = parent.Position.ToVector3Shifted();
                currentSpeedVal = 0f;
            }
        }

        public void StartMoveTo(IntVec3 cell)
        {
            currentEscortTarget = new LocalTargetInfo(cell);
            moveTargetPos = currentEscortTarget.Cell.ToVector3Shifted();
            SetMode(DroneMoveMode.MovingTo);
        }

        public void StartFollowing(Thing thing)
        {
            if (thing == null)
                return;

            currentEscortTarget = new LocalTargetInfo(thing);
            forcedAttackActive = false;
            manualForcedAttack = false;

            SetMode(DroneMoveMode.Following);
            followAngle = Rand.Range(0f, 360f);
            ResetFollowAngleTimer();
        }

        public void StartAttacking(Thing thing, bool forced = false,
                                   bool manualForced = false)
        {
            if (thing == null)
                return;

            currentAttackTarget = new LocalTargetInfo(thing);
            lastKnownEnemyPos = thing.DrawPos;
            lastKnownEnemyPos.y = 0f;
            lastKnownEnemyPosValid = true;

            forcedAttackActive = forced || forcedAttackActive;
            manualForcedAttack = manualForced || forced;

            if (manualForcedAttack)
                currentAttackStyle = AttackMoveStyle.CurveDash;

            attackStyleIntervalTicksLeft = 180 + (parent.thingIDNumber % 91);
            attackStyleLockTicksLeft = 0;

            ResetAttackPatternState();
            SetMode(DroneMoveMode.Attacking);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (var g in base.CompGetGizmosExtra()) yield return g;
            if (!Props.CanManualControl)
                yield break;

            if (IsFlying)
            {
                yield return gizmoMoveTo;
                yield return gizmoFollow;
                if (AttackVerb != null)
                    yield return gizmoAttack;
                yield return gizmoStop;
                yield return gizmoLand;
            }
            else
            {
                yield return gizmoTakeOff;
            }
        }

        private void CacheGizmos()
        {
            if (!Props.CanManualControl)
                return;

            gizmoTakeOff = new Command_Action
            {
                defaultLabel = "CMC_TakeOff_Label",
                defaultDesc = "CMC_TakeOff_Label",
                icon = ContentFinder<Texture2D>.Get("UI/Commands/Jump", true),
                action = () => SetMode(DroneMoveMode.Idle)
            };

            gizmoLand = new Command_Action
            {
                defaultLabel = "CMC_Land_Label",
                defaultDesc = "CMC_Land_Desc",
                icon = ContentFinder<Texture2D>.Get("UI/Commands/Land", true),
                action = () => SetMode(DroneMoveMode.Landed)
            };

            gizmoMoveTo = new Command_Action
            {
                defaultLabel = "CMC_MoveTo_Label",
                defaultDesc = "CMC_MoveTo_Desc",
                icon = ContentFinder<Texture2D>.Get("UI/Commands/Attack", true),
                action =
                  () => {
                      Find.Targeter.BeginTargeting(
                    new TargetingParameters { canTargetLocations = true },
                    t => StartMoveTo(t.Cell));
                  }
            };

            gizmoFollow = new Command_Action
            {
                defaultLabel = "CMC_Follow_Label",
                defaultDesc = "CMC_Follow_Desc",
                icon = ContentFinder<Texture2D>.Get("UI/Commands/Follow", true),
                action =
                  () => {
                      Find.Targeter.BeginTargeting(
                    new TargetingParameters
                        {
                            canTargetPawns = true,
                            canTargetAnimals = true,
                            canTargetItems = true
                        },
                    t => StartFollowing(t.Thing));
                  }
            };

            gizmoAttack = new Command_Action
            {
                defaultLabel = "CMC_Attack_Label",
                defaultDesc = "CMC_Attack_Desc",
                icon = ContentFinder<Texture2D>.Get("UI/Commands/Attack", true),
                action =
                  () => {
                      Find.Targeter.BeginTargeting(
                    new TargetingParameters
                        {
                            canTargetPawns = true,
                            canTargetBuildings = true,
                            canTargetMechs = true,
                            canTargetAnimals = true
                        },
                    t => StartAttacking(t.Thing, true, true));
                  }
            };

            gizmoStop = new Command_Action
            {
                defaultLabel = "CMC_Stop_Label",
                defaultDesc = "CMC_Stop_Desc",
                icon = ContentFinder<Texture2D>.Get("UI/Commands/Cancel", true),
                action = () => SetMode(DroneMoveMode.Idle)
            };
        }
    }
}