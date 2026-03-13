using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TOT_DLL_test
{
    public class Verb_LaserInstantFunnel : Verb_Laser_Instant
    {
        private CMC_Drone Drone
        {
            get
            {
                return this.caster as CMC_Drone;
            }
        }
        public override float StartingPosOffset_x
        {
            get
            {
                if(this.Drone != null)
                {
                    return Drone.flyingDrawPos.x - Drone.DrawPos.x;
                }
                return base.StartingPosOffset_x;
            }
        }
        public override float StartingPosOffset_y
        {
            get
            {
                if (this.Drone != null)
                {
                    return Drone.flyingDrawPos.z - Drone.DrawPos.z;
                }
                return base.StartingPosOffset_y;
            }
        }
    }
}
