using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace TOT_DLL_test
{
    public class CompSmartWeapon : ThingComp
    {
        public CompPreperties_SmartWeapon Props
        {
            get
            {
                return (CompPreperties_SmartWeapon)this.props;
            }
        }
        private Verb get_Verb()
        {
            if (this.verb == null)
            {
                this.verb = this.EquipmentSource.PrimaryVerb;
            }
            return this.verb;
        }
        public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
        {
            //base.SpecialDisplayStats();
            //IEnumerable<StatDrawEntry> enumerable = base.SpecialDisplayStats();
            //if (enumerable != null)
            //{
            //    foreach (StatDrawEntry statDrawEntry in enumerable)
            //    {
            //         yield return statDrawEntry;
            //    }
            //    IEnumerator<StatDrawEntry> enumerator = null;
            //}
            yield return new StatDrawEntry(StatCategoryDefOf.Weapon_Ranged, "Stat_CMCDMGDrange_Label".Translate(), "Stat_CMCDMGDrange_Desc".Translate(this.Props.DamageDeductionRange), "Stat_CMCDMGDrange_Text".Translate(this.Props.DamageDeductionRange), 101, null, null, false, false);
            yield return new StatDrawEntry(StatCategoryDefOf.Weapon_Ranged, "StatMinDamageMultiplier_Label".Translate(), "StatMinDamageMultiplier_Desc".Translate(this.Props.MinDamageMultiplier.ToStringPercent()), "StatMinDamageMultiplier_Text".Translate(this.Props.MinDamageMultiplier.ToStringPercent()), 102, null, null, false, false);
            yield return new StatDrawEntry(StatCategoryDefOf.Weapon_Ranged, "StatMinPeneMultiplier_Label".Translate(), "StatMinPeneMultiplier_Desc".Translate(this.Props.MinPenetrationMultiplier.ToStringPercent()), "StatMinPeneMultiplier_Text".Translate(this.Props.MinPenetrationMultiplier.ToStringPercent()), 103, null, null, false, false);
            yield break;
        }
        private CompEquippable EquipmentSource
        {
            get
            {
                if (this.compEquippable != null)
                {
                    return this.compEquippable;
                }
                this.compEquippable = this.parent.TryGetComp<CompEquippable>();
                if (this.compEquippable == null)
                {
                    Log.ErrorOnce(this.parent.LabelCap + " Comp_SmartWeapon but no CompEquippable", 50020);
                }
                return this.compEquippable;
            }
        }
        private Verb verb;
        private CompEquippable compEquippable;
    }
}
