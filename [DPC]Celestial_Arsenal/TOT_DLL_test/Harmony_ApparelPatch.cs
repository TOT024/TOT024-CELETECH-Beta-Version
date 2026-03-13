using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace TOT_DLL_test
{
    public class Harmony_AppareltPatch
    {
        //[HarmonyPatch(typeof(Apparel), "PawnCanWear")]
        //public static class Harmony_CheckUAVCountPreWear
        //{
        //    public static void Postfix(Apparel __instance, Pawn pawn, ref bool __result)
        //    {
        //        if (__instance is Apparel_FloatingGunRework)
        //        {
        //            Pawn_ApparelTracker apparel = pawn.apparel;
        //            List<Apparel> list = apparel?.WornApparel;
        //            int count = 0;
        //            foreach (Apparel indexer in list)
        //            {
        //                if (indexer is Apparel_FloatingGunRework)
        //                {
        //                    count++;
        //                }
        //            }
        //            if (count > GameComponent_CeleTech.Instance.FloatingGunMax)
        //            {
        //                GenPlace.TryPlaceThing(__instance, pawn.Position, pawn.Map, ThingPlaceMode.Near, null, null, default(Rot4));
        //                MoteMaker.ThrowText(pawn.PositionHeld.ToVector3(), pawn.MapHeld, "CMC_FailedToWear_ExceedNumLimit".Translate(), 5f);
        //                __result = false;
        //            }
        //            else
        //            {
        //                __result = true;
        //            }

        //        }
        //    }
        //}

        //[HarmonyPatch(typeof(Pawn_ApparelTracker), "Wear")]
        //public static class Harmony_Wear
        //{
        //    public static bool Prefix(Pawn_ApparelTracker __instance, Apparel newApparel)
        //    {
        //        if (newApparel is Apparel_FloatingGunRework)
        //        {
        //            Pawn pawn = __instance.pawn;
        //            List<Apparel> list = __instance.WornApparel;
        //            int count = 0;
        //            foreach (Apparel indexer in list)
        //            {
        //                if (indexer is Apparel_FloatingGunRework)
        //                {
        //                    count++;
        //                }
        //            }
        //            if (count >= GameComponent_CeleTech.Instance.FloatingGunMax)
        //            {
        //                GenPlace.TryPlaceThing(newApparel, pawn.Position, pawn.Map, ThingPlaceMode.Near, null, null, default(Rot4));
        //                MoteMaker.ThrowText(pawn.PositionHeld.ToVector3(), pawn.MapHeld, "CMC_FailedToWear_ExceedNumLimit".Translate(), 5f);
        //                return false;
        //            }
        //            return true;
        //        }
        //        return true;
        //    }
        //}

        //[HarmonyPatch(typeof(ApparelUtility), "CanWearTogether")]
        //public static class Harmony_CheckCanWearTogether
        //{
        //    public static void Postfix(ThingDef A, ThingDef B, ref bool __result)
        //    {
        //        if (__result != true)
        //        {
        //            if (A.thingClass.ToString() is "TOT_DLL_test.Apparel_FloatingGunRework" && B.thingClass.ToString() is "TOT_DLL_test.Apparel_FloatingGunRework")
        //            {
        //                __result = true;
        //                return;
        //            }
        //        }
        //    }
        //}
    }
}
