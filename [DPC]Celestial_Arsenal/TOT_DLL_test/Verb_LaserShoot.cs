using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace TOT_DLL_test
{
    public class Verb_LaserShoot : Verb_LaunchProjectile
    {
        public override void WarmupComplete()
        {
            base.WarmupComplete();
        }
        protected override bool TryCastShot()
        {
            bool Flag = base.TryCastShot();
            if (Flag)
            {
                if (base.EquipmentCompSource.parent.def.defName == "Gun_LaserSniper")
                {
                    int num = 0;
                    this.turrets.Clear();
                    this.turrets.Add((Building)this.caster);
                    int num2 = 0;
                    while ((float)num2 < 6)
                    {
                        this.cells.Clear();
                        this.cells = GenRadial.RadialCellsAround(this.turrets[num].Position, 10f, true).ToList<IntVec3>();
                        foreach (IntVec3 c in this.cells)
                        {
                            Building_Turret building_Turret = c.GetFirstBuilding(this.caster.Map) as Building_Turret;
                            bool flag0 = building_Turret != null && building_Turret.def.defName == this.caster.def.defName;
                            if (flag0)
                            {
                                bool flag2 = false;
                                Building_TurretGun building_TurretGun = c.GetFirstBuilding(this.caster.Map) as Building_TurretGun;
                                foreach (Building building in this.turrets)
                                {
                                    bool flag3 = building == building_TurretGun;
                                    if (flag3)
                                    {
                                        flag2 = true;
                                        break;
                                    }
                                }
                                bool flag4 = !flag2 & building_TurretGun.GetComp<CompPowerTrader>().PowerOn;
                                if (flag4)
                                {
                                    bool flag5 = building_TurretGun.CurrentTarget == null;
                                    if (flag5)
                                    {
                                        this.turrets.Add(building_TurretGun);
                                        num++;
                                        break;
                                    }
                                }
                            }
                        }
                        num2++;
                    }
                    int num3 = 0;
                    Comp_LaserData_Instant comp_LaserData_Instant = base.EquipmentSource.TryGetComp<Comp_LaserData_Instant>();
                    foreach (Building building2 in this.turrets)
                    {
                        bool flag13 = num3 + 1 < this.turrets.Count;
                        if (flag13)
                        {
                            float r = comp_LaserData_Instant.Props.Color_Red;
                            float g = comp_LaserData_Instant.Props.Color_Green;
                            float b = comp_LaserData_Instant.Props.Color_Blue;
                            r = r / 255f;
                            g = g / 255f;
                            b = b / 255f;
                            Vector3 zoffset = new Vector3(0f, 0f, 1.35f);
                            FleckCreationData dataStatic, dataStatic2, dataStatic3, dataStatic4, dataStatic5, dataStatic6;
                            Map map = this.caster.Map;
                            Vector3 vector = this.turrets[num3 + 1].DrawPos - this.turrets[num3].DrawPos;
                            float x = vector.MagnitudeHorizontal();
                            dataStatic = FleckMaker.GetDataStatic(this.turrets[num3].DrawPos + vector * 0.5f + zoffset, map, comp_LaserData_Instant.Props.LaserLine_FleckDef, 1f);
                            dataStatic2 = FleckMaker.GetDataStatic(this.turrets[num3].DrawPos + vector * 0.5f + zoffset, map, comp_LaserData_Instant.Props.LaserLine_FleckDef, 1f);
                            FloatRange randomwidth = new FloatRange(0.4f, 0.8f);
                            float width = randomwidth.RandomInRange;
                            dataStatic.exactScale = new Vector3?(new Vector3(x, 1f, width * 1.5f));
                            dataStatic.rotation = Mathf.Atan2(0f - vector.z, vector.x) * 57.29578f;
                            dataStatic.instanceColor = new Color?(new Color(r, g, b, 0.2f));
                            map.flecks.CreateFleck(dataStatic);

                            dataStatic2.exactScale = new Vector3?(new Vector3(x, 1f, width * 0.2f));
                            dataStatic2.rotation = Mathf.Atan2(0f - vector.z, vector.x) * 57.29578f;
                            dataStatic2.instanceColor = new Color?(new Color(Mathf.Max(r * 1.1f, 1f), Mathf.Max(g * 1.1f, 1f), Mathf.Max(b * 1.1f, 1f), 0.5f));
                            map.flecks.CreateFleck(dataStatic2);

                            dataStatic3 = FleckMaker.GetDataStatic(this.turrets[num3 + 1].DrawPos + zoffset, map, comp_LaserData_Instant.Props.MuzzleGlow, 1f);
                            dataStatic3.exactScale = new Vector3?(new Vector3(1.53f, 1.53f, 1.53f));
                            dataStatic3.rotation = Mathf.Atan2(0f - vector.z, vector.x) * 57.29578f;
                            dataStatic3.instanceColor = new Color?(new Color(Mathf.Max(r * 1.5f, 1f), Mathf.Max(g * 1.5f, 1f), Mathf.Max(b * 1.5f, 1f), 1f));
                            map.flecks.CreateFleck(dataStatic3);

                            dataStatic4 = FleckMaker.GetDataStatic(this.turrets[num3 + 1].DrawPos + zoffset, map, comp_LaserData_Instant.Props.MuzzleGlow, 1f);
                            dataStatic4.exactScale = new Vector3?(new Vector3(3f, 3f, 3f));
                            dataStatic4.rotation = Mathf.Atan2(0f - vector.z, vector.x) * 57.29578f;
                            dataStatic4.instanceColor = new Color?(new Color(r, g, b, 1f));
                            map.flecks.CreateFleck(dataStatic4);

                            dataStatic5 = FleckMaker.GetDataStatic(this.turrets[num3].DrawPos + zoffset, map, comp_LaserData_Instant.Props.MuzzleGlow, 1f);
                            dataStatic5.exactScale = new Vector3?(new Vector3(1.33f, 1.33f, 1.33f));
                            dataStatic5.rotation = Mathf.Atan2(0f - vector.z, vector.x) * 57.29578f;
                            dataStatic5.instanceColor = new Color?(new Color(Mathf.Max(r * 1.5f, 1f), Mathf.Max(g * 1.5f, 1f), Mathf.Max(b * 1.5f, 1f), 1f));
                            map.flecks.CreateFleck(dataStatic5);

                            dataStatic6 = FleckMaker.GetDataStatic(this.turrets[num3].DrawPos + zoffset, map, comp_LaserData_Instant.Props.MuzzleGlow, 1f);
                            dataStatic6.exactScale = new Vector3?(new Vector3(2f, 2f, 2f));
                            dataStatic6.rotation = Mathf.Atan2(0f - vector.z, vector.x) * 57.29578f;
                            dataStatic6.instanceColor = new Color?(new Color(r, g, b, 1f));
                            map.flecks.CreateFleck(dataStatic6);
                        }
                        num3++;
                    }
                    comp_LaserData_Instant.DMGmp = this.turrets.Count;
                }
            }
            return Flag;
        }
        protected List<Building> turrets = new List<Building>();
        protected List<IntVec3> cells = new List<IntVec3>();
    }
}
