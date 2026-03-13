using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TOT_DLL_test
{
    public class CompAbilityEffect_SelfSkip : CompAbilityEffect
    {
        public new CompProperties_AbilitySelfSkip Props
        {
            get
            {
                return (CompProperties_AbilitySelfSkip)this.props;
            }
        }
        public override IEnumerable<PreCastAction> GetPreCastActions()
        {
            yield return new PreCastAction
            {
                action = delegate (LocalTargetInfo t, LocalTargetInfo d)
                {
                    Pawn pawn = base.parent.pawn;
                    if (pawn != null && CanPlaceSelectedTargetAt(t))
                    {
                        SoundDefOf.Psycast_Skip_Entry.PlayOneShot(new TargetInfo(pawn.Position, this.parent.pawn.Map, false));
                        SoundDefOf.Psycast_Skip_Exit.PlayOneShot(new TargetInfo(t.Cell, this.parent.pawn.Map, false));
                        FleckMaker.Static(pawn.Position, this.parent.pawn.Map, CMC_Def.CMC_PulsingDistortionRing, 1f);
                        FleckMaker.Static(t.Cell, this.parent.pawn.Map, CMC_Def.CMC_PulsingDistortionRing, 1f);
                        FleckMaker.Static(pawn.Position, this.parent.pawn.Map, CMC_Def.CMC_TeleportExit, 1f);
                        FleckMaker.Static(t.Cell, this.parent.pawn.Map, CMC_Def.CMC_TeleportSpawn, 1f);
                    }
                },
                ticksAwayFromCast = 5
            };
            yield break;
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn pawn = base.parent.pawn;
            Map map = pawn.Map;
            if(CanPlaceSelectedTargetAt(target) && pawn != null)
            { 
                this.parent.AddEffecterToMaintain(CMC_Def.CMC_TeleportEffector.Spawn(pawn, pawn.Map, 1f), pawn.Position, 60, null);
                bool selected = Find.Selector.IsSelected(pawn);
                pawn.DeSpawn();
                this.parent.AddEffecterToMaintain(CMC_Def.CMC_TeleportEffector.Spawn(target.Cell, pawn.Map, 1f), target.Cell, 60, null);
                GenSpawn.Spawn(pawn, target.Cell, map, WipeMode.Vanish);
                pawn.drafter.Drafted = true;
                if(selected)
                {
                    Find.Selector.Select(pawn);
                }
            }
        }

        public bool CanPlaceSelectedTargetAt(LocalTargetInfo target)
        {
            Pawn pawn = base.parent.pawn;
            if(pawn!=null)
            {
                return !target.Cell.Impassable(this.parent.pawn.Map) && target.Cell.WalkableBy(this.parent.pawn.Map, pawn);
            }
            return false;
        }

        public LocalTargetInfo GetDestination(LocalTargetInfo target)
        {
            return target;
        }
    }
}
