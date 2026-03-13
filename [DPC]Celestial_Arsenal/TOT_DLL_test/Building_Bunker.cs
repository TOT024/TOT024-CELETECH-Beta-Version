using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TOT_DLL_test
{
    public class Building_Bunker : Building_CMCTurretGun, IAttackTarget
    {
        public override bool CanToggleHoldFire => false;
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            this.cachedLifter = this.TryGetComp<CompTurretLifter>();
            base.SpawnSetup(map, respawningAfterLoad);
        }
        public new float TargetPriorityFactor
        {
            get { return 2f; }
        }
        public new bool ThreatDisabled(IAttackTargetSearcher disabledFor)
        {
            return !this.Active;
        }
        public CompTurretLifter TurretLifter
        {
            get
            {
                if(this.cachedLifter != null)
                    return this.cachedLifter;
                else
                {
                    this.cachedLifter = this.TryGetComp<CompTurretLifter>();
                    return this.cachedLifter;
                }
            }
        }
        public override void PreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
            base.PreApplyDamage(ref dinfo, out absorbed);
            if (absorbed) return;
            if (TurretLifter!=null)
            {
                float mitigatedAmount = dinfo.Amount;
                if (TurretLifter.IsMechanicallyRetracted)
                {
                    mitigatedAmount *= 0.2f;
                }
                else if(!TurretLifter.IsMechanicallyRetracted && !TurretLifter.IsFullyDeployed)
                {
                    mitigatedAmount *= 2f;
                }
                dinfo.SetAmount(mitigatedAmount);
            }
        }
        public override bool Active
        {
            get
            {
                if (!base.Active) return false;
                return cachedLifter == null || cachedLifter.IsFullyDeployed;
            }
        }
    }
}