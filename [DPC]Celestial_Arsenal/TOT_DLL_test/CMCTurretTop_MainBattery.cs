using System;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace TOT_DLL_test
{
    public class CMCTurretTop_MainBattery
    {
        static CMCTurretTop_MainBattery()
        {
            CMCTurretTop_MainBattery.ArtworkRotation = -90;
        }
        public CMCTurretTop_MainBattery(Building_CMCTurretGun_MainBattery ParentTurret)
        {
            this.parentTurret = ParentTurret;
        }
        public virtual void DrawTurret(Vector3 drawLoc, Vector3 recoilDrawOffset, float recoilAngleOffset)
        {
            Vector3 b = new Vector3(this.parentTurret.def.building.turretTopOffset.x, 0f, this.parentTurret.def.building.turretTopOffset.y).RotatedBy(this.CurRotation);
            float turretTopDrawSize = this.parentTurret.def.building.turretTopDrawSize;
            Verb currentEffectiveVerb = this.parentTurret.CurrentEffectiveVerb;
            float num = ((currentEffectiveVerb != null) ? currentEffectiveVerb.AimAngleOverride : null) ?? this.CurRotation;
            Vector3 vector = drawLoc + Altitudes.AltIncVect + b;
            vector.y = AltitudeLayer.BuildingOnTop.AltitudeFor() + 0.13f;
            Quaternion q = ((float)TurretTop.ArtworkRotation + num).ToQuat();
            Vector3 s = new Vector3(turretTopDrawSize, 1f, turretTopDrawSize);
            Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(vector, q, s), this.parentTurret.TurretTopMaterial, 0);
            string texPath;
            texPath = this.parentTurret.def.building.turretGunDef.graphicData.texPath + "_Light";
            Material material = MaterialPool.MatFrom(texPath, ShaderDatabase.MoteGlow, new Color(255f, 255f, 255f));
            Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(vector, q, s), material, 0);

            Vector3 b2 = new Vector3(0f, 0f, 0.97f - parentTurret.CalculateRecoil()).RotatedBy(this.CurRotation);
            Vector3 pos = vector + b2;
            pos.y -= 0.11f;
            Quaternion q2 = ((float)TurretTop.ArtworkRotation + num).ToQuat();
            string texPath2 = this.parentTurret.def.building.turretGunDef.graphicData.texPath + "_Ext";
            Material material2 = MaterialPool.MatFrom(texPath2, ShaderDatabase.DefaultShader, new Color(255f, 255f, 255f));
            Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(pos, q2, s), material2, 0);
        }
        public void ForceFaceTarget(LocalTargetInfo targ)
        {
            if(!parentTurret.IsTargrtingWorld)
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
                //this.DestRotation = Find.World.grid.GetHeadingFromTo(this.parentTurret.Map.Tile, GameComponent_CeleTech.Instance.ASEA_observedMap.Tile);
            }
        }
        public void TurretTopTick()
        {
            LocalTargetInfo currentTarget = this.parentTurret.CurrentTarget;
            if (currentTarget.IsValid && !parentTurret.IsTargrtingWorld)
            {
                float destRotation = (currentTarget.Cell.ToVector3Shifted() - this.parentTurret.DrawPos).AngleFlat();
                this.DestRotation = destRotation;
            }
            else
            {
                if(GameComponent_CeleTech.Instance.ASEA_observedMap!=null && this.parentTurret.IsTargrtingWorld)
                {
                    this.DestRotation = Find.World.grid.GetHeadingFromTo(this.parentTurret.Map.Tile, GameComponent_CeleTech.Instance.ASEA_observedMap.Tile);
                }
            }
            if (Mathf.Abs(this.CurRotation - this.DestRotation) <= this.parentTurret.rotationVelocity * 1.225f)
            {
                this.CurRotation = this.DestRotation;
            }
            else
            {
                bool flag2 = this.DestRotation - this.CurRotation < 180f && this.CurRotation < this.DestRotation;
                bool flag3 = this.CurRotation - this.DestRotation >= 180f && this.CurRotation > this.DestRotation;
                bool flag4 = flag2 || flag3;
                if (flag4)
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
        private Building_CMCTurretGun_MainBattery parentTurret;
        public float curRotationInt;
        public float destRotationInt;
        public static readonly int ArtworkRotation = -90;
        public float IdledestRotation;
        public float recoiloffsetdistance;
    }
}
