using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;
using static HarmonyLib.Code;

namespace TOT_DLL_test
{
    public class CompProperties_FunnelHauler : CompProperties
    {
        public ThingDef droneThingDef;
        public List<ThingDef> availableDroneDefs;
        public List<FunnelDockPoint> dockPoints = new List<FunnelDockPoint>();
        public float renderBaseLayerOffset = 0.26f;
        public float dockBobAmplitude = 0.08f;
        public float dockBobFrequency = 0.01f;  
        public float dockBobPhaseStep = 0.55f;   
        [Unsaved]
        private int cachedMaxCharges = -1;
        public override void ResolveReferences(ThingDef parentDef)
        {
            base.ResolveReferences(parentDef);
            var reloadableProps = parentDef.GetCompProperties<CompProperties_ApparelReloadable>();
            if (reloadableProps != null)
                cachedMaxCharges = reloadableProps.maxCharges;
        }
        public override IEnumerable<StatDrawEntry> SpecialDisplayStats(StatRequest req)
        {
            foreach (var entry in base.SpecialDisplayStats(req))
                yield return entry;
            if (cachedMaxCharges > 0)
            {
                yield return new StatDrawEntry(
                    StatCategoryDefOf.Apparel,
                    "CMC_FunnelCapacity".Translate(),
                    cachedMaxCharges.ToString(),
                    "CMC_FunnelCapacityDesc".Translate(),
                    100
                );
            }
            if (availableDroneDefs != null && availableDroneDefs.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                List<Dialog_InfoCard.Hyperlink> hyperlinks = new List<Dialog_InfoCard.Hyperlink>();
                sb.AppendLine("CMC_FunnelTypesDesc".Translate());

                foreach (ThingDef def in availableDroneDefs.Distinct().OrderBy(d => d.label))
                {
                    sb.AppendLine($"- {def.LabelCap}");
                    hyperlinks.Add(new Dialog_InfoCard.Hyperlink(def));
                }

                yield return new StatDrawEntry(
                    StatCategoryDefOf.Apparel,
                    "CMC_FunnelTypes".Translate(),
                    $"{availableDroneDefs.Distinct().Count()} types",
                    sb.ToString(),
                    90,
                    hyperlinks: hyperlinks
                );
            }
        }
        public CompProperties_FunnelHauler()
        {
            compClass = typeof(CompFunnelHauler);
        }
    }

    [StaticConstructorOnStartup]
    public class CompFunnelHauler : ThingComp
    {
        public enum FunnelSlotState : byte
        {
            Disabled = 0,   // 无计划/不可用
            Ready = 1,      // 可释放
            Deployed = 2,   // 已释放
            Destroyed = 3   // 已损毁
        }
        public class FunnelSlotData : IExposable
        {
            public ThingDef plannedDef;
            public Thing deployedThing;
            public FunnelSlotState state = FunnelSlotState.Disabled;

            public void ExposeData()
            {
                Scribe_Defs.Look(ref plannedDef, "plannedDef");
                Scribe_References.Look(ref deployedThing, "deployedThing");
                Scribe_Values.Look(ref state, "state", FunnelSlotState.Disabled);
            }
        }
        public CompProperties_FunnelHauler Props => (CompProperties_FunnelHauler)props;

        public List<ThingDef> currentPlan = new List<ThingDef>();
        private int lastObservedCharges = -1;
        private static readonly int[] RepairOrder = { 2, 3, 1, 4, 0, 5 };
        private CompApparelReloadable reloadableCompCache;

        public static readonly Texture2D PanelBGTex = ContentFinder<Texture2D>.Get("UI/5X1FunnelBackground", true);
        private static Texture2D releaseIcon;
        private static Texture2D recallIcon;
        private static Texture2D attackIcon;
        private static Texture2D autoAttackIcon;
        private static Texture2D cancelAttackIcon;
        private static Texture2D autoDraftIcon_On;
        private static Texture2D autoDraftIcon_Off;

        public bool isRecalling = false;
        private bool isAssaultMode = false;
        private bool autoDeployOnDraft = false;
        private bool wasDrafted = false;

        private static FieldInfo remainingChargesField_FI;
        public static readonly int[] UiOrderToSlot = { 1, 2, 4, 3, 0, 5 };
        public static readonly int[] RankBySlot = BuildRank();

        public List<FunnelDockPoint> dockPointsRuntime = new List<FunnelDockPoint>();

        private readonly Dictionary<ThingDef, FunnelDockVisualRuntime> dockVisualCache = new Dictionary<ThingDef, FunnelDockVisualRuntime>();
        private bool dockVisualCacheBuilt;

