using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace TOT_DLL_test
{
    public static class EquipmentUtility
    {
        public static Verb GetRecoilVerb(List<Verb> allWeaponVerbs)
        {
            Verb verb_LaunchProjectile = null;
            foreach (Verb verb in allWeaponVerbs)
            {
                if (verb is Verb_LaunchProjectile verb_LaunchProjectile2 && (verb_LaunchProjectile == null || verb_LaunchProjectile.LastShotTick < verb_LaunchProjectile2.LastShotTick))
                {
                    verb_LaunchProjectile = verb_LaunchProjectile2;
                }
                if (verb is Verb_LauncherProjectileSwitchFire verb_LaunchProjectile3 && (verb_LaunchProjectile == null || verb_LaunchProjectile.LastShotTick < verb_LaunchProjectile3.LastShotTick))
                {
                    verb_LaunchProjectile = verb_LaunchProjectile3;
                }
            }
            return verb_LaunchProjectile;
        }
        public static void Recoil(ThingDef weaponDef, Verb shootVerb, out Vector3 drawOffset, out float angleOffset, float aimAngle)
        {
            drawOffset = Vector3.zero;
            angleOffset = 0f;
            if (weaponDef.recoilPower > 0f && shootVerb != null)
            {
                Rand.PushState(shootVerb.LastShotTick);
                try
                {
                    int num = Find.TickManager.TicksGame - shootVerb.LastShotTick;
                    if ((float)num < weaponDef.recoilRelaxation)
                    {
                        float num2 = Mathf.Clamp01((float)num / weaponDef.recoilRelaxation);
                        float num3 = Mathf.Lerp(weaponDef.recoilPower, 0f, num2);
                        drawOffset = new Vector3((float)Rand.Sign * EquipmentUtility.RecoilCurveAxisX.Evaluate(num2), 0f, -EquipmentUtility.RecoilCurveAxisY.Evaluate(num2)) * num3;
                        angleOffset = (float)Rand.Sign * EquipmentUtility.RecoilCurveRotation.Evaluate(num2) * num3;
                        aimAngle += angleOffset;
                        drawOffset = drawOffset.RotatedBy(aimAngle);
                    }
                }
                finally
                {
                    Rand.PopState();
                }
            }
        }
        public static void Recoil(ThingDef weaponDef, Verb_LaunchProjectile shootVerb, out Vector3 drawOffset, out float angleOffset, float aimAngle)
        {
            drawOffset = Vector3.zero;
            angleOffset = 0f;
            if (weaponDef.recoilPower > 0f && shootVerb != null)
            {
                Rand.PushState(shootVerb.LastShotTick);
                try
                {
                    int num = Find.TickManager.TicksGame - shootVerb.LastShotTick;
                    if ((float)num < weaponDef.recoilRelaxation)
                    {
                        float num2 = Mathf.Clamp01((float)num / weaponDef.recoilRelaxation);
                        float num3 = Mathf.Lerp(weaponDef.recoilPower, 0f, num2);
                        drawOffset = new Vector3((float)Rand.Sign * EquipmentUtility.RecoilCurveAxisX.Evaluate(num2), 0f, -EquipmentUtility.RecoilCurveAxisY.Evaluate(num2)) * num3;
                        angleOffset = (float)Rand.Sign * EquipmentUtility.RecoilCurveRotation.Evaluate(num2) * num3;
                        aimAngle += angleOffset;
                        drawOffset = drawOffset.RotatedBy(aimAngle);
                    }
                }
                finally
                {
                    Rand.PopState();
                }
            }
        }
        private static readonly SimpleCurve RecoilCurveAxisX = new SimpleCurve
        {
            {
                new CurvePoint(0f, 0f),
                true
            },
            {
                new CurvePoint(1f, 0.02f),
                true
            },
            {
                new CurvePoint(2f, 0.03f),
                true
            }
        };
        private static readonly SimpleCurve RecoilCurveAxisY = new SimpleCurve
        {
            {
                new CurvePoint(0f, 0f),
                true
            },
            {
                new CurvePoint(1f, 0.05f),
                true
            },
            {
                new CurvePoint(2f, 0.075f),
                true
            }
        };
        private static readonly SimpleCurve RecoilCurveRotation = new SimpleCurve
        {
            {
                new CurvePoint(0f, 0f),
                true
            },
            {
                new CurvePoint(1f, 3f),
                true
            },
            {
                new CurvePoint(2f, 4f),
                true
            }
        };
    }
}
