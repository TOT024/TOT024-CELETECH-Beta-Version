using System;
using LudeonTK;
using RimWorld;
using UnityEngine;
using Verse;

namespace TOT_DLL_test
{
    public class VerbProp_Flame : VerbProperties
    {
        public ThingDef MotedDef;
    }
    public class Verb_PlasmaIncinerator : Verb_ShootBeam
    {
        VerbProp_Flame Props => (VerbProp_Flame)verbProps;
        public override void WarmupComplete()
        {
            this.sprayer = (GenSpawn.Spawn(ThingDefOf.IncineratorSpray, this.caster.Position, this.caster.Map, WipeMode.Vanish) as IncineratorSpray);
            base.WarmupComplete();
            BattleLog battleLog = Find.BattleLog;
            Thing caster = this.caster;
            Thing target = this.currentTarget.HasThing ? this.currentTarget.Thing : null;
            ThingWithComps equipmentSource = base.EquipmentSource;
            battleLog.Add(new BattleLogEntry_RangedFire(caster, target, (equipmentSource != null) ? equipmentSource.def : null, null, false));
        }
        protected override bool TryCastShot()
        {
            bool result = base.TryCastShot();
            Vector3 vector = base.InterpolatedPosition.Yto0();
            IntVec3 intVec = vector.ToIntVec3();
            Vector3 vector2 = this.caster.DrawPos;
            Vector3 normalized = (vector - vector2).normalized;
            vector2 += normalized * BarrelOffset;
            IntVec3 position = this.caster.Position;
            MoteDualAttached mote = MoteMaker.MakeInteractionOverlay(this.Props.MotedDef, new TargetInfo(position, this.caster.Map, false), new TargetInfo(intVec, this.caster.Map, false));
            float num = Vector3.Distance(vector, vector2);
            float num2 = (num < BarrelOffset) ? 0.5f : 1f;
            IncineratorSpray incineratorSpray = this.sprayer;
            if (incineratorSpray == null)
            {
                return result;
            }
            incineratorSpray.Add(new IncineratorProjectileMotion
            {
                mote = mote,
                targetDest = intVec,
                worldSource = vector2,
                worldTarget = vector,
                moveVector = (vector - vector2).normalized,
                startScale = 1f * num2,
                endScale = (1f + Rand.Range(0.15f, 0.18f)) * num2,
                lifespanTicks = Mathf.FloorToInt(num * DistanceToLifetimeScalar)
            });
            return result;
        }

        [TweakValue("Incinerator", 0f, 10f)]
        public static float DistanceToLifetimeScalar = 5f;
        [TweakValue("Incinerator", -2f, 7f)]
        public static float BarrelOffset = 5f;
        private IncineratorSpray sprayer;
    }
}