        private class FunnelDockVisualRuntime
        {
            public Material baseMat;
            public Material glowMat;
            public Vector2 drawSize = Vector2.one;
        }
        private List<FunnelSlotData> slotData = new List<FunnelSlotData>();
        // ======================
        // CompFunnelHauler：未改动方法（完整）
        // ======================
        private static int[] BuildRank()
        {
            var r = new int[6];
            for (int i = 0; i < 6; i++) r[UiOrderToSlot[i]] = i;
            return r;
        }
        static CompFunnelHauler()
        {
            releaseIcon = ContentFinder<Texture2D>.Get("UI/UI_ReleaseUAV", true);
            recallIcon = ContentFinder<Texture2D>.Get("UI/UI_Return", true);
            attackIcon = ContentFinder<Texture2D>.Get("UI/UI_ATKFA_Funnel", true);
            autoAttackIcon = ContentFinder<Texture2D>.Get("UI/UI_ATKMode_Funnel", true);
            cancelAttackIcon = ContentFinder<Texture2D>.Get("UI/UI_ATKFAC_Funnel", true);
            autoDraftIcon_On = ContentFinder<Texture2D>.Get("UI/UI_ATKDraftOn_Funnel", true);
            autoDraftIcon_Off = ContentFinder<Texture2D>.Get("UI/UI_ATKDraftOff_Funnel", true);
        }
        public void NotifyDroneGoneFromSlot(int slotIndex, Thing drone, bool destroyed)
        {
            EnsureSlotDataSizeAndSyncPlan();

            if (slotIndex < 0 || slotIndex >= slotData.Count) return;
            var s = slotData[slotIndex];

            if (s.state != FunnelSlotState.Deployed) return;
            if (drone != null && s.deployedThing != null && s.deployedThing != drone) return;

            if (destroyed)
            {
                SetSlotDestroyed(slotIndex);
                RequestWearerRenderRefresh();
            }
        }
        private void EnsureDockPointsRuntime()
        {
            if (dockPointsRuntime == null) dockPointsRuntime = new List<FunnelDockPoint>();
            while (dockPointsRuntime.Count < 6)
            {
                int idx = dockPointsRuntime.Count;
                FunnelDockPoint src = null;

                if (Props.dockPoints != null && idx >= 0 && idx < Props.dockPoints.Count)
                    src = Props.dockPoints[idx];
                FunnelDockPoint pt = new FunnelDockPoint();
                if (src != null)
                {
                    pt.northOffset = src.northOffset; pt.eastOffset = src.eastOffset; pt.southOffset = src.southOffset; pt.westOffset = src.westOffset;
                    pt.northAngle = src.northAngle; pt.eastAngle = src.eastAngle; pt.southAngle = src.southAngle; pt.westAngle = src.westAngle;
                }
                dockPointsRuntime.Add(pt);
            }

            if (dockPointsRuntime.Count > 6)
                dockPointsRuntime.RemoveRange(6, dockPointsRuntime.Count - 6);
        }

        private void EnsureDockPointsRuntimeSize()
        {
            int n = 6;
            while (dockPointsRuntime.Count < n) dockPointsRuntime.Add(new FunnelDockPoint());
            if (dockPointsRuntime.Count > n) dockPointsRuntime.RemoveRange(n, dockPointsRuntime.Count - n);
        }

        public override void Notify_Unequipped(Pawn pawn)
        {
            CleanupDrones();
            RequestWearerRenderRefresh();
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            CleanupDrones();
            RequestWearerRenderRefresh();
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            base.PostDeSpawn(map, mode);
            CleanupDrones();
            RequestWearerRenderRefresh();
        }

        private void SyncObservedCharges()
        {
            CompApparelReloadable r = GetReloadableComp();
            lastObservedCharges = (r != null) ? r.RemainingCharges : -1;
        }

        public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
        {
            foreach (var g in CompGetGizmosExtra())
                yield return g;
        }

        public int GetDockSlotCountForRender()
        {
            int byConfig = Props.dockPoints?.Count ?? 0;
            if (byConfig <= 0)
            {
                var reloader = GetReloadableComp();
                byConfig = reloader?.MaxCharges ?? 0;
            }
            return Mathf.Clamp(byConfig, 0, 6);
        }

        public bool TryGetDockTransform(int slotIndex, Rot4 facing, out Vector3 offset, out float angle)
        {
            offset = Vector3.zero;
            angle = 0f;
            if (slotIndex < 0 || slotIndex >= 6) return false;
            EnsureDockPointsRuntime();
            bool mirror = (facing == Rot4.North || facing == Rot4.South) && slotIndex >= 3;
            int srcSlot = mirror ? (slotIndex - 3) : slotIndex;
            if (srcSlot < 0 || srcSlot >= dockPointsRuntime.Count) return false;
            FunnelDockPoint pt = dockPointsRuntime[srcSlot];
            if (pt == null) return false;
            offset = pt.OffsetFor(facing);
            angle = pt.AngleFor(facing);
            if (mirror)
            {
                offset.x = -offset.x;
                angle = -angle;
            }
            return true;
        }

        public float DockBobOffsetZ(int slotIndex, int ticksGame)
        {
            float phase = slotIndex * Props.dockBobPhaseStep;
            return Mathf.Sin(ticksGame * Props.dockBobFrequency + phase) * Props.dockBobAmplitude;
        }

        public bool TryGetDockTransformWithBob(int slotIndex, Rot4 facing, int ticksGame, out Vector3 offset, out float angle)
        {
            if (!TryGetDockTransform(slotIndex, facing, out offset, out angle))
                return false;

            offset.z += DockBobOffsetZ(slotIndex, ticksGame);
            return true;
        }

