using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TOT_DLL_test
{
    public class CompProperties_Knockback : CompProperties_AbilityEffect
    {
        public float knockbackDistance = 5f;
        public float stunDuration = 3f;
        public float backfireRadius = 1.9f;

        public CompProperties_Knockback()
        {
            this.compClass = typeof(CompAbilityEffect_Knockback);
        }
    }

    public class CompAbilityEffect_Knockback : CompAbilityEffect
    {
        public new CompProperties_Knockback Props => (CompProperties_Knockback)this.props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn caster = this.parent.pawn;
            if (caster == null)
            {
                return;
            }
            Map map = caster.Map;
            float backfireChance = 0.01f;

            if (caster.equipment != null && caster.equipment.Primary != null)
            {
                CompQuality weaponQuality = caster.equipment.Primary.TryGetComp<CompQuality>();

                if (weaponQuality != null)
                {
                    if (weaponQuality.Quality >= QualityCategory.Masterwork)
                    {
                        backfireChance = 0f;
                    }
                    else if (weaponQuality.Quality <= QualityCategory.Poor)
                    {
                        backfireChance = 0.1f;
                    }
                }
            }
            if (backfireChance > 0f && Rand.Chance(backfireChance))
            {
                SoundDefOf.Crunch.PlayOneShot(new TargetInfo(caster.Position, map));

                if (caster.equipment != null && caster.equipment.Primary != null)
                {
                    TaggedString letterLabel = "TOT_WeaponShatteredLabel".Translate();
                    TaggedString letterText = "TOT_WeaponShatteredText".Translate(caster.LabelShort, caster.equipment.Primary.Label);
                    Find.LetterStack.ReceiveLetter(letterLabel, letterText, LetterDefOf.NegativeEvent, new TargetInfo(caster.Position, map));
                    caster.equipment.DestroyEquipment(caster.equipment.Primary);
                }
                GenExplosion.DoExplosion(
                    center: caster.Position,
                    map: map,
                    radius: Props.backfireRadius > 0 ? Props.backfireRadius : 1.9f,
                    damType: DamageDefOf.Flame,
                    instigator: caster,
                    damAmount: -1,
                    armorPenetration: -1f,
                    explosionSound: null,
                    weapon: null,
                    projectile: null,
                    intendedTarget: null,
                    postExplosionSpawnThingDef: null,
                    postExplosionSpawnChance: 0f,
                    postExplosionSpawnThingCount: 1,
                    applyDamageToExplosionCellsNeighbors: false,
                    preExplosionSpawnThingDef: null,
                    preExplosionSpawnChance: 0f,
                    preExplosionSpawnThingCount: 1,
                    chanceToStartFire: 1.0f,
                    damageFalloff: false,
                    direction: null,
                    ignoredThings: null
                );

                return;
            }
            float radius = this.parent.def.EffectRadius;
            FleckCreationData data = FleckMaker.GetDataStatic(caster.DrawPos, map, FleckDefOf.PsycastAreaEffect, radius);
            data.rotationRate = 0f;
            map.flecks.CreateFleck(data);
            SoundDefOf.Psycast_Skip_Pulse.PlayOneShot(new TargetInfo(caster.Position, map));

            List<Pawn> targets = new List<Pawn>();
            Faction faction = caster.Faction;
            foreach (var cell in GenRadial.RadialCellsAround(caster.Position, radius, true))
            {
                Pawn victim = cell.GetFirstPawn(map);
                if (victim == null || victim == caster) continue;
                if (faction == null)
                {
                    targets.Add(victim);
                }
                else if (victim.Faction == null || victim.Faction.HostileTo(faction))
                {
                    targets.Add(victim);
                }
            }
            foreach (Pawn p in targets)
            {
                if (p == null || p.Destroyed || p.Dead) continue;
                PushPawn(caster, p, map);
                if (!p.Destroyed && !p.Dead)
                {
                    StunPawn(p);
                }
            }
        }

        private void PushPawn(Pawn caster, Pawn victim, Map map)
        {
            Vector3 direction = (victim.Position - caster.Position).ToVector3();
            direction.Normalize();

            IntVec3 finalPos = victim.Position;
            int pushDist = Mathf.RoundToInt(Props.knockbackDistance);

            for (int i = 0; i < pushDist; i++)
            {
                IntVec3 nextPos = finalPos + (direction * (i + 1)).ToIntVec3();

                if (nextPos.InBounds(map) && nextPos.Walkable(map))
                {
                    finalPos = nextPos;
                }
                else
                {
                    DamageInfo dinfo = new DamageInfo(DamageDefOf.Blunt, 10, 0, -1, caster);
                    victim.TakeDamage(dinfo);
                    break;
                }
            }

            if (finalPos != victim.Position)
            {
                FleckMaker.ThrowDustPuff(victim.Position, map, 1f);
                victim.Position = finalPos;
                victim.Notify_Teleported(true, false);
                FleckMaker.ThrowDustPuff(finalPos, map, 1f);
            }
        }

        private void StunPawn(Pawn victim)
        {
            int ticks = Mathf.RoundToInt(Props.stunDuration * 60);
            victim.stances.stunner.StunFor(ticks, victim, false);
        }
    }
}