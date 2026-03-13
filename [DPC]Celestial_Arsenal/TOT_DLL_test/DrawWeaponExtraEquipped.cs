using RimWorld;
using System;
using UnityEngine;
using Verse;
using static Verse.HediffCompProperties_RandomizeSeverityPhases;

namespace TOT_DLL_test
{
    [StaticConstructorOnStartup]
    public class DrawWeaponExtraEquipped
    {
        public enum SwingPhase
        {
            None,
            WindUp,   
            Strike,  
            Impact,   
            Recovery  
        }
        private static Pawn GetHolder(CompEquippable comp)
        {
            ThingWithComps parent = comp.parent;
            if (!(((parent != null) ? parent.ParentHolder : null) is Pawn_EquipmentTracker pawn_EquipmentTracker))
            {
                return null;
            }
            return pawn_EquipmentTracker.pawn;
        }
        private static float GetSwingOffset(Thing eq, out DefModExtension_WeaponAnimation animData, out SwingPhase phase)
        {
            animData = null;
            phase = SwingPhase.None;
            CompEquippable comp = eq.TryGetComp<CompEquippable>();
            Pawn pawn = GetHolder(comp);
            var ext = eq.def.GetModExtension<DefModExtension_WeaponAnimation>();
            if (ext == null)
                return 0f;
            animData = ext;
            if (pawn != null && pawn.stances.curStance is Stance_Cooldown cooldown)
            {
                if (cooldown.verb != null && cooldown.verb is Verb_MeleeSectorDamage)
                {
                    return CalculateSwingProgress(cooldown, pawn, ext, eq.def.equippedAngleOffset, out phase);
                }
            }
            return 0f;
        }
        private static float CalculateSwingProgress(Stance_Cooldown cooldown, Pawn pawn, DefModExtension_WeaponAnimation ext, float baseOffset, out SwingPhase phase)
        {
            float totalTicks = cooldown.verb.verbProps.AdjustedCooldownTicks(cooldown.verb, pawn);
            float ticksPassed = totalTicks - cooldown.ticksLeft;
            float attackDuration = ext.attackDuration;
            float targetStartAngle = ext.startAngle - baseOffset;
            float targetEndAngle = ext.endAngle - baseOffset;

            float windUpPercent = 0.3f; 
            float windUpDuration = attackDuration * windUpPercent;
            if (ticksPassed <= windUpDuration)
            {
                phase = SwingPhase.WindUp;
                float t = ticksPassed / windUpDuration;
                return Mathf.Lerp(0f, targetStartAngle, Mathf.SmoothStep(0f, 1f, t));
            }
            else if (ticksPassed <= attackDuration)
            {
                phase = SwingPhase.Strike;
                float currentSwingTicks = ticksPassed - windUpDuration;
                float actualSwingDuration = attackDuration - windUpDuration;
                float t = currentSwingTicks / actualSwingDuration;
                return Mathf.Lerp(targetStartAngle, targetEndAngle, t * t);
            }
            else if (ticksPassed <= attackDuration + ext.impactPause)
            {
                phase = SwingPhase.Impact;
                return targetEndAngle;
            }
            else if (ticksPassed <= attackDuration + ext.impactPause + ext.recoveryDuration)
            {
                phase = SwingPhase.Recovery;
                float currentRecoveryTicks = ticksPassed - (attackDuration + ext.impactPause);
                float t = currentRecoveryTicks / ext.recoveryDuration;
                return Mathf.Lerp(targetEndAngle, 0f, Mathf.SmoothStep(0f, 1f, t));
            }

            phase = SwingPhase.None;
            return 0f;
        }
        public static void DrawExtraMatStatic(Thing eq, Vector3 drawLoc, float aimAngle)
        {
            float swingOffset = GetSwingOffset(eq, out DefModExtension_WeaponAnimation animData, out SwingPhase phase);
            float currentAimAngle = aimAngle;
            if (currentAimAngle > 180f && currentAimAngle < 360f)
            {
                currentAimAngle -= swingOffset;
            }
            else
            {
                currentAimAngle += swingOffset;
            }
            float angle = currentAimAngle - 90f;
            Mesh mesh;
            bool isMeshFlipped = false;
            if (aimAngle > 20f && aimAngle < 160f)
            {
                mesh = MeshPool.plane10;
                angle += eq.def.equippedAngleOffset;
            }
            else if (aimAngle > 200f && aimAngle < 340f)
            {
                mesh = MeshPool.plane10Flip;
                isMeshFlipped = true;
                angle -= 180f;
                angle -= eq.def.equippedAngleOffset;
            }
            else
            {
                mesh = MeshPool.plane10;
                angle += eq.def.equippedAngleOffset;
            }
            angle %= 360f;
            CompEquippable compEquippable = eq.TryGetComp<CompEquippable>();
            if (compEquippable != null)
            {
                EquipmentUtility.Recoil(eq.def, EquipmentUtility.GetRecoilVerb(compEquippable.AllVerbs), out Vector3 recoilOffset, out float recoilAngle, aimAngle);
                drawLoc += recoilOffset;
                angle += recoilAngle;
            }
            Material material;
            if (eq.Graphic is Graphic_StackCount graphic_StackCount)
            {
                material = graphic_StackCount.SubGraphicForStackCount(1, eq.def).MatSingleFor(eq);
            }
            else
            {
                material = eq.Graphic.MatSingleFor(eq);
            }
            Vector3 size = new Vector3(eq.Graphic.drawSize.x, 1f, eq.Graphic.drawSize.y);
            if (animData != null && animData.pivotShift != 0f)
            {
                float pivotDistance = animData.pivotShift * size.x;
                if (isMeshFlipped) pivotDistance = -pivotDistance;

                Vector3 pivotOffset = new Vector3(pivotDistance, 0f, 0f);
                pivotOffset = Quaternion.AngleAxis(angle, Vector3.up) * pivotOffset;
                drawLoc += pivotOffset;
            }
            Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.up);
            Matrix4x4 matrixWeapon = Matrix4x4.TRS(drawLoc, Quaternion.AngleAxis(angle, Vector3.up), size);
            Graphics.DrawMesh(mesh, matrixWeapon, material, 0);
            Vector3 overlayPos = drawLoc;
            overlayPos.y += 0.003f;
            Matrix4x4 matrixOverlay = Matrix4x4.TRS(overlayPos, rotation, size);

            Comp_WeaponRenderDynamic compDynamic = eq.TryGetComp<Comp_WeaponRenderDynamic>();
            compDynamic?.PostDrawExtraGlower(mesh, matrixOverlay);

            Comp_WeaponRenderStatic compStatic = eq.TryGetComp<Comp_WeaponRenderStatic>();
            compStatic?.PostDrawExtraGlower(mesh, matrixOverlay);
        }
    }
}