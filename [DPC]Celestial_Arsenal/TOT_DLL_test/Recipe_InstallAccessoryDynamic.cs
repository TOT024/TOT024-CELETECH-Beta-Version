using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace TOT_DLL_test
{
    public class Recipe_InstallAccessoryDynamic : RecipeWorker
    {
        public override void Notify_IterationCompleted(Pawn billDoer, List<Thing> ingredients)
        {
            Bill bill = billDoer?.CurJob?.bill;
            if (bill == null)
            {
                Log.Error("Notify_IterationCompleted: Could not find Bill on Pawn. Aborting.");
                DropIngredients(billDoer, ingredients);
                return;
            }
            var bench = bill.billStack.billGiver as Building_WeaponModificationBench;
            if (bench == null)
            {
                Log.Error("Notify_IterationCompleted: BillGiver is not a WeaponModificationBench. Aborting.");
                DropIngredients(billDoer, ingredients);
                return;
            }
            var weapon = bench.GetComp<CompWeaponHolder>().HeldWeapon;
            if (weapon == null)
            {
                Log.Warning("Notify_IterationCompleted: No weapon on bench. Dropping ingredients.");
                DropIngredients(bench, ingredients);
                return;
            }
            var accessory = ingredients.FirstOrDefault();
            if (accessory == null)
            {
                Log.Warning("Notify_IterationCompleted: Ingredient list was empty. Aborting.");
                return;
            }
            var holder = weapon.TryGetComp<CompAccessoryHolder>();
            if (holder != null && holder.TryInstallAccessory(accessory))
            {
                Messages.Message($"Successfully installed {accessory.LabelCap} onto {weapon.LabelCap}.", weapon, MessageTypeDefOf.PositiveEvent);
            }
            else
            {
                GenPlace.TryPlaceThing(accessory, bench.Position, bench.Map, ThingPlaceMode.Near);
            }
        }

        public override void ConsumeIngredient(Thing ingredient, RecipeDef recipe, Map map)
        {
        }

        private void DropIngredients(Thing dropCenter, List<Thing> ingredients)
        {
            if (ingredients == null) return;
            foreach (var ing in ingredients)
            {
                GenPlace.TryPlaceThing(ing, dropCenter.Position, dropCenter.Map, ThingPlaceMode.Near);
            }
        }
    }
}