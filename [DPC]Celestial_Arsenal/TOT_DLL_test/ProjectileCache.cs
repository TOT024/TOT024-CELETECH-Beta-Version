using System.Collections.Generic;
using System.Linq;
using Verse;

namespace TOT_DLL_test
{
    [StaticConstructorOnStartup]
    public static class ProjectileCache
    {
        public static HashSet<ThingDef> ProjectileDefs = new HashSet<ThingDef>();
        static ProjectileCache()
        {
            ProjectileDefs = (from x in DefDatabase<ThingDef>.AllDefsListForReading
                              where x.projectile != null && (x.projectile.flyOverhead || x.projectile.explosionRadius > 0f)
                              && x.projectile.speed < 80f
                              select x).ToHashSet();

            Log.Message($">>> CMC projectile ref resolved. Count: {ProjectileDefs.Count}");
        }
    }
}