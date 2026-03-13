using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TOT_DLL_test
{
    public class MeleeAOE_Extension : DefModExtension
    {
        public float angle = 120f;       
        public float radius = 2.9f;      
        public int maxHitTarget = 3;
        public EffecterDef effcterDef;
    }
    public class Verb_MeleeSectorDamage : Verb_MeleeAttackDamage
    {
        public MeleeAOE_Extension Extension
        {
            get
            {
                if (this.maneuver == null) return null;
                return this.maneuver.GetModExtension<MeleeAOE_Extension>();
            }
        }
        public virtual void EffecterTrigger(LocalTargetInfo target)
        {
            ManeuverDef maneuver = this.maneuver;
            MeleeAOE_Extension effecter_Extension = (maneuver != null) ? maneuver.GetModExtension<MeleeAOE_Extension>() : null;
            bool flag = ((effecter_Extension != null) ? effecter_Extension.effcterDef : null) != null && this.CasterPawn != null && target.Thing != null && this.CasterPawn.Map != null;
            if (flag)
            {
                Effecter effecter = effecter_Extension.effcterDef.Spawn(this.CasterPawn.Position, target.Thing.Position, this.CasterPawn.Map, 1f);
                if (effecter != null)
                {
                    effecter.Cleanup();
                }
            }
        }

        public virtual List<Pawn> TargetPawns()
        {
            List<Pawn> list = new List<Pawn>();
            if (this.Extension == null || this.currentTarget.Thing == null) return list;
            Vector3 directionVec = (this.currentTarget.Thing.Position - this.CasterPawn.Position).ToVector3();
            directionVec.Normalize();
            float aimAngle = directionVec.AngleFlat();
            this.EffecterTrigger(this.currentTarget.Thing);
            foreach (Thing thing in GenRadial.RadialDistinctThingsAround(this.CasterPawn.Position, this.CasterPawn.Map, this.Extension.radius, true))
            {
                Pawn victim = thing as Pawn;
                if (victim != null && victim != this.CasterPawn && victim != this.currentTarget.Thing)
                {
                    if (victim.Faction == null || this.CasterPawn.Faction == null ||
                       (victim.Faction != this.CasterPawn.Faction && victim.Faction.RelationKindWith(this.CasterPawn.Faction) == FactionRelationKind.Hostile))
                    {
                        if (IsPointInCone(victim.Position, this.CasterPawn.Position, aimAngle, this.Extension.angle))
                        {
                            list.Add(victim);
                        }
                    }
                }
            }
            return list;
        }
        private bool IsPointInCone(IntVec3 point, IntVec3 origin, float aimAngle, float coneAngle)
        {
            float angleToPoint = (point - origin).ToVector3().AngleFlat();
            float angleDiff = Mathf.Abs(Mathf.DeltaAngle(aimAngle, angleToPoint));
            return angleDiff <= coneAngle / 2f;
        }
        protected override bool TryCastShot()
        {
            Pawn casterPawn = this.CasterPawn;
            bool flag = !casterPawn.Spawned;
            bool result;
            if (flag)
            {
                result = false;
            }
            else
            {
                bool fullBodyBusy = casterPawn.stances.FullBodyBusy;
                if (fullBodyBusy)
                {
                    result = false;
                }
                else
                {
                    Thing thing = this.currentTarget.Thing;
                    bool flag2 = !this.CanHitTarget(thing);
                    if (flag2)
                    {
                        Log.Warning(string.Concat(new object[]
                        {
                    casterPawn,
                    " meleed ",
                    thing,
                    " from out of melee position."
                        }));
                    }
                    casterPawn.rotationTracker.Face(thing.DrawPos);
                    List<Pawn> list = this.TargetPawns();
                    List<Thing> list2 = new List<Thing>();
                    bool flag3 = list.NullOrEmpty<Pawn>();
                    if (flag3)
                    {
                        result = base.TryCastShot();
                    }
                    else
                    {
                        bool flag4 = list.Count > this.Extension.maxHitTarget - 1;
                        if (flag4)
                        {
                            for (int i = 0; i < this.Extension.maxHitTarget - 1; i++)
                            {
                                Pawn item = list.RandomElement<Pawn>();
                                list2.Add(item);
                                list.Remove(item);
                            }
                        }
                        else
                        {
                            list2.AddRange(list);
                        }
                        list2.Add(thing);
                        bool flag5 = !this.IsTargetImmobile(this.currentTarget) && casterPawn.skills != null && (this.currentTarget.Pawn == null || !this.currentTarget.Pawn.IsColonyMech);
                        if (flag5)
                        {
                            casterPawn.skills.Learn(SkillDefOf.Melee, 200f * this.verbProps.AdjustedFullCycleTime(this, casterPawn), false, false);
                        }
                        bool flag6 = false;
                        foreach (Thing thing2 in list2)
                        {
                            Pawn pawn = thing2 as Pawn;
                            bool flag7 = pawn != null && !pawn.Dead && (casterPawn.MentalStateDef != MentalStateDefOf.SocialFighting || pawn.MentalStateDef != MentalStateDefOf.SocialFighting) && (casterPawn.story == null || !casterPawn.story.traits.DisableHostilityFrom(pawn));
                            if (flag7)
                            {
                                pawn.mindState.meleeThreat = casterPawn;
                                pawn.mindState.lastMeleeThreatHarmTick = Find.TickManager.TicksGame;
                            }
                            Map map = thing2.Map;
                            Vector3 drawPos = thing2.DrawPos;
                            SoundDef soundDef = null;
                            bool flag8 = thing2 == thing;
                            bool flag9 = Rand.Chance(this.GetNonMissChance(thing2));
                            if (flag9)
                            {
                                bool flag10 = !Rand.Chance(this.GetDodgeChance(thing2));
                                if (flag10)
                                {
                                    bool flag11 = thing2.def.category == ThingCategory.Building;
                                    if (flag11)
                                    {
                                        soundDef = this.SoundHitBuilding();
                                    }
                                    else
                                    {
                                        soundDef = this.SoundHitPawn();
                                    }
                                    bool flag12 = this.verbProps.impactMote != null;
                                    if (flag12)
                                    {
                                        MoteMaker.MakeStaticMote(drawPos, map, this.verbProps.impactMote, 1f, false, 0f);
                                    }
                                    bool flag13 = this.verbProps.impactFleck != null;
                                    if (flag13)
                                    {
                                        FleckMaker.Static(drawPos, map, this.verbProps.impactFleck, 1f);
                                    }
                                    BattleLogEntry_MeleeCombat battleLogEntry_MeleeCombat = this.CreateCombatLog((ManeuverDef maneuver) => maneuver.combatLogRulesHit, true, thing2);
                                    bool flag14 = flag8;
                                    if (flag14)
                                    {
                                        flag6 = true;
                                    }
                                    DamageWorker.DamageResult damageResult = this.ApplyMeleeDamageToTarget(thing2);
                                    bool flag15 = pawn != null && damageResult.totalDamageDealt > 0f;
                                    if (flag15)
                                    {
                                        this.ApplyMeleeSlaveSuppression(pawn, damageResult.totalDamageDealt);
                                    }
                                    bool flag16 = damageResult.stunned && damageResult.parts.NullOrEmpty<BodyPartRecord>();
                                    if (flag16)
                                    {
                                        Find.BattleLog.RemoveEntry(battleLogEntry_MeleeCombat);
                                    }
                                    else
                                    {
                                        damageResult.AssociateWithLog(battleLogEntry_MeleeCombat);
                                        bool deflected = damageResult.deflected;
                                        if (deflected)
                                        {
                                            battleLogEntry_MeleeCombat.RuleDef = this.maneuver.combatLogRulesDeflect;
                                            battleLogEntry_MeleeCombat.alwaysShowInCompact = false;
                                        }
                                    }
                                }
                                else
                                {
                                    bool flag17 = flag8;
                                    if (flag17)
                                    {
                                        flag6 = false;
                                        soundDef = this.SoundDodge(thing2);
                                    }
                                    MoteMaker.ThrowText(drawPos, map, "TextMote_Dodge".Translate(), 1.9f);
                                    base.CreateCombatLog((ManeuverDef maneuver) => maneuver.combatLogRulesDodge, false);
                                }
                            }
                            else
                            {
                                bool flag18 = flag8;
                                if (flag18)
                                {
                                    flag6 = false;
                                    soundDef = this.SoundMiss();
                                }
                                base.CreateCombatLog((ManeuverDef maneuver) => maneuver.combatLogRulesMiss, false);
                            }
                            bool flag19 = soundDef != null && flag8;
                            if (flag19)
                            {
                                soundDef.PlayOneShot(new TargetInfo(thing2.Position, map, false));
                            }
                            bool flag20 = casterPawn.Spawned && flag8;
                            if (flag20)
                            {
                                casterPawn.Drawer.Notify_MeleeAttackOn(thing2);
                            }
                            bool flag21 = pawn != null && !pawn.Dead && pawn.Spawned;
                            if (flag21)
                            {
                                pawn.stances.stagger.StaggerFor(95, 0.17f);
                            }
                            bool flag22 = casterPawn.Spawned && flag8;
                            if (flag22)
                            {
                                casterPawn.rotationTracker.FaceCell(thing.Position);
                            }
                            bool flag23 = casterPawn.caller != null && flag8;
                            if (flag23)
                            {
                                casterPawn.caller.Notify_DidMeleeAttack();
                            }
                        }
                        result = flag6;
                    }
                }
            }
            return result;
        }
        private void ApplyMeleeSlaveSuppression(Pawn target, float damage)
        {
            if (ModsConfig.IdeologyActive && this.CasterPawn.IsColonist && target.IsSlaveOfColony)
            {
                SlaveRebellionUtility.IncrementMeleeSuppression(this.CasterPawn, target, damage);
            }
        }
        private float GetNonMissChance(LocalTargetInfo target)
        {
            if (this.surpriseAttack || IsTargetImmobile(target)) return 1f;
            float num = this.CasterPawn.GetStatValue(StatDefOf.MeleeHitChance);
            if (ModsConfig.IdeologyActive && target.HasThing)
            {
                if (DarknessCombatUtility.IsOutdoorsAndLit(target.Thing)) num += this.CasterPawn.GetStatValue(StatDefOf.MeleeHitChanceOutdoorsLitOffset);
                else if (DarknessCombatUtility.IsOutdoorsAndDark(target.Thing)) num += this.CasterPawn.GetStatValue(StatDefOf.MeleeHitChanceOutdoorsDarkOffset);
                else if (DarknessCombatUtility.IsIndoorsAndDark(target.Thing)) num += this.CasterPawn.GetStatValue(StatDefOf.MeleeHitChanceIndoorsDarkOffset);
                else if (DarknessCombatUtility.IsIndoorsAndLit(target.Thing)) num += this.CasterPawn.GetStatValue(StatDefOf.MeleeHitChanceIndoorsLitOffset);
            }
            return num;
        }
        private float GetDodgeChance(LocalTargetInfo target)
        {
            if (this.surpriseAttack || IsTargetImmobile(target)) return 0f;
            Pawn p = target.Thing as Pawn;
            if (p == null || (p.stances.curStance is Stance_Busy b && b.verb != null && !b.verb.verbProps.IsMeleeAttack)) return 0f;

            float num = p.GetStatValue(StatDefOf.MeleeDodgeChance);
            if (ModsConfig.IdeologyActive)
            {
                if (DarknessCombatUtility.IsOutdoorsAndLit(target.Thing)) num += this.CasterPawn.GetStatValue(StatDefOf.MeleeHitChanceOutdoorsLitOffset);
                else if (DarknessCombatUtility.IsOutdoorsAndDark(target.Thing)) num += this.CasterPawn.GetStatValue(StatDefOf.MeleeHitChanceOutdoorsDarkOffset);
                else if (DarknessCombatUtility.IsIndoorsAndDark(target.Thing)) num += this.CasterPawn.GetStatValue(StatDefOf.MeleeHitChanceIndoorsDarkOffset);
                else if (DarknessCombatUtility.IsIndoorsAndLit(target.Thing)) num += this.CasterPawn.GetStatValue(StatDefOf.MeleeHitChanceIndoorsLitOffset);
            }
            return num;
        }

        private bool IsTargetImmobile(LocalTargetInfo target)
        {
            Thing thing = target.Thing;
            Pawn pawn = thing as Pawn;
            return thing.def.category != ThingCategory.Pawn || pawn.Downed || pawn.GetPosture() > PawnPosture.Standing;
        }
        private SoundDef SoundHitPawn()
        {
            if (base.EquipmentSource?.def.meleeHitSound != null) return base.EquipmentSource.def.meleeHitSound;
            if (this.tool?.soundMeleeHit != null) return this.tool.soundMeleeHit;
            if (base.EquipmentSource?.Stuff?.stuffProps?.soundMeleeHitSharp != null && this.verbProps.meleeDamageDef.armorCategory == DamageArmorCategoryDefOf.Sharp) return base.EquipmentSource.Stuff.stuffProps.soundMeleeHitSharp;
            if (base.EquipmentSource?.Stuff?.stuffProps?.soundMeleeHitBlunt != null) return base.EquipmentSource.Stuff.stuffProps.soundMeleeHitBlunt;
            return SoundDefOf.Pawn_Melee_Punch_HitPawn;
        }
        private SoundDef SoundHitBuilding()
        {
            Building building = this.currentTarget.Thing as Building;
            bool flag = building != null && !building.def.building.soundMeleeHitOverride.NullOrUndefined();
            SoundDef result;
            if (flag)
            {
                result = building.def.building.soundMeleeHitOverride;
            }
            else
            {
                bool flag2 = base.EquipmentSource != null && !base.EquipmentSource.def.meleeHitSound.NullOrUndefined();
                if (flag2)
                {
                    result = base.EquipmentSource.def.meleeHitSound;
                }
                else
                {
                    bool flag3 = this.tool != null && !this.tool.soundMeleeHit.NullOrUndefined();
                    if (flag3)
                    {
                        result = this.tool.soundMeleeHit;
                    }
                    else
                    {
                        bool flag4 = base.EquipmentSource != null && base.EquipmentSource.Stuff != null;
                        if (flag4)
                        {
                            bool flag5 = this.verbProps.meleeDamageDef.armorCategory == DamageArmorCategoryDefOf.Sharp;
                            if (flag5)
                            {
                                bool flag6 = !base.EquipmentSource.Stuff.stuffProps.soundMeleeHitSharp.NullOrUndefined();
                                if (flag6)
                                {
                                    return base.EquipmentSource.Stuff.stuffProps.soundMeleeHitSharp;
                                }
                            }
                            else
                            {
                                bool flag7 = !base.EquipmentSource.Stuff.stuffProps.soundMeleeHitBlunt.NullOrUndefined();
                                if (flag7)
                                {
                                    return base.EquipmentSource.Stuff.stuffProps.soundMeleeHitBlunt;
                                }
                            }
                        }
                        bool flag8 = this.CasterPawn != null && !this.CasterPawn.def.race.soundMeleeHitBuilding.NullOrUndefined();
                        if (flag8)
                        {
                            result = this.CasterPawn.def.race.soundMeleeHitBuilding;
                        }
                        else
                        {
                            result = SoundDefOf.MeleeHit_Unarmed;
                        }
                    }
                }
            }
            return result;
        }
        private SoundDef SoundMiss()
        {
            bool flag = this.CasterPawn != null;
            if (flag)
            {
                bool flag2 = this.tool != null && !this.tool.soundMeleeMiss.NullOrUndefined();
                if (flag2)
                {
                    return this.tool.soundMeleeMiss;
                }
                bool flag3 = !this.CasterPawn.def.race.soundMeleeMiss.NullOrUndefined();
                if (flag3)
                {
                    return this.CasterPawn.def.race.soundMeleeMiss;
                }
            }
            return SoundDefOf.Pawn_Melee_Punch_Miss;
        }
        private SoundDef SoundDodge(Thing target)
        {
            bool flag = target.def.race != null && target.def.race.soundMeleeDodge != null;
            SoundDef result;
            if (flag)
            {
                result = target.def.race.soundMeleeDodge;
            }
            else
            {
                result = this.SoundMiss();
            }
            return result;
        }

        public BattleLogEntry_MeleeCombat CreateCombatLog(Func<ManeuverDef, RulePackDef> rulePackGetter, bool alwaysShow, Thing target)
        {
            if (this.maneuver == null || this.tool == null) return null;
            BattleLogEntry_MeleeCombat entry = new BattleLogEntry_MeleeCombat(
                rulePackGetter(this.maneuver),
                alwaysShow,
                this.CasterPawn,
                target,
                base.ImplementOwnerType,
                this.tool.labelUsedInLogging ? this.tool.label : "",
                base.EquipmentSource?.def,
                base.HediffCompSource?.Def,
                this.maneuver.logEntryDef
            );
            Find.BattleLog.Add(entry);
            return entry;
        }
    }
}