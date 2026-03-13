using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace TOT_DLL_test
{
    public class Verb_Laser_Instant : Verb
    {
        protected override int ShotsPerBurst
        {
            get
            {
                return this.verbProps.burstShotCount;
            }
        }
        public Vector3 TargetPosition_Vector3
        {
            get
            {
                return base.CurrentTarget.CenterVector3;
            }
        }
        public virtual float StartingPosOffset_x
        {
            get
            {
                return 0;
            }
        }
        public virtual float StartingPosOffset_y
        {
            get
            {
                return 0;
            }
        }
        protected override bool TryCastShot()
        {
            bool flag = this.currentTarget.HasThing && this.currentTarget.Thing.Map != this.caster.Map;
            bool result;
            if (flag)
            {
                result = false;
            }
            else
            {
                ShootLine shootLine;
                bool flag2 = base.TryFindShootLineFromTo(this.caster.Position, this.currentTarget, out shootLine);
                bool flag3 = this.verbProps.stopBurstWithoutLos && !flag2;
                if (flag3)
                {
                    result = false;
                }
                else
                {
                    bool flag4 = base.EquipmentSource != null;
                    if (flag4)
                    {
                        CompChangeableProjectile comp = base.EquipmentSource.GetComp<CompChangeableProjectile>();
                        bool flag5 = comp != null;
                        if (flag5)
                        {
                            comp.Notify_ProjectileLaunched();
                        }
                        CompApparelReloadable comp2 = base.EquipmentSource.GetComp<CompApparelReloadable>();
                        bool flag6 = comp2 != null;
                        if (flag6)
                        {
                            comp2.UsedOnce();
                        }
                    }
                    for (int i = 0; i < base.EquipmentSource.AllComps.Count; i++)
                    {
                        bool flag7 = base.EquipmentSource.AllComps[i] is Comp_LaserData_Instant;
                        if (flag7)
                        {
                            Comp_LaserData_Instant comp_LaserData_Instant = base.EquipmentSource.AllComps[i] as Comp_LaserData_Instant;
                            comp_LaserData_Instant.TakeDamageToTarget(base.CurrentTarget.Thing, this.Caster, this, StartingPosOffset_x, StartingPosOffset_y);
                        }
                    }
                    result = true;
                }
            }
            return result;
        }
    }
}
