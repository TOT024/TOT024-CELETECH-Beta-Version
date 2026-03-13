using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace TOT_DLL_test
{
    public class CompProperties_FloatGunRework : CompProperties
    {
        public CompProperties_FloatGunRework()
        {
            this.compClass = typeof(Comp_FloatingGunRework);
        }
        public ThingDef turretDef;
        public GraphicData FloatingGunGraphicData;
        public int BatteryLifeTick = 7200;
        public int BatteryRecoverPerSec = 180;
        public SimpleColor RadiusColor = SimpleColor.White;
        public string saveKeysPrefix;
        public float ChargingSpeedMutiplier = 1;
    }
}
