using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace TOT_DLL_test
{
    public class ModExtension_Splitedbullet : DefModExtension
    { 
        public ThingDef BulletDef;
        public float SplitTime = 0.1f;
        public int SplitAmount = 6;
        public bool Homing = false;
    }
}
