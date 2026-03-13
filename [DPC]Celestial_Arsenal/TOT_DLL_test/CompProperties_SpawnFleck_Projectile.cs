using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace TOT_DLL_test
{
    public class CompProperties_SpawnFleck_Projectile : CompProperties
    {
        public CompProperties_SpawnFleck_Projectile()
        {
            this.compClass = typeof(Comp_SpawnFleck_Projectile);
        }

        public FleckDef FleckDef;

        // Token: 0x0400017F RID: 383
        public int Fleck_MakeFleckTickMax = 10;

        // Token: 0x04000180 RID: 384
        public IntRange Fleck_MakeFleckNum;

        // Token: 0x04000181 RID: 385
        public FloatRange Fleck_Angle = new FloatRange(-180f, 180f);

        // Token: 0x04000182 RID: 386
        public FloatRange Fleck_Scale = new FloatRange(1f, 2f);

        // Token: 0x04000183 RID: 387
        public FloatRange Fleck_Speed = new FloatRange(5f, 7f);

        // Token: 0x04000184 RID: 388
        public FloatRange Fleck_Rotation = new FloatRange(-180f, 180f);
    }
}
