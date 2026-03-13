using System.Collections.Generic;
using Verse;
using RimWorld;

namespace TOT_DLL_test
{
    public class VerbProp_SMT : VerbProperties
    {
        public int PPShootNum = 3;
        public float PPTargetAR = 2.5f;
        public bool AllHitTargets = true;
    }
    [StaticConstructorOnStartup]
    public class Verb_ShootMultiTarget : Verb_Shoot
    {
        VerbProp_SMT Props => (VerbProp_SMT)verbProps;
        protected override int ShotsPerBurst
        {
            get
            {
                return this.verbProps.burstShotCount;
            }
        }
        protected override bool TryCastShot()
        {
            //if (base.caster is Building_TurretGun || base.caster is Building_CMCTurretGun)
            //{
            //    CompRefuelable comp = caster.TryGetComp<CompRefuelable>();
            //    if (comp != null)
            //    {
            //        if (comp.Fuel < Props.PPShootNum)
            //        {
            //            return false;
            //        }
            //    }
            //}
            for (int i = 0; i < Props.PPShootNum; i++)
            {
                base.TryCastShot();
            }
            if (base.caster is Building_TurretGun || base.caster is Building_CMCTurretGun)
            {
                CompRefuelable comp = caster.TryGetComp<CompRefuelable>();
                if (comp != null)
                {
                    comp.ConsumeFuel(Props.PPShootNum);
                }
            }
            return true;
        }
        private void ThrowDebugText(string text)
        {
            bool drawShooting = DebugViewSettings.drawShooting;
            if (drawShooting)
            {
                MoteMaker.ThrowText(this.caster.DrawPos, this.caster.Map, text, -1f);
            }
        }
        private void ThrowDebugText(string text, IntVec3 c)
        {
            bool drawShooting = DebugViewSettings.drawShooting;
            if (drawShooting)
            {
                MoteMaker.ThrowText(c.ToVector3Shifted(), this.caster.Map, text, -1f);
            }
        }
        public int ShootNum
        {
            get
            {
                return Props.PPShootNum;
            }
        }
        public float TargetAquireRange = 3.3f;
        protected List<Thing> TargetList = new List<Thing>();
    }
}
