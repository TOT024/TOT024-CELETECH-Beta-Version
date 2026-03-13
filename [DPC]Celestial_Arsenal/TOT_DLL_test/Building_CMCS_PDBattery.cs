using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace TOT_DLL_test
{
    [StaticConstructorOnStartup]
    public class Building_PDBattery : Building_CMCTurretGun
    {
        public CompPowerTrader PowerTraderComp
        {
            get
            {
                CompPowerTrader result;
                if ((result = this.powerTraderComp) == null)
                {
                    result = (this.powerTraderComp = this.TryGetComp<CompPowerTrader>());
                }
                return result;
            }
        }
        public override bool CanSetForcedTarget
        {
            get
            {
                return true;
            }
        }
        protected override void Tick()
        {
            base.Tick();
        }
        public override LocalTargetInfo TryFindNewTarget()
        {

            LocalTargetInfo result;
            if (this.pawnTarget != null && !this.pawnTarget.DeadOrDowned && this.pawnTarget.Faction.HostileTo(base.Faction))
            {
                result = this.pawnTarget;
            }
            else
            {
                IAttackTargetSearcher attackTargetSearcher = base.TargSearcher();
                Faction faction = attackTargetSearcher.Thing.Faction;
                float range = this.AttackVerb.verbProps.range;
                Building t;
                if (Rand.Value < 0.5f && this.AttackVerb.ProjectileFliesOverhead() && faction.HostileTo(Faction.OfPlayer) && base.Map.listerBuildings.allBuildingsColonist.Where(delegate (Building x)
                {
                    float num = this.AttackVerb.verbProps.EffectiveMinRange(x, this);
                    float num2 = (float)x.Position.DistanceToSquared(this.Position);
                    return num2 > num * num && num2 < range * range;
                }).TryRandomElement(out t))
                {
                    return t;
                }
                else
                {
                    TargetScanFlags targetScanFlags = TargetScanFlags.NeedThreat | TargetScanFlags.NeedAutoTargetable;
                    if (!this.AttackVerb.ProjectileFliesOverhead())
                    {
                        targetScanFlags |= TargetScanFlags.NeedLOSToAll;
                    }
                    if (this.AttackVerb.IsIncendiary_Ranged())
                    {
                        targetScanFlags |= TargetScanFlags.NeedNonBurning;
                    }
                    bool isMortar = base.IsMortar;
                    if (isMortar)
                    {
                        targetScanFlags |= TargetScanFlags.NeedNotUnderThickRoof;
                    }
                    result = (Thing)AttackTargetFinder.BestShootTargetFromCurrentPosition(attackTargetSearcher, targetScanFlags, new Predicate<Thing>(this.IsValidTarget), 0f, 9999f);
                }
            }
            return result;
        }
        public CompPowerTrader powerTraderComp;
        private Pawn pawnTarget;
    }
}
