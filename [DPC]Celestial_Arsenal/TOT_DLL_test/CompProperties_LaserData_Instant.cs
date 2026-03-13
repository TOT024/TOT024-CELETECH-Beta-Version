using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TOT_DLL_test
{
    public class CompProperties_LaserData_Instant : CompProperties
    {
        public CompProperties_LaserData_Instant()
        {
            this.compClass = typeof(Comp_LaserData_Instant);
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
            string GetApString(float ap)
            {
                return (ap <= 0f) ? "TOT_Laser_IgnoresArmor".Translate().ToString() : ap.ToStringPercent();
            }
            float weaponRange = (req.Def is ThingDef weaponDef && !weaponDef.Verbs.NullOrEmpty())
                ? weaponDef.Verbs[0].range
                : 0f;
            yield return MakeStat("TOT_Laser_Range",
                weaponRange.ToString("0.0"),
                "TOT_Laser_RangeDesc".Translate(),
                4020);
            yield return MakeStat("TOT_Laser_Damage",
                this.DamageNum.ToString(),
                "TOT_Laser_DamageDesc".Translate(this.DamageDef.label),
                4000);
            yield return MakeStat("TOT_Laser_AP",
                GetApString(this.DamageArmorPenetration),
                "TOT_Laser_APDesc".Translate(),
                3990);
            if (this.IfSecondDamage)
            {
                yield return MakeStat("TOT_Laser_SecDamage",
                    "TOT_Laser_SecDamageValue".Translate(this.DamageNum_B, this.DamageDef_B.label),
                    "TOT_Laser_SecDamageDesc".Translate(),
                    3980);

                yield return MakeStat("TOT_Laser_SecAP",
                    GetApString(this.DamageArmorPenetration_B),
                    "TOT_Laser_SecAPDesc".Translate(),
                    3970);
            }
            if (this.IfCanScatter)
            {
                yield return MakeStat("TOT_Laser_ScatterEffect",
                    "Yes".Translate(),
                    "TOT_Laser_ScatterEffectDesc".Translate(),
                    3960);

                yield return MakeStat("TOT_Laser_ScatterCount",
                    this.ScatterNum.ToString(),
                    "TOT_Laser_ScatterCountDesc".Translate(),
                    3950);

                yield return MakeStat("TOT_Laser_ScatterExpDamage",
                    "TOT_Laser_ScatterExpDamageValue".Translate(this.ScatterExplosionDamage, this.ScatterExplosionDef.label),
                    "TOT_Laser_ScatterExpDamageDesc".Translate(),
                    3940);

                yield return MakeStat("TOT_Laser_ScatterExpRadius",
                    this.ScatterExplosionRadius.ToString("0.0"),
                    "TOT_Laser_ScatterExpRadiusDesc".Translate(),
                    3930);
            }
            if (this.IfCanDivide)
            {
                yield return MakeStat("TOT_Laser_DivideEffect",
                    "Yes".Translate(),
                    "TOT_Laser_DivideEffectDesc".Translate(),
                    3920);

                yield return MakeStat("TOT_Laser_DivideRadius",
                    this.DivideRadius.ToString(),
                    "TOT_Laser_DivideRadiusDesc".Translate(),
                    3910);
            }
        }
        public FleckDef LaserLine_FleckDef;
        public FleckDef LaserLine_FleckDef2;
        public FleckDef MuzzleGlow;
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
        public float turretyoffset = 0f;
        public bool useyoffset = false;
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
        public float ScatterRadius = 1f;
        public DamageDef ScatterExplosionDef = DamageDefOf.Bomb;
        public int ScatterExplosionDamage = 1;
        public float ScatterExplosionRadius = 1f;
        public float ScatterExplosionArmorPenetration = 1f;
        public int ScatterTickMax = 1;
        public FleckDef LaserFleck_ScatterLaser;
        public bool IfCanDivide = false;
        public int DivideRadius = 1;
        public bool RandomRGB = false;
    }
}