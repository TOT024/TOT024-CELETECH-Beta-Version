using RimWorld;
using System.Reflection;
using UnityEngine;
using Verse;

namespace TOT_DLL_test
{
    public class Projectile_Split : Bullet
    {
        protected override void Tick()
        {
            base.Tick();
            if (this.DistanceCoveredFraction > modExtension_Splitedbullet.SplitTime && !this.DestroyedOrNull())
            {
                this.Split();
            }
        }
        protected void Split()
        {
            
            if(modExtension_Splitedbullet != null && modExtension_Splitedbullet.BulletDef != null)
            {
                int Splitcount = modExtension_Splitedbullet.SplitAmount + Rand.Range(-1, 1);
                for (int i = 0; i < Splitcount; i++)
                {
                    ProjectileHitFlags projectileHitFlags = ProjectileHitFlags.All;
                    Projectile projectile = ThingMaker.MakeThing(modExtension_Splitedbullet.BulletDef) as Projectile;
                    FieldInfo damageField = typeof(ProjectileProperties).GetField("damageAmountBase", BindingFlags.NonPublic | BindingFlags.Instance);
                    damageField?.SetValue(projectile.def.projectile, this.def.projectile.GetDamageAmount(this.launcher, null) / modExtension_Splitedbullet.SplitAmount);
                    FieldInfo damagePField = typeof(ProjectileProperties).GetField("armorPenetrationBase", BindingFlags.NonPublic | BindingFlags.Instance);
                    damagePField?.SetValue(projectile.def.projectile, this.def.projectile.GetArmorPenetration(this.launcher, null));
                    Projectile projectile2 = (Projectile)GenSpawn.Spawn(projectile, Position, Map, WipeMode.Vanish);
                    if(Rand.Chance(Hitchance()))
                    {
                        projectile2.Launch(this.launcher, this.DrawPos, this.intendedTarget, this.intendedTarget, projectileHitFlags, this.preventFriendlyFire, null, targetCoverDef);
                    }
                    else
                    {
                        projectile2.Launch(this.launcher, this.DrawPos, this.usedTarget, this.intendedTarget, projectileHitFlags, this.preventFriendlyFire, null, targetCoverDef);
                    }
                }
            }
            this.Destroy();
        }
        private float Hitchance()
        {
            Pawn pawn = this.launcher as Pawn;
            bool flag = pawn != null && !pawn.NonHumanlikeOrWildMan();
            int level = 0;
            if (flag)
            {
                SkillDef named = DefDatabase<SkillDef>.GetNamed("Intellectual", true);
                SkillRecord skill = pawn.skills.GetSkill(named);
                if (skill != null)
                {
                    level = skill.GetLevel(true);
                }
                else
                {
                    level = 10;
                }
            }
            else
            {
                level = 8;
            }
            float t = Mathf.Clamp01(level / 20f); // 归一化到 [0,1]
            float smoothStep = 3 * t * t - 2 * t * t * t; // 平滑步函数
            return 0.33f + 0.62f * smoothStep;
        }
        public ModExtension_Splitedbullet modExtension_Splitedbullet
        {
            get
            {
                return this.def.GetModExtension<ModExtension_Splitedbullet>();
            }
        }   
    }
}
