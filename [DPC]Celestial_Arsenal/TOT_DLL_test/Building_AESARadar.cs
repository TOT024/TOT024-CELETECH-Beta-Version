using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace TOT_DLL_test
{
    [StaticConstructorOnStartup]
    public class Building_AESARadar : Building
    {
        private const int MaxWorldRange = 150;
        private static readonly Texture2D TargeterMouseAttachment = ContentFinder<Texture2D>.Get("UI/UI_Overlay_RadarTargeting", true);
        private static readonly Texture2D IconScan = ContentFinder<Texture2D>.Get("UI/UI_ScanMap_AESA", true);
        private static readonly Texture2D IconStop = ContentFinder<Texture2D>.Get("UI/UI_ScanMap_AESA_Stop", true);

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (!respawningAfterLoad)
            {
                Messages.Message("Message_AESASetUp".Translate(), MessageTypeDefOf.NeutralEvent, true);
            }
            map?.GetComponent<MissileDefenseManager>()?.RegisterRadar(true);
            Log.Message("radar registered");
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            Map map = base.Map;
            base.DeSpawn(mode);

            if (map == null) return;

            if (map.listerThings.ThingsOfDef(CMC_Def.CMC_CICAESA_Radar).Count == 0)
            {
                map.GetComponent<MissileDefenseManager>()?.RegisterRadar(false);
            }
            if (map.listerThings.ThingsOfDef(CMC_Def.CMCML).Count > 0)
            {
                Messages.Message("Message_Destroyed".Translate(), MessageTypeDefOf.NeutralEvent, true);
            }
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var g in base.GetGizmos()) yield return g;
            if (Faction != Faction.OfPlayer) yield break;

            var comp = GameComponent_CeleTech.Instance;
            if (comp == null || Map == null) yield break;
            if (comp.ASEA_observedMap != null && comp.ASEA_observedMap.Destroyed)
            {
                comp.ASEA_observedMap = null;
            }

            if (comp.ASEA_observedMap == null)
            {
                yield return new Command_Action
                {
                    defaultLabel = "CMC.ScanMap".Translate(),
                    defaultDesc = "CMC.ScanMapDesc".Translate(),
                    icon = IconScan,
                    action = delegate
                    {
                        CameraJumper.TryShowWorld();
                        Find.WorldTargeter.BeginTargeting(
                            new Func<GlobalTargetInfo, bool>(ChoseWorldTarget),
                            canTargetTiles: true,
                            mouseAttachment: TargeterMouseAttachment,
                            closeWorldTabWhenFinished: false,
                            onUpdate: DrawWorldRadius,
                            extraLabelGetter: null,
                            canSelectTarget: null,
                            originForClosest: Map.Tile,
                            showCancelButton: false
                        );
                    }
                };
            }
            else
            {
                string label = comp.ASEA_observedMap.Label;
                yield return new Command_Action
                {
                    defaultLabel = "CMC.ScanMapStop".Translate(),
                    defaultDesc = "CMC.ScanMapStopDesc".Translate(label),
                    icon = IconStop,
                    action = () => { comp.ASEA_observedMap = null; }
                };
            }
        }

        private void DrawWorldRadius()
        {
            if (Map != null)
            {
                GenDraw.DrawWorldRadiusRing(Map.Tile, MaxWorldRange);
            }
        }

        private bool ChoseWorldTarget(GlobalTargetInfo target)
        {
            if (!target.IsValid || Map == null) return false;
            PlanetTile tile = target.Tile;
            if (!tile.Valid) return false;
            MapParent mapParent = Find.WorldObjects.MapParentAt(tile);

            int distance = Find.WorldGrid.TraversalDistanceBetween(Map.Tile, tile, passImpassable: true, maxDist: int.MaxValue, canTraverseLayers: true);
            if (distance > MaxWorldRange)
            {
                Messages.Message("CMC.ScanMapOutOfRange".Translate(MaxWorldRange), MessageTypeDefOf.RejectInput, false);
                return false;
            }
            if (mapParent == null)
            {
                Messages.Message("CMC.ScanMapFailed".Translate(), MessageTypeDefOf.RejectInput);
                return false;
            }

            Map targetMap = mapParent.Map;
            if (targetMap == null)
            {
                if (mapParent.Faction != null && !mapParent.Faction.HostileTo(Faction.OfPlayer))
                {
                    Messages.Message("CMC.ScanMapFailed".Translate(), MessageTypeDefOf.RejectInput);
                    return false;
                }
                targetMap = GetOrGenerateMapUtility.GetOrGenerateMap(tile, null, null);
                if (targetMap == null)
                {
                    Messages.Message("CMC.ScanMapFailed".Translate(), MessageTypeDefOf.RejectInput);
                    return false;
                }
            }

            GameComponent_CeleTech.Instance.ASEA_observedMap = mapParent;
            Current.Game.CurrentMap = targetMap;
            CameraJumper.TryJump(new GlobalTargetInfo(targetMap.Center, targetMap, false), CameraJumper.MovementMode.Pan);
            return true;
        }

        public override string GetInspectString()
        {
            string inspectString = base.GetInspectString();
            var comp = GameComponent_CeleTech.Instance;

            if (comp != null && comp.ASEA_observedMap != null && !comp.ASEA_observedMap.Destroyed)
            {
                if (!inspectString.NullOrEmpty()) inspectString += "\n";
                inspectString += "CMC_Targeting".Translate(comp.ASEA_observedMap.Label);
            }
            else
            {
                if (!inspectString.NullOrEmpty()) inspectString += "\n";
                inspectString += "CMC_TargetingDefault".Translate();
            }

            return inspectString;
        }
    }
}