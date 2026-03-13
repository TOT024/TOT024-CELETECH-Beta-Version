using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace TOT_DLL_test
{
    public class GameComponent_CeleTech : GameComponent
    {
        public static GameComponent_CeleTech Instance;
        public List<CamoPreset> CustomCamoPresets = new List<CamoPreset>();
        public int LastAuxHr;
        public int LastArsHr;
        public int LastSSHr;

        public int CurrentPoint;
        public int TotalPoint;
        private const int SaveVersionCurrent = 2;
        private int saveVersion;
        private bool migratedThisSession;

        public MapParent ASEA_observedMap;
        public Dictionary<string, float> cycleDiscountFactorByDef;
        public int nextDiscountRefreshTick;
        public const int DiscountCycleDays = 15;
        private const int MaxAircraftCache = 2;
        public int MaxAircraft
        {
            get
            {
                if (CMCResearchProjectDefOf.CMC_CAS_HangerUpgrade.IsFinished)
                    return MaxAircraftCache + 1;
                else
                    return MaxAircraftCache;
            }
        }
        private Dictionary<string, int> strikeLastUseTick = new Dictionary<string, int>();
        private Dictionary<string, int> strikeMaxUsesOverride = new Dictionary<string, int>();
        private Dictionary<string, int> legacyStrikeChargesCurrent;
        private Dictionary<string, int> legacyStrikeRearmFinishTick;
        public bool CASManualDirectionEnabled;
        public class AircraftSlotData : IExposable
        {
            public int slotId;
            public string mountedStrikeId;
            public int chargesLeft;        
            public int rearmFinishTick;
            public int nextAttackReadyTick;
            public int standbyExpireTick;
            public void ExposeData()
            {
                Scribe_Values.Look(ref slotId, "slotId", 0);
                Scribe_Values.Look(ref mountedStrikeId, "mountedStrikeId", null);
                Scribe_Values.Look(ref chargesLeft, "chargesLeft", 0);
                Scribe_Values.Look(ref rearmFinishTick, "rearmFinishTick", 0);
                Scribe_Values.Look(ref nextAttackReadyTick, "nextAttackReadyTick");
                Scribe_Values.Look(ref standbyExpireTick, "standbyExpireTick", 0);
            }
        }
        private List<AircraftSlotData> aircraftSlots = new List<AircraftSlotData>();
        public int AircraftStandbyHours = 6;
        public int AircraftStandbyTicks
        {
            get { return Mathf.Max(1, AircraftStandbyHours) * GenDate.TicksPerHour; }
        }
        private Dictionary<string, int> purchasedBundlesByDef = new Dictionary<string, int>();
        public GameComponent_CeleTech(Game game)
        {
            Instance = this;
        }
        public GameComponent_CeleTech()
        {
            Instance = this;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref saveVersion, "CMC_SaveVersion", 0);
            Scribe_Values.Look(ref this.LastAuxHr, "Auxtradertick", 0, false);
            Scribe_Values.Look(ref this.LastArsHr, "Arstradertick", 0, false);
            Scribe_Values.Look(ref this.LastSSHr, "Slavetradertick", 0, false);
            Scribe_Values.Look(ref this.CurrentPoint, "points", 1000, false);
            Scribe_Values.Look(ref AircraftStandbyHours, "CMC_AircraftStandbyHours", 6);
            Scribe_Values.Look(ref this.CASManualDirectionEnabled, "CASManualDirectionEnabled", false, false);
            Scribe_Collections.Look(ref CustomCamoPresets, "CustomCamoPresets", LookMode.Deep);
            Scribe_Collections.Look(ref purchasedBundlesByDef, "purchasedBundlesByDef", LookMode.Value, LookMode.Value);
            //Scribe_Values.Look(ref MaxAircraftCache, "CMC_MaxAircraft", 2);
            Scribe_Collections.Look(ref strikeLastUseTick, "CMC_StrikeLastUseTick", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref strikeMaxUsesOverride, "CMC_StrikeMaxUsesOverride", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref aircraftSlots, "CMC_AircraftSlots", LookMode.Deep);
            Scribe_Collections.Look(ref legacyStrikeChargesCurrent, "CMC_StrikeChargesCurrent", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref legacyStrikeRearmFinishTick, "CMC_StrikeRearmFinishTick", LookMode.Value, LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (strikeLastUseTick == null) strikeLastUseTick = new Dictionary<string, int>();
                if (strikeMaxUsesOverride == null) strikeMaxUsesOverride = new Dictionary<string, int>();
                if (aircraftSlots == null) aircraftSlots = new List<AircraftSlotData>();

                EnsureDiscountState();
                EnsureAircraftSlotsCount();
                NormalizeAircraftSlots();
                if (saveVersion != SaveVersionCurrent)
                    ApplyMigrationsIfNeeded();
                List<string> badKeys = null;
                foreach (var kv in cycleDiscountFactorByDef)
                {
                    if (string.IsNullOrEmpty(kv.Key) || kv.Value <= 0f || kv.Value > 1f)
                    {
                        if (badKeys == null) badKeys = new List<string>();
                        badKeys.Add(kv.Key);
                    }
                }
                if (badKeys != null)
                {
                    for (int i = 0; i < badKeys.Count; i++)
                        cycleDiscountFactorByDef.Remove(badKeys[i]);
                }

                if (nextDiscountRefreshTick <= 0)
                    nextDiscountRefreshTick = Find.TickManager.TicksGame + DiscountCycleDays * GenDate.TicksPerDay;
            }

            if (CustomCamoPresets == null)
                CustomCamoPresets = new List<CamoPreset>();
        }
        public override void FinalizeInit()
        {
            base.FinalizeInit();
            if (Instance == null) Instance = this;
            EnsureDiscountState();
            EnsureAircraftSlotsCount();
            NormalizeAircraftSlots();
            if (saveVersion != SaveVersionCurrent)
                ApplyMigrationsIfNeeded();
        }
        public override void StartedNewGame()
        {
            base.StartedNewGame();
            EnsureDiscountState();
            EnsureAircraftSlotsCount();
            NormalizeAircraftSlots();
            nextDiscountRefreshTick = Find.TickManager.TicksGame + DiscountCycleDays * GenDate.TicksPerDay;
        }
        public override void LoadedGame()
        {
            base.LoadedGame();
            EnsureDiscountState();
            EnsureAircraftSlotsCount();
            NormalizeAircraftSlots();
            if (saveVersion != SaveVersionCurrent)
                ApplyMigrationsIfNeeded();
            if (nextDiscountRefreshTick <= 0)
                nextDiscountRefreshTick = Find.TickManager.TicksGame + DiscountCycleDays * GenDate.TicksPerDay;
        }
        public override void GameComponentTick()
        {
            base.GameComponentTick();

            if (Find.TickManager.TicksGame % 3600 == 0)
            {
                LastArsHr += 1;
                LastAuxHr += 1;
                LastSSHr += 1;
            }
            EnsureDiscountState();
            EnsureAircraftSlotsCount();
            int nowTick = Find.TickManager.TicksGame;
            ShopTick(nowTick);
            CASTick(nowTick);
        }
        private void RefreshAircraftStates(int now)
        {
            for (int i = 0; i < ActiveSlotCount; i++)
            {
                AircraftSlotData s = aircraftSlots[i];
                if (s.rearmFinishTick > 0 && s.rearmFinishTick <= now)
                {
                    s.rearmFinishTick = 0;
                    s.mountedStrikeId = null;
                    s.chargesLeft = 0;
                    s.standbyExpireTick = 0;
                    s.nextAttackReadyTick = 0;
                    continue;
                }
                if (s.rearmFinishTick <= now &&
                    !string.IsNullOrEmpty(s.mountedStrikeId) &&
                    s.chargesLeft > 0 &&
                    s.standbyExpireTick > 0 &&
                    s.standbyExpireTick <= now)
                {
                    s.mountedStrikeId = null;
                    s.chargesLeft = 0; 
                    s.standbyExpireTick = 0;
                    s.nextAttackReadyTick = 0;
                }
            }
        }
        private void ShopTick(int now)
        {
            if (now < nextDiscountRefreshTick) return;
            nextDiscountRefreshTick = now + DiscountCycleDays * GenDate.TicksPerDay;
            cycleDiscountFactorByDef.Clear();
            Window_PointsShop.RollCycleDiscounts(this);
        }
        private void CASTick(int now)
        {
            RefreshAircraftStates(now);
            for (int i = 0; i < ActiveSlotCount; i++)
            {
                AircraftSlotData s = aircraftSlots[i];
                if (s.rearmFinishTick > 0 && s.rearmFinishTick <= now)
                {
                    s.rearmFinishTick = 0;
                    s.mountedStrikeId = null;
                    s.chargesLeft = 0;
                }
                if (s.rearmFinishTick <= now && s.chargesLeft <= 0 && !string.IsNullOrEmpty(s.mountedStrikeId))
                {
                    s.mountedStrikeId = null;
                    s.chargesLeft = 0;
                }
            }
        }
        public int GetStrikeMaxUses(string strikeId)
        {
            if (string.IsNullOrEmpty(strikeId))
                return 1;
            if (strikeMaxUsesOverride != null && strikeMaxUsesOverride.TryGetValue(strikeId, out int overrideValue))
                return Mathf.Max(1, overrideValue);
            if(!CMCResearchProjectDefOf.CMC_CAS_AmmoUpgrade.IsFinished)
                switch (strikeId)
                {
                    case "CMC_Airstrike_AC": return 4;
                    case "CMC_Airstrike": return 2;
                    case "CMC_ATGMStrike": return 4;
                    case "CMC_ATGM_LStrike": return 3;
                    case "CMC_PrecisionEMPStrike": return 1;
                    case "CMC_PrecisionStrike": return 1;
                    default: return 1;
                }
            else
                switch (strikeId)
                {
                    case "CMC_Airstrike_AC": return 6;
                    case "CMC_Airstrike": return 4;
                    case "CMC_ATGMStrike": return 6;
                    case "CMC_ATGM_LStrike": return 4;
                    case "CMC_PrecisionEMPStrike": return 2;
                    case "CMC_PrecisionStrike": return 2;
                    default: return 1;
                }
        }
        public void SetStrikeMaxUsesOverride(string strikeId, int maxUses)
        {
            if (string.IsNullOrEmpty(strikeId)) return;
            if (strikeMaxUsesOverride == null) strikeMaxUsesOverride = new Dictionary<string, int>();
            strikeMaxUsesOverride[strikeId] = Mathf.Max(1, maxUses);
        }
        public void GetAircraftSlotSnapshot(
        int slotIndex,
        out bool unlocked,
        out string mountedStrikeId,
        out int chargesLeft,
        out int standbyRemainingTicks,
        out int rearmRemainingTicks,
        out int attackCdRemainingTicks)
        {
            EnsureAircraftSlotsCount();
            int now = Find.TickManager.TicksGame;
            RefreshAircraftStates(now);
            unlocked = (slotIndex >= 0 && slotIndex < MaxAircraft);
            mountedStrikeId = null;
            chargesLeft = 0;
            standbyRemainingTicks = 0;
            rearmRemainingTicks = 0;
            attackCdRemainingTicks = 0;

            if (!unlocked) return;
            if (slotIndex < 0 || slotIndex >= aircraftSlots.Count) return;

            AircraftSlotData s = aircraftSlots[slotIndex];

            if (s.rearmFinishTick > now) rearmRemainingTicks = s.rearmFinishTick - now;
            if (s.standbyExpireTick > now) standbyRemainingTicks = s.standbyExpireTick - now;
            if (s.nextAttackReadyTick > now) attackCdRemainingTicks = s.nextAttackReadyTick - now;

            mountedStrikeId = s.mountedStrikeId;
            chargesLeft = s.chargesLeft;
        }
        private int ActiveSlotCount
        {
            get
            {
                if (aircraftSlots == null) return 0;
                return Mathf.Min(MaxAircraft, aircraftSlots.Count);
            }
        }
        private void EnsureAircraftSlotsCount()
        {
            if (aircraftSlots == null) aircraftSlots = new List<AircraftSlotData>();
            while (aircraftSlots.Count < MaxAircraft)
            {
                AircraftSlotData slot = new AircraftSlotData();
                slot.slotId = aircraftSlots.Count;
                slot.mountedStrikeId = null;
                slot.chargesLeft = 0;
                slot.rearmFinishTick = 0;
                aircraftSlots.Add(slot);
            }
        }
        private void NormalizeAircraftSlots()
        {
            if (aircraftSlots == null) aircraftSlots = new List<AircraftSlotData>();
            int now = Find.TickManager != null ? Find.TickManager.TicksGame : 0;

            for (int i = 0; i < aircraftSlots.Count; i++)
            {
                if (aircraftSlots[i] == null)
                {
                    aircraftSlots[i] = new AircraftSlotData();
                    aircraftSlots[i].slotId = i;
                    continue;
                }

                aircraftSlots[i].slotId = i;

                if (aircraftSlots[i].rearmFinishTick <= now && aircraftSlots[i].chargesLeft <= 0)
                {
                    aircraftSlots[i].chargesLeft = 0;
                    if (!string.IsNullOrEmpty(aircraftSlots[i].mountedStrikeId))
                        aircraftSlots[i].mountedStrikeId = null;
                }
            }
        }
        public int GetStrikeCooldownRemaining(string strikeId, int attackIntervalTicks)
        {
            if (attackIntervalTicks <= 0 || string.IsNullOrEmpty(strikeId)) return 0;
            int last;
            if (!strikeLastUseTick.TryGetValue(strikeId, out last)) return 0;

            int left = last + attackIntervalTicks - Find.TickManager.TicksGame;
            return left > 0 ? left : 0;
        }
        public int GetReadyAircraftCount()
        {
            int now = Find.TickManager.TicksGame;
            int cnt = 0;
            for (int i = 0; i < ActiveSlotCount; i++)
            {
                if (aircraftSlots[i].rearmFinishTick <= now) cnt++;
            }
            return cnt;
        }
        public int GetIdleAircraftCount()
        {
            int now = Find.TickManager.TicksGame;
            int cnt = 0;
            for (int i = 0; i < ActiveSlotCount; i++)
            {
                AircraftSlotData s = aircraftSlots[i];
                if (s.rearmFinishTick <= now && string.IsNullOrEmpty(s.mountedStrikeId)) cnt++;
            }
            return cnt;
        }
        public int GetRearmingAircraftCount()
        {
            int now = Find.TickManager.TicksGame;
            int cnt = 0;
            for (int i = 0; i < ActiveSlotCount; i++)
            {
                if (aircraftSlots[i].rearmFinishTick > now) cnt++;
            }
            return cnt;
        }
        public int GetMountedAircraftCount(string strikeId)
        {
            if (string.IsNullOrEmpty(strikeId)) return 0;
            int now = Find.TickManager.TicksGame;
            int cnt = 0;
            for (int i = 0; i < ActiveSlotCount; i++)
            {
                AircraftSlotData s = aircraftSlots[i];
                if (s.rearmFinishTick <= now && s.mountedStrikeId == strikeId && s.chargesLeft > 0) cnt++;
            }
            return cnt;
        }
        public int GetMountedChargesTotal(string strikeId)
        {
            if (string.IsNullOrEmpty(strikeId)) return 0;
            int now = Find.TickManager.TicksGame;
            int total = 0;
            for (int i = 0; i < ActiveSlotCount; i++)
            {
                AircraftSlotData s = aircraftSlots[i];
                if (s.rearmFinishTick <= now && s.mountedStrikeId == strikeId && s.chargesLeft > 0)
                    total += s.chargesLeft;
            }
            return total;
        }
        public int GetAvailableAircraftForStrike(string strikeId)
        {
            if (string.IsNullOrEmpty(strikeId)) return 0;
            int now = Find.TickManager.TicksGame;
            int cnt = 0;

            for (int i = 0; i < ActiveSlotCount; i++)
            {
                AircraftSlotData s = aircraftSlots[i];
                if (s.rearmFinishTick > now) continue;

                if (s.mountedStrikeId == strikeId && s.chargesLeft > 0)
                    cnt++;
                else if (string.IsNullOrEmpty(s.mountedStrikeId))
                    cnt++;
            }

            return cnt;
        }
        public int GetRearmRemainingTicks(string strikeId)
        {
            if (string.IsNullOrEmpty(strikeId)) return 0;
            int now = Find.TickManager.TicksGame;
            int min = int.MaxValue;

            for (int i = 0; i < ActiveSlotCount; i++)
            {
                AircraftSlotData s = aircraftSlots[i];
                if (s.mountedStrikeId == strikeId && s.rearmFinishTick > now)
                {
                    int left = s.rearmFinishTick - now;
                    if (left < min) min = left;
                }
            }

            return min == int.MaxValue ? 0 : min;
        }
        public int GetCurrentCharges(string strikeId, int maxUses)
        {
            return GetMountedChargesTotal(strikeId);
        }
        private List<int> PickAircraftForStrike(string strikeId, int aircraftNeededPerCall)
        {
            List<int> picked = new List<int>();
            int now = Find.TickManager.TicksGame;
            for (int i = 0; i < ActiveSlotCount; i++)
            {
                if (picked.Count >= aircraftNeededPerCall) break;
                AircraftSlotData s = aircraftSlots[i];
                if (s.rearmFinishTick > now) continue;
                if (s.nextAttackReadyTick > now) continue;
                if (s.mountedStrikeId == strikeId && s.chargesLeft > 0)
                    picked.Add(i);
            }
            for (int i = 0; i < ActiveSlotCount; i++)
            {
                if (picked.Count >= aircraftNeededPerCall) break;
                AircraftSlotData s = aircraftSlots[i];
                if (s.rearmFinishTick > now) continue;
                if (s.nextAttackReadyTick > now) continue;
                if (string.IsNullOrEmpty(s.mountedStrikeId))
                    picked.Add(i);
            }
            return picked;
        }
        public int GetStrikeShortestAircraftCooldown(string strikeId)
        {
            if (string.IsNullOrEmpty(strikeId)) return 0;
            int now = Find.TickManager.TicksGame;
            int min = int.MaxValue;

            for (int i = 0; i < ActiveSlotCount; i++)
            {
                var s = aircraftSlots[i];
                if (s.rearmFinishTick > now) continue;
                if (s.mountedStrikeId != strikeId && !string.IsNullOrEmpty(s.mountedStrikeId)) continue;

                int left = s.nextAttackReadyTick - now;
                if (left > 0 && left < min) min = left;
            }

            return min == int.MaxValue ? 0 : min;
        }
        public bool CanLaunchStrike(string strikeId, int cost, int attackIntervalTicks, int rearmTicks, int aircraftNeededPerCall, out string failReason)
        {
            if (aircraftNeededPerCall < 1) aircraftNeededPerCall = 1;

            if (CurrentPoint < cost)
            {
                failReason = "CMC_InsufficientPoints".Translate().ToString();
                return false;
            }
            int available = GetAvailableAircraftForStrike(strikeId);
            if (available < aircraftNeededPerCall)
            {
                failReason = "CMC_NotEnoughAircraftForStrike".Translate(available, aircraftNeededPerCall).ToString();
                return false;
            }

            failReason = null;
            return true;
        }
        public bool TryConsumeForLaunch(string strikeId, int cost, int attackIntervalTicks, int rearmTicks, int aircraftNeededPerCall, out string failReason)
        {
            if (!CanLaunchStrike(strikeId, cost, attackIntervalTicks, rearmTicks, aircraftNeededPerCall, out failReason))
                return false;

            int now = Find.TickManager.TicksGame;
            int maxUses = GetStrikeMaxUses(strikeId);
            List<int> picked = PickAircraftForStrike(strikeId, aircraftNeededPerCall);

            if (picked.Count < aircraftNeededPerCall)
            {
                failReason = "CMC_NotEnoughAircraftForStrike".Translate(picked.Count, aircraftNeededPerCall).ToString();
                return false;
            }

            CurrentPoint -= cost;
            strikeLastUseTick[strikeId] = now;
            for (int i = 0; i < picked.Count; i++)
            {
                AircraftSlotData s = aircraftSlots[picked[i]];
                if (string.IsNullOrEmpty(s.mountedStrikeId))
                {
                    s.mountedStrikeId = strikeId;
                    s.chargesLeft = maxUses;
                    s.standbyExpireTick = now + AircraftStandbyTicks;
                }
                s.chargesLeft -= 1;
                s.nextAttackReadyTick = now + Mathf.Max(0, attackIntervalTicks);
                if (s.chargesLeft <= 0)
                {
                    s.chargesLeft = 0;
                    s.rearmFinishTick = now + Mathf.Max(0, rearmTicks);
                    s.standbyExpireTick = 0;
                }
                else
                {
                    s.standbyExpireTick = now + AircraftStandbyTicks;
                }
            }
            return true;
        }
        private void ApplyMigrationsIfNeeded()
        {
            if (migratedThisSession) return;
            migratedThisSession = true;

            EnsureDiscountState();
            EnsureAircraftSlotsCount();
            NormalizeAircraftSlots();

            int now = Find.TickManager?.TicksGame ?? 0;
            if (saveVersion < 1 && nextDiscountRefreshTick <= 0)
                nextDiscountRefreshTick = now;
            if (now >= nextDiscountRefreshTick)
                ShopTick(now);

            saveVersion = SaveVersionCurrent;
        }
        public void ReinitializeShopState(bool rerollNow = true)
        {
            EnsureDiscountState();
            cycleDiscountFactorByDef.Clear();

            int now = Find.TickManager?.TicksGame ?? 0;
            if (rerollNow)
            {
                nextDiscountRefreshTick = now;
                ShopTick(now);
            }
            else
            {
                nextDiscountRefreshTick = now + DiscountCycleDays * GenDate.TicksPerDay;
            }
        }
        public float GetCycleDiscountFactor(ThingDef def)
        {
            if (def == null || cycleDiscountFactorByDef == null) return 1f;
            float f;
            if (!cycleDiscountFactorByDef.TryGetValue(def.defName, out f)) return 1f;
            return Mathf.Clamp(f, 0.01f, 1f);
        }
        public void SetCycleDiscountFactor(ThingDef def, float factor)
        {
            if (def == null) return;
            EnsureDiscountState();
            cycleDiscountFactorByDef[def.defName] = Mathf.Clamp(factor, 0.01f, 1f);
        }
        private void EnsureDiscountState()
        {
            if (purchasedBundlesByDef == null)
                purchasedBundlesByDef = new Dictionary<string, int>();

            if (cycleDiscountFactorByDef == null)
                cycleDiscountFactorByDef = new Dictionary<string, float>();
        }
        public int GetPurchasedBundles(ThingDef def)
        {
            if (def == null) return 0;
            int v;
            return purchasedBundlesByDef.TryGetValue(def.defName, out v) ? v : 0;
        }
        public void AddPurchasedBundles(ThingDef def, int bundles)
        {
            if (def == null || bundles <= 0) return;
            string key = def.defName;
            purchasedBundlesByDef[key] = GetPurchasedBundles(def) + bundles;
        }
        public bool IsFirstPurchase(ThingDef def)
        {
            return GetPurchasedBundles(def) == 0;
        }
        public int ExtraPenetration()
        {
            if (CMCResearchProjectDefOf.CMC_EMweaponTech_AP.IsFinished)
                return 1;
            return 0;
        }
        public float ShieldCost()
        {
            if (CMCResearchProjectDefOf.CMC_EMweaponTech_AP.IsFinished)
                return 0.5f;
            return 1f;
        }
    }
}