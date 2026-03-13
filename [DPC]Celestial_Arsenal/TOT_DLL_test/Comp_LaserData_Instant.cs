using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TOT_DLL_test
{
    public class Comp_LaserData_Instant : ThingComp
    {
        public CompProperties_LaserData_Instant Props
        {
            get
            {
                return (CompProperties_LaserData_Instant)this.props;
            }
        }
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
        public void TakeDamageToTarget(LocalTargetInfo TargetPlace, Thing Caster, Verb Verb, float Offset_x, float Offset_y)
        {
            Thing TargetThing;
            Vector3 drawPos2;
            if (TargetPlace.HasThing)
            {
                TargetThing = TargetPlace.Thing;
                drawPos2 = TargetThing.DrawPos;
            }
            else
            {
                TargetThing = null;
                drawPos2 = TargetPlace.CenterVector3;
            }
            Map map = Caster.Map;
            Vector3 drawPos = Caster.DrawPos;
            
            IntVec3 intVec = drawPos2.ToIntVec3();
            DamageDef damageDef = this.Props.DamageDef;
            int damageNum = (int)(this.Props.DamageNum * DMGmp * QualityNum);
            float damageArmorPenetration = this.Props.DamageArmorPenetration;
            if (TargetThing != null && damageDef != null)
            {
                float angleFlat = (TargetPlace.CenterVector3.ToIntVec3() - Caster.Position).AngleFlat;
                BattleLogEntry_RangedImpact log = new BattleLogEntry_RangedImpact(Caster, TargetThing, TargetThing, Verb.EquipmentSource.def, null, null);
                DamageInfo dinfo = new DamageInfo(damageDef, (float)damageNum, damageArmorPenetration, angleFlat, Caster, null, Verb.EquipmentSource.def, DamageInfo.SourceCategory.ThingOrUnknown, TargetThing, true, true);
                TargetThing.TakeDamage(dinfo).AssociateWithLog(log);
                bool ifSecondDamage = this.Props.IfSecondDamage;
                if (ifSecondDamage)
                {
                    DamageDef damageDef_B = this.Props.DamageDef_B;
                    int damageNum_B = this.Props.DamageNum_B;
                    float damageArmorPenetration_B = this.Props.DamageArmorPenetration_B;
                    DamageInfo dinfo2 = new DamageInfo(damageDef_B, (float)damageNum_B, damageArmorPenetration_B, angleFlat, Caster, null, Verb.EquipmentSource.def, DamageInfo.SourceCategory.ThingOrUnknown, TargetThing, true, true);
                    TargetThing.TakeDamage(dinfo2).AssociateWithLog(log);
                }
                bool ifCanScatter = this.Props.IfCanScatter;
                if (ifCanScatter)
                {
                    for (int i = 0; i < this.Props.ScatterNum; i++)
                    {
                        CellRect cellRect = CellRect.CenteredOn(intVec, (int)(this.Props.ScatterRadius));
                        cellRect.ClipInsideMap(map);
                        IntVec3 randomCell = cellRect.RandomCell;
                        GenExplosion.DoExplosion(randomCell, map, this.Props.ScatterExplosionRadius, this.Props.ScatterExplosionDef, Caster, this.Props.ScatterExplosionDamage, this.Props.ScatterExplosionArmorPenetration, null, Verb.EquipmentSource.def, null, null, null, 0f, 0, null, null, 255, false, null, 0f, 0, 0f, false, null, null, null, true, 1f, 0f, true, null, 0f);
                        FleckMaker.ConnectingLine(drawPos2, randomCell.ToVector3Shifted(), this.Props.LaserFleck_ScatterLaser, map, 1f);
                    }
                }
                bool flag3 = this.Props.LaserLine_FleckDef != null;
                bool flag3b = this.Props.LaserLine_FleckDef2 != null;
                if (flag3 && flag3b)
                {
                    float r, g, b;
                    if(!this.Props.RandomRGB)
                    {
                        r = (float)this.Props.Color_Red / 255f;
                        g = (float)this.Props.Color_Green / 255f;
                        b = (float)this.Props.Color_Blue / 255f;
                    }
                    else
                    {
                        r = (float)Rand.RangeInclusive(1, 255) / 255f;
                        g = (float)Rand.RangeInclusive(1, 255) / 255f;
                        b = (float)Rand.RangeInclusive(1, 255) / 255f;
                    }
                    float color_Alpha = this.Props.Color_Alpha;
                    Vector3 StartingPos, vector;
                    float y_offsetforturret = this.Props.turretyoffset;
                    if (this.Props.useyoffset || Offset_x != 0 || Offset_y !=0)
                    {
                        StartingPos = Caster.DrawPos;
                        StartingPos.z += y_offsetforturret;
                        StartingPos.z += Offset_y;
                        StartingPos.x += Offset_x;
                    }
                    else
                    {
                        float angle = (Caster.DrawPos - TargetThing.DrawPos).AngleFlat();
                        StartingPos = MYDE_ModFront.GetVector3_By_AngleFlat(Caster.DrawPos, this.Props.StartPositionOffset_Range, angle);
                    }
                    vector = TargetThing.DrawPos - StartingPos;
                    float x = vector.MagnitudeHorizontal();
                    FleckCreationData dataStatic, dataStatic2, dataStatic3, dataStatic4, dataStatic5, dataStatic6;
                    if (Rand.RangeInclusive(0, 100) <= 85)
                    {
                        dataStatic = FleckMaker.GetDataStatic(StartingPos + vector * 0.5f, map, this.Props.LaserLine_FleckDef, 1f);
                        dataStatic2 = FleckMaker.GetDataStatic(StartingPos + vector * 0.5f, map, this.Props.LaserLine_FleckDef, 1f);
                    }
                    else
                    {
                        dataStatic = FleckMaker.GetDataStatic(StartingPos + vector * 0.5f, map, this.Props.LaserLine_FleckDef2, 1f);
                        dataStatic2 = FleckMaker.GetDataStatic(StartingPos + vector * 0.5f, map, this.Props.LaserLine_FleckDef2, 1f);
                    }
                    dataStatic3 = FleckMaker.GetDataStatic(TargetThing.DrawPos, map, this.Props.MuzzleGlow, 1f);
                    dataStatic3.exactScale = new Vector3?(new Vector3(1.53f, 1.53f, 1.53f));
                    dataStatic3.rotation = Mathf.Atan2(0f - vector.z, vector.x) * 57.29578f;
                    dataStatic3.instanceColor = new Color?(new Color(Mathf.Max(r * 1.5f, 1f), Mathf.Max(g * 1.5f, 1f), Mathf.Max(b * 1.5f, 1f), 1f));
                    map.flecks.CreateFleck(dataStatic3);

                    dataStatic4 = FleckMaker.GetDataStatic(TargetThing.DrawPos, map, this.Props.MuzzleGlow, 1f);
                    dataStatic4.exactScale = new Vector3?(new Vector3(3f, 3f, 3f));
                    dataStatic4.rotation = Mathf.Atan2(0f - vector.z, vector.x) * 57.29578f;
                    dataStatic4.instanceColor = new Color?(new Color(r, g, b, 1f));
                    map.flecks.CreateFleck(dataStatic4);

                    dataStatic5 = FleckMaker.GetDataStatic(StartingPos, map, this.Props.MuzzleGlow, 1f);
                    dataStatic5.exactScale = new Vector3?(new Vector3(1.33f, 1.33f, 1.33f));
                    dataStatic5.rotation = Mathf.Atan2(0f - vector.z, vector.x) * 57.29578f;
                    dataStatic5.instanceColor = new Color?(new Color(Mathf.Max(r * 1.5f, 1f), Mathf.Max(g * 1.5f, 1f), Mathf.Max(b * 1.5f, 1f), 1f));
                    map.flecks.CreateFleck(dataStatic5);

                    dataStatic6 = FleckMaker.GetDataStatic(StartingPos, map, this.Props.MuzzleGlow, 1f);
                    dataStatic6.exactScale = new Vector3?(new Vector3(2f, 2f, 2f));
                    dataStatic6.rotation = Mathf.Atan2(0f - vector.z, vector.x) * 57.29578f;
                    dataStatic6.instanceColor = new Color?(new Color(r, g, b, 1f));
                    map.flecks.CreateFleck(dataStatic6);

                    FloatRange randomwidth = new FloatRange(0.4f, 0.8f);
                    float width = randomwidth.RandomInRange;
                    dataStatic.exactScale = new Vector3?(new Vector3(x, 1f, width * 1.5f));
                    dataStatic.rotation = Mathf.Atan2(0f - vector.z, vector.x) * 57.29578f;
                    dataStatic.instanceColor = new Color?(new Color(r, g, b, color_Alpha * 0.3f));
                    map.flecks.CreateFleck(dataStatic);

                    dataStatic2.exactScale = new Vector3?(new Vector3(x, 1f, width * 0.2f));
                    dataStatic2.rotation = Mathf.Atan2(0f - vector.z, vector.x) * 57.29578f;
                    dataStatic2.instanceColor = new Color?(new Color(Mathf.Max(r * 1.1f, 1f), Mathf.Max(g * 1.1f, 1f), Mathf.Max(b * 1.1f, 1f), 0.5f));
                    map.flecks.CreateFleck(dataStatic2);
                }
                bool flag4 = this.Props.LaserFleck_Spark != null && Rand.Chance(this.Props.LaserFleck_Spark_Spawn_Chance);
                if (flag4)
                {
                    float num = (drawPos2 - drawPos).AngleFlat();
                    for (int j = 0; j < this.Props.LaserFleck_Spark_Num; j++)
                    {
                        float scale = this.Props.LaserFleck_Spark_Scale_Base + Rand.Range(-this.Props.LaserFleck_Spark_Scale_Deviation, this.Props.LaserFleck_Spark_Scale_Deviation);
                        FleckCreationData dataStatic2 = FleckMaker.GetDataStatic(drawPos2, map, this.Props.LaserFleck_Spark, scale);
                        float num2 = num + Rand.Range(-30f, 30f);
                        bool flag5 = num2 > 180f;
                        if (flag5)
                        {
                            num2 = num2 - 180f + -180f;
                        }
                        bool flag6 = num2 < -180f;
                        if (flag6)
                        {
                            num2 = num2 + 180f + 180f;
                        }
                        dataStatic2.velocityAngle = num2;
                        dataStatic2.velocitySpeed = Rand.Range(5f, 10f);
                        map.flecks.CreateFleck(dataStatic2);
                    }
                }
                bool flag7 = this.Props.SoundDef != null;
                if (flag7)
                {
                    this.Props.SoundDef.PlayOneShot(new TargetInfo(Caster.Position, Caster.MapHeld, false));
                }
            }
            else
            {
                bool ifCanScatter = this.Props.IfCanScatter;
                if (ifCanScatter)
                {
                    for (int i = 0; i < this.Props.ScatterNum; i++)
                    {
                        CellRect cellRect = CellRect.CenteredOn(intVec, (int)(this.Props.ScatterRadius));
                        cellRect.ClipInsideMap(map);
                        IntVec3 randomCell = cellRect.RandomCell;
                        GenExplosion.DoExplosion(randomCell, map, this.Props.ScatterExplosionRadius, this.Props.ScatterExplosionDef, Caster, this.Props.ScatterExplosionDamage, this.Props.ScatterExplosionArmorPenetration, null, Verb.EquipmentSource.def, null, null, null, 0f, 0, null, null, 255, false, null, 0f, 0, 0f, false, null, null, null, true, 1f, 0f, true, null, 0f);
                        FleckMaker.ConnectingLine(drawPos2, randomCell.ToVector3Shifted(), this.Props.LaserFleck_ScatterLaser, map, 1f);
                    }
                }
                bool flag3 = this.Props.LaserLine_FleckDef != null;
                bool flag3b = this.Props.LaserLine_FleckDef2 != null;
                if (flag3 && flag3b)
                {
                    float r, g, b;
                    if (!this.Props.RandomRGB)
                    {
                        r = (float)this.Props.Color_Red / 255f;
                        g = (float)this.Props.Color_Green / 255f;
                        b = (float)this.Props.Color_Blue / 255f;
                    }
                    else
                    {
                        r = (float)Rand.RangeInclusive(1, 255) / 255f;
                        g = (float)Rand.RangeInclusive(1, 255) / 255f;
                        b = (float)Rand.RangeInclusive(1, 255) / 255f;
                    }
                    float color_Alpha = this.Props.Color_Alpha;
                    Vector3 StartingPos, vector;
                    float y_offsetforturret = this.Props.turretyoffset;
                    if (this.Props.useyoffset)
                    {
                        StartingPos = Caster.DrawPos;
                        StartingPos.z += y_offsetforturret;
                    }
                    else
                    {
                        float angle = (Caster.DrawPos - TargetPlace.CenterVector3).AngleFlat();
                        StartingPos = MYDE_ModFront.GetVector3_By_AngleFlat(Caster.DrawPos, this.Props.StartPositionOffset_Range, angle);
                    }

                    vector = TargetPlace.CenterVector3 - StartingPos;
                    float x = vector.MagnitudeHorizontal();
                    FleckCreationData dataStatic, dataStatic2, dataStatic3, dataStatic4, dataStatic5, dataStatic6;
                    if (Rand.RangeInclusive(0, 100) <= 85)
                    {
                        dataStatic = FleckMaker.GetDataStatic(StartingPos + vector * 0.5f, map, this.Props.LaserLine_FleckDef, 1f);
                        dataStatic2 = FleckMaker.GetDataStatic(StartingPos + vector * 0.5f, map, this.Props.LaserLine_FleckDef, 1f);
                    }
                    else
                    {
                        dataStatic = FleckMaker.GetDataStatic(StartingPos + vector * 0.5f, map, this.Props.LaserLine_FleckDef2, 1f);
                        dataStatic2 = FleckMaker.GetDataStatic(StartingPos + vector * 0.5f, map, this.Props.LaserLine_FleckDef2, 1f);
                    }
                    dataStatic3 = FleckMaker.GetDataStatic(TargetPlace.CenterVector3, map, this.Props.MuzzleGlow, 1f);
                    dataStatic3.exactScale = new Vector3?(new Vector3(1.53f, 1.53f, 1.53f));
                    dataStatic3.rotation = Mathf.Atan2(0f - vector.z, vector.x) * 57.29578f;
                    dataStatic3.instanceColor = new Color?(new Color(Mathf.Max(r * 1.5f, 1f), Mathf.Max(g * 1.5f, 1f), Mathf.Max(b * 1.5f, 1f), 1f));
                    map.flecks.CreateFleck(dataStatic3);

                    dataStatic4 = FleckMaker.GetDataStatic(TargetPlace.CenterVector3, map, this.Props.MuzzleGlow, 1f);
                    dataStatic4.exactScale = new Vector3?(new Vector3(3f, 3f, 3f));
                    dataStatic4.rotation = Mathf.Atan2(0f - vector.z, vector.x) * 57.29578f;
                    dataStatic4.instanceColor = new Color?(new Color(r, g, b, 1f));
                    map.flecks.CreateFleck(dataStatic4);

                    dataStatic5 = FleckMaker.GetDataStatic(StartingPos, map, this.Props.MuzzleGlow, 1f);
                    dataStatic5.exactScale = new Vector3?(new Vector3(1.33f, 1.33f, 1.33f));
                    dataStatic5.rotation = Mathf.Atan2(0f - vector.z, vector.x) * 57.29578f;
                    dataStatic5.instanceColor = new Color?(new Color(Mathf.Max(r * 1.5f, 1f), Mathf.Max(g * 1.5f, 1f), Mathf.Max(b * 1.5f, 1f), 1f));
                    map.flecks.CreateFleck(dataStatic5);

                    dataStatic6 = FleckMaker.GetDataStatic(StartingPos, map, this.Props.MuzzleGlow, 1f);
                    dataStatic6.exactScale = new Vector3?(new Vector3(2f, 2f, 2f));
                    dataStatic6.rotation = Mathf.Atan2(0f - vector.z, vector.x) * 57.29578f;
                    dataStatic6.instanceColor = new Color?(new Color(r, g, b, 1f));
                    map.flecks.CreateFleck(dataStatic6);

                    FloatRange randomwidth = new FloatRange(0.4f, 0.8f);
                    float width = randomwidth.RandomInRange;
                    dataStatic.exactScale = new Vector3?(new Vector3(x, 1f, width * 1.5f));
                    dataStatic.rotation = Mathf.Atan2(0f - vector.z, vector.x) * 57.29578f;
                    dataStatic.instanceColor = new Color?(new Color(r, g, b, color_Alpha * 0.3f));
                    map.flecks.CreateFleck(dataStatic);

                    dataStatic2.exactScale = new Vector3?(new Vector3(x, 1f, width * 0.2f));
                    dataStatic2.rotation = Mathf.Atan2(0f - vector.z, vector.x) * 57.29578f;
                    dataStatic2.instanceColor = new Color?(new Color(Mathf.Max(r * 1.1f, 1f), Mathf.Max(g * 1.1f, 1f), Mathf.Max(b * 1.1f, 1f), 0.5f));
                    map.flecks.CreateFleck(dataStatic2);
                }
                bool flag4 = this.Props.LaserFleck_Spark != null && Rand.Chance(this.Props.LaserFleck_Spark_Spawn_Chance);
                if (flag4)
                {
                    float num = (drawPos2 - drawPos).AngleFlat();
                    for (int j = 0; j < this.Props.LaserFleck_Spark_Num; j++)
                    {
                        float scale = this.Props.LaserFleck_Spark_Scale_Base + Rand.Range(-this.Props.LaserFleck_Spark_Scale_Deviation, this.Props.LaserFleck_Spark_Scale_Deviation);
                        FleckCreationData dataStatic2 = FleckMaker.GetDataStatic(drawPos2, map, this.Props.LaserFleck_Spark, scale);
                        float num2 = num + Rand.Range(-30f, 30f);
                        bool flag5 = num2 > 180f;
                        if (flag5)
                        {
                            num2 = num2 - 180f + -180f;
                        }
                        bool flag6 = num2 < -180f;
                        if (flag6)
                        {
                            num2 = num2 + 180f + 180f;
                        }
                        dataStatic2.velocityAngle = num2;
                        dataStatic2.velocitySpeed = Rand.Range(5f, 10f);
                        map.flecks.CreateFleck(dataStatic2);
                        map.flecks.CreateFleck(dataStatic2);
                        map.flecks.CreateFleck(dataStatic2);
                    }
                }
                bool flag7 = this.Props.SoundDef != null;
                if (flag7)
                {
                    this.Props.SoundDef.PlayOneShot(new TargetInfo(Caster.Position, Caster.MapHeld, false));
                }
            }
        }
        public void TakeDamageToTarget(LocalTargetInfo TargetPlace, Vector3 SecondPos, Thing Caster, Verb Verb)
        {
            Thing TargetThing;
            Vector3 drawPos2;
            if (TargetPlace.HasThing)
            {
                TargetThing = TargetPlace.Thing;
                drawPos2 = TargetThing.DrawPos;
            }
            else
            {
                TargetThing = null;
                drawPos2 = TargetPlace.CenterVector3;
            }
            Map map = Caster.Map;
            Vector3 drawPos = SecondPos;

            IntVec3 intVec = drawPos2.ToIntVec3();
            DamageDef damageDef = this.Props.DamageDef;
            int damageNum = (int)(this.Props.DamageNum * DMGmp * QualityNum);
            float damageArmorPenetration = this.Props.DamageArmorPenetration;
            if (TargetThing != null && damageDef != null)
            {
                float angleFlat = (TargetPlace.CenterVector3.ToIntVec3() - SecondPos.ToIntVec3()).AngleFlat;
                BattleLogEntry_RangedImpact log = new BattleLogEntry_RangedImpact(Caster, TargetThing, TargetThing, Verb.EquipmentSource.def, null, null);
                DamageInfo dinfo = new DamageInfo(damageDef, (float)damageNum, damageArmorPenetration, angleFlat, Caster, null, Verb.EquipmentSource.def, DamageInfo.SourceCategory.ThingOrUnknown, TargetThing, true, true);
                TargetThing.TakeDamage(dinfo).AssociateWithLog(log);
                bool ifSecondDamage = this.Props.IfSecondDamage;
                if (ifSecondDamage)
                {
                    DamageDef damageDef_B = this.Props.DamageDef_B;
                    int damageNum_B = this.Props.DamageNum_B;
                    float damageArmorPenetration_B = this.Props.DamageArmorPenetration_B;
                    DamageInfo dinfo2 = new DamageInfo(damageDef_B, (float)damageNum_B, damageArmorPenetration_B, angleFlat, Caster, null, Verb.EquipmentSource.def, DamageInfo.SourceCategory.ThingOrUnknown, TargetThing, true, true);
                    TargetThing.TakeDamage(dinfo2).AssociateWithLog(log);
                }
                bool ifCanScatter = this.Props.IfCanScatter;
                if (ifCanScatter)
                {
                    for (int i = 0; i < this.Props.ScatterNum; i++)
                    {
                        CellRect cellRect = CellRect.CenteredOn(intVec, (int)(this.Props.ScatterRadius));
                        cellRect.ClipInsideMap(map);
                        IntVec3 randomCell = cellRect.RandomCell;
                        GenExplosion.DoExplosion(randomCell, map, this.Props.ScatterExplosionRadius, this.Props.ScatterExplosionDef, Caster, this.Props.ScatterExplosionDamage, this.Props.ScatterExplosionArmorPenetration, null, Verb.EquipmentSource.def, null, null, null, 0f, 0, null, null, 255, false, null, 0f, 0, 0f, false, null, null, null, true, 1f, 0f, true, null, 0f);
                        FleckMaker.ConnectingLine(drawPos2, randomCell.ToVector3Shifted(), this.Props.LaserFleck_ScatterLaser, map, 1f);
                    }
                }
                bool flag3 = this.Props.LaserLine_FleckDef != null;
                bool flag3b = this.Props.LaserLine_FleckDef2 != null;
                if (flag3 && flag3b)
                {
                    float r, g, b;
                    if (!this.Props.RandomRGB)
                    {
                        r = (float)this.Props.Color_Red / 255f;
                        g = (float)this.Props.Color_Green / 255f;
                        b = (float)this.Props.Color_Blue / 255f;
                    }
                    else
                    {
                        r = (float)Rand.RangeInclusive(1, 255) / 255f;
                        g = (float)Rand.RangeInclusive(1, 255) / 255f;
                        b = (float)Rand.RangeInclusive(1, 255) / 255f;
                    }
                    float color_Alpha = this.Props.Color_Alpha;
                    Vector3 StartingPos, vector;
                    float y_offsetforturret = this.Props.turretyoffset;
                    if (this.Props.useyoffset)
                    {
                        StartingPos = SecondPos;
                        StartingPos.z += y_offsetforturret;
                    }
                    else
                    {
                        float angle = (SecondPos - TargetThing.DrawPos).AngleFlat();
                        StartingPos = MYDE_ModFront.GetVector3_By_AngleFlat(SecondPos, this.Props.StartPositionOffset_Range, angle);
                    }

                    vector = TargetThing.DrawPos - StartingPos;
                    float x = vector.MagnitudeHorizontal();
                    FleckCreationData dataStatic, dataStatic2, dataStatic3, dataStatic4, dataStatic5, dataStatic6;
                    if (Rand.RangeInclusive(0, 100) <= 85)
                    {
                        dataStatic = FleckMaker.GetDataStatic(StartingPos + vector * 0.5f, map, this.Props.LaserLine_FleckDef, 1f);
                        dataStatic2 = FleckMaker.GetDataStatic(StartingPos + vector * 0.5f, map, this.Props.LaserLine_FleckDef, 1f);
                    }
                    else
                    {
                        dataStatic = FleckMaker.GetDataStatic(StartingPos + vector * 0.5f, map, this.Props.LaserLine_FleckDef2, 1f);
                        dataStatic2 = FleckMaker.GetDataStatic(StartingPos + vector * 0.5f, map, this.Props.LaserLine_FleckDef2, 1f);
                    }
                    dataStatic3 = FleckMaker.GetDataStatic(TargetThing.DrawPos, map, this.Props.MuzzleGlow, 1f);
                    dataStatic3.exactScale = new Vector3?(new Vector3(1.53f, 1.53f, 1.53f));
                    dataStatic3.rotation = Mathf.Atan2(0f - vector.z, vector.x) * 57.29578f;
                    dataStatic3.instanceColor = new Color?(new Color(Mathf.Max(r * 1.5f, 1f), Mathf.Max(g * 1.5f, 1f), Mathf.Max(b * 1.5f, 1f), 1f));
                    map.flecks.CreateFleck(dataStatic3);

                    dataStatic4 = FleckMaker.GetDataStatic(TargetThing.DrawPos, map, this.Props.MuzzleGlow, 1f);
                    dataStatic4.exactScale = new Vector3?(new Vector3(3f, 3f, 3f));
                    dataStatic4.rotation = Mathf.Atan2(0f - vector.z, vector.x) * 57.29578f;
                    dataStatic4.instanceColor = new Color?(new Color(r, g, b, 1f));
                    map.flecks.CreateFleck(dataStatic4);

                    dataStatic5 = FleckMaker.GetDataStatic(StartingPos, map, this.Props.MuzzleGlow, 1f);
                    dataStatic5.exactScale = new Vector3?(new Vector3(1.33f, 1.33f, 1.33f));
                    dataStatic5.rotation = Mathf.Atan2(0f - vector.z, vector.x) * 57.29578f;
                    dataStatic5.instanceColor = new Color?(new Color(Mathf.Max(r * 1.5f, 1f), Mathf.Max(g * 1.5f, 1f), Mathf.Max(b * 1.5f, 1f), 1f));
                    map.flecks.CreateFleck(dataStatic5);

                    dataStatic6 = FleckMaker.GetDataStatic(StartingPos, map, this.Props.MuzzleGlow, 1f);
                    dataStatic6.exactScale = new Vector3?(new Vector3(2f, 2f, 2f));
                    dataStatic6.rotation = Mathf.Atan2(0f - vector.z, vector.x) * 57.29578f;
                    dataStatic6.instanceColor = new Color?(new Color(r, g, b, 1f));
                    map.flecks.CreateFleck(dataStatic6);

                    FloatRange randomwidth = new FloatRange(0.4f, 0.8f);
                    float width = randomwidth.RandomInRange;
                    dataStatic.exactScale = new Vector3?(new Vector3(x, 1f, width * 1.5f));
                    dataStatic.rotation = Mathf.Atan2(0f - vector.z, vector.x) * 57.29578f;
                    dataStatic.instanceColor = new Color?(new Color(r, g, b, color_Alpha * 0.3f));
                    map.flecks.CreateFleck(dataStatic);

                    dataStatic2.exactScale = new Vector3?(new Vector3(x, 1f, width * 0.2f));
                    dataStatic2.rotation = Mathf.Atan2(0f - vector.z, vector.x) * 57.29578f;
                    dataStatic2.instanceColor = new Color?(new Color(Mathf.Max(r * 1.1f, 1f), Mathf.Max(g * 1.1f, 1f), Mathf.Max(b * 1.1f, 1f), 0.5f));
                    map.flecks.CreateFleck(dataStatic2);
                }
                bool flag4 = this.Props.LaserFleck_Spark != null && Rand.Chance(this.Props.LaserFleck_Spark_Spawn_Chance);
                if (flag4)
                {
                    float num = (drawPos2 - drawPos).AngleFlat();
                    for (int j = 0; j < this.Props.LaserFleck_Spark_Num; j++)
                    {
                        float scale = this.Props.LaserFleck_Spark_Scale_Base + Rand.Range(-this.Props.LaserFleck_Spark_Scale_Deviation, this.Props.LaserFleck_Spark_Scale_Deviation);
                        FleckCreationData dataStatic2 = FleckMaker.GetDataStatic(drawPos2, map, this.Props.LaserFleck_Spark, scale);
                        float num2 = num + Rand.Range(-30f, 30f);
                        bool flag5 = num2 > 180f;
                        if (flag5)
                        {
                            num2 = num2 - 180f + -180f;
                        }
                        bool flag6 = num2 < -180f;
                        if (flag6)
                        {
                            num2 = num2 + 180f + 180f;
                        }
                        dataStatic2.velocityAngle = num2;
                        dataStatic2.velocitySpeed = Rand.Range(5f, 10f);
                        map.flecks.CreateFleck(dataStatic2);
                    }
                }
                bool flag7 = this.Props.SoundDef != null;
                if (flag7)
                {
                    this.Props.SoundDef.PlayOneShot(new TargetInfo(Caster.Position, Caster.MapHeld, false));
                }
            }
            else
            {
                bool ifCanScatter = this.Props.IfCanScatter;
                if (ifCanScatter)
                {
                    for (int i = 0; i < this.Props.ScatterNum; i++)
                    {
                        CellRect cellRect = CellRect.CenteredOn(intVec, (int)(this.Props.ScatterRadius));
                        cellRect.ClipInsideMap(map);
                        IntVec3 randomCell = cellRect.RandomCell;
                        GenExplosion.DoExplosion(randomCell, map, this.Props.ScatterExplosionRadius, this.Props.ScatterExplosionDef, Caster, this.Props.ScatterExplosionDamage, this.Props.ScatterExplosionArmorPenetration, null, Verb.EquipmentSource.def, null, null, null, 0f, 0, null, null, 255, false, null, 0f, 0, 0f, false, null, null, null, true, 1f, 0f, true, null, 0f);
                        FleckMaker.ConnectingLine(drawPos2, randomCell.ToVector3Shifted(), this.Props.LaserFleck_ScatterLaser, map, 1f);
                    }
                }
                bool flag3 = this.Props.LaserLine_FleckDef != null;
                bool flag3b = this.Props.LaserLine_FleckDef2 != null;
                if (flag3 && flag3b)
                {
                    float r, g, b;
                    if (!this.Props.RandomRGB)
                    {
                        r = (float)this.Props.Color_Red / 255f;
                        g = (float)this.Props.Color_Green / 255f;
                        b = (float)this.Props.Color_Blue / 255f;
                    }
                    else
                    {
                        r = (float)Rand.RangeInclusive(1, 255) / 255f;
                        g = (float)Rand.RangeInclusive(1, 255) / 255f;
                        b = (float)Rand.RangeInclusive(1, 255) / 255f;
                    }
                    float color_Alpha = this.Props.Color_Alpha;
                    Vector3 StartingPos, vector;
                    float y_offsetforturret = this.Props.turretyoffset;
                    if (this.Props.useyoffset)
                    {
                        StartingPos = SecondPos;
                        StartingPos.z += y_offsetforturret;
                    }
                    else
                    {
                        float angle = (SecondPos - TargetPlace.CenterVector3).AngleFlat();
                        StartingPos = MYDE_ModFront.GetVector3_By_AngleFlat(SecondPos, this.Props.StartPositionOffset_Range, angle);
                    }

                    vector = TargetPlace.CenterVector3 - StartingPos;
                    float x = vector.MagnitudeHorizontal();
                    FleckCreationData dataStatic, dataStatic2, dataStatic3, dataStatic4, dataStatic5, dataStatic6;
                    if (Rand.RangeInclusive(0, 100) <= 85)
                    {
                        dataStatic = FleckMaker.GetDataStatic(StartingPos + vector * 0.5f, map, this.Props.LaserLine_FleckDef, 1f);
                        dataStatic2 = FleckMaker.GetDataStatic(StartingPos + vector * 0.5f, map, this.Props.LaserLine_FleckDef, 1f);
                    }
                    else
                    {
                        dataStatic = FleckMaker.GetDataStatic(StartingPos + vector * 0.5f, map, this.Props.LaserLine_FleckDef2, 1f);
                        dataStatic2 = FleckMaker.GetDataStatic(StartingPos + vector * 0.5f, map, this.Props.LaserLine_FleckDef2, 1f);
                    }
                    dataStatic3 = FleckMaker.GetDataStatic(TargetPlace.CenterVector3, map, this.Props.MuzzleGlow, 1f);
                    dataStatic3.exactScale = new Vector3?(new Vector3(1.53f, 1.53f, 1.53f));
                    dataStatic3.rotation = Mathf.Atan2(0f - vector.z, vector.x) * 57.29578f;
                    dataStatic3.instanceColor = new Color?(new Color(Mathf.Max(r * 1.5f, 1f), Mathf.Max(g * 1.5f, 1f), Mathf.Max(b * 1.5f, 1f), 1f));
                    map.flecks.CreateFleck(dataStatic3);

                    dataStatic4 = FleckMaker.GetDataStatic(TargetPlace.CenterVector3, map, this.Props.MuzzleGlow, 1f);
                    dataStatic4.exactScale = new Vector3?(new Vector3(3f, 3f, 3f));
                    dataStatic4.rotation = Mathf.Atan2(0f - vector.z, vector.x) * 57.29578f;
                    dataStatic4.instanceColor = new Color?(new Color(r, g, b, 1f));
                    map.flecks.CreateFleck(dataStatic4);

                    dataStatic5 = FleckMaker.GetDataStatic(StartingPos, map, this.Props.MuzzleGlow, 1f);
                    dataStatic5.exactScale = new Vector3?(new Vector3(1.33f, 1.33f, 1.33f));
                    dataStatic5.rotation = Mathf.Atan2(0f - vector.z, vector.x) * 57.29578f;
                    dataStatic5.instanceColor = new Color?(new Color(Mathf.Max(r * 1.5f, 1f), Mathf.Max(g * 1.5f, 1f), Mathf.Max(b * 1.5f, 1f), 1f));
                    map.flecks.CreateFleck(dataStatic5);

                    dataStatic6 = FleckMaker.GetDataStatic(StartingPos, map, this.Props.MuzzleGlow, 1f);
                    dataStatic6.exactScale = new Vector3?(new Vector3(2f, 2f, 2f));
                    dataStatic6.rotation = Mathf.Atan2(0f - vector.z, vector.x) * 57.29578f;
                    dataStatic6.instanceColor = new Color?(new Color(r, g, b, 1f));
                    map.flecks.CreateFleck(dataStatic6);

                    FloatRange randomwidth = new FloatRange(0.4f, 0.8f);
                    float width = randomwidth.RandomInRange;
                    dataStatic.exactScale = new Vector3?(new Vector3(x, 1f, width * 1.5f));
                    dataStatic.rotation = Mathf.Atan2(0f - vector.z, vector.x) * 57.29578f;
                    dataStatic.instanceColor = new Color?(new Color(r, g, b, color_Alpha * 0.3f));
                    map.flecks.CreateFleck(dataStatic);

                    dataStatic2.exactScale = new Vector3?(new Vector3(x, 1f, width * 0.2f));
                    dataStatic2.rotation = Mathf.Atan2(0f - vector.z, vector.x) * 57.29578f;
                    dataStatic2.instanceColor = new Color?(new Color(Mathf.Max(r * 1.1f, 1f), Mathf.Max(g * 1.1f, 1f), Mathf.Max(b * 1.1f, 1f), 0.5f));
                    map.flecks.CreateFleck(dataStatic2);
                }
                bool flag4 = this.Props.LaserFleck_Spark != null && Rand.Chance(this.Props.LaserFleck_Spark_Spawn_Chance);
                if (flag4)
                {
                    float num = (drawPos2 - drawPos).AngleFlat();
                    for (int j = 0; j < this.Props.LaserFleck_Spark_Num; j++)
                    {
                        float scale = this.Props.LaserFleck_Spark_Scale_Base + Rand.Range(-this.Props.LaserFleck_Spark_Scale_Deviation, this.Props.LaserFleck_Spark_Scale_Deviation);
                        FleckCreationData dataStatic2 = FleckMaker.GetDataStatic(drawPos2, map, this.Props.LaserFleck_Spark, scale);
                        float num2 = num + Rand.Range(-30f, 30f);
                        bool flag5 = num2 > 180f;
                        if (flag5)
                        {
                            num2 = num2 - 180f + -180f;
                        }
                        bool flag6 = num2 < -180f;
                        if (flag6)
                        {
                            num2 = num2 + 180f + 180f;
                        }
                        dataStatic2.velocityAngle = num2;
                        dataStatic2.velocitySpeed = Rand.Range(5f, 10f);
                        map.flecks.CreateFleck(dataStatic2);
                        map.flecks.CreateFleck(dataStatic2);
                        map.flecks.CreateFleck(dataStatic2);
                    }
                }
                bool flag7 = this.Props.SoundDef != null;
                if (flag7)
                {
                    this.Props.SoundDef.PlayOneShot(new TargetInfo(Caster.Position, Caster.MapHeld, false));
                }
            }
        }
        public int DMGmp = 1;
        private float qualityNum;
    }
}