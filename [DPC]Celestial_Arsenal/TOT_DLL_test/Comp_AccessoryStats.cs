using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using static HarmonyLib.Code;

namespace TOT_DLL_test
{
    public class StatModifier
    {
        public StatDef stat;
        public ModifierType modifier;
        public float value;
    }
    public enum ModifierType
    {
        Add,
        Multiply,
    }

    public class CompProperties_AccessoryStats : CompProperties
    {
        public List<StatModifier> stats = new List<StatModifier>();
        public List<AbilityDef> abilities = new List<AbilityDef>();
        [Unsaved]
        private ThingDef myParentDef;
        [Unsaved]
        private List<ThingDef> cachedSupportedWeapons;
        public override void ResolveReferences(ThingDef parentDef)
        {
            base.ResolveReferences(parentDef);
            this.myParentDef = parentDef;
        }
        private List<ThingDef> GetSupportedWeapons()
        {
            if (cachedSupportedWeapons != null) return cachedSupportedWeapons;

            cachedSupportedWeapons = new List<ThingDef>();
            if (myParentDef == null) return cachedSupportedWeapons;

            string myDefName = myParentDef.defName;
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
            {
                var holderProps = def.GetCompProperties<CompProperties_AccessoryHolder>();
                if (holderProps != null &&
                    holderProps.allowedAccessoryDefs != null &&
                    holderProps.allowedAccessoryDefs.Contains(myDefName))
                {
                    cachedSupportedWeapons.Add(def);
                }
            }
            cachedSupportedWeapons.SortBy(x => x.label);
            return cachedSupportedWeapons;
        }
        public override IEnumerable<StatDrawEntry> SpecialDisplayStats(StatRequest req)
        {
            foreach (var entry in base.SpecialDisplayStats(req))
            {
                yield return entry;
            }
            if (stats != null)
            {
                foreach (StatModifier mod in stats)
                {
                    if (mod == null || mod.stat == null) continue;
                    yield return new StatDrawEntry(
                        StatCategoryDefOf.Basics,
                        mod.stat.LabelCap,
                        FormatModifierValue(mod),      
                        FormatModifierExplanation(mod),
                        1000,
                        null,
                        null,
                        false
                    );
                }
            }
            List<ThingDef> weapons = GetSupportedWeapons();
            if (weapons != null && weapons.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                List<Dialog_InfoCard.Hyperlink> hyperlinks = new List<Dialog_InfoCard.Hyperlink>();

                sb.AppendLine("CMC_CompatibleDescription".Translate());

                foreach (ThingDef w in weapons)
                {
                    sb.AppendLine($"- {w.LabelCap}");
                    hyperlinks.Add(new Dialog_InfoCard.Hyperlink(w));
                }

                yield return new StatDrawEntry(
                    StatCategoryDefOf.Basics,
                    "SupportedWeapons".Translate(),
                    $"{weapons.Count}",
                    sb.ToString(),
                    900,
                    null,
                    hyperlinks,
                    false
                );
            }
        }
        private string FormatModifierValue(StatModifier mod)
        {
            if (mod.stat == null) return mod.value.ToString();

            ToStringStyle style = mod.stat.toStringStyle;
            if (style == ToStringStyle.Integer && mod.stat.toStringStyle == ToStringStyle.Integer)
                style = ToStringStyle.FloatMaxTwo;

            if (mod.modifier == ModifierType.Add)
                return mod.value.ToStringByStyle(style, ToStringNumberSense.Offset);
            if (mod.modifier == ModifierType.Multiply)
                return mod.value.ToStringByStyle(ToStringStyle.PercentZero, ToStringNumberSense.Factor);

            return mod.value.ToString();
        }

        private string FormatModifierExplanation(StatModifier mod)
        {
            if (mod.stat == null) return "";
            return mod.stat.description ?? "";
        }
        public CompProperties_AccessoryStats()
        {
            compClass = typeof(CompAccessoryStats);
        }
    }

    public class CompAccessoryStats : ThingComp
    {
        public CompProperties_AccessoryStats Props => (CompProperties_AccessoryStats)props;
    }
}