        private void BuildVisualRuntime(ThingDef droneDef)
        {
            string basePath = null;
            string glowPath = null;
            Vector2 drawSize = Vector2.one;

            CompProperties_DroneMovement mv = droneDef.GetCompProperties<CompProperties_DroneMovement>();
            if (mv != null)
            {
                if (mv.graphicDataTurret != null && !mv.graphicDataTurret.texPath.NullOrEmpty())
                {
                    basePath = mv.graphicDataTurret.texPath;
                    if (mv.graphicDataTurret.drawSize != Vector2.zero) drawSize = mv.graphicDataTurret.drawSize;
                }
                if (mv.graphicDataTurretOverlay != null && !mv.graphicDataTurretOverlay.texPath.NullOrEmpty())
                {
                    glowPath = mv.graphicDataTurretOverlay.texPath;
                    if (drawSize == Vector2.one && mv.graphicDataTurretOverlay.drawSize != Vector2.zero)
                        drawSize = mv.graphicDataTurretOverlay.drawSize;
                }
            }

            Material baseMat = null;
            Material glowMat = null;

            if (!basePath.NullOrEmpty())
                baseMat = MaterialPool.MatFrom(basePath, ShaderDatabase.Cutout, Color.white);
            if (!glowPath.NullOrEmpty())
                glowMat = MaterialPool.MatFrom(glowPath, ShaderDatabase.MoteGlow, Color.white);

            if (baseMat == null && glowMat == null) return;

            dockVisualCache[droneDef] = new FunnelDockVisualRuntime
            {
                baseMat = baseMat,
                glowMat = glowMat,
                drawSize = drawSize
            };
        }

        public CompApparelReloadable GetReloadableComp()
        {
            if (reloadableCompCache == null)
                reloadableCompCache = parent.GetComp<CompApparelReloadable>();
            return reloadableCompCache;
        }

        private void SetCharges(CompApparelReloadable reloader, int newChargeCount)
        {
            if (reloader == null) return;
            int clamped = Mathf.Clamp(newChargeCount, 0, reloader.MaxCharges);

            if (remainingChargesField_FI == null)
            {
                remainingChargesField_FI = typeof(CompApparelReloadable).GetField(
                    "remainingCharges",
                    BindingFlags.NonPublic | BindingFlags.Instance
                );
                if (remainingChargesField_FI == null)
                {
                    Log.Error("CMC Funnel: remainingCharges not found");
                    return;
                }
            }
            remainingChargesField_FI.SetValue(reloader, clamped);
        }

