using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TOT_DLL_test
{
    public class CompAbilityEffect_SwordShower : CompAbilityEffect
    {
        public new CompProperties_AbilitySwordShower Props
        {
            get
            {
                return (CompProperties_AbilitySwordShower) this.props;
            }
        }
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn pawn = this.parent.pawn;
            SkillDummy_Sword dummy = (SkillDummy_Sword)ThingMaker.MakeThing(CMC_Def.CMC_SkillDummy, null);
            GenSpawn.Spawn(dummy, pawn.Position, pawn.Map, WipeMode.Vanish);
            dummy.Insert(pawn);
            dummy.IsSword = true;
        }
        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (target.Cell.Roofed(this.parent.pawn.Map))
            {
                if (throwMessages)
                {
                    Messages.Message("CannotUseAbility".Translate(this.parent.def.label) + ": " + "AbilityRoofed".Translate(), target.ToTargetInfo(this.parent.pawn.Map), MessageTypeDefOf.RejectInput, false);
                }
                return false;
            }
            return true;
        }
        public override void OnGizmoUpdate()
        {
        }

    }
}
