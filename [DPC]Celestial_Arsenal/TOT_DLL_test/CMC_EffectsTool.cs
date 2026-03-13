using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using UnityEngine;

namespace TOT_DLL_test
{
    [StaticConstructorOnStartup]
    public static class CMC_EffectsTool
    {
        public static FleckDef FleckDef_Blask = DefDatabase<FleckDef>.GetNamed("CMC_Filth_BlastMarkFleck", true);
        public static void SpawnExplosionBlask(Vector3 loc, Map map, FleckDef fleckDef, float scale, float rotrate, float speed, float rot)
        {
            if(fleckDef == null)
                return;
            FleckCreationData dataStatic = FleckMaker.GetDataStatic(loc, map, fleckDef, scale);
            dataStatic.rotationRate = rotrate;
            dataStatic.velocityAngle = Rand.Range(-180f, 180f);
            dataStatic.velocitySpeed = speed;
            dataStatic.rotation = rot;
            map.flecks.CreateFleck(dataStatic);
        }
    }
}