        private void ReleaseAllDrones(bool showMessage)
        {
            var reloader = GetReloadableComp();
            if (reloader == null) return;

            int maxLoops = reloader.RemainingCharges + 6;
            int count = 0;

            while (CanReleaseDrone() && count < maxLoops)
            {
                if (!TryReleaseSingleDrone()) break;
                count++;
            }

            if (count > 0)
            {
                RequestWearerRenderRefresh();
                if (showMessage)
                    Messages.Message("TOT_AllDronesReleased".Translate(), MessageTypeDefOf.NeutralEvent);
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

        private bool TryFindSpawnPos(Thing master, int slotIndex, out IntVec3 spawnPos)
        {
            Map map = master.MapHeld;
            spawnPos = master.Position;

            bool IsSafeCell(IntVec3 c)
            {
                if (!c.InBounds(map) || !c.Walkable(map)) return false;
                List<Thing> things = c.GetThingList(map);
                for (int i = 0; i < things.Count; i++)
                {
                    if (things[i].def.category == ThingCategory.Building) return false;
                }
                return true;
            }

            Pawn pawn = master as Pawn;
            if (pawn != null)
            {
                Vector3 localOffset;
                float ang;
                int t = GenTicks.TicksGame;
                if (TryGetDockTransformWithBob(slotIndex, pawn.Rotation, t, out localOffset, out ang))
                {
                    Vector3 idealV = pawn.DrawPos + localOffset;
                    IntVec3 ideal = new IntVec3(Mathf.RoundToInt(idealV.x), 0, Mathf.RoundToInt(idealV.z));

                    if (IsSafeCell(ideal))
                    {
                        spawnPos = ideal;
                        return true;
                    }
                    if (CellFinder.TryFindRandomCellNear(ideal, map, 2, IsSafeCell, out spawnPos))
                        return true;
                }
            }
            if (CellFinder.TryFindRandomCellNear(master.Position, map, 3, IsSafeCell, out spawnPos))
                return true;
            if (CellFinder.TryFindRandomSpawnCellForPawnNear(master.Position, map, out spawnPos, 3))
                return true;
            spawnPos = master.Position;
            return spawnPos.InBounds(map);
        }

        private bool IsActive()
        {
            if (parent.Spawned) return true;
            Apparel ap = parent as Apparel;
            if (ap?.Wearer != null && ap.Wearer.Spawned) return true;
            return false;
        }

        private Pawn GetWearerPawn()
        {
            return (parent as Apparel)?.Wearer;
        }

        private Thing GetMaster()
        {
            Apparel ap = parent as Apparel;
            if (ap?.Wearer != null) return ap.Wearer;
            return parent;
        }

        public void RequestWearerRenderRefresh()
        {
            Pawn wearer = GetWearerPawn();
            if (wearer?.Drawer?.renderer != null)
            {
                wearer.Drawer.renderer.SetAllGraphicsDirty();
            }
        }

        public void InvalidateDockVisualCache()
        {
            dockVisualCacheBuilt = false;
            dockVisualCache.Clear();
        }

        private void EnsureDockVisualCacheBuilt()
        {
            if (dockVisualCacheBuilt) return;
            if (!UnityData.IsInMainThread) return;

            dockVisualCache.Clear();

            HashSet<ThingDef> defs = new HashSet<ThingDef>();
            if (Props.droneThingDef != null) defs.Add(Props.droneThingDef);
            if (Props.availableDroneDefs != null)
            {
                for (int i = 0; i < Props.availableDroneDefs.Count; i++)
                    if (Props.availableDroneDefs[i] != null) defs.Add(Props.availableDroneDefs[i]);
            }
            if (currentPlan != null)
            {
                for (int i = 0; i < currentPlan.Count; i++)
                    if (currentPlan[i] != null) defs.Add(currentPlan[i]);
            }

            foreach (ThingDef def in defs)
            {
                BuildVisualRuntime(def);
            }

            dockVisualCacheBuilt = true;
        }

        private bool IsAtRecallAnchor(Thing drone, int slotIndex, Thing master)
        {
            Pawn pawn = master as Pawn;
            return drone.Position.InHorDistOf(master.Position, 5f);
        }

        public bool TryGetDockMaterial(int slotIndex, bool glowLayer, out Material mat, out Vector2 drawSize)
        {
            mat = null;
            drawSize = Vector2.one;
            if (!ShouldDrawDockSlot(slotIndex)) return false;
            ThingDef def = GetSlotDroneDef(slotIndex);
            if (def == null) return false;

            if (!dockVisualCacheBuilt)
            {
                if (UnityData.IsInMainThread) EnsureDockVisualCacheBuilt();
                else return false;
            }
            if (!dockVisualCache.TryGetValue(def, out FunnelDockVisualRuntime rt)) return false;

            drawSize = rt.drawSize;
            mat = glowLayer ? rt.glowMat : rt.baseMat;
            return mat != null;
        }

        public bool TryGetDockDrawSize(int slotIndex, out Vector2 drawSize)
        {
            drawSize = Vector2.one;
            ThingDef def = GetSlotDroneDef(slotIndex);
            if (def == null) return false;
            if (!dockVisualCacheBuilt)
            {
                if (UnityData.IsInMainThread) EnsureDockVisualCacheBuilt();
                else return false;
            }
            if (!dockVisualCache.TryGetValue(def, out FunnelDockVisualRuntime rt)) return false;
            drawSize = rt.drawSize;
            return true;
        }
        private ThingDef PickDefaultDroneDef()
        {
            if (Props.availableDroneDefs != null && Props.availableDroneDefs.Count > 0)
                return Props.availableDroneDefs.RandomElement();
            return Props.droneThingDef;
        }
        private void EnsureSlotDataSizeAndSyncPlan()
        {
            int n = Mathf.Clamp(GetDockSlotCountForRender(), 0, 6);

            if (currentPlan == null) currentPlan = new List<ThingDef>();
            while (currentPlan.Count < n) currentPlan.Add(null);
            if (currentPlan.Count > n) currentPlan.RemoveRange(n, currentPlan.Count - n);

            if (slotData == null) slotData = new List<FunnelSlotData>();
            while (slotData.Count < n) slotData.Add(new FunnelSlotData());
            if (slotData.Count > n) slotData.RemoveRange(n, slotData.Count - n);

            for (int i = 0; i < n; i++)
            {
                var s = slotData[i];
                s.plannedDef = currentPlan[i];

                if (s.plannedDef == null)
                {
                    s.deployedThing = null;
                    s.state = FunnelSlotState.Disabled;
                    continue;
                }

                if (s.state == FunnelSlotState.Disabled)
                    s.state = FunnelSlotState.Ready;

                if (s.state == FunnelSlotState.Deployed && (s.deployedThing == null || s.deployedThing.Destroyed))
                {
                    s.deployedThing = null;
                    s.state = FunnelSlotState.Destroyed;
                }
            }
        }

        private bool SanitizeSlotData()
        {
            EnsureSlotDataSizeAndSyncPlan();
            bool changed = false;
            for (int i = 0; i < slotData.Count; i++)
            {
                var s = slotData[i];
                if (s.state == FunnelSlotState.Deployed && (s.deployedThing == null || s.deployedThing.Destroyed))
                {
                    s.deployedThing = null;
                    s.state = FunnelSlotState.Destroyed;
                    changed = true;
                }
            }
            return changed;
        }

        private void SetSlotReady(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= slotData.Count) return;
            slotData[slotIndex].deployedThing = null;
            slotData[slotIndex].state = (slotData[slotIndex].plannedDef != null)
                ? FunnelSlotState.Ready
                : FunnelSlotState.Disabled;
        }

        private void SetSlotDeployed(int slotIndex, Thing drone)
        {
            if (slotIndex < 0 || slotIndex >= slotData.Count) return;
            slotData[slotIndex].deployedThing = drone;
            slotData[slotIndex].state = FunnelSlotState.Deployed;
        }

        private void SetSlotDestroyed(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= slotData.Count) return;
            slotData[slotIndex].deployedThing = null;
            slotData[slotIndex].state = (slotData[slotIndex].plannedDef != null)
                ? FunnelSlotState.Destroyed
                : FunnelSlotState.Disabled;
        }

        private int GetDeployedCount()
        {
            int c = 0;
            for (int i = 0; i < slotData.Count; i++)
            {
                if (slotData[i].state == FunnelSlotState.Deployed &&
                    slotData[i].deployedThing != null &&
                    !slotData[i].deployedThing.Destroyed)
                {
                    c++;
                }
            }
            return c;
        }

