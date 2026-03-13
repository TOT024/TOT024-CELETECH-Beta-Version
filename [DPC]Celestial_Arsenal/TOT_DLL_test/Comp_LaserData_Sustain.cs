using System;
using TOT_DLL_test;
using Verse;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse.Sound;

namespace TOT_DLL_test
{
    public class CompProperties_LaserData_Sustain : CompProperties
    {
        public CompProperties_LaserData_Sustain()
        {
            this.compClass = typeof(Comp_LaserData_Sustain);
        }
        public override IEnumerable<StatDrawEntry> SpecialDisplayStats(StatRequest req)
        {
            foreach (var stat in base.SpecialDisplayStats(req))
            {
                yield return stat;
            }
            StatDrawEntry MakeStat(string labelKey, string value, string desc, int priority)
            {
                return new StatDrawEntry(
                    StatCategoryDefOf.Weapon,
                    labelKey.Translate(),
                    value,
                    desc,
                    priority
                );
            }
            string GetApString(float ap) =>
                (ap <= 0f) ? "TOT_Laser_IgnoresArmor".Translate().ToString() : ap.ToStringPercent();
            int damageTickInterval = 20;
            int weaponRange = 0;
            if (req.Def is ThingDef weaponDef && !weaponDef.Verbs.NullOrEmpty())
            {
                damageTickInterval = weaponDef.Verbs[0].ticksBetweenBurstShots;
                weaponRange = (int)weaponDef.Verbs[0].range;
            }
            if (damageTickInterval <= 0) damageTickInterval = 1;
            float attacksPerSecond = 60f / (float)damageTickInterval;
            float dps = (float)this.DamageNum * attacksPerSecond;
            float damageIntervalInSeconds = (float)damageTickInterval / 60f;
            yield return MakeStat("TOT_Laser_Range",
                weaponRange.ToString("0"),
                "TOT_Laser_RangeDesc".Translate(),
                4020);
            yield return MakeStat("TOT_Sustain_DPSLabel",
                dps.ToString("0.0"),
                "TOT_Sustain_DPSDesc".Translate(this.DamageNum, damageTickInterval),
                4010);
            yield return MakeStat("TOT_Sustain_DamageLabel",
                this.DamageNum.ToString(),
                "TOT_Sustain_DamageDesc".Translate(),
                4000);
            yield return MakeStat("TOT_Sustain_APLabel",
                GetApString(this.DamageArmorPenetration),
                "TOT_Sustain_APDesc".Translate(),
                3990);
            yield return MakeStat("TOT_Sustain_IntervalLabel",
                damageIntervalInSeconds.ToString("0.00") + " s",
                "TOT_Sustain_IntervalDesc".Translate(damageTickInterval, damageIntervalInSeconds.ToString("0.00")),
                3985);
            if (this.IfSecondDamage)
            {
                yield return MakeStat("TOT_Sustain_SecDamageLabel",
                    "TOT_Sustain_ValueWithLabel".Translate(this.DamageNum_B, this.DamageDef_B.label),
                    "TOT_Sustain_SecDamageDesc".Translate(),
                    3980);

                yield return MakeStat("TOT_Sustain_SecAPLabel",
                    GetApString(this.DamageArmorPenetration_B),
                    "TOT_Sustain_SecAPDesc".Translate(),
                    3970);
            }
            if (this.IfCanScatter)
            {
                yield return MakeStat("TOT_Sustain_ScatterLabel",
                    "TOT_Sustain_Yes".Translate(),
                    "TOT_Sustain_ScatterDesc".Translate(),
                    3960);

                yield return MakeStat("TOT_Sustain_ScatterCountLabel",
                    this.ScatterNum.ToString(),
                    "TOT_Sustain_ScatterCountDesc".Translate(),
                    3950);

                yield return MakeStat("TOT_Sustain_ScatterExplosionLabel",
                    "TOT_Sustain_ValueWithLabel".Translate(this.ScatterExplosionDamage, this.ScatterExplosionDef.label),
                    "TOT_Sustain_ScatterExplosionDesc".Translate(),
                    3940);

                yield return MakeStat("TOT_Sustain_ScatterRadiusLabel",
                    this.ScatterExplosionRadius.ToString("0.0"),
                    "TOT_Sustain_ScatterRadiusDesc".Translate(),
                    3930);
            }
        }
        public ThingDef LaserLine_MoteDef;
        public FleckDef LaserFleck_End;
        public float LaserFleck_End_Scale_Base = 1f;
        public FleckDef LaserFleck_Spark;
        public float LaserFleck_Spark_Scale_Base = 1f;
        public float LaserFleck_Spark_Scale_Deviation = 0f;
        public int LaserFleck_Spark_Num = 1;
        public float LaserFleck_Spark_Spawn_Chance = 1f;
        public int Color_Red = 255;
        public int Color_Green = 255;
        public int Color_Blue = 255;
        public float Color_Alpha = 0.5f;
        public float StartPositionOffset_Range = 0f;
        public SoundDef SoundDef;
        public DamageDef DamageDef = DamageDefOf.Cut;
        public int DamageNum = 1;
        public float DamageArmorPenetration = 0f;
        public bool IfSecondDamage = false;
        public DamageDef DamageDef_B = DamageDefOf.Cut;
        public int DamageNum_B = 1;
        public float DamageArmorPenetration_B = 0f;
        public bool IfCanScatter = false;
        public int ScatterNum = 1;
        public int ScatterRadius = 1;
        public DamageDef ScatterExplosionDef = DamageDefOf.Bomb;
        public int ScatterExplosionDamage = 1;
        public float ScatterExplosionRadius = 1f;
        public float ScatterExplosionArmorPenetration = 1f;
        public int ScatterTickMax = 1;
        public FleckDef LaserFleck_ScatterLaser;
        public ThingDef LaserLine_MoteDef_Core = null;
        public bool ifPeriodicEffect = true;
        public FleckDef periodicEffectFleck = null;
        public float periodicEffectChance = 0.57f;
        public float periodicEffectScaleMin = 2.5f;
        public float periodicEffectScaleMax = 3.0f;
        public bool ifPeriodicLine = true;
        public FleckDef periodicLineFleck = null;
        public float periodicLineChance = 0.33f;
        public float DefaultRetargetRadius = 5f;
        public int DefaultRetargetTransitionShots = 5;
    }
    public class Comp_LaserData_Sustain : ThingComp
    {
        private float qualityNum;
        public float QualityNum
        {
            get
            {
                float result;
                try
                {
                    CompQuality compQuality = this.parent.TryGetComp<CompQuality>();
                    switch (compQuality.Quality)
                    {
                        case QualityCategory.Awful:
                            this.qualityNum = 0.9f;
                            break;
                        case QualityCategory.Poor:
                        case QualityCategory.Normal:
                        case QualityCategory.Good:
                        case QualityCategory.Excellent:
                            this.qualityNum = 1f;
                            break;
                        case QualityCategory.Masterwork:
                            this.qualityNum = 1.25f;
                            break;
                        case QualityCategory.Legendary:
                            this.qualityNum = 1.5f;
                            break;
                    }
                    result = this.qualityNum;
                }
                catch
                {
                    result = 1f;
                }
                return result;
            }
        }
        public CompProperties_LaserData_Sustain Props
        {
            get
            {
                return (CompProperties_LaserData_Sustain)this.props;
            }
        }
    }
}