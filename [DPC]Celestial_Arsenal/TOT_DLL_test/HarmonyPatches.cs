using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TOT_DLL_test
{
    [StaticConstructorOnStartup]
    public class HarmonyPatches
    {
        static HarmonyPatches()
        {
            Harmony harmony = new Harmony("TOT.CMC.Weaponry");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            harmony.Patch(AccessTools.Method(typeof(Projectile), "CheckForFreeInterceptBetween", null, null), new HarmonyMethod(typeof(HarmonyPatches.Harmony_CheckForFreeInterceptBetween), "Prefix", null), null, null, null);
            Log.Message("CMC:projectileinterceptor patched");
            harmony.Patch(AccessTools.Method(typeof(PawnRenderUtility), "DrawEquipmentAiming", null, null), new HarmonyMethod(typeof(HarmonyPatches.HarmonyPatch_PawnWeaponRenderer), "Prefix", null), null, null, null);
            Log.Message("CMC:GunRender patched");
        }
        internal static class Harmony_CheckForFreeInterceptBetween
        {
            public static bool Prefix(Projectile __instance, Vector3 lastExactPos, Vector3 newExactPos, ref bool __result)
            {
                bool flag = lastExactPos == newExactPos;
                if (flag)
                {
                    return false;
                }
                if (CMC_Def.CMCShieldGenerator == null)
                {
                    return true;
                }
                List<Thing> list = __instance.Map.listerThings.ThingsOfDef(CMC_Def.CMCShieldGenerator);
                for (int i = 0; i < list.Count; i++)
                {
                    try
                    {
                        Building_FRShield building_FRShield = list[i] as Building_FRShield;
                        bool flag3 = building_FRShield != null && building_FRShield.TryGetComp<CompFullProjectileInterceptor>().CheckIntercept(__instance, lastExactPos, newExactPos);
                        if (flag3)
                        {
                            __instance.Destroy(DestroyMode.Vanish);
                            __result = true;
                            return false;
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(PawnRenderUtility), "DrawEquipmentAiming")]
        private class HarmonyPatch_PawnWeaponRenderer
        {
            public static bool Prefix(Thing eq, Vector3 drawLoc, float aimAngle)
            {
                if (eq != null && eq.TryGetComp<Comp_WeaponRenderStatic>() != null && eq.TryGetComp<CompEquippable>().ParentHolder != null)
                {
                    DrawWeaponExtraEquipped.DrawExtraMatStatic(eq, drawLoc, aimAngle);
                    return false;
                }
                return true;
            }
        }

        [StaticConstructorOnStartup]
        [HarmonyPatch(typeof(MapPawns), "PlayerEjectablePodHolder")]
        private static class PlayerEjectablePodHolder_PostFix
        {
            [HarmonyPostfix]
            public static void PostFix(Thing thing, ref IThingHolder __result)
            {
                SkillDummy_Sword skillDummy_Sword = thing as SkillDummy_Sword;
                bool flag = skillDummy_Sword != null && skillDummy_Sword.innerContainer.Any;
                if (flag)
                {
                    __result = (thing as IThingHolder);
                }
            }
        }

        [StaticConstructorOnStartup]
        [HarmonyPatch(typeof(PawnsArrivalModeWorker_CenterDrop), "TryResolveRaidSpawnCenter", null)]
        public static class Harmony_CenterDrop_TryResolveRaidSpawnCenter
        {
            public static bool Prefix(IncidentParms parms)
            {
                Map map = parms.target as Map;
                bool flag = map != null;
                if (flag)
                {
                    List<Thing> list = map.listerThings.ThingsOfDef(CMC_Def.CMC_CICAESA_Radar);
                    if (list != null && list.Count > 0)
                    {
                        parms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeDrop;
                        parms.spawnCenter = DropCellFinder.FindRaidDropCenterDistant(map, false);
                        parms.raidNeverFleeIndividual = true;
                        parms.podOpenDelay = 14;
                        parms.pointMultiplier = 1.4f;
                        return false;
                    }
                }
                return true;
            }
        }
        [StaticConstructorOnStartup]
        [HarmonyPatch(typeof(VerbProperties), "get_Ranged")]
        public static class VerbProps_Patch
        {
            public static bool Prefix(VerbProperties __instance, ref bool __result)
            {
                bool flag = __instance.verbClass == typeof(Verb_ShootSwitchFire);
                bool result;
                if (flag)
                {
                    __result = true;
                    result = false;
                }
                else
                {
                    result = true;
                }
                return result;
            }
        }
        [HarmonyPatch(typeof(MapParent), "CheckRemoveMapNow")]
        public static class MapParent_CheckRemoveMapNow_Patch
        {
            public static bool Prefix(MapParent __instance)
            {
                bool flag = __instance == GameComponent_CeleTech.Instance.ASEA_observedMap;
                return !flag;
            }
        }
        [HarmonyPatch(typeof(Settlement), "ShouldRemoveMapNow")]
        public static class SettlementShouldRemoveMapPatch
        {
            [HarmonyPostfix]
            public static void Postfix(ref bool __result, Settlement __instance)
            {
                bool flag = __result && __instance == GameComponent_CeleTech.Instance.ASEA_observedMap;
                if (flag)
                {
                    __result = false;
                }
            }
        }
        [HarmonyPatch(typeof(CompApparelReloadable), nameof(CompApparelReloadable.NeedsReload))]
        public static class CompApparelReloadable_NeedsReload_Patch
        {
            [HarmonyPrefix]
            public static bool Prefix(CompApparelReloadable __instance, bool allowForcedReload, ref bool __result)
            {
                try
                {
                    var funnelHaulerComp = __instance?.parent?.TryGetComp<CompFunnelHauler>();
                    if (funnelHaulerComp == null) return true;

                    int slotCount = funnelHaulerComp.GetDockSlotCountForRender();
                    for (int i = 0; i < slotCount; i++)
                    {
                        if (!funnelHaulerComp.TryGetSlotStatus(
                                i,
                                out CompFunnelHauler.FunnelSlotState state,
                                out _,
                                out _))
                        {
                            continue;
                        }

                        if (state == CompFunnelHauler.FunnelSlotState.Deployed)
                        {
                            __result = false; 
                            return false; 
                        }
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    Log.Error($"Error in NeedsReload prefix: {ex}");
                    return true;
                }
            }
        }
        [HarmonyPatch(
        typeof(Projectile),
        nameof(Projectile.Launch),
        new Type[]
        {
            typeof(Thing),              // launcher
            typeof(Vector3),            // origin
            typeof(LocalTargetInfo),    // usedTarget
            typeof(LocalTargetInfo),    // intendedTarget
            typeof(ProjectileHitFlags), // hitFlags
            typeof(bool),               // preventFriendlyFire
            typeof(Thing),              // equipment
            typeof(ThingDef)            // targetCoverDef
        })]
        public static class MissileLaunchPatch
        {
            public static HashSet<ThingDef> TargetMissileDefs = ProjectileCache.ProjectileDefs;

            [HarmonyPostfix]
            public static void Postfix(Projectile __instance)
            {
                if (__instance == null || __instance.Map == null) return;
                if (TargetMissileDefs == null || !TargetMissileDefs.Contains(__instance.def)) return;
                var mgr = __instance.Map.GetComponent<MissileDefenseManager>();
                if (mgr != null && mgr.HasActiveRadar)
                {
                    mgr.RegisterIncomingMissile(__instance);
                }
            }
        }
        //[HarmonyPatch(typeof(ApparelGraphicRecordGetter), "TryGetGraphicApparel")]
        //public static class Patch_ApparelGraphicRecordGetter
        //{
        //    [HarmonyPostfix]
        //    public static void Postfix(Apparel apparel, ref ApparelGraphicRecord rec, ref bool __result)
        //    {
        //        if (!__result) return;
        //        var comp = apparel.GetComp<CompCamo>();
        //        if (comp != null && comp.HasCamoData)
        //        {
        //            rec.graphic = comp.GetCamoGraphic(rec.graphic);
        //        }
        //    }
        //}
        //[HarmonyPatch(typeof(Thing), "Graphic", MethodType.Getter)]
        //public static class Patch_Item_Graphic
        //{
        //    [HarmonyPostfix]
        //    public static void Postfix(Thing __instance, ref Graphic __result)
        //    {
        //        if (__instance is Apparel apparel)
        //        {
        //            var comp = apparel.GetComp<CompCamo>();
        //            if (comp != null && comp.HasCamoData)
        //            {
        //                __result = comp.GetCamoGraphic(__result);
        //            }
        //        }
        //    }
        //}
    }
}