        private int GetDestroyedCount()
        {
            int c = 0;
            for (int i = 0; i < slotData.Count; i++)
                if (slotData[i].state == FunnelSlotState.Destroyed) c++;
            return c;
        }

        private bool IsTrackedDeployedDrone(Thing t)
        {
            if (t == null) return false;
            for (int i = 0; i < slotData.Count; i++)
            {
                if (slotData[i].state == FunnelSlotState.Deployed && slotData[i].deployedThing == t)
                    return true;
            }
            return false;
        }

        public bool TryGetSlotStatus(int slotIndex, out FunnelSlotState state, out ThingDef planDef, out Thing deployedThing)
        {
            EnsureSlotDataSizeAndSyncPlan();
            if (slotIndex < 0 || slotIndex >= slotData.Count)
            {
                state = FunnelSlotState.Disabled;
                planDef = null;
                deployedThing = null;
                return false;
            }

            var s = slotData[slotIndex];
            state = s.state;
            planDef = s.plannedDef;
            deployedThing = s.deployedThing;
            return true;
        }
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

            Pawn wearer = GetWearerPawn();
            if (wearer != null)
                wasDrafted = wearer.Drafted;

            EnsureDockPointsRuntime();
            EnsurePlanInitialized();
            EnsureSlotDataSizeAndSyncPlan();

            if (!respawningAfterLoad)
                TryAutoInitAndDeployForNPC();

            InvalidateDockVisualCache();
            EnsureDockVisualCacheBuilt();
            RequestWearerRenderRefresh();
        }

        public override void Notify_Equipped(Pawn pawn)
        {
            wasDrafted = pawn.Drafted;
            EnsureDockPointsRuntime();
            EnsurePlanInitialized();
            EnsureSlotDataSizeAndSyncPlan();
            TryAutoInitAndDeployForNPC();

            InvalidateDockVisualCache();
            EnsureDockVisualCacheBuilt();
            RequestWearerRenderRefresh();
        }

        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Collections.Look(ref dockPointsRuntime, "dockPointsRuntime", LookMode.Deep);
            Scribe_Collections.Look(ref currentPlan, "currentPlan", LookMode.Def);
            Scribe_Collections.Look(ref slotData, "slotData", LookMode.Deep);

            Scribe_Values.Look(ref isRecalling, "isRecalling", false);
            Scribe_Values.Look(ref isAssaultMode, "isAssaultMode", false);
            Scribe_Values.Look(ref autoDeployOnDraft, "autoDeployOnDraft", false);
            Scribe_Values.Look(ref wasDrafted, "wasDrafted", false);
            Scribe_Values.Look(ref lastObservedCharges, "lastObservedCharges", -1);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (currentPlan == null) currentPlan = new List<ThingDef>();
                if (slotData == null) slotData = new List<FunnelSlotData>();
                if (dockPointsRuntime == null) dockPointsRuntime = new List<FunnelDockPoint>();

                SyncObservedCharges();
                EnsureDockPointsRuntimeSize();
                EnsurePlanInitialized();
                EnsureSlotDataSizeAndSyncPlan();
                SanitizeSlotData();

