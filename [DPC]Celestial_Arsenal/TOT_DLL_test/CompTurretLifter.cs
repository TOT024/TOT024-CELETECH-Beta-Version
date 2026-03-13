using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace TOT_DLL_test
{
    public class CompProperties_TurretLifter : CompProperties
    {
        public string texturePath;
        public string doorLeftTexturePath;
        public string doorRightTexturePath;
        public int ticksToFull = 120;
        public int ticksToOpen = 120;
        public int retractDelay = 120;
        public float yOffset = 0.05f;
        public Vector2 textureScale = Vector2.one;
        public Vector3 drawOffset = Vector3.zero;
        public float minDarkness = 0.5f;
        public float minVisiblePct = 0.65f;
        public Vector2 doorSize = Vector2.zero;
        public Vector3 doorOffset = Vector3.zero;
        public string gunTopTexturePath;
        public string gunBarrelTexturePath;
        public float barrelExtensionDist = 0.5f;
        public Vector2 barrelSize = Vector2.zero;
        public int ticksToBarrel = 30;
        public Vector2 gunSize = Vector2.zero;

        public CompProperties_TurretLifter()
        {
            this.compClass = typeof(CompTurretLifter);
        }
    }
    [StaticConstructorOnStartup]
    public class CompTurretLifter : ThingComp
    {
        private static readonly MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
        private static readonly int MainTex_ST = Shader.PropertyToID("_MainTex_ST");
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");

        private Material cachedMat;
        private Material cachedDoorLeftMat;
        private Material cachedDoorRightMat;
        private Material cachedGunTopMat;
        private Material cachedGunBarrelMat;

        private float curHeightPct = 0f;
        private float curDoorPct = 0f;
        private float curBarrelPct = 0f;
        private int delayTicksLeft = 0;
        private bool targetStateUp = false;
        private bool userWantsUp = false;
        private bool lastTickDeployed = false;
        private Building_Bunker parentTurret;
        public CompProperties_TurretLifter Props => (CompProperties_TurretLifter)props;
        private CompPowerTrader powerComp;
        public bool IsFullyDeployed => curHeightPct >= 0.99f && curDoorPct >= 0.99f && curBarrelPct >= 0.99f;
        public bool IsMechanicallyRetracted => curHeightPct <= 0.01f && curBarrelPct <= 0.01f;
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref curHeightPct, "curHeightPct", 0f);
            Scribe_Values.Look(ref curDoorPct, "curDoorPct", 0f);
            Scribe_Values.Look(ref curBarrelPct, "curBarrelPct", 0f);
            Scribe_Values.Look(ref delayTicksLeft, "delayTicksLeft", 0);
            Scribe_Values.Look(ref targetStateUp, "targetStateUp", false);
            Scribe_Values.Look(ref userWantsUp, "userWantsUp", false);
        }
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            parentTurret = parent as Building_Bunker;
            powerComp = parent.GetComp<CompPowerTrader>();
        }
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            bool hasPower = (powerComp == null ||(powerComp != null && powerComp.PowerOn));
            yield return new Command_Toggle
            {
                icon = TexCommand.Install,
                defaultLabel = "Turret State",
                defaultDesc = "Toggle the turret position. Needs power to operate.",
                isActive = () => userWantsUp,
                toggleAction = () =>
                {
                    userWantsUp = !userWantsUp;
                    if(userWantsUp && parentTurret.holdFire)
                    {
                        parentTurret.holdFire = false;
                    }
                },
                Disabled = !hasPower,
                disabledReason = "No power"
            };
        }
        public override void CompTick()
        {
            base.CompTick();
            if (userWantsUp)
            {
                if (curDoorPct < 1f)
                    curDoorPct = Mathf.MoveTowards(curDoorPct, 1f, 1f / Props.ticksToOpen);
                else if (curHeightPct < 1f)
                    curHeightPct = Mathf.MoveTowards(curHeightPct, 1f, 1f / Props.ticksToFull);
                else if (curBarrelPct < 1f)
                    curBarrelPct = Mathf.MoveTowards(curBarrelPct, 1f, (Props.ticksToBarrel > 0 ? 1f / Props.ticksToBarrel : 1f));

                delayTicksLeft = 0;
                if (IsFullyDeployed && !lastTickDeployed)
                {
                    if (parentTurret != null)
                    {
                        parentTurret.turrettop.CurRotation = 0f; 
                    }
                    lastTickDeployed = true;
                }
            }
            else
            {
                lastTickDeployed = false;
                bool readyToRetract = true;
                if (!IsMechanicallyRetracted && parentTurret != null)
                {
                    this.parentTurret.holdFire = true;
                    float targetAngle = 0f;
                    float curAngle = parentTurret.turrettop.CurRotation;
                    if (Mathf.Abs(Mathf.DeltaAngle(curAngle, targetAngle)) > 1f)
                    {
                        parentTurret.turrettop.DestRotation = 0f;
                        parentTurret.turrettop.TurretTopTick();
                        readyToRetract = false;
                    }
                }
                if (readyToRetract)
                {
                    if (curBarrelPct > 0f)
                    {
                        float speed = Props.ticksToBarrel > 0 ? 1f / Props.ticksToBarrel : 1f;
                        curBarrelPct = Mathf.MoveTowards(curBarrelPct, 0f, speed);
                        delayTicksLeft = Props.retractDelay;
                    }
                    else if (curHeightPct > 0f)
                    {
                        float speed = 1f / Props.ticksToFull;
                        curHeightPct = Mathf.MoveTowards(curHeightPct, 0f, speed);
                        delayTicksLeft = Props.retractDelay;
                    }
                    else
                    {
                        if (delayTicksLeft > 0) delayTicksLeft--;
                        else if (curDoorPct > 0f)
                            curDoorPct = Mathf.MoveTowards(curDoorPct, 0f, 1f / Props.ticksToOpen);
                    }
                }
            }
            //if (targetStateUp)
            //{
            //    if (curDoorPct < 1f)
            //    {
            //        curDoorPct = Mathf.MoveTowards(curDoorPct, 1f, 1f / Props.ticksToOpen);
            //    }
            //    else if (curHeightPct < 1f)
            //    {
            //        float speed = 1f / Props.ticksToFull;
            //        curHeightPct = Mathf.MoveTowards(curHeightPct, 1f, speed);
            //    }
            //    else if (curBarrelPct < 1f)
            //    {
            //        float speed = Props.ticksToBarrel > 0 ? 1f / Props.ticksToBarrel : 1f;
            //        curBarrelPct = Mathf.MoveTowards(curBarrelPct, 1f, speed);
            //    }
            //    delayTicksLeft = 0;
            //}
            //else
            //{
            //    if (curBarrelPct > 0f)
            //    {
            //        float speed = Props.ticksToBarrel > 0 ? 1f / Props.ticksToBarrel : 1f;
            //        curBarrelPct = Mathf.MoveTowards(curBarrelPct, 0f, speed);
            //        delayTicksLeft = Props.retractDelay;
            //    }
            //    else if (curHeightPct > 0f)
            //    {
            //        float speed = 1f / Props.ticksToFull;
            //        curHeightPct = Mathf.MoveTowards(curHeightPct, 0f, speed);
            //        delayTicksLeft = Props.retractDelay;
            //    }
            //    else
            //    {
            //        if (delayTicksLeft > 0)
            //        {
            //            delayTicksLeft--;
            //        }
            //        else if (curDoorPct > 0f)
            //        {
            //            curDoorPct = Mathf.MoveTowards(curDoorPct, 0f, 1f / Props.ticksToOpen);
            //        }
            //    }
            //}
        }
        public override void PostDraw()
        {
            if (cachedMat == null) cachedMat = LoadMat(Props.texturePath);
            if (cachedDoorLeftMat == null) cachedDoorLeftMat = LoadMat(Props.doorLeftTexturePath);
            if (cachedDoorRightMat == null) cachedDoorRightMat = LoadMat(Props.doorRightTexturePath);
            if (cachedGunTopMat == null) cachedGunTopMat = LoadMat(Props.gunTopTexturePath);
            if (cachedGunBarrelMat == null) cachedGunBarrelMat = LoadMat(Props.gunBarrelTexturePath);

            Vector3 doorAnchor = parent.DrawPos + (parent.Rotation.AsQuat * Props.doorOffset);
            doorAnchor.y += Props.yOffset + 0.1f;

            Vector3 platformAnchor = parent.DrawPos + (parent.Rotation.AsQuat * Props.drawOffset);
            platformAnchor.y += Props.yOffset;

            if (curDoorPct < 0.99f) DrawDoors(doorAnchor);

            if (curHeightPct > 0.001f || Props.minVisiblePct > 0) DrawLiftColumn(platformAnchor);

            if (!IsFullyDeployed && curDoorPct > 0.01f)
            {
                DrawGunAnimation(platformAnchor);
            }
        }
        private void DrawGunAnimation(Vector3 anchorPos)
        {
            Quaternion buildingRot = parent.Rotation.AsQuat;
            Vector3 trackDir = buildingRot * Vector3.forward;
            Quaternion fixedVisualRot = buildingRot * Quaternion.Euler(0f, -90f, 0f);
            Vector3 finalCenter = anchorPos - (buildingRot * Props.drawOffset);
            finalCenter.y = anchorPos.y + Props.yOffset + 0.005f;
            Vector2 gSize = Props.gunSize != Vector2.zero ? Props.gunSize : Props.textureScale;
            float halfLen = gSize.x * 0.5f;
            float visualPct = Mathf.Lerp(Props.minVisiblePct, 1f, curHeightPct);
            Vector3 currentTurretCenter = finalCenter - trackDir * (halfLen * (1f - visualPct));
            if (cachedGunTopMat != null)
            {
                DrawClippedPart(currentTurretCenter, fixedVisualRot, gSize, anchorPos, trackDir, cachedGunTopMat);
            }
            if (cachedGunBarrelMat != null)
            {
                float staticOffset = 0.15f;
                float animOffset = Props.barrelExtensionDist * curBarrelPct;

                Vector3 barrelPos = currentTurretCenter + (trackDir * (staticOffset + animOffset));
                barrelPos.y -= 0.002f;

                Vector2 bSize = Props.barrelSize != Vector2.zero ? Props.barrelSize : Props.textureScale;
                DrawClippedPart(barrelPos, fixedVisualRot, bSize, anchorPos, trackDir, cachedGunBarrelMat);
            }
        }
        private void DrawClippedPart(Vector3 logicPos, Quaternion visualRot, Vector2 size, Vector3 holePos, Vector3 trackDir, Material mat)
        {
            float fullLen = size.x;
            float width = size.y;
            Vector3 diff = logicPos - holePos;
            float dist = Vector3.Dot(diff, trackDir);
            float backDist = dist - (fullLen * 0.5f);
            float clipLen = 0f;
            if (backDist < 0)
            {
                clipLen = -backDist;
            }

            float visibleLen = fullLen - clipLen;
            if (visibleLen <= 0.001f) return;
            float uvStart = clipLen / fullLen;
            Vector3 visualOffset = trackDir * (clipLen * 0.5f);
            Vector3 drawPos = logicPos + visualOffset;
            Vector3 drawScale = new Vector3(visibleLen, 1f, width);

            propertyBlock.Clear();
            float cVal = Mathf.Lerp(Props.minDarkness + 0.12f,1f, curHeightPct);
            propertyBlock.SetColor(ColorProperty, new Color(cVal, cVal, cVal, 1f));
            propertyBlock.SetVector(MainTex_ST, new Vector4(1f - uvStart, 1f, uvStart, 0f));

            Matrix4x4 matrix = Matrix4x4.TRS(drawPos, visualRot, drawScale);
            Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0, null, 0, propertyBlock);
        }
        private void DrawDoors(Vector3 centerPos)
        {
            Vector2 size = (Props.doorSize != Vector2.zero) ? Props.doorSize : Props.textureScale;
            float fullWidth = size.x;
            float doorLen = size.y;
            float currentVisWidth = fullWidth * (1f - curDoorPct);
            if (currentVisWidth <= 0.001f) return;

            Quaternion rot = parent.Rotation.AsQuat;
            Vector3 rightDir = rot * Vector3.right;
            Vector3 leftWallEdge = centerPos - (rightDir * fullWidth / 2);
            Vector3 leftPos = leftWallEdge + (rightDir * (currentVisWidth * 0.5f));

            Vector3 leftScale = new Vector3(currentVisWidth, 1f, doorLen);

            propertyBlock.Clear();
            propertyBlock.SetVector(MainTex_ST, new Vector4(1f - curDoorPct, 1f, curDoorPct, 0f));
            Matrix4x4 leftMatrix = Matrix4x4.TRS(leftPos, rot, leftScale);
            Graphics.DrawMesh(MeshPool.plane10, leftMatrix, cachedDoorLeftMat, 0, null, 0, propertyBlock);
            Vector3 rightWallEdge = centerPos + (rightDir * fullWidth / 2);
            Vector3 rightPos = rightWallEdge - (rightDir * (currentVisWidth * 0.5f));
            Vector3 rightScale = new Vector3(currentVisWidth, 1f, doorLen);

            propertyBlock.Clear();
            propertyBlock.SetVector(MainTex_ST, new Vector4(1f - curDoorPct, 1f, 0f, 0f));
            Matrix4x4 rightMatrix = Matrix4x4.TRS(rightPos, rot, rightScale);
            Graphics.DrawMesh(MeshPool.plane10, rightMatrix, cachedDoorRightMat, 0, null, 0, propertyBlock);
        }
        private void DrawLiftColumn(Vector3 anchorPos)
        {
            float minVis = Props.minVisiblePct;
            float visualPct = Mathf.Lerp(minVis, 1f, curHeightPct);
            float finalHeight = Props.textureScale.y * visualPct;

            Vector3 meshScale = new Vector3(Props.textureScale.x, 1f, finalHeight);
            Vector3 forwardDir = parent.Rotation.AsQuat * Vector3.forward;
            Vector3 centerPos = anchorPos + (forwardDir * (finalHeight * 0.5f));
            float colorVal = Mathf.Lerp(Props.minDarkness, 1f, curHeightPct);
            Color drawColor = new Color(colorVal, colorVal, colorVal, 1f);

            propertyBlock.Clear();
            propertyBlock.SetVector(MainTex_ST, new Vector4(1f, visualPct, 0f, 1f - visualPct));
            propertyBlock.SetColor(ColorProperty, drawColor);

            Matrix4x4 matrix = Matrix4x4.TRS(centerPos, parent.Rotation.AsQuat, meshScale);
            Graphics.DrawMesh(MeshPool.plane10, matrix, cachedMat, 0, null, 0, propertyBlock);
        }
        private Material LoadMat(string path)
        {
            if (path.NullOrEmpty()) return BaseContent.BadMat;
            return MaterialPool.MatFrom(path, ShaderDatabase.Cutout);
        }

        public void SetExtended(bool extended) => targetStateUp = extended;
    }
}