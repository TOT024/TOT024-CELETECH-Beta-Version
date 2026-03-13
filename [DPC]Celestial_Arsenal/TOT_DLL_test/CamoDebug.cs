using LudeonTK;
using RimWorld;
using UnityEngine;
using Verse;

namespace TOT_DLL_test
{
    public static class CMC_DebugActions
    {
        [DebugAction("CMC", "Reinit Shop+Discount",
        actionType = DebugActionType.Action,
        allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void ReinitShopDiscount()
        {
            var comp = Current.Game?.GetComponent<GameComponent_CeleTech>();
            if (comp == null) return;
            comp.ReinitializeShopState(true);
            Messages.Message("CMC shop/discount reinitialized.", MessageTypeDefOf.NeutralEvent, false);
        }
        //[DebugAction("CMC_Camo Test", "Set Camo Data (Comp)", false, false, false, false, false, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        //private static void SetCamoData()
        //{
        //    if (ShaderLoader.CustomShader == null)
        //    {
        //        Log.Error("Shader not loaded!");
        //        return;
        //    }
        //    Find.Targeter.BeginTargeting(TargetingParameters.ForThing(), (LocalTargetInfo target) =>
        //    {
        //        Apparel apparel = null;
        //        if (target.Thing is Apparel a)
        //            apparel = a;
        //        else if (target.Thing is Pawn p && p.apparel != null && p.apparel.WornApparel.Count > 0)
        //            apparel = p.apparel.WornApparel[0];

        //        if (apparel != null)
        //        {
        //            ApplyToApparel(apparel);
        //        }
        //        else
        //        {
        //            Messages.Message("No apparel found.", MessageTypeDefOf.RejectInput);
        //        }
        //    });
        //}
        //private static void ApplyToApparel(Apparel apparel)
        //{
        //    CompCamo comp = apparel.GetComp<CompCamo>();
        //    if (comp == null)
        //    {
        //        Messages.Message($"Error: {apparel.Label} does not have CompCamo in XML!", MessageTypeDefOf.RejectInput);
        //        return;
        //    }
        //    Color c1 = Color.red;
        //    Color c2 = Color.green;
        //    Color c3 = Color.blue;
        //    string mask = "Patterns/Hex";
        //    Vector2 scale = new Vector2(3f, 3f);
        //    Vector2 offset = Vector2.zero;
        //    comp.SetData(c1, c2, c3, mask, scale, offset);

        //    Messages.Message($"Camo applied to {apparel.LabelShort}", MessageTypeDefOf.TaskCompletion);
        //}
    }
}