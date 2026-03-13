using System;
using UnityEngine;
using Verse;
using RimWorld;
namespace TOT_DLL_test
{
    public class ShuttleEntry
    {
        public ThingDef ThingDef;
        public TraderKindDef TraderKindDef;
        public Texture2D Icon;
        public Color BackgroundColor;
        public Func<int> GetCurrentCooldownHours;
        public Action ResetCooldown;
        public int CooldownDurationHours;
        public int EarlyCallCost;
        public string DescriptionKey;
        public ShuttleEntry(string shipThingName, string traderDefName, string iconPath, Color bgColor, Func<int> getCooldown, Action resetCooldown, int cooldownHours, int earlyCost, string descriptionKey)
        {
            this.ThingDef = DefDatabase<ThingDef>.GetNamed(shipThingName, false);
            this.TraderKindDef = DefDatabase<TraderKindDef>.GetNamed(traderDefName, false);
            if (this.TraderKindDef == null)
            {
                Log.Error($"无法加载 TraderKindDef: {traderDefName}");
            }
            this.Icon = ContentFinder<Texture2D>.Get(iconPath, true);
            if (this.Icon == null)
            {
                Log.Warning($"无法加载图标: {iconPath}");
                this.Icon = BaseContent.BadTex;
            }
            this.BackgroundColor = bgColor;
            this.GetCurrentCooldownHours = getCooldown;
            this.ResetCooldown = resetCooldown;
            this.CooldownDurationHours = cooldownHours;
            this.EarlyCallCost = earlyCost;
            this.DescriptionKey = descriptionKey;
        }
    }
}
    