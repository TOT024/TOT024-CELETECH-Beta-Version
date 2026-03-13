using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MYDE_CMC_Dll
{
    // Token: 0x02000008 RID: 8
    [StaticConstructorOnStartup]
    public class Comp_ShowBuildingShieldGizmos : ThingComp
    {
        // Token: 0x17000002 RID: 2
        // (get) Token: 0x06000009 RID: 9 RVA: 0x0000216C File Offset: 0x0000036C
        public CompProperties_ShowBuildingShieldGizmos PropsSpawner
        {
            get
            {
                return (CompProperties_ShowBuildingShieldGizmos)this.props;
            }
        }

        // Token: 0x0600000A RID: 10 RVA: 0x00002189 File Offset: 0x00000389
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            yield return new Gizmo_ProjectileInterceptorHitPoints
            {
                interceptor = this.parent.TryGetComp<CompProjectileInterceptor>()
            };
            yield break;
        }
    }
}
