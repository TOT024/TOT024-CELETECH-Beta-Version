using System;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace TOT_DLL_test
{
    internal class Skyfaller_TSShip : Skyfaller
    {
        private Comp_TraderShuttle Ship
        {
            get
            {
                return this.innerContainer.Any ? this.innerContainer[0].TryGetComp<Comp_TraderShuttle>() : null;
            }
        }
        private new Material ShadowMaterial
        {
            get
            {
                bool flag = this.cachedShadowMaterial == null && !this.def.skyfaller.shadow.NullOrEmpty();
                if (flag)
                {
                    this.cachedShadowMaterial = MaterialPool.MatFrom(this.def.skyfaller.shadow, ShaderDatabase.Transparent);
                }
                return this.cachedShadowMaterial;
            }
        }
        private Material ExactShadow
        {
            get
            {
                return this.cachedExactShadow;
            }
        }
        private Skyfaller skyfaller
        {
            get
            {
                return this as Skyfaller;
            }
        }
        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            base.DrawAt(drawLoc, flip);
            Material shadowMaterial = this.ExactShadow;
            if (shadowMaterial != null)
            {
                Vector3 shadowPos = this.DrawPos;
                shadowPos.z = this.TrueCenter().z - 0.2f;
                shadowPos.y = AltitudeLayer.Shadows.AltitudeFor();
                Color color = shadowMaterial.color;
                color.a = Mathf.Clamp(1f - (float)this.skyfaller.ticksToImpact / 150f, 0.2f, 1f);
                shadowMaterial.color = color;
                Matrix4x4 matrix = default(Matrix4x4);
                matrix.SetTRS(shadowPos, this.Rotation.AsQuat, new Vector3(this.DrawSize.x, 1f, this.DrawSize.y));
                Graphics.DrawMesh(MeshPool.plane10Back, matrix, shadowMaterial, 0, null, 0);
            }
        }
        private Material cachedShadowMaterial;
        private Material cachedExactShadow = MaterialPool.MatFrom("Things/Skyfaller/TradeShadow", ShaderDatabase.Transparent);
    }
}
