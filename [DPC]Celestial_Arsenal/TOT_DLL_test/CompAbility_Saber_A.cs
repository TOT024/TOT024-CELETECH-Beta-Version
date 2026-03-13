using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Steamworks;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TOT_DLL_test
{
    public class CompAbility_Saber_A : CompAbilityEffect
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
                            FleckCreationData dataAttachedOverlay = FleckMaker.GetDataAttachedOverlay(pawn, FleckDefOf.GravshipThrusterExhaust, new Vector3(-0.5f, 0f, -0.5f), 1f, -1f);
                            dataAttachedOverlay.link.detachAfterTicks = 5;
                            pawn.Map.flecks.CreateFleck(dataAttachedOverlay);
                            FleckMaker.Static(pawn.Position, this.parent.pawn.Map, CMC_Def.CMC_PulsingDistortionRing, 3f);
                            SoundDefOf.Psycast_Skip_Entry.PlayOneShot(new TargetInfo(pawn.Position, this.parent.pawn.Map, false));
                            SoundDefOf.Psycast_Skip_Exit.PlayOneShot(new TargetInfo(t.Cell, this.parent.pawn.Map, false));
                            FleckMaker.Static(t.Cell, this.parent.pawn.Map, CMC_Def.CMC_PulsingDistortionRing, 3f);
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
                if (CanPlaceSelectedTargetAt(target) && pawn != null)
                {
                    this.parent.AddEffecterToMaintain(RimWorld.EffecterDefOf.Skip_EntryNoDelay.Spawn(pawn, pawn.Map, 1f), pawn.Position, 60, null);
                    bool selected = Find.Selector.IsSelected(pawn);
                    pawn.DeSpawn();
                    this.parent.AddEffecterToMaintain(RimWorld.EffecterDefOf.Skip_ExitNoDelay.Spawn(target.Cell, pawn.Map, 1f), target.Cell, 60, null);
                    GenSpawn.Spawn(pawn, target.Cell, map, WipeMode.Vanish);
                    pawn.drafter.Drafted = true;
                    if (selected)
                    {
                        Find.Selector.Select(pawn);
                    }
                }
            }

            public bool CanPlaceSelectedTargetAt(LocalTargetInfo target)
            {
                Pawn pawn = base.parent.pawn;
                if (pawn != null)
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
}