                InvalidateDockVisualCache();
                EnsureDockVisualCacheBuilt();
                RequestWearerRenderRefresh();
                EnsureDockPointsRuntime();
            }
        }
        private void EnsurePlanInitialized()
        {
            var reloader = GetReloadableComp();
            if (reloader == null) return;

            if (currentPlan == null) currentPlan = new List<ThingDef>();
            int targetCount = Mathf.Clamp(reloader.MaxCharges, 0, 6);

            if (currentPlan.Count == 0)
            {
                for (int i = 0; i < targetCount; i++)
                    currentPlan.Add(PickDefaultDroneDef());
                RequestWearerRenderRefresh();
            }
            else
            {
                while (currentPlan.Count < targetCount)
                    currentPlan.Add(PickDefaultDroneDef());

                if (currentPlan.Count > targetCount)
                    currentPlan.RemoveRange(targetCount, currentPlan.Count - targetCount);
            }

            EnsureSlotDataSizeAndSyncPlan();
        }

        private void TryAutoInitAndDeployForNPC()
        {
            Pawn wearer = GetWearerPawn();
            if (wearer == null || wearer.IsPlayerControlled) return;

            EnsurePlanInitialized();
            EnsureSlotDataSizeAndSyncPlan();

            if (GetDeployedCount() == 0 && CanReleaseDrone())
                ReleaseAllDrones(showMessage: false);
        }

        private void CheckRepairByChargeGain()
        {
            CompApparelReloadable r = GetReloadableComp();
            if (r == null) return;

            int now = r.RemainingCharges;
            if (lastObservedCharges < 0)
            {
                lastObservedCharges = now;
                return;
            }

            int gained = now - lastObservedCharges;
            if (gained > 0 && GetDestroyedCount() > 0)
            {
                int repaired = RepairDestroyedSlots(gained);
                if (repaired > 0)
                {
                    RequestWearerRenderRefresh();
                    Pawn w = GetWearerPawn();
                    if (w != null)
                        Messages.Message("TOT_FunnelRepairedByRefuel".Translate(repaired), w, MessageTypeDefOf.PositiveEvent);
                }
            }

            lastObservedCharges = now;
        }

        private int RepairDestroyedSlots(int maxRepairCount)
        {
            if (maxRepairCount <= 0) return 0;
            EnsureSlotDataSizeAndSyncPlan();

            int repaired = 0;
            int slotCount = slotData.Count;

            for (int i = 0; i < RepairOrder.Length && repaired < maxRepairCount; i++)
            {
                int s = RepairOrder[i];
                if (s < 0 || s >= slotCount) continue;

                if (slotData[s].state == FunnelSlotState.Destroyed)
                {
                    SetSlotReady(s);
                    repaired++;
                }
            }

            if (repaired < maxRepairCount)
            {
                for (int i = 0; i < slotCount && repaired < maxRepairCount; i++)
                {
                    if (slotData[i].state == FunnelSlotState.Destroyed)
                    {
                        SetSlotReady(i);
                        repaired++;
                    }
                }
            }

            return repaired;
        }
        public override void CompTick()
        {
            base.CompTick();
            if (!IsActive()) return;

            if (SanitizeSlotData()) RequestWearerRenderRefresh();
            CheckRepairByChargeGain();

            if (!parent.IsHashIntervalTick(60)) return;

            Pawn wearer = GetWearerPawn();
            if (wearer != null && wearer.IsPlayerControlled)
            {
                bool currentlyDrafted = wearer.Drafted;
                if (autoDeployOnDraft && currentlyDrafted != wasDrafted)
                {
                    if (currentlyDrafted)
                    {
                        if (!isRecalling && CanReleaseDrone())
                        {
                            ReleaseAllDrones(showMessage: true);
                            Messages.Message("TOT_AutoDraftDeploy".Translate(), wearer, MessageTypeDefOf.NeutralEvent);
                        }
                    }
                    else
                    {
                        if (GetDeployedCount() > 0)
                        {
                            TryRecallAllDrones();
                            Messages.Message("TOT_AutoDraftRecall".Translate(), wearer, MessageTypeDefOf.NeutralEvent);
                        }
                    }
                }
                wasDrafted = currentlyDrafted;
            }

            if (isRecalling)
            {
                Thing master = GetMaster();
                if (master == null || master.DestroyedOrNull() || master.MapHeld == null)
                {
                    isRecalling = false;
                    return;
                }

                bool changed = false;
                for (int slot = slotData.Count - 1; slot >= 0; slot--)
                {
                    var s = slotData[slot];
                    if (s.state != FunnelSlotState.Deployed) continue;

                    Thing drone = s.deployedThing;
                    if (drone == null || drone.Destroyed)
                    {
                        SetSlotDestroyed(slot);
                        changed = true;
                        continue;
                    }

                    if (IsAtRecallAnchor(drone, slot, master))
                    {
                        if (!drone.Destroyed && drone.Spawned)
                            drone.DeSpawn(DestroyMode.Vanish);

                        var r = GetReloadableComp();
                        if (r != null && r.RemainingCharges < r.MaxCharges)
                            SetCharges(r, r.RemainingCharges + 1);

                        SetSlotReady(slot);
                        changed = true;
                    }
                }

                if (changed) RequestWearerRenderRefresh();

                if (GetDeployedCount() == 0)
                {
                    isRecalling = false;
                    Messages.Message("TOT_AllFunnelsRecalled".Translate(), MessageTypeDefOf.NeutralEvent);
                }
            }
            else
            {
                bool changed = false;
                for (int i = 0; i < slotData.Count; i++)
                {
                    if (slotData[i].state == FunnelSlotState.Deployed &&
                        (slotData[i].deployedThing == null || slotData[i].deployedThing.Destroyed))
                    {
                        SetSlotDestroyed(i);
                        changed = true;
                    }
                }
                if (changed) RequestWearerRenderRefresh();
            }
        }
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            bool isDrafted = (parent as Apparel)?.Wearer?.Drafted ?? (parent as Pawn)?.Drafted ?? false;
            if (!IsActive() && !isDrafted) yield break;

            var reloader = GetReloadableComp();
            if (reloader == null) yield break;

            EnsureSlotDataSizeAndSyncPlan();

            int currentCharges = reloader.RemainingCharges;
            int deployedCount = GetDeployedCount();

            string disableRelease = null;
            if (currentCharges <= 0) disableRelease = "TOT_NoAvailableDroneCore".Translate();
            else if (isRecalling) disableRelease = "TOT_FunnelsRecalling".Translate();
            else if (GetNextDeployableSlotIndex() < 0) disableRelease = "TOT_NoConfigPlan".Translate();

            string disableRecall = null;
            if (deployedCount == 0) disableRecall = "TOT_NoDeployedFunnels".Translate();
            else if (isRecalling) disableRecall = "TOT_FunnelsAlreadyRecalling".Translate();

            string disableTarget = null;
            if (deployedCount == 0) disableTarget = "TOT_NoDeployedFunnels".Translate();
            else if (isRecalling) disableTarget = "TOT_FunnelsRecalling".Translate();

            string disableCancel = null;
            if (deployedCount == 0 || !IsAnyDroneForcedAttacking()) disableCancel = "TOT_NoFunnelsAttacking".Translate();
            else if (isRecalling) disableCancel = "TOT_FunnelsRecalling".Translate();

            string disableToggleMode = isRecalling ? "TOT_FunnelsRecalling".Translate() : null;

            string pawnName = "None";
            if (parent is Apparel ap && ap.Wearer != null) pawnName = ap.Wearer.NameShortColored.Resolve();
            else if (parent is Pawn p) pawnName = p.NameShortColored.Resolve();

            string itemName = parent?.def?.label?.Translate().Resolve() ?? "TOT_FunnelSystem".Translate().Resolve();
            string combinedName = pawnName + ":" + itemName;

            Command_FunnelPanel panel = new Command_FunnelPanel
            {
                customBackground = PanelBGTex,
                funnelName = combinedName,
                containedCount = currentCharges,
                maxCount = reloader.MaxCharges,

                actionRelease = () => ReleaseAllDrones(showMessage: true),
                disableReasonRelease = disableRelease,
                iconRelease = releaseIcon,

                actionRecall = TryRecallAllDrones,
                disableReasonRecall = disableRecall,
                iconRecall = recallIcon,

                actionToggleMode = ToggleAutoAttackMode,
                disableReasonToggleMode = disableToggleMode,
                iconMode = autoAttackIcon,
                isAssaultMode = isAssaultMode,

                actionTarget = StartTargetedAttack,
                disableReasonTarget = disableTarget,
                iconTarget = attackIcon,

                actionCancelTarget = CancelForcedAttack,
                disableReasonCancelTarget = disableCancel,
                iconCancelTarget = cancelAttackIcon,

                actionAutoDraft = () => { autoDeployOnDraft = !autoDeployOnDraft; },
                isAutoDraftOn = autoDeployOnDraft,
                iconAutoDraft = autoDraftIcon_On,
                iconAutoDraftOff = autoDraftIcon_Off
            };
            yield return panel;
        }
        private int GetNextDeployableSlotIndex()
        {
            EnsureSlotDataSizeAndSyncPlan();
            for (int i = 0; i < slotData.Count; i++)
            {
                if (slotData[i].state == FunnelSlotState.Ready && slotData[i].plannedDef != null)
                    return i;
            }
            return -1;
        }

        private bool CanReleaseDrone()
        {
            var reloader = GetReloadableComp();
            if (reloader == null) return false;
            if (reloader.RemainingCharges <= 0) return false;
            if (!IsActive()) return false;
            if (isRecalling) return false;
            if (currentPlan == null || currentPlan.Count == 0) return false;
            if (GetNextDeployableSlotIndex() < 0) return false;
            return true;
        }
        private bool TryReleaseSingleDrone()
        {
            if (!CanReleaseDrone()) return false;

            var reloader = GetReloadableComp();
            if (reloader == null || reloader.RemainingCharges < 1) return false;

            Thing master = GetMaster();
            if (master == null || master.MapHeld == null) return false;

            EnsureSlotDataSizeAndSyncPlan();
            int slotIndex = GetNextDeployableSlotIndex();
            if (slotIndex < 0) return false;

            ThingDef droneToSpawnDef = slotData[slotIndex].plannedDef;
            if (droneToSpawnDef == null) return false;

            if (!TryFindSpawnPos(master, slotIndex, out IntVec3 spawnPos)) return false;

            Thing drone = ThingMaker.MakeThing(droneToSpawnDef);
            drone.SetFaction(master.Faction);
            GenSpawn.Spawn(drone, spawnPos, master.MapHeld, WipeMode.Vanish);
            drone.Position = spawnPos;

            var moveComp = drone.TryGetComp<CompDroneMovement>();
            if (moveComp != null)
            {
                moveComp.SetMaster(master);
                moveComp.StartFollowing(master);
                moveComp.AssaultMode = this.isAssaultMode;
                moveComp.SetFollowDockSlot(slotIndex);
                Color apColor = parent != null ? parent.DrawColor : Color.white;
                moveComp.SetDockTintColor(apColor);

                Pawn pawn = master as Pawn;
                if (pawn != null)
                {
                    Vector3 localOffset;
                    float ang;
                    int t = GenTicks.TicksGame;
                    if (TryGetDockTransformWithBob(slotIndex, pawn.Rotation, t, out localOffset, out ang))
                    {
                        Vector3 snapWorldPos = pawn.DrawPos + localOffset;
                        moveComp.SnapTo(snapWorldPos);
                        moveComp.CurRotation = ang;
                        moveComp.AssignFollowAngle(ang);
                    }
                }
            }

            reloader.UsedOnce();
            SetSlotDeployed(slotIndex, drone);
            return true;
        }

        private void TryRecallAllDrones()
        {
            EnsureSlotDataSizeAndSyncPlan();
            if (isRecalling) return;
            if (GetDeployedCount() == 0) return;

            Thing master = GetMaster();
            if (master == null || master.DestroyedOrNull()) return;

            isRecalling = true;
            for (int i = 0; i < slotData.Count; i++)
            {
                var s = slotData[i];
                if (s.state != FunnelSlotState.Deployed) continue;

                Thing drone = s.deployedThing;
                if (drone == null || drone.Destroyed) continue;

                var moveComp = drone.TryGetComp<CompDroneMovement>();
                if (moveComp != null)
                {
                    moveComp.ResetCurrentTarget();
                    moveComp.StartFollowing(master);
                }
            }

            Messages.Message("TOT_FunnelsOrderedRecall".Translate(), MessageTypeDefOf.NeutralEvent);
        }

        private void CleanupDrones()
        {
            EnsureSlotDataSizeAndSyncPlan();

            int refunded = 0;
            for (int i = 0; i < slotData.Count; i++)
            {
                var s = slotData[i];
                if (s.state != FunnelSlotState.Deployed) continue;

                Thing t = s.deployedThing;
                if (t != null && !t.Destroyed && t.Spawned)
                    t.DeSpawn(DestroyMode.Vanish);

                refunded++;
                SetSlotReady(i);
            }

            var reloader = GetReloadableComp();
            if (reloader != null && refunded > 0)
                SetCharges(reloader, reloader.RemainingCharges + refunded);

            isRecalling = false;
        }
        // =========================
        // 8) 替换：死亡/攻击相关遍历
        // =========================

        public override void Notify_WearerDied()
        {
            EnsureSlotDataSizeAndSyncPlan();

            for (int i = 0; i < slotData.Count; i++)
            {
                var s = slotData[i];
                if (s.state != FunnelSlotState.Deployed) continue;

                Thing t = s.deployedThing;
                if (t is CMC_Drone d && !d.Destroyed)
                {
                    d.CreateExplosionEffect();
                    d.Destroy(DestroyMode.Vanish);
                }

                SetSlotDestroyed(i);
            }

            RequestWearerRenderRefresh();
        }

        private void StartTargetedAttack()
        {
            Thing master = GetMaster();

            bool AttackValidator(TargetInfo x)
            {
                if (x.Thing == null) return false;
                if (x.Thing == master) return false;
                if (IsTrackedDeployedDrone(x.Thing)) return false;
                if (master.Faction != null && x.Thing.Faction == master.Faction) return false;
                return true;
            }

            TargetingParameters targetingParams = new TargetingParameters
            {
                canTargetPawns = true,
                canTargetBuildings = true,
                canTargetAnimals = true,
                canTargetMechs = true,
                validator = AttackValidator
            };

            Find.Targeter.BeginTargeting(targetingParams, target =>
            {
                if (!target.HasThing || target.Thing == null) return;

                for (int i = 0; i < slotData.Count; i++)
                {
                    var s = slotData[i];
                    if (s.state != FunnelSlotState.Deployed) continue;

                    Thing drone = s.deployedThing;
                    if (drone == null || drone.Destroyed) continue;

                    var moveComp = drone.TryGetComp<CompDroneMovement>();
                    if (moveComp != null) moveComp.StartAttacking(target.Thing, true);
                }

                Messages.Message("TOT_FunnelsCommandedToAttack".Translate(target.Thing.Label), MessageTypeDefOf.NeutralEvent);
            });
        }

        private void ToggleAutoAttackMode()
        {
            isAssaultMode = !isAssaultMode;

            for (int i = 0; i < slotData.Count; i++)
            {
                var s = slotData[i];
                if (s.state != FunnelSlotState.Deployed) continue;

                Thing drone = s.deployedThing;
                if (drone == null || drone.Destroyed) continue;

                var moveComp = drone.TryGetComp<CompDroneMovement>();
                if (moveComp != null) moveComp.AssaultMode = isAssaultMode;
            }

            string modeStr = isAssaultMode ? "TOT_ModeAttack".Translate() : "TOT_ModeGuard".Translate();
            Messages.Message("TOT_AutoAttackToggled".Translate(modeStr), MessageTypeDefOf.NeutralEvent);
        }

        private void CancelForcedAttack()
        {
            Thing master = GetMaster();

            for (int i = 0; i < slotData.Count; i++)
            {
                var s = slotData[i];
                if (s.state != FunnelSlotState.Deployed) continue;

                Thing drone = s.deployedThing;
                if (drone == null || drone.Destroyed) continue;

                var moveComp = drone.TryGetComp<CompDroneMovement>();
                if (moveComp == null) continue;

                if (master != null) moveComp.StartFollowing(master);
                else
                {
                    moveComp.SetMode(CompDroneMovement.DroneMoveMode.Idle);
                    moveComp.ResetCurrentTarget();
                }
            }

            Messages.Message("TOT_ForcedAttackCancelled".Translate(), MessageTypeDefOf.NeutralEvent);
        }

        private bool IsAnyDroneForcedAttacking()
        {
            for (int i = 0; i < slotData.Count; i++)
            {
                var s = slotData[i];
                if (s.state != FunnelSlotState.Deployed) continue;

                Thing drone = s.deployedThing;
                if (drone == null || drone.Destroyed) continue;

                var moveComp = drone.TryGetComp<CompDroneMovement>();
                if (moveComp != null && moveComp.mode == CompDroneMovement.DroneMoveMode.Attacking)
                    return true;
            }
            return false;
        }
        public bool ShouldDrawDockSlot(int slotIndex)
        {
            Pawn wearer = GetWearerPawn();
            if (wearer == null || !wearer.Spawned) return false;

            EnsureSlotDataSizeAndSyncPlan();
            if (slotIndex < 0 || slotIndex >= slotData.Count) return false;
            if (isRecalling && GetDeployedCount() > 0) return false;

            var s = slotData[slotIndex];
            return s.state == FunnelSlotState.Ready && s.plannedDef != null;
        }
        private ThingDef GetSlotDroneDef(int slotIndex)
        {
            EnsureSlotDataSizeAndSyncPlan();
            if (slotIndex < 0 || slotIndex >= slotData.Count) return null;
            return slotData[slotIndex].plannedDef;
        }
        public void Notify_PlanChanged()
        {
            EnsureSlotDataSizeAndSyncPlan();
            InvalidateDockVisualCache();
            EnsureDockVisualCacheBuilt();
            RequestWearerRenderRefresh();
        }
    }
}