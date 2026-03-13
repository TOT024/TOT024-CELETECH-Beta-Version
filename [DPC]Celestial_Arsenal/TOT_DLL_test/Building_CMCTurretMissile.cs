using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TOT_DLL_test
{
    [StaticConstructorOnStartup]
    public class Building_CMCTurretMissile : Building_CMCTurretGun
    {
        private MissileDefenseManager cachedMapComp;
        private static Dictionary<string, List<Material>> cachedSheetMaterials = new Dictionary<string, List<Material>>();

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            this.cachedMapComp = map.GetComponent<MissileDefenseManager>();
            InitializeMaterialsIfNeeded();
        }
        private void InitializeMaterialsIfNeeded()
        {
            if (cachedSheetMaterials.ContainsKey(this.def.defName))
            {
                return;
            }
            TurretExtension_CMC modExtension = this.def.GetModExtension<TurretExtension_CMC>();
            if (modExtension != null && !modExtension.turrettopspritePath.NullOrEmpty())
            {
                Material originalMat = MaterialPool.MatFrom(modExtension.turrettopspritePath, ShaderDatabase.Cutout);
                int count = modExtension.frameCount > 0 ? modExtension.frameCount : 1;
                List<Material> frames = new List<Material>();
                float step = 1.0f / count;
                for (int i = 0; i < count; i++)
                {
                    Material frameMat = new Material(originalMat)
                    {
                        mainTextureScale = new Vector2(step, 1.0f),
                        mainTextureOffset = new Vector2(i * step, 0f)
                    };
                    frames.Add(frameMat);
                }
                cachedSheetMaterials.Add(this.def.defName, frames);
            }
            else
            {
                cachedSheetMaterials.Add(this.def.defName, null);
            }
        }
        public override Material TurretTopMaterial
        {
            get
            {
                if (!cachedSheetMaterials.TryGetValue(this.def.defName, out List<Material> frames) || frames == null)
                {
                    return this.def.building.turretTopMat;
                }
                if (this.refuelableComp == null)
                {
                    return frames[0];
                }
                return frames[(int)this.refuelableComp.Fuel];
            }
        }
        public override LocalTargetInfo TryFindNewTarget()
        {
            if (this.cachedMapComp != null && this.cachedMapComp.HasActiveRadar)
            {
                return base.TryFindNewTarget();
            }
            return null;
        }
        public override bool CanSetForcedTarget
        {
            get
            {
                return true;
            }
        }
    }
}