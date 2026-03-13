using UnityEngine;
using RimWorld;
using Verse;
using Verse.Sound;
using System.Collections.Generic;
namespace TOT_DLL_test
{
    [StaticConstructorOnStartup]
    public class Projectile_ChargedNormal: Bullet
    {
        public override bool AnimalsFleeImpact => true;
        private Vector3 CurretPos(float t)
        {
            Vector3 result;
            result = this.origin + (this.destination - this.origin) * t;
            return result;
        }
        protected override void DrawAt(Vector3 position, bool flip = false)
        {
            Vector3 position2 = CurretPos(this.DistanceCoveredFraction - 0.01f);
            position = CurretPos(this.DistanceCoveredFraction);
            rotation = Quaternion.LookRotation((position - position2));
            if (this.tickcount >= 4)
            {
                Vector3 drawloc = position;
                drawloc.y = AltitudeLayer.Projectile.AltitudeFor();
                Graphics.DrawMesh(MeshPool.GridPlane(this.def.graphicData.drawSize), drawloc, rotation, this.DrawMat, 0);
                base.Comps_PostDraw();
            }
        }
        protected override void Tick()
        {
            this.tickcount++;
            base.Tick();
        }
        private bool CheckForFreeIntercept(IntVec3 c)
        {
            if (this.destination.ToIntVec3() == c)
            {
                return false;
            }
            float num = VerbUtility.InterceptChanceFactorFromDistance(this.origin, c);
            if (num <= 0f)
            {
                return false;
            }
            bool flag = false;
            List<Thing> thingList = c.GetThingList(base.Map);
            for (int i = 0; i < thingList.Count; i++)
            {
                Thing thing = thingList[i];
                if (this.CanHit(thing))
                {
                    bool flag2 = false;
                    if (thing.def.Fillage == FillCategory.Full)
                    {
                        Building_Door building_Door;
                        if ((building_Door = (thing as Building_Door)) == null || !building_Door.Open)
                        {
                            this.ThrowDebugText("int-wall", c);
                            this.Impact(thing, false);
                            return true;
                        }
                        flag2 = true;
                    }
                    float num2 = 0f;
                    Pawn pawn;
                    if ((pawn = (thing as Pawn)) != null)
                    {
                        num2 = 0.4f * Mathf.Clamp(pawn.BodySize, 0.1f, 2f);
                        if (pawn.GetPosture() != PawnPosture.Standing)
                        {
                            num2 *= 0.1f;
                        }
                        if (this.launcher != null && pawn.Faction != null && this.launcher.Faction != null && !pawn.Faction.HostileTo(this.launcher.Faction))
                        {
                            if (this.preventFriendlyFire)
                            {
                                num2 = 0f;
                                this.ThrowDebugText("ff-miss", c);
                            }
                            else
                            {
                                num2 *= Find.Storyteller.difficulty.friendlyFireChanceFactor;
                            }
                        }
                    }
                    else if (thing.def.fillPercent > 0.2f)
                    {
                        if (flag2)
                        {
                            num2 = 0.05f;
                        }
                        else if (this.DestinationCell.AdjacentTo8Way(c))
                        {
                            num2 = thing.def.fillPercent * 1f;
                        }
                        else
                        {
                            num2 = thing.def.fillPercent * 0.15f;
                        }
                    }
                    num2 *= num;
                    if (num2 > 1E-05f)
                    {
                        if (Rand.Chance(num2))
                        {
                            this.ThrowDebugText("int-" + num2.ToStringPercent(), c);
                            this.Impact(thing, false);
                            return true;
                        }
                        flag = true;
                        this.ThrowDebugText(num2.ToStringPercent(), c);
                    }
                }
            }
            if (!flag)
            {
                this.ThrowDebugText("o", c);
            }
            return false;
        }
        private void ThrowDebugText(string text, IntVec3 c)
        {
            if (DebugViewSettings.drawShooting)
            {
                MoteMaker.ThrowText(c.ToVector3Shifted(), base.Map, text, -1f);
            }
        }
        public bool CheckForFreeInterceptBetween(Vector3 lastExactPos, Vector3 newExactPos)
        {
            if (lastExactPos == newExactPos)
            {
                return false;
            }
            List<Thing> list = base.Map.listerThings.ThingsInGroup(ThingRequestGroup.ProjectileInterceptor);
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].TryGetComp<CompProjectileInterceptor>().CheckIntercept(this, lastExactPos, newExactPos))
                {
                    this.Impact(null, true);
                    return true;
                }
            }
            IntVec3 intVec = lastExactPos.ToIntVec3();
            IntVec3 intVec2 = newExactPos.ToIntVec3();
            if (intVec2 == intVec)
            {
                return false;
            }
            if (!intVec.InBounds(base.Map) || !intVec2.InBounds(base.Map))
            {
                return false;
            }
            if (intVec2.AdjacentToCardinal(intVec))
            {
                return this.CheckForFreeIntercept(intVec2);
            }
            if (VerbUtility.InterceptChanceFactorFromDistance(this.origin, intVec2) <= 0f)
            {
                return false;
            }
            Vector3 vector = lastExactPos;
            Vector3 v = newExactPos - lastExactPos;
            Vector3 b = v.normalized * 0.2f;
            int num = (int)(v.MagnitudeHorizontal() / 0.2f);
            checkedCells.Clear();
            int num2 = 0;
            for (; ; )
            {
                vector += b;
                IntVec3 intVec3 = vector.ToIntVec3();
                if (!checkedCells.Contains(intVec3))
                {
                    if (this.CheckForFreeIntercept(intVec3))
                    {
                        break;
                    }
                    checkedCells.Add(intVec3);
                }
                num2++;
                if (num2 > num)
                {
                    return false;
                }
                if (intVec3 == intVec2)
                {
                    return false;
                }
            }
            return true;
        }
        private void SpawnFleck()
        {
            Map map = base.Map;
            if (map == null)
            {
                return;
            }
            Vector3 drawPos = this.DrawPos;

            FleckCreationData fleckData = new FleckCreationData
            {
                def = DefDatabase<FleckDef>.GetNamed("CMC_Fleck_HitFlash", true),
                spawnPosition = drawPos,
                scale = 0.39f,
                rotation = (float)Rand.Range(0, 360),
                velocitySpeed = 0f,
                rotationRate = 0f,
                orbitSpeed = 0f,
                ageTicksOverride = -1
            };
            map.flecks.CreateFleck(fleckData);

            for (int i = 0; i <= 7; i++)
            {
                FleckCreationData fleckData2 = new FleckCreationData
                {
                    def = DefDatabase<FleckDef>.GetNamed("SparkFlash", true),
                    spawnPosition = drawPos,
                    scale = 0.64f,
                    velocitySpeed = 0f,
                    rotationRate = 0f,
                    rotation = (float)Rand.Range(0, 360),
                    ageTicksOverride = -1
                };
                map.flecks.CreateFleck(fleckData2);
            }
            FleckMaker.ThrowAirPuffUp(drawPos, map);
        }
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map map = base.Map;
            IntVec3 position = base.Position;
            //base.Impact(hitThing, blockedByShield);
            this.SpawnFleck();
            BattleLogEntry_RangedImpact battleLogEntry_RangedImpact = new BattleLogEntry_RangedImpact(this.launcher, hitThing, this.intendedTarget.Thing, this.equipmentDef, this.def, this.targetCoverDef);
            Find.BattleLog.Add(battleLogEntry_RangedImpact);
            this.NotifyImpact(hitThing, map, position);
            if (hitThing != null)
            {
                Pawn pawn;
                bool instigatorGuilty = (pawn = (this.launcher as Pawn)) == null || !pawn.Drafted;
                Pawn pawn2 = hitThing as Pawn;
                float Amount = DamageAmount;
                if (Rand.Chance(0.05f) && this.launcher != null && this.launcher.Faction != null && this.launcher.Faction == Faction.OfPlayer)
                {
                    Amount = DamageAmount * 1.98f;
                }
                if (pawn2 != null)
                {
                    if (pawn2.RaceProps.IsMechanoid)
                    {
                        Amount *= 1.5f;
                        if (Rand.Chance(0.2f))
                        {
                            DamageInfo dinfo2 = new DamageInfo(DamageDefOf.EMP, DamageAmount, 2f, this.ExactRotation.eulerAngles.y, this.launcher, null, this.equipmentDef, DamageInfo.SourceCategory.ThingOrUnknown, this.intendedTarget.Thing, instigatorGuilty, true);
                            hitThing.TakeDamage(dinfo2).AssociateWithLog(battleLogEntry_RangedImpact);
                        }
                    }
                }
                DamageInfo dinfo = new DamageInfo(this.def.projectile.damageDef, Amount, this.ArmorPenetration, this.ExactRotation.eulerAngles.y, this.launcher, null, this.equipmentDef, DamageInfo.SourceCategory.ThingOrUnknown, this.intendedTarget.Thing, instigatorGuilty, true);
                hitThing.TakeDamage(dinfo).AssociateWithLog(battleLogEntry_RangedImpact);
                if (pawn2 != null && pawn2.stances != null)
                {
                    pawn2.stances.stagger.Notify_BulletImpact(this);
                }
                if (this.def.projectile.extraDamages != null)
                {
                    foreach (ExtraDamage extraDamage in this.def.projectile.extraDamages)
                    {
                        if (Rand.Chance(extraDamage.chance))
                        {
                            DamageInfo dinfo2 = new DamageInfo(extraDamage.def, extraDamage.amount, extraDamage.AdjustedArmorPenetration(), this.ExactRotation.eulerAngles.y, this.launcher, null, this.equipmentDef, DamageInfo.SourceCategory.ThingOrUnknown, this.intendedTarget.Thing, instigatorGuilty, true);
                            hitThing.TakeDamage(dinfo2).AssociateWithLog(battleLogEntry_RangedImpact);
                        }
                    }
                }
            }
            else
            {
                if (!blockedByShield)
                {
                    SoundDefOf.BulletImpact_Ground.PlayOneShot(new TargetInfo(base.Position, map, false));
                    if (base.Position.GetTerrain(map).takeSplashes)
                    {
                        FleckMaker.WaterSplash(this.ExactPosition, map, Mathf.Sqrt((float)this.DamageAmount) * 1f, 4f);
                    }
                    else
                    {
                        FleckMaker.Static(this.ExactPosition, map, FleckDefOf.ShotHit_Dirt, 1f);
                    }
                }
            }
            this.Destroy(DestroyMode.Vanish);
        }
        private void NotifyImpact(Thing hitThing, Map map, IntVec3 position)
        {
            BulletImpactData impactData = new BulletImpactData
            {
                bullet = this,
                hitThing = hitThing,
                impactPosition = position
            };
            if (hitThing != null)
            {
                hitThing.Notify_BulletImpactNearby(impactData);
            }
            int num = 9;
            for (int i = 0; i < num; i++)
            {
                IntVec3 c = position + GenRadial.RadialPattern[i];
                if (c.InBounds(map))
                {
                    List<Thing> thingList = c.GetThingList(map);
                    for (int j = 0; j < thingList.Count; j++)
                    {
                        if (thingList[j] != hitThing)
                        {
                            thingList[j].Notify_BulletImpactNearby(impactData);
                        }
                    }
                }
            }
        }
        private int tickcount;
        public FleckDef FleckDef = DefDatabase<FleckDef>.GetNamed("CMC_SparkFlash_Blue_Small", true);
        public FleckDef FleckDef2 = DefDatabase<FleckDef>.GetNamed("CMC_SparkFlash_Blue_LongLasting_Small", true);
        public int Fleck_MakeFleckTickMax = 1;
        public IntRange Fleck_MakeFleckNum = new IntRange(2, 2);
        public FloatRange Fleck_Angle = new FloatRange(-180f, 180f);
        public FloatRange Fleck_Scale = new FloatRange(1.6f, 1.7f);
        public FloatRange Fleck_Speed = new FloatRange(5f, 7f);
        public FloatRange Fleck_Speed2 = new FloatRange(0.1f, 0.2f);
        public FloatRange Fleck_Rotation = new FloatRange(-180f, 180f);
        public int Fleck_MakeFleckTick;
        public Quaternion rotation;
        private static List<IntVec3> checkedCells = new List<IntVec3>();
    }
}
