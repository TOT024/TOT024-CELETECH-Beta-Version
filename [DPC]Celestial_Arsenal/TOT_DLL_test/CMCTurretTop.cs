using RimWorld;
using RimWorld.Planet;
using System;
using UnityEngine;
using UnityEngine.UI;
using Verse;
using Verse.Sound;

namespace TOT_DLL_test
{
    public class CMCTurretTop
    {
        static CMCTurretTop()
        {
            CMCTurretTop.ArtworkRotation = -90;
        }
        public CMCTurretTop(Building_CMCTurretGun ParentTurret)
        {
            this.parentTurret = ParentTurret;
        }
        public virtual void DrawTurret(Vector3 drawLoc, Vector3 recoilDrawOffset)
        {
            Vector3 b = new Vector3(this.parentTurret.def.building.turretTopOffset.x, 0f, this.parentTurret.def.building.turretTopOffset.y).RotatedBy(this.CurRotation);
            float turretTopDrawSize = this.parentTurret.def.building.turretTopDrawSize;
            Verb currentEffectiveVerb = this.parentTurret.CurrentEffectiveVerb;
            float num = ((currentEffectiveVerb != null) ? currentEffectiveVerb.AimAngleOverride : null) ?? this.CurRotation;
            Vector3 vector = drawLoc + Altitudes.AltIncVect + b;
            vector.y = AltitudeLayer.Item.AltitudeFor() + 0.33f;
            Quaternion q = ((float)TurretTop.ArtworkRotation + num).ToQuat();
            Vector3 s = new Vector3(turretTopDrawSize, 1f, turretTopDrawSize);
            Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(vector, q, s), this.parentTurret.TurretTopMaterial, 0);
            var def = this.parentTurret.def;
            string texPath = def == CMC_Def.CMCML ? "Things/Buildings/CMC_MissileLauncherTop_Light" :
                                                     def.building.turretGunDef.graphicData.texPath + "_Light";
            Material material;
            try
            {
                material = MaterialPool.MatFrom(texPath, ShaderDatabase.MoteGlow, new Color(255f, 255f, 255f));
            }
            catch (Exception ex)
            {
                material = MaterialPool.MatFrom("Things/Mote/MoteGlow", ShaderDatabase.MoteGlow, new Color(255f, 255f, 255f));
            }
            if (material != null && material != BaseContent.BadMat)
            {
                Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(vector, q, s), material, 0);
            }
        }
        public void ForceFaceTarget(LocalTargetInfo targ)
        {
            if (!parentTurret.IsTargrtingWorld)
            {
                bool isValid = targ.IsValid;
                if (isValid)
                {
                    float destRotation = (targ.Cell.ToVector3Shifted() - this.parentTurret.DrawPos).AngleFlat();
                    this.DestRotation = destRotation;
                }
            }
            else
            {
                PlanetLayer planetLayer = PlanetLayer.Selected;
                this.DestRotation = planetLayer.GetHeadingFromTo(this.parentTurret.Map.Tile, GameComponent_CeleTech.Instance.ASEA_observedMap.Tile);
            }
        }
        public void TurretTopTick()
        {
            LocalTargetInfo currentTarget = this.parentTurret.CurrentTarget;
            if (currentTarget.IsValid)
            {
                float destRotation = (currentTarget.Cell.ToVector3Shifted() - this.parentTurret.DrawPos).AngleFlat();
                this.DestRotation = destRotation;
            }
            if (Mathf.Abs(this.CurRotation - this.DestRotation) <= this.parentTurret.rotationVelocity * 1.225f)
            {
                this.CurRotation = this.DestRotation;
            }
            else
            {
                bool flag4 = this.DestRotation - this.CurRotation < 180f && this.CurRotation < this.DestRotation;
                bool flag5 = this.CurRotation - this.DestRotation >= 180f && this.CurRotation > this.DestRotation;
                if (flag4 || flag5)
                {
                    this.CurRotation += this.parentTurret.rotationVelocity;
                }
                else
                {
                    this.CurRotation -= this.parentTurret.rotationVelocity;
                }
            }
        }
        public void SetRotationFromOrientation()
        {
            this.curRotationInt = 0f;
            this.destRotationInt = 0f;
        }
        public float CurRotation
        {
            get
            {
                return this.curRotationInt;
            }
            set
            {
                this.curRotationInt = value;
                bool flag = this.curRotationInt > 360f;
                if (flag)
                {
                    this.curRotationInt -= 360f;
                }
                bool flag2 = this.curRotationInt < 0f;
                if (flag2)
                {
                    this.curRotationInt += 360f;
                }
            }
        }
        public float DestRotation
        {
            get
            {
                return this.destRotationInt;
            }
            set
            {
                this.destRotationInt = value;
                bool flag = this.destRotationInt > 360f;
                if (flag)
                {
                    this.destRotationInt -= 360f;
                }
                bool flag2 = this.destRotationInt < 0f;
                if (flag2)
                {
                    this.destRotationInt += 360f;
                }
            }
        }
        private readonly Building_CMCTurretGun parentTurret;
        public float curRotationInt;
        public float destRotationInt;
        public static readonly int ArtworkRotation = -90;
        public float IdledestRotation;
    }
}
