using System;
using RimWorld;
using Verse;

namespace TOT_DLL_test
{
    public class Verb_ShootSwitchFire : Verb_LauncherProjectileSwitchFire
    {
        protected override int ShotsPerBurst
        {
            get
            {
                return this.verbProps.burstShotCount;
            }
        }
        public override void WarmupComplete()
        {
            base.WarmupComplete();
            Pawn pawn = this.currentTarget.Thing as Pawn;
            bool flag = pawn != null && !pawn.Downed && !pawn.IsColonyMech && this.CasterIsPawn && this.CasterPawn.skills != null;
            if (flag)
            {
                float num = pawn.HostileTo(this.caster) ? 170f : 20f;
                float num2 = this.verbProps.AdjustedFullCycleTime(this, this.CasterPawn);
                this.CasterPawn.skills.Learn(SkillDefOf.Shooting, num * num2, false, false);
            }
        }
        protected override bool TryCastShot()
        {
            base.Retarget();
            this.doRetarget = true;
            bool flag = base.TryCastShot();
            bool flag2 = flag && this.CasterIsPawn;
            if (flag2)
            {
                this.CasterPawn.records.Increment(RecordDefOf.ShotsFired);
            }
            return flag;
        }
    }
}
