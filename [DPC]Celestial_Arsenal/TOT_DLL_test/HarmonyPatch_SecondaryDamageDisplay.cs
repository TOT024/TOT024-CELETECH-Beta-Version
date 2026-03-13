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
    internal class HarmonyPatch_SecondaryDamageDisplay
    {
        [HarmonyPatch(typeof(ThingDef), nameof(ThingDef.SpecialDisplayStats))]
        public static class Patch_ThingDef_SpecialDisplayStats
        {
            public static void Postfix(ref IEnumerable<StatDrawEntry> __result, ThingDef __instance, StatRequest req)
            {
                __result = InjectSecondaryDamage(__result, __instance, req);
            }

            private static IEnumerable<StatDrawEntry> InjectSecondaryDamage(
                IEnumerable<StatDrawEntry> source, ThingDef def, StatRequest req)
            {
                foreach (var e in source) yield return e;

                if (def?.Verbs.NullOrEmpty() ?? true) yield break;
                var verb = def.Verbs.FirstOrDefault(v => v.isPrimary);
                var proj = verb?.defaultProjectile?.projectile;
                if (verb == null) yield break;

                var extras = new List<ExtraDamage>();
                if (!proj?.extraDamages.NullOrEmpty() ?? false)
                    extras.AddRange(proj.extraDamages);

                if (req.HasThing && req.Thing.TryGetComp<CompUniqueWeapon>(out var comp))
                {
                    foreach (var t in comp.TraitsListForReading)
                        if (!t.extraDamages.NullOrEmpty())
                            extras.AddRange(t.extraDamages);
                }

                if (extras.Count == 0) yield break;

                var cat = (def.category == ThingCategory.Pawn) ? StatCategoryDefOf.PawnCombat : StatCategoryDefOf.Weapon_Ranged;
                int prio = 5499;

                foreach (var ex in extras)
                {
                    string amountText = ex.amount.ToString("0.#");
                    string damageType = ex.def?.label ?? "CMC_UnknownDamageType".Translate().ToString();

                    string chanceSuffix = ex.chance < 1f
                        ? "CMC_SecondaryDamage_ChanceSuffix".Translate(ex.chance.ToStringPercent()).ToString()
                        : string.Empty;

                    string rightText = "CMC_SecondaryDamage_Desc".Translate(amountText, damageType, chanceSuffix).ToString();

                    yield return new StatDrawEntry(
                        cat,
                        "CMC_SecondaryDamage_Label".Translate(),
                        amountText,
                        rightText,     
                        prio--);
                }
            }
        }
    }
}
